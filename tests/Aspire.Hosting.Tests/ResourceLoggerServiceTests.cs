// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Tests;

[TestClass]
public class ResourceLoggerServiceTests
{
    [TestMethod]
    public async Task AddingResourceLoggerAnnotationAllowsLogging()
    {
        var testResource = new TestResource("myResource");
        var service = ConsoleLoggingTestHelpers.GetResourceLoggerService();
        var logger = service.GetLogger(testResource);

        var subsLoop = WatchForSubscribers(service);

        var logsEnumerator1 = service.WatchAsync(testResource).GetAsyncEnumerator();
        var logsLoop = ConsoleLoggingTestHelpers.WatchForLogsAsync(logsEnumerator1, 2);

        // Wait for subscriber to be added
        await subsLoop.DefaultTimeout();

        // Log
        logger.LogInformation("Hello, world!");
        logger.LogError("Hello, error!");

        // Wait for logs to be read
        var allLogs = await logsLoop.DefaultTimeout();

        Assert.AreEqual("2000-12-29T20:59:59.0000000Z Hello, world!", allLogs[0].Content);
        Assert.IsFalse(allLogs[0].IsErrorMessage);

        Assert.AreEqual("2000-12-29T20:59:59.0000000Z Hello, error!", allLogs[1].Content);
        Assert.IsTrue(allLogs[1].IsErrorMessage);

        // New sub should get the previous logs
        subsLoop = WatchForSubscribers(service);
        var logsEnumerator2 = service.WatchAsync(testResource).GetAsyncEnumerator();
        logsLoop = ConsoleLoggingTestHelpers.WatchForLogsAsync(logsEnumerator2, 2);
        await subsLoop.DefaultTimeout();
        allLogs = await logsLoop.DefaultTimeout();

        Assert.AreEqual(2, allLogs.Count);
        Assert.AreEqual("2000-12-29T20:59:59.0000000Z Hello, world!", allLogs[0].Content);
        Assert.AreEqual("2000-12-29T20:59:59.0000000Z Hello, error!", allLogs[1].Content);

        await logsEnumerator1.DisposeAsync().DefaultTimeout();
        await logsEnumerator2.DisposeAsync().DefaultTimeout();
    }

    [TestMethod]
    public async Task StreamingLogsCancelledAfterComplete()
    {
        var testResource = new TestResource("myResource");
        var service = ConsoleLoggingTestHelpers.GetResourceLoggerService();
        var logger = service.GetLogger(testResource);

        var subsLoop = WatchForSubscribers(service);
        var logsLoop = ConsoleLoggingTestHelpers.WatchForLogsAsync(service, 2, testResource);

        // Wait for subscriber to be added
        await subsLoop.DefaultTimeout();

        logger.LogInformation("Hello, world!");
        logger.LogError("Hello, error!");

        // Complete the log stream & log afterwards
        service.Complete(testResource);
        logger.LogInformation("The third log");

        // Wait for logs to be read
        var allLogs = await logsLoop.DefaultTimeout();

        Assert.That.Collection(allLogs,
            l => Assert.AreEqual("2000-12-29T20:59:59.0000000Z Hello, world!", l.Content),
            l => Assert.AreEqual("2000-12-29T20:59:59.0000000Z Hello, error!", l.Content));

        // The backlog should be cleared once there are no subscribers.
        Assert.IsEmpty(service.GetResourceLoggerState(testResource.Name).GetBacklogSnapshot());

        // New sub should replay logs again.
        logsLoop = ConsoleLoggingTestHelpers.WatchForLogsAsync(service, 100, testResource);
        allLogs = await logsLoop.DefaultTimeout();

        Assert.That.Collection(allLogs,
            l => Assert.AreEqual("2000-12-29T20:59:59.0000000Z Hello, world!", l.Content),
            l => Assert.AreEqual("2000-12-29T20:59:59.0000000Z Hello, error!", l.Content));
    }

    [TestMethod]
    public async Task SecondSubscriberGetsBacklog()
    {
        var testResource = new TestResource("myResource");
        var service = ConsoleLoggingTestHelpers.GetResourceLoggerService();
        var logger = service.GetLogger(testResource);

        var subsLoop = WatchForSubscribers(service);
        var logsEnumerator1 = service.WatchAsync(testResource).GetAsyncEnumerator();
        var logsLoop = ConsoleLoggingTestHelpers.WatchForLogsAsync(logsEnumerator1, 2);

        // Wait for subscriber to be added
        await subsLoop.DefaultTimeout();

        // Log
        logger.LogInformation("Hello, world!");
        logger.LogError("Hello, error!");

        // Wait for logs to be read
        var allLogs = await logsLoop.DefaultTimeout();

        Assert.AreEqual("2000-12-29T20:59:59.0000000Z Hello, world!", allLogs[0].Content);
        Assert.IsFalse(allLogs[0].IsErrorMessage);

        Assert.AreEqual("2000-12-29T20:59:59.0000000Z Hello, error!", allLogs[1].Content);
        Assert.IsTrue(allLogs[1].IsErrorMessage);

        // New sub should get the previous logs (backlog)
        subsLoop = WatchForSubscribers(service);
        var logsEnumerator2 = service.WatchAsync(testResource).GetAsyncEnumerator();
        logsLoop = ConsoleLoggingTestHelpers.WatchForLogsAsync(logsEnumerator2, 2);
        await subsLoop.DefaultTimeout();
        allLogs = await logsLoop.DefaultTimeout();

        Assert.AreEqual(2, allLogs.Count);
        Assert.AreEqual("2000-12-29T20:59:59.0000000Z Hello, world!", allLogs[0].Content);
        Assert.AreEqual("2000-12-29T20:59:59.0000000Z Hello, error!", allLogs[1].Content);

        // Clear the backlog and ensure new subs only get new logs
        service.ClearBacklog(testResource.Name);

        subsLoop = WatchForSubscribers(service);
        var logsEnumerator3 = service.WatchAsync(testResource).GetAsyncEnumerator();
        logsLoop = ConsoleLoggingTestHelpers.WatchForLogsAsync(logsEnumerator3, 1);
        await subsLoop.DefaultTimeout();
        logger.LogInformation("The third log");
        allLogs = await logsLoop.DefaultTimeout();

        // The backlog should be cleared so only new logs are received
        Assert.ContainsSingle(allLogs);
        Assert.AreEqual("2000-12-29T20:59:59.0000000Z The third log", allLogs[0].Content);
    }

    [TestMethod]
    public async Task InMemoryLogsPreservedBetweenWatches()
    {
        var testResource = new TestResource("myResource");
        var service = ConsoleLoggingTestHelpers.GetResourceLoggerService();
        var logger = service.GetLogger(testResource);

        // Log before watching
        logger.LogInformation("Before watching!");

        var subsLoop = WatchForSubscribers(service);
        var logsEnumerator1 = service.WatchAsync(testResource).GetAsyncEnumerator();
        var logsLoop = ConsoleLoggingTestHelpers.WatchForLogsAsync(logsEnumerator1, 1);

        // Wait for subscriber to be added
        await subsLoop.DefaultTimeout();

        // Read before watching log
        var allLogs = await logsLoop.DefaultTimeout();

        Assert.AreEqual("2000-12-29T20:59:59.0000000Z Before watching!", allLogs[0].Content);
        Assert.IsFalse(allLogs[0].IsErrorMessage);

        // Log while watching
        logger.LogInformation("While watching!");

        logsLoop = ConsoleLoggingTestHelpers.WatchForLogsAsync(logsEnumerator1, 1);
        allLogs = await logsLoop.DefaultTimeout();

        Assert.AreEqual("2000-12-29T20:59:59.0000000Z While watching!", allLogs[0].Content);
        Assert.IsFalse(allLogs[0].IsErrorMessage);

        // New sub should get the previous logs (backlog)
        subsLoop = WatchForSubscribers(service);
        var logsEnumerator2 = service.WatchAsync(testResource).GetAsyncEnumerator();
        logsLoop = ConsoleLoggingTestHelpers.WatchForLogsAsync(logsEnumerator2, 2);
        await subsLoop.DefaultTimeout();
        allLogs = await logsLoop.DefaultTimeout();

        Assert.AreEqual(2, allLogs.Count);
        Assert.AreEqual("2000-12-29T20:59:59.0000000Z Before watching!", allLogs[0].Content);
        Assert.AreEqual("2000-12-29T20:59:59.0000000Z While watching!", allLogs[1].Content);

        await logsEnumerator1.DisposeAsync().DefaultTimeout();
        await logsEnumerator2.DisposeAsync().DefaultTimeout();

        logger.LogInformation("After watching!");

        // The backlog should be cleared once there are no subscribers.
        Assert.IsEmpty(service.GetResourceLoggerState(testResource.Name).GetBacklogSnapshot());

        subsLoop = WatchForSubscribers(service);
        var logsEnumerator3 = service.WatchAsync(testResource).GetAsyncEnumerator();
        logsLoop = ConsoleLoggingTestHelpers.WatchForLogsAsync(logsEnumerator3, 4);
        await subsLoop.DefaultTimeout();
        logger.LogInformation("While watching again!");
        allLogs = await logsLoop.DefaultTimeout();

        Assert.AreEqual(4, allLogs.Count);
        Assert.AreEqual("2000-12-29T20:59:59.0000000Z Before watching!", allLogs[0].Content);
        Assert.AreEqual("2000-12-29T20:59:59.0000000Z While watching!", allLogs[1].Content);
        Assert.AreEqual("2000-12-29T20:59:59.0000000Z After watching!", allLogs[2].Content);
        Assert.AreEqual("2000-12-29T20:59:59.0000000Z While watching again!", allLogs[3].Content);
    }

    [TestMethod]
    public async Task MultipleInstancesLogsToAll()
    {
        var testResource = new TestResource("myResource");
        testResource.Annotations.Add(new DcpInstancesAnnotation([new DcpInstance("instance0", "0", 0), new DcpInstance("instance1", "1", 1)]));

        var service = ConsoleLoggingTestHelpers.GetResourceLoggerService();
        var logger = service.GetLogger(testResource);

        var subsLoop = WatchForSubscribers(service);

        var logsEnumerator = service.WatchAsync(testResource).GetAsyncEnumerator();
        var logsLoop = ConsoleLoggingTestHelpers.WatchForLogsAsync(logsEnumerator, 4);

        // Wait for subscriber to be added
        await subsLoop.DefaultTimeout();

        // Log
        logger.LogInformation("Hello, world!");
        logger.LogError("Hello, error!");

        Assert.IsTrue(service.Loggers.ContainsKey("instance0"));
        Assert.IsTrue(service.Loggers.ContainsKey("instance1"));

        // Wait for logs to be read
        var allLogs = await logsLoop.DefaultTimeout();

        var sortedLogs = allLogs.OrderBy(l => l.LineNumber).ToList();

        Assert.AreEqual("2000-12-29T20:59:59.0000000Z Hello, world!", sortedLogs[0].Content);
        Assert.AreEqual("2000-12-29T20:59:59.0000000Z Hello, world!", sortedLogs[1].Content);
        Assert.AreEqual("2000-12-29T20:59:59.0000000Z Hello, error!", sortedLogs[2].Content);
        Assert.AreEqual("2000-12-29T20:59:59.0000000Z Hello, error!", sortedLogs[3].Content);

        service.Complete(testResource);

        Assert.IsFalse(await logsEnumerator.MoveNextAsync().DefaultTimeout());

        await logsEnumerator.DisposeAsync().DefaultTimeout();
    }

    private sealed class TestResource(string name) : Resource(name)
    {

    }

    private static Task WatchForSubscribers(ResourceLoggerService service)
    {
        return Task.Run(async () =>
        {
            await foreach (var sub in service.WatchAnySubscribersAsync())
            {
                if (sub.AnySubscribers)
                {
                    break;
                }
            }
        });
    }
}
