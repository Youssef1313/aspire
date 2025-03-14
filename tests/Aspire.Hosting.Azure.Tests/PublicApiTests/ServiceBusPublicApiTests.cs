// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Tests.PublicApiTests;

[TestClass]
public class ServiceBusPublicApiTests
{
    [TestMethod]
    public void CtorAzureServiceBusEmulatorResourceShouldThrowWhenInnerResourceIsNull()
    {
        AzureServiceBusResource innerResource = null!;

        var action = () => new AzureServiceBusEmulatorResource(innerResource);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(innerResource), exception.ParamName);
    }

    [TestMethod]
    public void AddAzureServiceBusShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "service-bus";

        var action = () => builder.AddAzureServiceBus(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AddAzureServiceBusShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddAzureServiceBus(name);

        var exception = isNull
           ? Assert.Throws<ArgumentNullException>(action)
           : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    [Obsolete($"This method is obsolete because it has the wrong return type and will be removed in a future version. Use AddServiceBusQueue instead to add an Azure Service Bus Queue.")]
    public void AddQueueShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<AzureServiceBusResource> builder = null!;
        const string name = "service-queue";

        var action = () => builder.AddQueue(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    [Obsolete($"This method is obsolete because it has the wrong return type and will be removed in a future version. Use AddServiceBusQueue instead to add an Azure Service Bus Queue.")]
    public void AddQueueShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        using var testBuilder = TestDistributedApplicationBuilder.Create();
        var builder = testBuilder.AddAzureServiceBus("service-bus");
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddQueue(name);

        var exception = isNull
           ? Assert.Throws<ArgumentNullException>(action)
           : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void AddServiceBusQueueShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<AzureServiceBusResource> builder = null!;
        const string name = "service-queue";

        var action = () => builder.AddServiceBusQueue(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AddServiceBusQueueShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var testBuilder = TestDistributedApplicationBuilder.Create();
        var builder = testBuilder.AddAzureServiceBus("service-bus");
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddServiceBusQueue(name);

        var exception = isNull
           ? Assert.Throws<ArgumentNullException>(action)
           : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void WithPropertiesShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<AzureServiceBusQueueResource> builder = null!;
        Action<AzureServiceBusQueueResource> configure = (_) => { };

        var action = () => builder.WithProperties(configure);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void WithPropertiesShouldThrowWhenConfigureIsNull()
    {
        using var testBuilder = TestDistributedApplicationBuilder.Create();
        var builder = testBuilder.AddAzureServiceBus("service-bus").AddServiceBusQueue("service-queue");
        Action<AzureServiceBusQueueResource> configure = null!;

        var action = () => builder.WithProperties(configure);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(configure), exception.ParamName);
    }

    [TestMethod]
    public void AddServiceBusTopicShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<AzureServiceBusResource> builder = null!;
        const string name = "topic";

        var action = () => builder.AddServiceBusTopic(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AddServiceBusTopicShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var testBuilder = TestDistributedApplicationBuilder.Create();
        var builder = testBuilder.AddAzureServiceBus("service-bus");
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddServiceBusTopic(name);

        var exception = isNull
           ? Assert.Throws<ArgumentNullException>(action)
           : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void WithPropertiesTopicShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<AzureServiceBusTopicResource> builder = null!;
        Action<AzureServiceBusTopicResource> configure = (_) => { };

        var action = () => builder.WithProperties(configure);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void WithPropertiesTopicShouldThrowWhenConfigureIsNull()
    {
        using var testBuilder = TestDistributedApplicationBuilder.Create();
        var builder = testBuilder.AddAzureServiceBus("service-bus").AddServiceBusTopic("service-topic");
        Action<AzureServiceBusTopicResource> configure = null!;

        var action = () => builder.WithProperties(configure);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(configure), exception.ParamName);
    }

    [TestMethod]
    public void AddServiceBusSubscriptionShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<AzureServiceBusTopicResource> builder = null!;
        const string name = "topic";

        var action = () => builder.AddServiceBusSubscription(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AddServiceBusSubscriptionShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var testBuilder = TestDistributedApplicationBuilder.Create();
        var builder = testBuilder.AddAzureServiceBus("service-bus")
            .AddServiceBusTopic("service-topic");
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddServiceBusSubscription(name);

        var exception = isNull
           ? Assert.Throws<ArgumentNullException>(action)
           : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void WithPropertiesSubscriptionShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<AzureServiceBusSubscriptionResource> builder = null!;
        Action<AzureServiceBusSubscriptionResource> configure = (_) => { };

        var action = () => builder.WithProperties(configure);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void WithPropertiesSubscriptionShouldThrowWhenConfigureIsNull()
    {
        using var testBuilder = TestDistributedApplicationBuilder.Create();
        var builder = testBuilder.AddAzureServiceBus("service-bus")
            .AddServiceBusTopic("service-topic")
            .AddServiceBusSubscription("service-subscription");
        Action<AzureServiceBusSubscriptionResource> configure = null!;

        var action = () => builder.WithProperties(configure);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(configure), exception.ParamName);
    }

    [TestMethod]
    public void RunAsEmulatorShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<AzureServiceBusResource> builder = null!;

        var action = () => builder.RunAsEmulator();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void WithConfigurationFileShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<AzureServiceBusEmulatorResource> builder = null!;
        const string path = "/ServiceBus_Emulator/ConfigFiles/Config.json";

        var action = () => builder.WithConfigurationFile(path);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void WithConfigurationFileShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        using var testBuilder = TestDistributedApplicationBuilder.Create();
        var builder = testBuilder.AddAzureServiceBus("service-bus");
        var path = isNull ? null! : string.Empty;

        var action = () => builder.RunAsEmulator(configure => configure.WithConfigurationFile(path));

        var exception = isNull
           ? Assert.Throws<ArgumentNullException>(action)
           : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(path), exception.ParamName);
    }

    [TestMethod]
    public void WithConfigurationShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<AzureServiceBusEmulatorResource> builder = null!;
        Action<JsonNode> configJson = (_) => { };

        var action = () => builder.WithConfiguration(configJson);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void WithConfigurationShouldThrowWhenConfigJsonIsNull()
    {
        using var testBuilder = TestDistributedApplicationBuilder.Create();
        var builder = testBuilder.AddAzureServiceBus("service-bus");
        Action<JsonNode> configJson = null!;

        var action = () => builder.RunAsEmulator(configure => configure.WithConfiguration(configJson));

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(configJson), exception.ParamName);
    }

    [TestMethod]
    public void WithHostPortShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<AzureServiceBusEmulatorResource> builder = null!;
        int? port = null;

        var action = () => builder.WithHostPort(port);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }
}
