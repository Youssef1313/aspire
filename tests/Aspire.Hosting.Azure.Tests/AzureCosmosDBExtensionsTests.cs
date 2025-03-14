// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Tests;

[TestClass]
public class AzureCosmosDBExtensionsTests
{
    [TestMethod]
    [DataRow(null)]
    [DataRow(8081)]
    [DataRow(9007)]
    public void AddAzureCosmosDBWithEmulatorGetsExpectedPort(int? port = null)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var cosmos = builder.AddAzureCosmosDB("cosmos");

        cosmos.RunAsEmulator(container =>
        {
            container.WithGatewayPort(port);
        });

        var endpointAnnotation = cosmos.Resource.Annotations.OfType<EndpointAnnotation>().FirstOrDefault();
        Assert.IsNotNull(endpointAnnotation);

        var actualPort = endpointAnnotation.Port;
        Assert.AreEqual(port, actualPort);
    }

    [TestMethod]
    [DataRow("2.3.97-preview")]
    [DataRow("1.0.7")]
    public void AddAzureCosmosDBWithEmulatorGetsExpectedImageTag(string imageTag)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var cosmos = builder.AddAzureCosmosDB("cosmos");

        cosmos.RunAsEmulator(container =>
        {
            container.WithImageTag(imageTag);
        });

        var containerImageAnnotation = cosmos.Resource.Annotations.OfType<ContainerImageAnnotation>().FirstOrDefault();
        Assert.IsNotNull(containerImageAnnotation);

        var actualTag = containerImageAnnotation.Tag;
        Assert.AreEqual(imageTag ?? "latest", actualTag);
    }

    [TestMethod]
    [DataRow(30)]
    [DataRow(12)]
    public async Task AddAzureCosmosDBWithPartitionCountCanOverrideNumberOfPartitions(int partitionCount)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var cosmos = builder.AddAzureCosmosDB("cosmos");

        cosmos.RunAsEmulator(r => r.WithPartitionCount(partitionCount));
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(cosmos.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.AreEqual(partitionCount.ToString(CultureInfo.InvariantCulture), config["AZURE_COSMOS_EMULATOR_PARTITION_COUNT"]);
    }

    [TestMethod]
    public void AddAzureCosmosDBWithDataExplorer()
    {
#pragma warning disable ASPIRECOSMOSDB001 // RunAsPreviewEmulator is experimental
        using var builder = TestDistributedApplicationBuilder.Create();

        var cosmos = builder.AddAzureCosmosDB("cosmos");
        cosmos.RunAsPreviewEmulator(e => e.WithDataExplorer());

        var endpoint = cosmos.GetEndpoint("data-explorer");
        Assert.IsNotNull(endpoint);
        Assert.AreEqual(1234, endpoint.TargetPort);

        // WithDataExplorer doesn't work against the non-preview emulator
        var cosmos2 = builder.AddAzureCosmosDB("cosmos2");
        Assert.Throws<NotSupportedException>(() => cosmos2.RunAsEmulator(e => e.WithDataExplorer()));
#pragma warning restore ASPIRECOSMOSDB001 // RunAsPreviewEmulator is experimental
    }

    [TestMethod]
    public void AzureCosmosDBHasCorrectConnectionStrings()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var cosmos = builder.AddAzureCosmosDB("cosmos");
        var db1 = cosmos.AddCosmosDatabase("db1");
        var container1 = db1.AddContainer("container1", "id");

        // database and container should have the same connection string as the cosmos account, for now.
        // In the future, we can add the database and container info to the connection string.
        Assert.AreEqual("{cosmos.outputs.connectionString}", cosmos.Resource.ConnectionStringExpression.ValueExpression);
        Assert.AreEqual("{cosmos.outputs.connectionString}", db1.Resource.ConnectionStringExpression.ValueExpression);
        Assert.AreEqual("{cosmos.outputs.connectionString}", container1.Resource.ConnectionStringExpression.ValueExpression);
    }

    [TestMethod]
    public void AzureCosmosDBAppliesAzureFunctionsConfiguration()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var cosmos = builder.AddAzureCosmosDB("cosmos");
        var db1 = cosmos.AddCosmosDatabase("db1");
        var container1 = db1.AddContainer("container1", "id");

        var target = new Dictionary<string, object>();
        ((IResourceWithAzureFunctionsConfig)cosmos.Resource).ApplyAzureFunctionsConfiguration(target, "cosmos");
        Assert.That.Collection(target.Keys.OrderBy(k => k),
            k => Assert.AreEqual("Aspire__Microsoft__Azure__Cosmos__cosmos__AccountEndpoint", k),
            k => Assert.AreEqual("Aspire__Microsoft__EntityFrameworkCore__Cosmos__cosmos__AccountEndpoint", k),
            k => Assert.AreEqual("cosmos__accountEndpoint", k));

        target.Clear();
        ((IResourceWithAzureFunctionsConfig)db1.Resource).ApplyAzureFunctionsConfiguration(target, "db1");
        Assert.That.Collection(target.Keys.OrderBy(k => k),
            k => Assert.AreEqual("Aspire__Microsoft__Azure__Cosmos__db1__AccountEndpoint", k),
            k => Assert.AreEqual("Aspire__Microsoft__EntityFrameworkCore__Cosmos__db1__AccountEndpoint", k),
            k => Assert.AreEqual("db1__accountEndpoint", k));

        target.Clear();
        ((IResourceWithAzureFunctionsConfig)container1.Resource).ApplyAzureFunctionsConfiguration(target, "container1");
        Assert.That.Collection(target.Keys.OrderBy(k => k),
            k => Assert.AreEqual("Aspire__Microsoft__Azure__Cosmos__container1__AccountEndpoint", k),
            k => Assert.AreEqual("Aspire__Microsoft__EntityFrameworkCore__Cosmos__container1__AccountEndpoint", k),
            k => Assert.AreEqual("container1__accountEndpoint", k));
    }
}
