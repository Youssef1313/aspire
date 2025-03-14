// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Azure.Messaging.ServiceBus.Tests;

[TestClass]
public class AspireServiceBusExtensionsTests
{
    private const string ConnectionString = "Endpoint=sb://aspireservicebustests.servicebus.windows.net/;SharedAccessKeyName=fake;SharedAccessKey=fake";

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ReadsFromConnectionStringsCorrectly(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:sb", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureServiceBusClient("sb");
        }
        else
        {
            builder.AddAzureServiceBusClient("sb");
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<ServiceBusClient>("sb") :
            host.Services.GetRequiredService<ServiceBusClient>();

        Assert.AreEqual(ConformanceTests.FullyQualifiedNamespace, client.FullyQualifiedNamespace);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ConnectionStringCanBeSetInCode(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:sb", "Endpoint=sb://unused.servicebus.windows.net/;SharedAccessKeyName=fake;SharedAccessKey=fake")
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureServiceBusClient("sb", settings => settings.ConnectionString = ConnectionString);
        }
        else
        {
            builder.AddAzureServiceBusClient("sb", settings => settings.ConnectionString = ConnectionString);
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<ServiceBusClient>("sb") :
            host.Services.GetRequiredService<ServiceBusClient>();

        Assert.AreEqual(ConformanceTests.FullyQualifiedNamespace, client.FullyQualifiedNamespace);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ConnectionNameWinsOverConfigSection(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var key = useKeyed ? "sb" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(ConformanceTests.CreateConfigKey("Aspire:Azure:Messaging:ServiceBus", key, "ConnectionString"), "unused"),
            new KeyValuePair<string, string?>("ConnectionStrings:sb", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureServiceBusClient("sb");
        }
        else
        {
            builder.AddAzureServiceBusClient("sb");
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<ServiceBusClient>("sb") :
            host.Services.GetRequiredService<ServiceBusClient>();

        Assert.AreEqual(ConformanceTests.FullyQualifiedNamespace, client.FullyQualifiedNamespace);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void NamespaceWorksInConnectionStrings(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:sb", ConformanceTests.FullyQualifiedNamespace)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureServiceBusClient("sb");
        }
        else
        {
            builder.AddAzureServiceBusClient("sb");
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<ServiceBusClient>("sb") :
            host.Services.GetRequiredService<ServiceBusClient>();

        Assert.AreEqual(ConformanceTests.FullyQualifiedNamespace, client.FullyQualifiedNamespace);
    }

    [TestMethod]
    public void CanAddMultipleKeyedServices()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:sb1", ConnectionString),
            new KeyValuePair<string, string?>("ConnectionStrings:sb2", "Endpoint=sb://aspireservicebustests2.servicebus.windows.net/;SharedAccessKeyName=fake;SharedAccessKey=fake"),
            new KeyValuePair<string, string?>("ConnectionStrings:sb3", "Endpoint=sb://aspireservicebustests3.servicebus.windows.net/;SharedAccessKeyName=fake;SharedAccessKey=fake")
        ]);

        builder.AddAzureServiceBusClient("sb1");
        builder.AddKeyedAzureServiceBusClient("sb2");
        builder.AddKeyedAzureServiceBusClient("sb3");

        using var host = builder.Build();

        // Unkeyed services don't work with keyed services. See https://github.com/dotnet/aspire/issues/3890
        //var client1 = host.Services.GetRequiredService<ServiceBusClient>();
        var client2 = host.Services.GetRequiredKeyedService<ServiceBusClient>("sb2");
        var client3 = host.Services.GetRequiredKeyedService<ServiceBusClient>("sb3");

        //Assert.AreNotSame(client1, client2);
        //Assert.AreNotSame(client1, client3);
        Assert.AreNotSame(client2, client3);

        //Assert.AreEqual(ConformanceTests.FullyQualifiedNamespace, client1.FullyQualifiedNamespace);
        Assert.AreEqual("aspireservicebustests2.servicebus.windows.net", client2.FullyQualifiedNamespace);
        Assert.AreEqual("aspireservicebustests3.servicebus.windows.net", client3.FullyQualifiedNamespace);
    }

    [TestMethod]
    public void FavorsNamedClientOptionsOverTopLevelClientOptionsWhenBothProvided()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("Aspire:Azure:Messaging:ServiceBus:ClientOptions:Identifier", "top-level-identifier"),
            new KeyValuePair<string, string?>(ConformanceTests.CreateConfigKey("Aspire:Azure:Messaging:ServiceBus", "sb", "ConnectionString"), ConnectionString),
            new KeyValuePair<string, string?>(ConformanceTests.CreateConfigKey("Aspire:Azure:Messaging:ServiceBus", "sb", "ClientOptions:Identifier"), "local-identifier"),
        ]);

        builder.AddAzureServiceBusClient("sb");

        using var host = builder.Build();

        var client = host.Services.GetRequiredService<ServiceBusClient>();
        Assert.AreEqual("local-identifier", client.Identifier);
    }

    [TestMethod]
    [DataRow("Endpoint=sb://aspireservicebustests.servicebus.windows.net;EntityPath=myqueue;", "aspireservicebustests.servicebus.windows.net")]
    [DataRow("Endpoint=sb://aspireservicebustests.servicebus.windows.net;EntityPath=mytopic;", "aspireservicebustests.servicebus.windows.net")]
    [DataRow("Endpoint=sb://aspireservicebustests.servicebus.windows.net;EntityPath=mytopic/Subscriptions/mysub;", "aspireservicebustests.servicebus.windows.net")]
    [DataRow("Endpoint=sb://localhost:50418;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true", "localhost")]
    [DataRow("Endpoint=sb://localhost:50418;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;EntityPath=myqueue", "localhost")]
    [DataRow("Endpoint=sb://localhost:50418;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;EntityPath=mytopic/Subscriptions/mysub;", "localhost")]
    [DataRow("aspireservicebustests.servicebus.windows.net", "aspireservicebustests.servicebus.windows.net")]
    public void AddAzureServiceBusClient_EnsuresConnectionStringIsCorrect(string connectionString, string expectedEndpoint)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        PopulateConfiguration(builder.Configuration, connectionString);

        builder.AddAzureServiceBusClient("sb");

        using var host = builder.Build();
        var client = host.Services.GetRequiredService<ServiceBusClient>();

        Assert.AreEqual(expectedEndpoint, client.FullyQualifiedNamespace);
    }

    private static void PopulateConfiguration(ConfigurationManager configuration, string connectionString) =>
    configuration.AddInMemoryCollection([
        new KeyValuePair<string, string?>("ConnectionStrings:sb", connectionString)
    ]);
}
