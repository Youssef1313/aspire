// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Tests.PublicApiTests;

[TestClass]
public class EventHubsPublicApiTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CtorAzureEventHubConsumerGroupResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var resource = builder.AddAzureEventHubs("event-hubs");
        var name = isNull ? null! : string.Empty;
        const string consumerGroupName = "group";
        var parent = new AzureEventHubResource("hub", "event-hub", resource.Resource);

        var action = () => new AzureEventHubConsumerGroupResource(name, consumerGroupName, parent);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CtorAzureEventHubConsumerGroupResourceShouldThrowWhenConsumerGroupNameIsNullOrEmpty(bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var resource = builder.AddAzureEventHubs("event-hubs");
        const string name = "consumer";
        var consumerGroupName = isNull ? null! : string.Empty;
        var parent = new AzureEventHubResource("hub", "event-hub", resource.Resource);

        var action = () => new AzureEventHubConsumerGroupResource(name, consumerGroupName, parent);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(consumerGroupName), exception.ParamName);
    }

    [TestMethod]
    public void CtorAzureEventHubConsumerGroupResourceShouldThrowWhenParentIsNullOrEmpty()
    {
        const string name = "consumer";
        const string consumerGroupName = "group";
        AzureEventHubResource parent = null!;

        var action = () => new AzureEventHubConsumerGroupResource(name, consumerGroupName, parent);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(parent), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CtorAzureEventHubResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var name = isNull ? null! : string.Empty;
        const string hubName = "event-hub";
        var parent = new AzureEventHubsResource("hub", (configureInfrastructure) => { });

        var action = () => new AzureEventHubResource(name, hubName, parent);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CtorAzureEventHubResourceShouldThrowWhenHubNameIsNullOrEmpty(bool isNull)
    {
        const string name = "hub";
        var hubName = isNull ? null! : string.Empty;
        var parent = new AzureEventHubsResource("event-hubs", (configureInfrastructure) => { });

        var action = () => new AzureEventHubResource(name, hubName, parent);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(hubName), exception.ParamName);
    }

    [TestMethod]
    public void CtorAzureEventHubResourceShouldThrowWhenParentIsNullOrEmpty()
    {
        const string name = "hub";
        const string hubName = "event-hub";
        AzureEventHubsResource parent = null!;

        var action = () => new AzureEventHubResource(name, hubName, parent);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(parent), exception.ParamName);
    }

    [TestMethod]
    public void CtorAzureEventHubsEmulatorResourceShouldThrowWhenInnerResourceIsNullOrEmpty()
    {
        AzureEventHubsResource innerResource = null!;

        var action = () => new AzureEventHubsEmulatorResource(innerResource);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(innerResource), exception.ParamName);
    }

    [TestMethod]
    public void AddAzureEventHubsShouldThrowWhenBuilderIsNullOrEmpty()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "event-hubs";

        var action = () => builder.AddAzureEventHubs(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AddAzureEventHubsShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddAzureEventHubs(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    [Obsolete($"This method is obsolete because it has the wrong return type and will be removed in a future version. Use AddHub instead to add an Azure Event Hub.")]
    public void AddEventHubShouldThrowWhenBuilderIsNullOrEmpty()
    {
        IResourceBuilder<AzureEventHubsResource> builder = null!;
        const string name = "event-hubs";

        var action = () => builder.AddEventHub(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    [Obsolete($"This method is obsolete because it has the wrong return type and will be removed in a future version. Use AddHub instead to add an Azure Event Hub.")]
    public void AddEventHubShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        using var hostBuilder = TestDistributedApplicationBuilder.Create();
        var builder = hostBuilder.AddAzureEventHubs("event-hubs");
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddEventHub(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void AddHubShouldThrowWhenBuilderIsNullOrEmpty()
    {
        IResourceBuilder<AzureEventHubsResource> builder = null!;
        const string name = "event-hubs";

        var action = () => builder.AddHub(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AddHubShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        using var hostBuilder = TestDistributedApplicationBuilder.Create();
        var builder = hostBuilder.AddAzureEventHubs("event-hubs");
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddHub(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void WithPropertiesShouldThrowWhenBuilderIsNullOrEmpty()
    {
        IResourceBuilder<AzureEventHubResource> builder = null!;
        Action<AzureEventHubResource> configure = (c) => { };

        var action = () => builder.WithProperties(configure);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void WithPropertiesShouldThrowWhenConfigureIsNullOrEmpty()
    {
        using var hostBuilder = TestDistributedApplicationBuilder.Create();
        var builder = hostBuilder.AddAzureEventHubs("event-hubs").AddHub("hub");
        Action<AzureEventHubResource> configure = null!;

        var action = () => builder.WithProperties(configure);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(configure), exception.ParamName);
    }

    [TestMethod]
    public void AddConsumerGroupShouldThrowWhenBuilderIsNullOrEmpty()
    {
        IResourceBuilder<AzureEventHubResource> builder = null!;
        const string name = "consumer";

        var action = () => builder.AddConsumerGroup(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AddConsumerGroupShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        using var hostBuilder = TestDistributedApplicationBuilder.Create();
        var builder = hostBuilder.AddAzureEventHubs("event-hubs").AddHub("hub");
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddConsumerGroup(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void RunAsEmulatorShouldThrowWhenBuilderIsNullOrEmpty()
    {
        IResourceBuilder<AzureEventHubsResource> builder = null!;

        var action = () => builder.RunAsEmulator();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void WithDataBindMountShouldThrowWhenBuilderIsNullOrEmpty()
    {
        IResourceBuilder<AzureEventHubsEmulatorResource> builder = null!;

        var action = () => builder.WithDataBindMount();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void WithDataVolumeShouldThrowWhenBuilderIsNullOrEmpty()
    {
        IResourceBuilder<AzureEventHubsEmulatorResource> builder = null!;

        var action = () => builder.WithDataVolume();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [Obsolete("Use WithHostPort instead.")]
    public void WithGatewayPortShouldThrowWhenBuilderIsNullOrEmpty()
    {
        IResourceBuilder<AzureEventHubsEmulatorResource> builder = null!;
        int? port = null;

        var action = () => builder.WithGatewayPort(port);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void WithHostPortShouldThrowWhenBuilderIsNullOrEmpty()
    {
        IResourceBuilder<AzureEventHubsEmulatorResource> builder = null!;
        int? port = null;

        var action = () => builder.WithHostPort(port);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void WithConfigurationFileShouldThrowWhenBuilderIsNullOrEmpty()
    {
        IResourceBuilder<AzureEventHubsEmulatorResource> builder = null!;
        const string path = "/Eventhubs_Emulator/ConfigFiles/Config.json";

        var action = () => builder.WithConfigurationFile(path);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void WithConfigurationFileShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        using var hostBuilder = TestDistributedApplicationBuilder.Create();
        var builder = hostBuilder.AddAzureEventHubs("event-hubs");

        var path = isNull ? null! : string.Empty;

        var action = () => builder.RunAsEmulator(configure => configure.WithConfigurationFile(path));

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(path), exception.ParamName);
    }

    [TestMethod]
    public void WithConfigurationShouldThrowWhenBuilderIsNullOrEmpty()
    {
        IResourceBuilder<AzureEventHubsEmulatorResource> builder = null!;
        Action<JsonNode> configJson = (_) => { };

        var action = () => builder.WithConfiguration(configJson);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void WithConfigurationShouldThrowWhenConfigJsonIsNullOrEmpty()
    {
        using var hostBuilder = TestDistributedApplicationBuilder.Create();
        var builder = hostBuilder.AddAzureEventHubs("event-hubs");
        Action<JsonNode> configJson = null!;

        var action = () => builder.RunAsEmulator(configure => configure.WithConfiguration(configJson));

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(configJson), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CtorAzureEventHubsResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var name = isNull ? null! : string.Empty;
        Action<AzureResourceInfrastructure> configureInfrastructure = (_) => { };

        var action = () => new AzureEventHubsResource(name, configureInfrastructure);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void CtorAzureEventHubsResourceShouldThrowWhenConfigureInfrastructureIsNullOrEmpty()
    {
        const string name = "hub";
        Action<AzureResourceInfrastructure> configureInfrastructure = null!;

        var action = () => new AzureEventHubsResource(name, configureInfrastructure);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(configureInfrastructure), exception.ParamName);
    }
}
