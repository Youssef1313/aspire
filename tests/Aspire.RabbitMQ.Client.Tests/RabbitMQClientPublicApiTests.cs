// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;

namespace Aspire.RabbitMQ.Client.Tests;

[TestClass]
public class RabbitMQClientPublicApiTests
{
    [TestMethod]
    public void AddRabbitMQClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string connectionName = "rabbitmq";

        var action = () => builder.AddRabbitMQClient(connectionName);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddRabbitMQClientShouldThrowWhenConnectionNameIsNullOrEmpty(bool isNull)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var connectionName = isNull ? null! : string.Empty;

        var action = () => builder.AddRabbitMQClient(connectionName);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(connectionName), exception.ParamName);
    }

    [TestMethod]
    public void AddKeyedRabbitMQClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string name = "rabbitmq";

        var action = () => builder.AddKeyedRabbitMQClient(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddKeyedRabbitMQClientShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        IHostApplicationBuilder builder = new HostApplicationBuilder();
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddKeyedRabbitMQClient(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }
}
