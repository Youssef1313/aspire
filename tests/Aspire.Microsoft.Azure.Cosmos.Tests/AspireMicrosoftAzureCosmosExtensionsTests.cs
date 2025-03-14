// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Microsoft.Azure.Cosmos.Tests;

[TestClass]
public class AspireMicrosoftAzureCosmosExtensionsTests
{
    [TestMethod]
    [DataRow("AccountEndpoint=https://localhost:8081;AccountKey=fake;", "https://localhost:8081/")]
    [DataRow("AccountEndpoint=https://localhost:8081;Database=db;", "https://localhost:8081/")]
    [DataRow("AccountEndpoint=https://localhost:8081;Database=db;Container=mycontainer;", "https://localhost:8081/")]
    [DataRow("AccountEndpoint=https://localhost:8081;AccountKey=fake;Database=db", "https://localhost:8081/")]
    [DataRow("AccountEndpoint=https://localhost:8081;AccountKey=fake;Database=db;DisableServerCertificateValidation=True", "https://localhost:8081/")]
    [DataRow("AccountEndpoint=https://localhost:8081;AccountKey=fake;Database=db;Container=mycontainer", "https://localhost:8081/")]
    [DataRow("https://example1.documents.azure.com:443", "https://example1.documents.azure.com/")]
    public void AddAzureCosmosClient_EnsuresConnectionStringIsCorrect(string connectionString, string expectedEndpoint)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        PopulateConfiguration(builder.Configuration, connectionString);

        builder.AddAzureCosmosClient("cosmos");

        using var host = builder.Build();
        var client = host.Services.GetRequiredService<CosmosClient>();

        Assert.AreEqual(expectedEndpoint, client.Endpoint.ToString());
    }

    [TestMethod]
    public void AddAzureCosmosClient_FailsWithError()
    {
        var e = Assert.Throws<ArgumentException>(() =>
            AddAzureCosmosClient_EnsuresConnectionStringIsCorrect("this=isnt;a=valid;cosmos=connectionstring", string.Empty));

        Assert.Contains("missing", e.Message);
        Assert.Contains("AccountEndpoint", e.Message);
    }

    private static void PopulateConfiguration(ConfigurationManager configuration, string connectionString) =>
        configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:cosmos", connectionString)
        ]);
}
