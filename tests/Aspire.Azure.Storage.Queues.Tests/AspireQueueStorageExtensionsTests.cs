// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Azure.Storage.Queues.Tests;

[TestClass]
public class AspireQueueStorageExtensionsTests
{
    private const string ConnectionString = "AccountName=aspirestoragetests;AccountKey=fake";

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ReadsFromConnectionStringsCorrectly(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:queue", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureQueueClient("queue");
        }
        else
        {
            builder.AddAzureQueueClient("queue");
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<QueueServiceClient>("queue") :
            host.Services.GetRequiredService<QueueServiceClient>();

        Assert.AreEqual("aspirestoragetests", client.AccountName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ConnectionStringCanBeSetInCode(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:queue", "AccountName=unused;AccountKey=fake")
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureQueueClient("queue", settings => settings.ConnectionString = ConnectionString);
        }
        else
        {
            builder.AddAzureQueueClient("queue", settings => settings.ConnectionString = ConnectionString);
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<QueueServiceClient>("queue") :
            host.Services.GetRequiredService<QueueServiceClient>();

        Assert.AreEqual("aspirestoragetests", client.AccountName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ConnectionNameWinsOverConfigSection(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var key = useKeyed ? "queue" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(ConformanceTests.CreateConfigKey("Aspire:Azure:Storage:Queues", key, "ServiceUri"), "unused"),
            new KeyValuePair<string, string?>("ConnectionStrings:queue", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureQueueClient("queue");
        }
        else
        {
            builder.AddAzureQueueClient("queue");
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<QueueServiceClient>("queue") :
            host.Services.GetRequiredService<QueueServiceClient>();

        Assert.AreEqual("aspirestoragetests", client.AccountName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ServiceUriWorksInConnectionStrings(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:queue", ConformanceTests.ServiceUri)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureQueueClient("queue");
        }
        else
        {
            builder.AddAzureQueueClient("queue");
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<QueueServiceClient>("queue") :
            host.Services.GetRequiredService<QueueServiceClient>();

        Assert.AreEqual("aspirestoragetests", client.AccountName);
    }

    [TestMethod]
    public void CanAddMultipleKeyedServices()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:queue1", ConformanceTests.ServiceUri),
            new KeyValuePair<string, string?>("ConnectionStrings:queue2", "https://aspirestoragetests2.queue.core.windows.net"),
            new KeyValuePair<string, string?>("ConnectionStrings:queue3", "https://aspirestoragetests3.queue.core.windows.net")
        ]);

        builder.AddAzureQueueClient("queue1");
        builder.AddKeyedAzureQueueClient("queue2");
        builder.AddKeyedAzureQueueClient("queue3");

        using var host = builder.Build();

        // Unkeyed services don't work with keyed services. See https://github.com/dotnet/aspire/issues/3890
        //var client1 = host.Services.GetRequiredService<QueueServiceClient>();
        var client2 = host.Services.GetRequiredKeyedService<QueueServiceClient>("queue2");
        var client3 = host.Services.GetRequiredKeyedService<QueueServiceClient>("queue3");

        //Assert.AreNotSame(client1, client2);
        //Assert.AreNotSame(client1, client3);
        Assert.AreNotSame(client2, client3);

        //Assert.AreEqual("aspirestoragetests", client1.AccountName);
        Assert.AreEqual("aspirestoragetests2", client2.AccountName);
        Assert.AreEqual("aspirestoragetests3", client3.AccountName);
    }
}
