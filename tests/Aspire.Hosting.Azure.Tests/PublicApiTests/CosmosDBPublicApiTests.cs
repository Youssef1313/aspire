// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Tests.PublicApiTests;

[TestClass]
public class CosmosDBPublicApiTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CtorAzureCosmosDBContainerResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var resource = builder.AddAzureCosmosDB("cosmos");
        var name = isNull ? null! : string.Empty;
        const string containerName = "db";
        const string partitionKeyPath = "data";
        var parent = new AzureCosmosDBDatabaseResource("database", "cosmos-db", resource.Resource);

        var action = () => new AzureCosmosDBContainerResource(name, containerName, partitionKeyPath, parent);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CtorAzureCosmosDBContainerResourceShouldThrowWhenContainerNameIsNullOrEmpty(bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var resource = builder.AddAzureCosmosDB("cosmos");
        const string name = "cosmos";
        var containerName = isNull ? null! : string.Empty;
        const string partitionKeyPath = "data";
        var parent = new AzureCosmosDBDatabaseResource("database", "cosmos-db", resource.Resource);

        var action = () => new AzureCosmosDBContainerResource(name, containerName, partitionKeyPath, parent);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(containerName), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CtorAzureCosmosDBContainerResourceShouldThrowWhenPartitionKeyPathIsNullOrEmpty(bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var resource = builder.AddAzureCosmosDB("cosmos");
        const string name = "cosmos";
        const string containerName = "db";
        var partitionKeyPath = isNull ? null! : string.Empty;
        var parent = new AzureCosmosDBDatabaseResource("database", "cosmos-db", resource.Resource);

        var action = () => new AzureCosmosDBContainerResource(name, containerName, partitionKeyPath, parent);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(partitionKeyPath), exception.ParamName);
    }

    [TestMethod]
    public void CtorAzureCosmosDBContainerResourceShouldThrowWhenParentIsNull()
    {
        const string name = "cosmos";
        const string containerName = "db";
        const string partitionKeyPath = "data";
        AzureCosmosDBDatabaseResource parent = null!;

        var action = () => new AzureCosmosDBContainerResource(name, containerName, partitionKeyPath, parent);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(parent), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CtorAzureCosmosDBDatabaseResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var parent = builder.AddAzureCosmosDB("cosmos");
        var name = isNull ? null! : string.Empty;
        const string databaseName = "database";

        var action = () => new AzureCosmosDBDatabaseResource(name, databaseName, parent.Resource);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CtorAzureCosmosDBDatabaseResourceShouldThrowWhenDatabaseNameIsNullOrEmpty(bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var parent = builder.AddAzureCosmosDB("cosmos");
        const string name = "cosmos";
        var databaseName = isNull ? null! : string.Empty;

        var action = () => new AzureCosmosDBDatabaseResource(name, databaseName, parent.Resource);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(databaseName), exception.ParamName);
    }

    [TestMethod]
    public void CtorAzureCosmosDBDatabaseResourceShouldThrowWhenParentIsNull()
    {
        const string name = "cosmos";
        const string databaseName = "database";
        AzureCosmosDBResource parent = null!;

        var action = () => new AzureCosmosDBDatabaseResource(name, databaseName, parent);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(parent), exception.ParamName);
    }

    [TestMethod]
    public void CtorAzureCosmosDBEmulatorResourceShouldThrowWhenInnerResourceIsNull()
    {
        AzureCosmosDBResource innerResource = null!;

        var action = () => new AzureCosmosDBEmulatorResource(innerResource);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(innerResource), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CtorAzureCosmosDBResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var name = isNull ? null! : string.Empty;
        var configureInfrastructure = (AzureResourceInfrastructure _) => { };

        var action = () => new AzureCosmosDBResource(name, configureInfrastructure);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void CtorAzureCosmosDBResourceShouldThrowWhenConfigureInfrastructureIsNull()
    {
        const string name = "cosmos";
        Action<AzureResourceInfrastructure> configureInfrastructure = null!;

        var action = () => new AzureCosmosDBResource(name, configureInfrastructure);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(configureInfrastructure), exception.ParamName);
    }

    [TestMethod]
    public void AddAzureCosmosDBShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "cosmos";

        var action = () => builder.AddAzureCosmosDB(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AddAzureCosmosDBShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddAzureCosmosDB(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void RunAsEmulatorShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<AzureCosmosDBResource> builder = null!;
        Action<IResourceBuilder<AzureCosmosDBEmulatorResource>>? configureContainer = null;

        var action = () => builder.RunAsEmulator(configureContainer);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [Experimental("ASPIRECOSMOSDB001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public void RunAsPreviewEmulatorShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<AzureCosmosDBResource> builder = null!;
        Action<IResourceBuilder<AzureCosmosDBEmulatorResource>>? configureContainer = null;

        var action = () => builder.RunAsPreviewEmulator(configureContainer);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void WithDataVolumeShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<AzureCosmosDBEmulatorResource> builder = null!;

        var action = () => builder.WithDataVolume();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void WithGatewayPortShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<AzureCosmosDBEmulatorResource> builder = null!;
        int? port = null;

        var action = () => builder.WithGatewayPort(port);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void WithPartitionCountShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<AzureCosmosDBEmulatorResource> builder = null!;
        const int count = 1;

        var action = () => builder.WithPartitionCount(count);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [Obsolete($"This method is obsolete because it has the wrong return type and will be removed in a future version. Use AddCosmosDatabase instead to add a Cosmos DB database.")]
    public void AddDatabaseShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<AzureCosmosDBResource> builder = null!;
        const string databaseName = "cosmos-db";

        var action = () => builder.AddDatabase(databaseName);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    [Obsolete($"This method is obsolete because it has the wrong return type and will be removed in a future version. Use AddCosmosDatabase instead to add a Cosmos DB database.")]
    public void AddDatabaseShouldThrowWhenDatabaseNameIsNullOrEmpty(bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var cosmos = builder.AddAzureCosmosDB("cosmos");
        var databaseName = isNull ? null! : string.Empty;

        var action = () => cosmos.AddDatabase(databaseName);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(databaseName), exception.ParamName);
    }

    [TestMethod]
    public void AddCosmosDatabaseShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<AzureCosmosDBResource> builder = null!;
        const string name = "cosmos";

        var action = () => builder.AddCosmosDatabase(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AddCosmosDatabaseShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var cosmos = builder.AddAzureCosmosDB("cosmos");
        var name = isNull ? null! : string.Empty;

        var action = () => cosmos.AddCosmosDatabase(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void AddContainerShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<AzureCosmosDBDatabaseResource> builder = null!;
        const string name = "cosmos";
        const string partitionKeyPath = "data";

        var action = () => builder.AddContainer(name, partitionKeyPath);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AddContainerShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var cosmos = builder.AddAzureCosmosDB("cosmos")
            .AddCosmosDatabase("cosmos-db");
        var name = isNull ? null! : string.Empty;
        const string partitionKeyPath = "data";

        var action = () => cosmos.AddContainer(name, partitionKeyPath);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AddContainerShouldThrowWhenPartitionKeyPathIsNullOrEmpty(bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var cosmos = builder.AddAzureCosmosDB("cosmos")
            .AddCosmosDatabase("cosmos-db");
        const string name = "cosmos";
        var partitionKeyPath = isNull ? null! : string.Empty;

        var action = () => cosmos.AddContainer(name, partitionKeyPath);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(partitionKeyPath), exception.ParamName);
    }

    [TestMethod]
    [Experimental("ASPIRECOSMOSDB001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public void WithDataExplorerShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<AzureCosmosDBEmulatorResource> builder = null!;

        var action = () => builder.WithDataExplorer();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void WithAccessKeyAuthenticationShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<AzureCosmosDBResource> builder = null!;

        var action = () =>
        {
            builder.WithAccessKeyAuthentication();
        };

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }
}
