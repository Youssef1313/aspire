// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Azure.Core;
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Primitives;
using Azure.Messaging.EventHubs.Producer;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Azure.Messaging.EventHubs.Tests;

[TestClass]
public class AspireEventHubsExtensionsTests
{
    private const string AspireEventHubsSection = "Aspire:Azure:Messaging:EventHubs:";
    internal const string EhConnectionString = "Endpoint=sb://aspireeventhubstests.servicebus.windows.net/;" +
                                              "SharedAccessKeyName=fake;SharedAccessKey=fake;EntityPath=MyHub";
    private const string FullyQualifiedNamespace = "aspireeventhubstests.servicebus.windows.net";
    private const string BlobsConnectionString = "https://fake.blob.core.windows.net";

    private const int EventHubProducerClientIndex = 0;
    private const int EventHubConsumerClientIndex = 1;
    private const int EventProcessorClientIndex = 2;
    private const int PartitionReceiverIndex = 3;
    private const int EventBufferedProducerClientIndex = 4;

    private static readonly Action<HostApplicationBuilder, string, Action<AzureMessagingEventHubsSettings>?>[] s_keyedClientAdders =
    [
        (builder, key, settings) => builder.AddKeyedAzureEventHubProducerClient(key, settings),
        (builder, key, settings) => builder.AddKeyedAzureEventHubConsumerClient(key, settings),
        (builder, key, settings) => builder.AddKeyedAzureEventProcessorClient(key, settings),
        (builder, key, settings) => builder.AddKeyedAzurePartitionReceiverClient(key, settings),
        (builder, key, settings) => builder.AddKeyedAzureEventHubBufferedProducerClient(key, settings),
    ];

    private static readonly Action<HostApplicationBuilder, string, Action<AzureMessagingEventHubsSettings>?>[] s_clientAdders =
    [
        (builder, name, settings) => builder.AddAzureEventHubProducerClient(name, settings),
        (builder, name, settings) => builder.AddAzureEventHubConsumerClient(name, settings),
        (builder, name, settings) => builder.AddAzureEventProcessorClient(name, settings),
        (builder, name, settings) => builder.AddAzurePartitionReceiverClient(name, settings),
        (builder, name, settings) => builder.AddAzureEventHubBufferedProducerClient(name, settings),
    ];

    private static readonly Type[] s_clientTypes =
    [
        typeof(EventHubProducerClient),
        typeof(EventHubConsumerClient),
        typeof(EventProcessorClient),
        typeof(PartitionReceiver),
        typeof(EventHubBufferedProducerClient)
    ];

    private static void ConfigureBlobServiceClient(bool useKeyed, IServiceCollection services)
    {
        var blobClient = new BlobServiceClient(new Uri(BlobsConnectionString), new DefaultAzureCredential());
        if (useKeyed)
        {
            services.AddKeyedSingleton("blobs", blobClient);
        }
        else
        {
            services.AddSingleton(blobClient);
        }
    }

    [TestMethod]
    [DataRow(false, EventProcessorClientIndex)]
    [DataRow(true, EventProcessorClientIndex)]
    public void ProcessorClientShouldNotTryCreateContainerWithBlobContainerSpecified(bool useKeyed, int clientIndex)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        ConfigureBlobServiceClient(useKeyed, builder.Services);

        var key = useKeyed ? "eh" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "BlobClientServiceKey"), useKeyed ? "blobs" : null),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "BlobContainerName"), "checkpoints"),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    AspireEventHubsSection + s_clientTypes[clientIndex].Name, key, "PartitionId"), "foo"),
            new KeyValuePair<string, string?>("ConnectionStrings:eh", EhConnectionString),
        ]);

        if (useKeyed)
        {
            s_keyedClientAdders[clientIndex](builder, "eh", null);
        }
        else
        {
            s_clientAdders[clientIndex](builder, "eh", null);
        }

        using var host = builder.Build();

        RetrieveAndAssert(useKeyed, clientIndex, host);
    }

    [TestMethod]
    [DataRow(false, EventHubProducerClientIndex)]
    [DataRow(true, EventHubProducerClientIndex)]
    [DataRow(false, EventHubConsumerClientIndex)]
    [DataRow(true, EventHubConsumerClientIndex)]
    [DataRow(false, EventProcessorClientIndex)]
    [DataRow(true, EventProcessorClientIndex)]
    [DataRow(false, PartitionReceiverIndex)]
    [DataRow(true, PartitionReceiverIndex)]
    [DataRow(false, EventBufferedProducerClientIndex)]
    [DataRow(true, EventBufferedProducerClientIndex)]
    public void BindsClientOptionsFromConfigurationWithNamespace(bool useKeyed, int clientIndex)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        ConfigureBlobServiceClient(useKeyed, builder.Services);

        var key = useKeyed ? "eh" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "ClientOptions:Identifier"), "customidentifier"),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "BlobContainerName"), "checkpoints"),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "BlobClientServiceKey"), useKeyed ? "blobs" : null),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    AspireEventHubsSection + s_clientTypes[clientIndex].Name, key, "PartitionId"), "foo"),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "FullyQualifiedNamespace"), FullyQualifiedNamespace),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "EventHubName"), "MyHub"),
        ]);

        if (useKeyed)
        {
            s_keyedClientAdders[clientIndex](builder, "eh", null);
        }
        else
        {
            s_clientAdders[clientIndex](builder, "eh", null);
        }

        using var host = builder.Build();

        var assignedIdentifier = RetrieveClient(key, clientIndex, host) switch
        {
            EventProcessorClient processor => processor.Identifier,
            EventHubConsumerClient consumer => consumer.Identifier,
            EventHubProducerClient producer => producer.Identifier,
            PartitionReceiver receiver => receiver.Identifier,
            EventHubBufferedProducerClient producer => producer.Identifier,
            _ => null
        };

        Assert.IsNotNull(assignedIdentifier);
        Assert.AreEqual("customidentifier", assignedIdentifier);
    }

    [TestMethod]
    [DataRow(false, EventHubProducerClientIndex)]
    [DataRow(true, EventHubProducerClientIndex)]
    [DataRow(false, EventHubConsumerClientIndex)]
    [DataRow(true, EventHubConsumerClientIndex)]
    [DataRow(false, EventProcessorClientIndex)]
    [DataRow(true, EventProcessorClientIndex)]
    [DataRow(false, PartitionReceiverIndex)]
    [DataRow(true, PartitionReceiverIndex)]
    [DataRow(false, EventBufferedProducerClientIndex)]
    [DataRow(true, EventBufferedProducerClientIndex)]
    public void BindsClientOptionsFromConfigurationWithConnectionString(bool useKeyed, int clientIndex)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        ConfigureBlobServiceClient(useKeyed, builder.Services);

        var key = useKeyed ? "eh" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "ClientOptions:Identifier"), "customidentifier"),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "BlobContainerName"), "checkpoints"),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "BlobClientServiceKey"), useKeyed ? "blobs" : null),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    AspireEventHubsSection + s_clientTypes[clientIndex].Name, key, "PartitionId"), "foo"),
            new KeyValuePair<string, string?>("ConnectionStrings:eh", EhConnectionString)
        ]);

        if (useKeyed)
        {
            s_keyedClientAdders[clientIndex](builder, "eh", null);
        }
        else
        {
            s_clientAdders[clientIndex](builder, "eh", null);
        }

        using var host = builder.Build();

        var assignedIdentifier = RetrieveClient(key, clientIndex, host) switch
        {
            EventProcessorClient processor => processor.Identifier,
            EventHubConsumerClient consumer => consumer.Identifier,
            EventHubProducerClient producer => producer.Identifier,
            PartitionReceiver receiver => receiver.Identifier,
            EventHubBufferedProducerClient producer => producer.Identifier,
            _ => null
        };

        Assert.IsNotNull(assignedIdentifier);
        Assert.AreEqual("customidentifier", assignedIdentifier);
    }

    [TestMethod]
    [DataRow(false, EventHubProducerClientIndex)]
    [DataRow(true, EventHubProducerClientIndex)]
    [DataRow(false, EventHubConsumerClientIndex)]
    [DataRow(true, EventHubConsumerClientIndex)]
    [DataRow(false, EventProcessorClientIndex)]
    [DataRow(true, EventProcessorClientIndex)]
    [DataRow(false, PartitionReceiverIndex)]
    [DataRow(true, PartitionReceiverIndex)]
    [DataRow(false, EventBufferedProducerClientIndex)]
    [DataRow(true, EventBufferedProducerClientIndex)]
    public void BindsClientOptionsFromConfigurationWithConnectionStringAndEventHubName(bool useKeyed, int clientIndex)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        ConfigureBlobServiceClient(useKeyed, builder.Services);

        var key = useKeyed ? "eh" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "ClientOptions:Identifier"), "customidentifier"),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "BlobContainerName"), "checkpoints"),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "BlobClientServiceKey"), useKeyed ? "blobs" : null),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    AspireEventHubsSection + s_clientTypes[clientIndex].Name, key, "PartitionId"), "foo"),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "EventHubName"), "MyHub"),
            new KeyValuePair<string, string?>("ConnectionStrings:eh", EhConnectionString),
        ]);

        if (useKeyed)
        {
            s_keyedClientAdders[clientIndex](builder, "eh", null);
        }
        else
        {
            s_clientAdders[clientIndex](builder, "eh", null);
        }

        using var host = builder.Build();

        var assignedIdentifier = RetrieveClient(key, clientIndex, host) switch
        {
            EventProcessorClient processor => processor.Identifier,
            EventHubConsumerClient consumer => consumer.Identifier,
            EventHubProducerClient producer => producer.Identifier,
            PartitionReceiver receiver => receiver.Identifier,
            EventHubBufferedProducerClient producer => producer.Identifier,
            _ => null
        };

        Assert.IsNotNull(assignedIdentifier);
        Assert.AreEqual("customidentifier", assignedIdentifier);
    }

    [TestMethod]
    [DataRow(false, EventHubProducerClientIndex)]
    [DataRow(true, EventHubProducerClientIndex)]
    [DataRow(false, EventHubConsumerClientIndex)]
    [DataRow(true, EventHubConsumerClientIndex)]
    [DataRow(false, EventProcessorClientIndex)]
    [DataRow(true, EventProcessorClientIndex)]
    [DataRow(false, PartitionReceiverIndex)]
    [DataRow(true, PartitionReceiverIndex)]
    [DataRow(false, EventBufferedProducerClientIndex)]
    [DataRow(true, EventBufferedProducerClientIndex)]
    public void ReadsFromConnectionStringsCorrectly(bool useKeyed, int clientIndex)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        ConfigureBlobServiceClient(useKeyed, builder.Services);

        var key = useKeyed ? "eh" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "BlobClientServiceKey"), useKeyed ? "blobs" : null),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "BlobContainerName"), "checkpoints"),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    AspireEventHubsSection + s_clientTypes[clientIndex].Name, key, "PartitionId"), "foo"),
            new KeyValuePair<string, string?>("ConnectionStrings:eh", EhConnectionString)
        ]);

        if (useKeyed)
        {
            s_keyedClientAdders[clientIndex](builder, "eh", null);
        }
        else
        {
            s_clientAdders[clientIndex](builder, "eh", null);
        }

        using var host = builder.Build();
        RetrieveAndAssert(useKeyed, clientIndex, host);
    }

    [TestMethod]
    [DataRow(false, EventHubProducerClientIndex)]
    [DataRow(true, EventHubProducerClientIndex)]
    [DataRow(false, EventHubConsumerClientIndex)]
    [DataRow(true, EventHubConsumerClientIndex)]
    [DataRow(false, EventProcessorClientIndex)]
    [DataRow(true, EventProcessorClientIndex)]
    [DataRow(false, PartitionReceiverIndex)]
    [DataRow(true, PartitionReceiverIndex)]
    [DataRow(false, EventBufferedProducerClientIndex)]
    [DataRow(true, EventBufferedProducerClientIndex)]
    public void ConnectionStringCanBeSetInCode(bool useKeyed, int clientIndex)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        ConfigureBlobServiceClient(useKeyed, builder.Services);

        var key = useKeyed ? "eh" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "BlobClientServiceKey"), useKeyed? "blobs" : null),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "BlobContainerName"), "checkpoints"),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    AspireEventHubsSection + s_clientTypes[clientIndex].Name, key, "PartitionId"), "foo")
        ]);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:eh", EhConnectionString)
        ]);

        if (useKeyed)
        {
            s_keyedClientAdders[clientIndex](builder, "eh", settings => settings.ConnectionString = EhConnectionString);
        }
        else
        {
            s_clientAdders[clientIndex](builder, "eh", settings => settings.ConnectionString = EhConnectionString);
        }

        using var host = builder.Build();
        RetrieveAndAssert(useKeyed, clientIndex, host);
    }

    [TestMethod]
    [DataRow(false, EventHubProducerClientIndex)]
    [DataRow(true, EventHubProducerClientIndex)]
    [DataRow(false, EventHubConsumerClientIndex)]
    [DataRow(true, EventHubConsumerClientIndex)]
    [DataRow(false, EventProcessorClientIndex)]
    [DataRow(true, EventProcessorClientIndex)]
    [DataRow(false, PartitionReceiverIndex)]
    [DataRow(true, PartitionReceiverIndex)]
    [DataRow(false, EventBufferedProducerClientIndex)]
    [DataRow(true, EventBufferedProducerClientIndex)]
    public void ConnectionNameWinsOverConfigSection(bool useKeyed, int clientIndex)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        ConfigureBlobServiceClient(useKeyed, builder.Services);

        var key = useKeyed ? "eh" : null;
        builder.Configuration.AddInMemoryCollection([
            // component settings
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "ConnectionString"), "unused"),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "BlobContainerName"), "checkpoints"),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "BlobClientServiceKey"), useKeyed ? "blobs" : null),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "PartitionId"), "foo"),

            // ambient connection strings
            new KeyValuePair<string, string?>("ConnectionStrings:eh", EhConnectionString),
        ]);

        if (useKeyed)
        {
            s_keyedClientAdders[clientIndex](builder, "eh", null);
        }
        else
        {
            s_clientAdders[clientIndex](builder, "eh", null);
        }

        using var host = builder.Build();
        RetrieveAndAssert(useKeyed, clientIndex, host);
    }

    private static void RetrieveAndAssert(bool useKeyed, int clientIndex, IHost host)
    {
        var client = RetrieveClient(useKeyed ? "eh" : null, clientIndex, host);

        AssertFullyQualifiedNamespace(FullyQualifiedNamespace, client);
    }

    private static object RetrieveClient(object? key, int clientIndex, IHost host)
    {
        var client = key is not null ?
            host.Services.GetRequiredKeyedService(s_clientTypes[clientIndex], key) :
            host.Services.GetRequiredService(s_clientTypes[clientIndex]);

        return client;
    }

    private static void AssertFullyQualifiedNamespace(string expectedNamespace, object client)
    {
        Assert.AreEqual(expectedNamespace, client switch
        {
            EventHubProducerClient producer => producer.FullyQualifiedNamespace,
            EventHubConsumerClient consumer => consumer.FullyQualifiedNamespace,
            EventProcessorClient processor => processor.FullyQualifiedNamespace,
            PartitionReceiver receiver => receiver.FullyQualifiedNamespace,
            EventHubBufferedProducerClient producer => producer.FullyQualifiedNamespace,
            _ => throw new InvalidOperationException()
        });
    }

    [TestMethod]
    [DataRow(false, EventHubProducerClientIndex)]
    [DataRow(true, EventHubProducerClientIndex)]
    [DataRow(false, EventHubConsumerClientIndex)]
    [DataRow(true, EventHubConsumerClientIndex)]
    [DataRow(false, EventProcessorClientIndex)]
    [DataRow(true, EventProcessorClientIndex)]
    [DataRow(false, PartitionReceiverIndex)]
    [DataRow(true, PartitionReceiverIndex)]
    [DataRow(false, EventBufferedProducerClientIndex)]
    public void NamespaceWorksInConnectionStrings(bool useKeyed, int clientIndex)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        ConfigureBlobServiceClient(useKeyed, builder.Services);

        var key = useKeyed ? "eh" : null;

        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "EventHubName"), "MyHub"),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "BlobClientServiceKey"), useKeyed ? "blobs" : null),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "BlobContainerName"), "checkpoints"),
            new KeyValuePair<string, string?>(
                CreateConfigKey(
                    $"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}",
                    key, "PartitionId"), "foo"),

            new KeyValuePair<string, string?>("ConnectionStrings:eh", EhConnectionString),
        ]);

        if (useKeyed)
        {
            s_keyedClientAdders[clientIndex](builder, "eh", null);
        }
        else
        {
            s_clientAdders[clientIndex](builder, "eh", null);
        }

        using var host = builder.Build();
        RetrieveAndAssert(useKeyed, clientIndex, host);
    }

    [TestMethod]
    [DataRow(EventHubProducerClientIndex)]
    [DataRow(EventHubConsumerClientIndex)]
    [DataRow(EventProcessorClientIndex)]
    [DataRow(PartitionReceiverIndex)]
    [DataRow(EventBufferedProducerClientIndex)]
    public void CanAddMultipleKeyedServices(int clientIndex)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:eh1", EhConnectionString),
            new KeyValuePair<string, string?>($"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}:BlobContainerName", "checkpoints"),
            new KeyValuePair<string, string?>($"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}:PartitionId", "foo"),

            new KeyValuePair<string, string?>("ConnectionStrings:eh2", EhConnectionString.Replace("aspireeventhubstests", "aspireeventhubstests2")),
            new KeyValuePair<string, string?>($"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}:eh2:BlobContainerName", "checkpoints"),
            new KeyValuePair<string, string?>($"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}:eh2:PartitionId", "foo"),

            new KeyValuePair<string, string?>("ConnectionStrings:eh3", EhConnectionString.Replace("aspireeventhubstests", "aspireeventhubstests3")),
            new KeyValuePair<string, string?>($"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}:eh3:BlobContainerName", "checkpoints"),
            new KeyValuePair<string, string?>($"Aspire:Azure:Messaging:EventHubs:{s_clientTypes[clientIndex].Name}:eh3:PartitionId", "foo"),
        ]);

        ConfigureBlobServiceClient(useKeyed: false, builder.Services);

        s_clientAdders[clientIndex](builder, "eh1", null);
        s_keyedClientAdders[clientIndex](builder, "eh2", null);
        s_keyedClientAdders[clientIndex](builder, "eh3", null);

        using var host = builder.Build();

        // Unkeyed services don't work with keyed services. See https://github.com/dotnet/aspire/issues/3890
        //var client1 = RetrieveClient(key: null, clientIndex, host);
        var client2 = RetrieveClient(key: "eh2", clientIndex, host);
        var client3 = RetrieveClient(key: "eh3", clientIndex, host);

        //Assert.AreNotSame(client1, client2);
        //Assert.AreNotSame(client1, client3);
        Assert.AreNotSame(client2, client3);

        //AssertFullyQualifiedNamespace("aspireeventhubstests.servicebus.windows.net", client1);
        AssertFullyQualifiedNamespace("aspireeventhubstests2.servicebus.windows.net", client2);
        AssertFullyQualifiedNamespace("aspireeventhubstests3.servicebus.windows.net", client3);
    }

    public static string CreateConfigKey(string prefix, string? key, string suffix)
        => string.IsNullOrEmpty(key) ? $"{prefix}:{suffix}" : $"{prefix}:{key}:{suffix}";

    /// <summary>
    /// Tests that the BlobContainerName defaults correctly when the connection string doesn't contain ".servicebus" and
    /// contains invalid container name characters.
    /// </summary>
    [TestMethod]
    public void ProcessorBlobContainerNameDefaultsCorrectly()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:eh1", "Endpoint=sb://127.0.0.1:53589;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;"),
            new KeyValuePair<string, string?>("Aspire:Azure:Messaging:EventHubs:EventProcessorClient:EventHubName", "MyHub"),
        ]);

        var mockTransport = InjectMockBlobClient(builder);

        builder.AddAzureEventProcessorClient("eh1");

        using var host = builder.Build();

        var client = host.Services.GetRequiredService<EventProcessorClient>();
        Assert.IsNotNull(client);

        Assert.ContainsSingle(mockTransport.Requests);
        // the container name should be based on the Endpoint, EventHubName, and ConsumerGroup
        Assert.AreEqual("https://fake.blob.core.windows.net/127-0-0-1-MyHub-default?restype=container", mockTransport.Requests[0].Uri.ToString());
    }

    internal static MockTransport InjectMockBlobClient(HostApplicationBuilder builder)
    {
        var mockTransport = new MockTransport(
            CreateResponse("""{}"""));
        var blobClient = new BlobServiceClient(new Uri(BlobsConnectionString), new BlobClientOptions() { Transport = mockTransport });
        builder.Services.AddSingleton(blobClient);
        return mockTransport;
    }

    internal static MockResponse CreateResponse(string content)
    {
        var buffer = Encoding.UTF8.GetBytes(content);
        var response = new MockResponse(201)
        {
            ClientRequestId = Guid.NewGuid().ToString(),
            ContentStream = new MemoryStream(buffer),
        };

        response.AddHeader(new HttpHeader("Content-Type", "application/json; charset=utf-8"));

        return response;
    }

    [TestMethod]
    [MemberData(nameof(ConnectionString_MemberData))]
    public void AddAzureCosmosClient_EnsuresConnectionStringIsCorrect(EventHubTestConnectionInfo testInfo)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:eh1", testInfo.TestConnectionString),
            new KeyValuePair<string, string?>("Aspire:Azure:Messaging:EventHubs:EventHubProducerClient:EventHubName", "NotInConnectionInfo"),
            new KeyValuePair<string, string?>("Aspire:Azure:Messaging:EventHubs:EventHubConsumerClient:EventHubName", "NotInConnectionInfo"),
            new KeyValuePair<string, string?>("Aspire:Azure:Messaging:EventHubs:EventProcessorClient:EventHubName", "NotInConnectionInfo"),
            new KeyValuePair<string, string?>("Aspire:Azure:Messaging:EventHubs:PartitionReceiver:EventHubName", "NotInConnectionInfo"),
            new KeyValuePair<string, string?>("Aspire:Azure:Messaging:EventHubs:PartitionReceiver:PartitionId", "foo"),
            new KeyValuePair<string, string?>("Aspire:Azure:Messaging:EventHubs:EventHubBufferedProducerClient:EventHubName", "NotInConnectionInfo"),
        ]);

        var expectedEventHubName = testInfo.EventHubName ?? "NotInConnectionInfo";

        var settingsCalled = 0;
        void VerifySettings(AzureMessagingEventHubsSettings settings)
        {
            settingsCalled++;

            Assert.AreEqual(testInfo.ConnectionString, settings.ConnectionString);
            Assert.AreEqual(testInfo.FullyQualifiedNamespace, settings.FullyQualifiedNamespace);
            Assert.AreEqual(expectedEventHubName, settings.EventHubName);

            var consumerGroupProperty = settings.GetType().GetProperty("ConsumerGroup");
            if (consumerGroupProperty != null)
            {
                Assert.AreEqual(testInfo.ConsumerGroup, consumerGroupProperty.GetValue(settings));
            }
        }

        InjectMockBlobClient(builder);

        builder.AddAzureEventHubProducerClient("eh1", VerifySettings);
        builder.AddAzureEventHubConsumerClient("eh1", VerifySettings);
        builder.AddAzureEventProcessorClient("eh1", VerifySettings);
        builder.AddAzurePartitionReceiverClient("eh1", VerifySettings);
        builder.AddAzureEventHubBufferedProducerClient("eh1", VerifySettings);

        Assert.AreEqual(5, settingsCalled);

        using var app = builder.Build();

        var producerClient = app.Services.GetRequiredService<EventHubProducerClient>();
        Assert.AreEqual(testInfo.ClientFullyQualifiedNamespace, producerClient.FullyQualifiedNamespace);
        Assert.AreEqual(expectedEventHubName, producerClient.EventHubName);

        var consumerClient = app.Services.GetRequiredService<EventHubConsumerClient>();
        Assert.AreEqual(testInfo.ClientFullyQualifiedNamespace, consumerClient.FullyQualifiedNamespace);
        Assert.AreEqual(expectedEventHubName, consumerClient.EventHubName);

        var processorClient = app.Services.GetRequiredService<EventProcessorClient>();
        Assert.AreEqual(testInfo.ClientFullyQualifiedNamespace, processorClient.FullyQualifiedNamespace);
        Assert.AreEqual(expectedEventHubName, processorClient.EventHubName);

        var partitionReceiver = app.Services.GetRequiredService<PartitionReceiver>();
        Assert.AreEqual(testInfo.ClientFullyQualifiedNamespace, partitionReceiver.FullyQualifiedNamespace);
        Assert.AreEqual(expectedEventHubName, partitionReceiver.EventHubName);

        var bufferedProducerClient = app.Services.GetRequiredService<EventHubBufferedProducerClient>();
        Assert.AreEqual(testInfo.ClientFullyQualifiedNamespace, bufferedProducerClient.FullyQualifiedNamespace);
        Assert.AreEqual(expectedEventHubName, bufferedProducerClient.EventHubName);
    }

    public static TheoryData<EventHubTestConnectionInfo> ConnectionString_MemberData()
    {
        return new()
        {
            new EventHubTestConnectionInfo()
            {
                TestConnectionString = "Endpoint=sb://localhost:55184;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;",
                ConnectionString = "Endpoint=sb://localhost:55184;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;",
                ClientFullyQualifiedNamespace = "localhost"
            },
            new EventHubTestConnectionInfo()
            {
                TestConnectionString ="Endpoint=sb://localhost:55184;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;EntityPath=myhub",
                ConnectionString = "Endpoint=sb://localhost:55184;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;EntityPath=myhub",
                EventHubName = "myhub",
                ClientFullyQualifiedNamespace = "localhost"
            },
            new EventHubTestConnectionInfo()
            {
                TestConnectionString ="Endpoint=sb://localhost:55184;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;ConsumerGroup=mygroup;EntityPath=myhub",
                ConnectionString = "Endpoint=sb://localhost:55184;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;ConsumerGroup=mygroup;EntityPath=myhub",
                EventHubName = "myhub",
                ConsumerGroup = "mygroup",
                ClientFullyQualifiedNamespace = "localhost"
            },
            new EventHubTestConnectionInfo()
            {
                TestConnectionString ="Endpoint=https://eventhubns-cetg3lr.servicebus.windows.net:443/;EntityPath=myhub;ConsumerGroup=mygroup",
                FullyQualifiedNamespace = "https://eventhubns-cetg3lr.servicebus.windows.net:443/",
                EventHubName = "myhub",
                ConsumerGroup = "mygroup",
                ClientFullyQualifiedNamespace = "eventhubns-cetg3lr.servicebus.windows.net"
            },
            new EventHubTestConnectionInfo()
            {
                TestConnectionString ="Endpoint=https://eventhubns-cetg3lr.servicebus.windows.net:443/;EntityPath=myhub",
                FullyQualifiedNamespace = "https://eventhubns-cetg3lr.servicebus.windows.net:443/",
                EventHubName = "myhub",
                ClientFullyQualifiedNamespace = "eventhubns-cetg3lr.servicebus.windows.net"
            },
            new EventHubTestConnectionInfo()
            {
                TestConnectionString ="Endpoint=https://eventhubns-cetg3lr.servicebus.windows.net:443/;ConsumerGroup=mygroup",
                FullyQualifiedNamespace = "https://eventhubns-cetg3lr.servicebus.windows.net:443/",
                ConsumerGroup = "mygroup",
                ClientFullyQualifiedNamespace = "eventhubns-cetg3lr.servicebus.windows.net"
            },
            new EventHubTestConnectionInfo()
            {
                TestConnectionString ="https://eventhubns-cetg3lr.servicebus.windows.net:443/",
                FullyQualifiedNamespace = "https://eventhubns-cetg3lr.servicebus.windows.net:443/",
                ClientFullyQualifiedNamespace = "eventhubns-cetg3lr.servicebus.windows.net"
            }
        };
    }

    public record EventHubTestConnectionInfo
    {
        public required string TestConnectionString { get; set; }
        public string? FullyQualifiedNamespace { get; set; }
        public string? ConnectionString { get; set; }
        public string? EventHubName { get; set; }
        public string? ConsumerGroup { get; set; }
        public string? ClientFullyQualifiedNamespace { get; set; }
    }
}
