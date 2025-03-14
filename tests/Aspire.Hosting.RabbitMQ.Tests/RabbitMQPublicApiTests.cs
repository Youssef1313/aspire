// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.RabbitMQ.Tests;

[TestClass]
public class RabbitMQPublicApiTests
{
    [TestMethod]
    public void AddRabbitMQShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "rabbitMQ";

        Action action = () => builder.AddRabbitMQ(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddRabbitMQShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;

        Action action = () => builder.AddRabbitMQ(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void WithDataVolumeShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<RabbitMQServerResource> builder = null!;

        Action action = () => builder.WithDataVolume();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void WithDataBindMountShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<RabbitMQServerResource> builder = null!;
        const string source = "/var/lib/rabbitmq";

        Action action = () => builder.WithDataBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void WithDataBindMountShouldThrowWhenSourceIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var rabbitMQ = builder.AddRabbitMQ("rabbitMQ");
        var source = isNull ? null! : string.Empty;

        Action action = () => rabbitMQ.WithDataBindMount(source);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(source), exception.ParamName);
    }

    [TestMethod]
    public void WithManagementPluginShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<RabbitMQServerResource> builder = null!;

        Action action = () => builder.WithManagementPlugin();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void WithManagementPluginWithPortShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<RabbitMQServerResource> builder = null!;
        const int port = 15672;

        Action action = () => builder.WithManagementPlugin(port);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void CtorRabbitMQServerResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;
        const string passwordValue = nameof(passwordValue);
        ParameterResource? userName = null;
        var password = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, passwordValue);

        Action action = () => new RabbitMQServerResource(name: name, userName: userName, password: password);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void CtorRabbitMQServerResourceShouldThrowWhenPasswordIsNull()
    {
        string name = "rabbitMQ";
        ParameterResource? userName = null;
        ParameterResource password = null!;

        Action action = () => new RabbitMQServerResource(name: name, userName: userName, password: password);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(password), exception.ParamName);
    }
}
