// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Aspire.Hosting.Tests;

[TestClass]
public class ResourceNotificationTests
{
    [TestMethod]
    public void InitialStateCanBeSpecified()
    {
        var builder = DistributedApplication.CreateBuilder();

        var custom = builder.AddResource(new CustomResource("myResource"))
            .WithEndpoint(name: "ep", scheme: "http", port: 8080)
            .WithEnvironment("x", "1000")
            .WithInitialState(new()
            {
                ResourceType = "MyResource",
                Properties = [new("A", "B")],
            });

        var annotation = custom.Resource.Annotations.OfType<ResourceSnapshotAnnotation>().SingleOrDefault();

        Assert.IsNotNull(annotation);

        var state = annotation.InitialSnapshot;

        Assert.AreEqual("MyResource", state.ResourceType);
        Assert.IsEmpty(state.EnvironmentVariables);
        Assert.That.Collection(state.Properties, c =>
        {
            Assert.AreEqual("A", c.Name);
            Assert.AreEqual("B", c.Value);
        });
    }

    [TestMethod]
    public async Task ResourceUpdatesAreQueued()
    {
        var resource = new CustomResource("myResource");

        var notificationService = ResourceNotificationServiceTestHelpers.Create();

        async Task<List<ResourceEvent>> GetValuesAsync(CancellationToken cancellationToken)
        {
            var values = new List<ResourceEvent>();

            await foreach (var item in notificationService.WatchAsync(cancellationToken))
            {
                values.Add(item);

                if (values.Count == 2)
                {
                    break;
                }
            }

            return values;
        }

        using var cts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource();
        var enumerableTask = GetValuesAsync(cts.Token);

        await notificationService.PublishUpdateAsync(resource, state => state with { Properties = state.Properties.Add(new("A", "value")) }).DefaultTimeout();

        await notificationService.PublishUpdateAsync(resource, state => state with { Properties = state.Properties.Add(new("B", "value")) }).DefaultTimeout();

        var values = await enumerableTask.DefaultTimeout();

        Assert.That.Collection(values,
            c =>
            {
                Assert.AreEqual(resource, c.Resource);
                Assert.AreEqual("myResource", c.ResourceId);
                Assert.AreEqual("CustomResource", c.Snapshot.ResourceType);
                Assert.AreEqual("value", c.Snapshot.Properties.Single(p => p.Name == "A").Value);
                Assert.IsNull(c.Snapshot.HealthStatus);
            },
            c =>
            {
                Assert.AreEqual(resource, c.Resource);
                Assert.AreEqual("myResource", c.ResourceId);
                Assert.AreEqual("CustomResource", c.Snapshot.ResourceType);
                Assert.AreEqual("value", c.Snapshot.Properties.Single(p => p.Name == "B").Value);
                Assert.IsNull(c.Snapshot.HealthStatus);
            });
    }

    [TestMethod]
    public async Task WatchingAllResourcesNotifiesOfAnyResourceChange()
    {
        var resource1 = new CustomResource("myResource1");
        var resource2 = new CustomResource("myResource2");

        var notificationService = ResourceNotificationServiceTestHelpers.Create();

        async Task<List<ResourceEvent>> GetValuesAsync(CancellationToken cancellation)
        {
            var values = new List<ResourceEvent>();

            await foreach (var item in notificationService.WatchAsync(cancellation))
            {
                values.Add(item);

                if (values.Count == 3)
                {
                    break;
                }
            }

            return values;
        }

        using var cts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource();
        var enumerableTask = GetValuesAsync(cts.Token);

        await notificationService.PublishUpdateAsync(resource1, state => state with { Properties = state.Properties.Add(new("A", "value")) }).DefaultTimeout();

        await notificationService.PublishUpdateAsync(resource2, state => state with { Properties = state.Properties.Add(new("B", "value")) }).DefaultTimeout();

        await notificationService.PublishUpdateAsync(resource1, "replica1", state => state with { Properties = state.Properties.Add(new("C", "value")) }).DefaultTimeout();

        var values = await enumerableTask.DefaultTimeout();

        Assert.That.Collection(values,
            c =>
            {
                Assert.AreEqual(resource1, c.Resource);
                Assert.AreEqual("myResource1", c.ResourceId);
                Assert.AreEqual("CustomResource", c.Snapshot.ResourceType);
                Assert.AreEqual("value", c.Snapshot.Properties.Single(p => p.Name == "A").Value);
            },
            c =>
            {
                Assert.AreEqual(resource2, c.Resource);
                Assert.AreEqual("myResource2", c.ResourceId);
                Assert.AreEqual("CustomResource", c.Snapshot.ResourceType);
                Assert.AreEqual("value", c.Snapshot.Properties.Single(p => p.Name == "B").Value);
            },
            c =>
            {
                Assert.AreEqual(resource1, c.Resource);
                Assert.AreEqual("replica1", c.ResourceId);
                Assert.AreEqual("CustomResource", c.Snapshot.ResourceType);
                Assert.AreEqual("value", c.Snapshot.Properties.Single(p => p.Name == "C").Value);
            });
    }

    [TestMethod]
    public async Task WaitingOnResourceReturnsWhenResourceReachesTargetState()
    {
        var resource1 = new CustomResource("myResource1");

        var notificationService = ResourceNotificationServiceTestHelpers.Create();

        var waitTask = notificationService.WaitForResourceAsync("myResource1", "SomeState");

        await notificationService.PublishUpdateAsync(resource1, snapshot => snapshot with { State = "SomeState" }).DefaultTimeout();
        await waitTask.DefaultTimeout();

        Assert.IsTrue(waitTask.IsCompletedSuccessfully);
    }

    [TestMethod]
    public async Task WaitingOnResourceReturnsWhenResourceReachesTargetStateWithDifferentCasing()
    {
        var resource1 = new CustomResource("myResource1");

        var notificationService = ResourceNotificationServiceTestHelpers.Create();

        using var cts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource();
        var waitTask = notificationService.WaitForResourceAsync("MYreSouRCe1", "sOmeSTAtE", cts.Token);

        await notificationService.PublishUpdateAsync(resource1, snapshot => snapshot with { State = "SomeState" }).DefaultTimeout();
        await waitTask.DefaultTimeout();

        Assert.IsTrue(waitTask.IsCompletedSuccessfully);
    }

    [TestMethod]
    public async Task WaitingOnResourceReturnsImmediatelyWhenResourceIsInTargetStateAlready()
    {
        var resource1 = new CustomResource("myResource1");

        var notificationService = ResourceNotificationServiceTestHelpers.Create();

        // Publish the state update first
        await notificationService.PublishUpdateAsync(resource1, snapshot => snapshot with { State = "SomeState" }).DefaultTimeout();

        var waitTask = notificationService.WaitForResourceAsync("myResource1", "SomeState");

        Assert.IsTrue(waitTask.IsCompletedSuccessfully);
    }

    [TestMethod]
    public async Task WaitingOnResourceReturnsWhenResourceReachesRunningStateIfNoTargetStateSupplied()
    {
        var resource1 = new CustomResource("myResource1");

        var notificationService = ResourceNotificationServiceTestHelpers.Create();

        var waitTask = notificationService.WaitForResourceAsync("myResource1", targetState: null);

        await notificationService.PublishUpdateAsync(resource1, snapshot => snapshot with { State = KnownResourceStates.Running }).DefaultTimeout();
        await waitTask.DefaultTimeout();

        Assert.IsTrue(waitTask.IsCompletedSuccessfully);
    }

    [TestMethod]
    public async Task WaitingOnResourceReturnsCorrectStateWhenResourceReachesOneOfTargetStatesBeforeCancellation()
    {
        var resource1 = new CustomResource("myResource1");

        var notificationService = ResourceNotificationServiceTestHelpers.Create();

        var waitTask = notificationService.WaitForResourceAsync("myResource1", ["SomeState", "SomeOtherState"]);

        await notificationService.PublishUpdateAsync(resource1, snapshot => snapshot with { State = "SomeOtherState" }).DefaultTimeout();
        var reachedState = await waitTask.DefaultTimeout();

        Assert.AreEqual("SomeOtherState", reachedState);
    }

    [TestMethod]
    public async Task WaitingOnResourceReturnsCorrectStateWhenResourceReachesOneOfTargetStates()
    {
        var resource1 = new CustomResource("myResource1");

        var notificationService = ResourceNotificationServiceTestHelpers.Create();

        var waitTask = notificationService.WaitForResourceAsync("myResource1", ["SomeState", "SomeOtherState"], default);

        await notificationService.PublishUpdateAsync(resource1, snapshot => snapshot with { State = "SomeOtherState" }).DefaultTimeout();
        var reachedState = await waitTask.DefaultTimeout();

        Assert.AreEqual("SomeOtherState", reachedState);
    }

    [TestMethod]
    public async Task WaitingOnResourceReturnsItReachesStateAfterApplicationStoppingCancellationTokenSignaled()
    {
        var resource1 = new CustomResource("myResource1");

        using var hostApplicationLifetime = new TestHostApplicationLifetime();
        var notificationService = ResourceNotificationServiceTestHelpers.Create(hostApplicationLifetime: hostApplicationLifetime);

        var waitTask = notificationService.WaitForResourceAsync("myResource1", "SomeState");
        hostApplicationLifetime.StopApplication();

        await notificationService.PublishUpdateAsync(resource1, snapshot => snapshot with { State = "SomeState" }).DefaultTimeout();

        await waitTask.DefaultTimeout();

        Assert.IsTrue(waitTask.IsCompletedSuccessfully);
    }

    [TestMethod]
    public async Task WaitingOnResourceThrowsOperationCanceledExceptionIfResourceDoesntReachStateBeforeCancellationTokenSignaled()
    {
        var notificationService = ResourceNotificationServiceTestHelpers.Create();

        using var cts = new CancellationTokenSource();
        var waitTask = notificationService.WaitForResourceAsync("myResource1", "SomeState", cts.Token);

        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await waitTask;
        }).DefaultTimeout();
    }

    [TestMethod]
    public async Task WaitingOnResourceThrowsOperationCanceledExceptionIfResourceDoesntReachStateBeforeServiceIsDisposed()
    {
        var notificationService = ResourceNotificationServiceTestHelpers.Create();

        var waitTask = notificationService.WaitForResourceAsync("myResource1", "SomeState");

        notificationService.Dispose();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await waitTask;
        }).DefaultTimeout();
    }

    [TestMethod]
    public async Task WaitingOnResourceThrowsOperationCanceledExceptionIfResourceDoesntReachStateBeforeCancellationTokenSignalledWhenApplicationStoppingTokenExists()
    {
        using var hostApplicationLifetime = new TestHostApplicationLifetime();
        var notificationService = ResourceNotificationServiceTestHelpers.Create(hostApplicationLifetime: hostApplicationLifetime);

        using var cts = new CancellationTokenSource();
        var waitTask = notificationService.WaitForResourceAsync("myResource1", "SomeState", cts.Token);

        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await waitTask;
        }).DefaultTimeout();
    }

    [TestMethod]
    public async Task PublishLogsStateTextChangesCorrectly()
    {
        var resource1 = new CustomResource("resource1");
        var logger = new FakeLogger<ResourceNotificationService>();
        var notificationService = ResourceNotificationServiceTestHelpers.Create(logger: logger);

        await notificationService.PublishUpdateAsync(resource1, snapshot => snapshot with { State = "SomeState" }).DefaultTimeout();

        var logs = logger.Collector.GetSnapshot();

        // Initial state text, log just the new state
        Assert.ContainsSingle(logs.Where(l => l.Level == LogLevel.Debug));
        Assert.Contains(logs, l => l.Level == LogLevel.Debug && l.Message.Contains("Resource resource1/resource1 changed state: SomeState"));

        logger.Collector.Clear();

        // Same state text as previous state, no log
        await notificationService.PublishUpdateAsync(resource1, snapshot => snapshot with { State = "SomeState" }).DefaultTimeout();

        logs = logger.Collector.GetSnapshot();

        Assert.DoesNotContain(logs, l => l.Level == LogLevel.Debug);
        Assert.DoesNotContain(logs, l => l.Level == LogLevel.Debug && l.Message.Contains("Resource resource1/resource1 changed state: SomeState"));

        logger.Collector.Clear();

        // Different state text, log the transition from the previous state to the new state
        await notificationService.PublishUpdateAsync(resource1, snapshot => snapshot with { State = "NewState" }).DefaultTimeout();

        logs = logger.Collector.GetSnapshot();

        Assert.ContainsSingle(logs.Where(l => l.Level == LogLevel.Debug));
        Assert.Contains(logs, l => l.Level == LogLevel.Debug && l.Message.Contains("Resource resource1/resource1 changed state: SomeState -> NewState"));

        logger.Collector.Clear();

        // Null state text, no log
        await notificationService.PublishUpdateAsync(resource1, snapshot => snapshot with { State = null }).DefaultTimeout();

        logs = logger.Collector.GetSnapshot();

        Assert.DoesNotContain(logs, l => l.Level == LogLevel.Debug);
        Assert.DoesNotContain(logs, l => l.Level == LogLevel.Debug && l.Message.Contains("Resource resource1/resource1 changed state:"));

        logger.Collector.Clear();

        // Empty state text, no log
        await notificationService.PublishUpdateAsync(resource1, snapshot => snapshot with { State = "" }).DefaultTimeout();

        logs = logger.Collector.GetSnapshot();

        Assert.DoesNotContain(logs, l => l.Level == LogLevel.Debug);
        Assert.DoesNotContain(logs, l => l.Level == LogLevel.Debug && l.Message.Contains("Resource resource1/resource1 changed state:"));

        logger.Collector.Clear();

        // White space state text, no log
        await notificationService.PublishUpdateAsync(resource1, snapshot => snapshot with { State = " " }).DefaultTimeout();

        logs = logger.Collector.GetSnapshot();

        Assert.DoesNotContain(logs, l => l.Level == LogLevel.Debug);
        Assert.DoesNotContain(logs, l => l.Level == LogLevel.Debug && l.Message.Contains("Resource resource1/resource1 changed state:"));

        logger.Collector.Clear();
    }

    [TestMethod]
    public async Task PublishLogsTraceStateDetailsCorrectly()
    {
        var resource1 = new CustomResource("resource1");
        var logger = new FakeLogger<ResourceNotificationService>();
        var notificationService = ResourceNotificationServiceTestHelpers.Create(logger: logger);

        var createdDate = DateTime.Now;
        await notificationService.PublishUpdateAsync(resource1, snapshot => snapshot with { CreationTimeStamp = createdDate }).DefaultTimeout();
        await notificationService.PublishUpdateAsync(resource1, snapshot => snapshot with { State = "SomeState" }).DefaultTimeout();
        await notificationService.PublishUpdateAsync(resource1, snapshot => snapshot with { ExitCode = 0 }).DefaultTimeout();

        var logs = logger.Collector.GetSnapshot();

        Assert.ContainsSingle(logs.Where(l => l.Level == LogLevel.Debug));
        Assert.AreEqual(3, logs.Where(l => l.Level == LogLevel.Trace).Count());
        Assert.Contains(logs, l => l.Level == LogLevel.Trace && l.Message.Contains("Resource resource1/resource1 update published:") && l.Message.Contains($"CreationTimeStamp = {createdDate:s}"));
        Assert.Contains(logs, l => l.Level == LogLevel.Trace && l.Message.Contains("Resource resource1/resource1 update published:") && l.Message.Contains("State = { Text = SomeState"));
        Assert.Contains(logs, l => l.Level == LogLevel.Trace && l.Message.Contains("Resource resource1/resource1 update published:") && l.Message.Contains("ExitCode = 0"));
    }

    private sealed class CustomResource(string name) : Resource(name),
        IResourceWithEnvironment,
        IResourceWithConnectionString,
        IResourceWithEndpoints
    {
        public ReferenceExpression ConnectionStringExpression =>
            ReferenceExpression.Create($"CustomConnectionString");
    }

    private sealed class TestHostApplicationLifetime : IHostApplicationLifetime, IDisposable
    {
        private readonly CancellationTokenSource _stoppingCts = new();

        public TestHostApplicationLifetime()
        {
            ApplicationStopping = _stoppingCts.Token;
        }

        public CancellationToken ApplicationStarted { get; }
        public CancellationToken ApplicationStopped { get; }
        public CancellationToken ApplicationStopping { get; }

        public void StopApplication()
        {
            _stoppingCts.Cancel();
        }

        public void Dispose()
        {
            _stoppingCts.Dispose();
        }
    }
}
