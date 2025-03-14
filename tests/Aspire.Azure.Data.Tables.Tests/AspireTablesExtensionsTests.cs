// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Azure.Data.Tables.Tests;

[TestClass]
public class AspireTablesExtensionsTests
{
    private const string ConnectionString = "AccountName=aspirestoragetests;AccountKey=fake";

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ReadsFromConnectionStringsCorrectly(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:tables", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureTableClient("tables");
        }
        else
        {
            builder.AddAzureTableClient("tables");
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<TableServiceClient>("tables") :
            host.Services.GetRequiredService<TableServiceClient>();

        Assert.AreEqual("aspirestoragetests", client.AccountName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ConnectionStringCanBeSetInCode(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:tables", "AccountName=unused;AccountKey=myAccountKey")
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureTableClient("tables", settings => settings.ConnectionString = ConnectionString);
        }
        else
        {
            builder.AddAzureTableClient("tables", settings => settings.ConnectionString = ConnectionString);
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<TableServiceClient>("tables") :
            host.Services.GetRequiredService<TableServiceClient>();

        Assert.AreEqual("aspirestoragetests", client.AccountName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ConnectionNameWinsOverConfigSection(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var key = useKeyed ? "tables" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(ConformanceTests.CreateConfigKey("Aspire:Azure:Data:Tables", key, "ServiceUri"), "unused"),
            new KeyValuePair<string, string?>("ConnectionStrings:tables", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureTableClient("tables");
        }
        else
        {
            builder.AddAzureTableClient("tables");
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<TableServiceClient>("tables") :
            host.Services.GetRequiredService<TableServiceClient>();

        Assert.AreEqual("aspirestoragetests", client.AccountName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ServiceUriWorksInConnectionStrings(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:tables", ConformanceTests.ServiceUri)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureTableClient("tables");
        }
        else
        {
            builder.AddAzureTableClient("tables");
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<TableServiceClient>("tables") :
            host.Services.GetRequiredService<TableServiceClient>();

        Assert.AreEqual("aspirestoragetests", client.AccountName);
    }

    [TestMethod]
    public void CanAddMultipleKeyedServices()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:tables1", ConnectionString),
            new KeyValuePair<string, string?>("ConnectionStrings:tables2", "AccountName=account2;AccountKey=fake"),
            new KeyValuePair<string, string?>("ConnectionStrings:tables3", "AccountName=account3;AccountKey=fake")
        ]);

        builder.AddAzureTableClient("tables1");
        builder.AddKeyedAzureTableClient("tables2");
        builder.AddKeyedAzureTableClient("tables3");

        using var host = builder.Build();

        // Unkeyed services don't work with keyed services. See https://github.com/dotnet/aspire/issues/3890
        //var client1 = host.Services.GetRequiredService<TableServiceClient>();
        var client2 = host.Services.GetRequiredKeyedService<TableServiceClient>("tables2");
        var client3 = host.Services.GetRequiredKeyedService<TableServiceClient>("tables3");

        //Assert.AreNotSame(client1, client2);
        //Assert.AreNotSame(client1, client3);
        Assert.AreNotSame(client2, client3);

        //Assert.AreEqual("aspirestoragetests", client1.AccountName);
        Assert.AreEqual("account2", client2.AccountName);
        Assert.AreEqual("account3", client3.AccountName);
    }
}
