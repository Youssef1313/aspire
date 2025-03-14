// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Aspire.Dashboard.Model;
using Aspire.Tests.Shared.DashboardModel;
using Microsoft.AspNetCore.InternalTesting;

namespace Aspire.Dashboard.Tests;

[TestClass]
public class ResourceOutgoingPeerResolverTests
{
    private static ResourceViewModel CreateResource(string name, string? serviceAddress = null, int? servicePort = null, string? displayName = null)
    {
        return ModelTestHelpers.CreateResource(
            appName: name,
            displayName: displayName,
            urls: serviceAddress is null || servicePort is null ? [] : [new UrlViewModel(name, new($"http://{serviceAddress}:{servicePort}"), isInternal: false, isInactive: false)]);
    }

    [TestMethod]
    public void EmptyAttributes_NoMatch()
    {
        // Arrange
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["test"] = CreateResource("test", "localhost", 5000)
        };

        // Act & Assert
        Assert.IsFalse(TryResolvePeerName(resources, [], out _));
    }

    [TestMethod]
    public void EmptyUrlAttribute_NoMatch()
    {
        // Arrange
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["test"] = CreateResource("test", "localhost", 5000)
        };

        // Act & Assert
        Assert.IsFalse(TryResolvePeerName(resources, [KeyValuePair.Create("peer.service", "")], out _));
    }

    [TestMethod]
    public void NullUrlAttribute_NoMatch()
    {
        // Arrange
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["test"] = CreateResource("test", "localhost", 5000)
        };

        // Act & Assert
        Assert.IsFalse(TryResolvePeerName(resources, [KeyValuePair.Create<string, string>("peer.service", null!)], out _));
    }

    [TestMethod]
    public void ExactValueAttribute_Match()
    {
        // Arrange
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["test"] = CreateResource("test", "localhost", 5000)
        };

        // Act & Assert
        Assert.IsTrue(TryResolvePeerName(resources, [KeyValuePair.Create("peer.service", "localhost:5000")], out var value));
        Assert.AreEqual("test", value);
    }

    [TestMethod]
    public void NumberAddressValueAttribute_Match()
    {
        // Arrange
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["test"] = CreateResource("test", "localhost", 5000)
        };

        // Act & Assert
        Assert.IsTrue(TryResolvePeerName(resources, [KeyValuePair.Create("peer.service", "127.0.0.1:5000")], out var value));
        Assert.AreEqual("test", value);
    }

    [TestMethod]
    public void CommaAddressValueAttribute_Match()
    {
        // Arrange
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["test"] = CreateResource("test", "localhost", 5000)
        };

        // Act & Assert
        Assert.IsTrue(TryResolvePeerName(resources, [KeyValuePair.Create("peer.service", "127.0.0.1,5000")], out var value));
        Assert.AreEqual("test", value);
    }

    [TestMethod]
    public void ServerAddressAndPort_Match()
    {
        // Arrange
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["test"] = CreateResource("test", "localhost", 5000)
        };

        // Act & Assert
        Assert.IsTrue(TryResolvePeerName(resources, [KeyValuePair.Create("server.address", "localhost"), KeyValuePair.Create("server.port", "5000")], out var value));
        Assert.AreEqual("test", value);
    }

    [TestMethod]
    public async Task OnPeerChanges_DataUpdates_EventRaised()
    {
        // Arrange
        var tcs = new TaskCompletionSource<ResourceViewModelSubscription>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sourceChannel = Channel.CreateUnbounded<ResourceViewModelChange>();
        var resultChannel = Channel.CreateUnbounded<int>();
        var dashboardClient = new MockDashboardClient(tcs.Task);
        var resolver = new ResourceOutgoingPeerResolver(dashboardClient);
        var changeCount = 1;
        resolver.OnPeerChanges(async () =>
        {
            await resultChannel.Writer.WriteAsync(changeCount++);
        });

        var readValue = 0;
        Assert.IsFalse(resultChannel.Reader.TryRead(out readValue));

        // Act 1
        tcs.SetResult(new ResourceViewModelSubscription(
            [CreateResource("test")],
            GetChanges()));

        // Assert 1
        readValue = await resultChannel.Reader.ReadAsync().DefaultTimeout();
        Assert.AreEqual(1, readValue);

        // Act 2
        await sourceChannel.Writer.WriteAsync(new ResourceViewModelChange(ResourceViewModelChangeType.Upsert, CreateResource("test2")));

        // Assert 2
        readValue = await resultChannel.Reader.ReadAsync().DefaultTimeout();
        Assert.AreEqual(2, readValue);

        await resolver.DisposeAsync().DefaultTimeout();

        async IAsyncEnumerable<IReadOnlyList<ResourceViewModelChange>> GetChanges([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var item in sourceChannel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return [item];
            }
        }
    }

    [TestMethod]
    public void NameAndDisplayNameDifferent_OneInstance_ReturnDisplayName()
    {
        // Arrange
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["test-abc"] = CreateResource("test-abc", "localhost", 5000, displayName: "test")
        };

        // Act & Assert
        Assert.IsTrue(TryResolvePeerName(resources, [KeyValuePair.Create("server.address", "localhost"), KeyValuePair.Create("server.port", "5000")], out var value));
        Assert.AreEqual("test", value);
    }

    [TestMethod]
    public void NameAndDisplayNameDifferent_MultipleInstances_ReturnName()
    {
        // Arrange
        var resources = new Dictionary<string, ResourceViewModel>
        {
            ["test-abc"] = CreateResource("test-abc", "localhost", 5000, displayName: "test"),
            ["test-def"] = CreateResource("test-def", "localhost", 5001, displayName: "test")
        };

        // Act & Assert
        Assert.IsTrue(TryResolvePeerName(resources, [KeyValuePair.Create("server.address", "localhost"), KeyValuePair.Create("server.port", "5000")], out var value1));
        Assert.AreEqual("test-abc", value1);

        Assert.IsTrue(TryResolvePeerName(resources, [KeyValuePair.Create("server.address", "localhost"), KeyValuePair.Create("server.port", "5001")], out var value2));
        Assert.AreEqual("test-def", value2);
    }

    private static bool TryResolvePeerName(IDictionary<string, ResourceViewModel> resources, KeyValuePair<string, string>[] attributes, out string? peerName)
    {
        return ResourceOutgoingPeerResolver.TryResolvePeerNameCore(resources, attributes, out peerName);
    }

    private sealed class MockDashboardClient(Task<ResourceViewModelSubscription> subscribeResult) : IDashboardClient
    {
        public bool IsEnabled => true;
        public Task WhenConnected => Task.CompletedTask;
        public string ApplicationName => "ApplicationName";
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public Task<ResourceCommandResponseViewModel> ExecuteResourceCommandAsync(string resourceName, string resourceType, CommandViewModel command, CancellationToken cancellationToken) => throw new NotImplementedException();
        public IAsyncEnumerable<IReadOnlyList<ResourceLogLine>> SubscribeConsoleLogs(string resourceName, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ResourceViewModelSubscription> SubscribeResourcesAsync(CancellationToken cancellationToken) => subscribeResult;
    }
}
