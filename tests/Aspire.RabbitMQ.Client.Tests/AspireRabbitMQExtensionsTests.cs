// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using Aspire.Components.Common.Tests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;

namespace Aspire.RabbitMQ.Client.Tests;

[TestClass]
public class AspireRabbitMQExtensionsTests : IClassFixture<RabbitMQContainerFixture>
{
    private readonly RabbitMQContainerFixture _containerFixture;

    public AspireRabbitMQExtensionsTests(RabbitMQContainerFixture containerFixture)
    {
        _containerFixture = containerFixture;
    }

    [TestMethod]
    [RequiresDocker]
    [DataRow(true)]
    [DataRow(false)]
    public void ReadsFromConnectionStringsCorrectly(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:messaging", _containerFixture.GetConnectionString())
        ]);

        if (useKeyed)
        {
            builder.AddKeyedRabbitMQClient("messaging");
        }
        else
        {
            builder.AddRabbitMQClient("messaging");
        }

        using var host = builder.Build();
        var connection = useKeyed ?
            host.Services.GetRequiredKeyedService<IConnection>("messaging") :
            host.Services.GetRequiredService<IConnection>();

        AssertEquals(_containerFixture.GetConnectionString(), connection.Endpoint);
    }

    [TestMethod]
    [RequiresDocker]
    [DataRow(true)]
    [DataRow(false)]
    public void ConnectionStringCanBeSetInCode(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:messaging", "unused")
        ]);

        void SetConnectionString(RabbitMQClientSettings settings) => settings.ConnectionString = _containerFixture.GetConnectionString();
        if (useKeyed)
        {
            builder.AddKeyedRabbitMQClient("messaging", SetConnectionString);
        }
        else
        {
            builder.AddRabbitMQClient("messaging", SetConnectionString);
        }

        using var host = builder.Build();
        var connection = useKeyed ?
            host.Services.GetRequiredKeyedService<IConnection>("messaging") :
            host.Services.GetRequiredService<IConnection>();

        AssertEquals(_containerFixture.GetConnectionString(), connection.Endpoint);
    }

    [TestMethod]
    [RequiresDocker]
    [DataRow(true)]
    [DataRow(false)]
    public void ConnectionNameWinsOverConfigSection(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var key = useKeyed ? "messaging" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(ConformanceTests.CreateConfigKey("Aspire:RabbitMQ:Client", key, "ConnectionString"), "unused"),
            new KeyValuePair<string, string?>("ConnectionStrings:messaging", _containerFixture.GetConnectionString())
        ]);

        if (useKeyed)
        {
            builder.AddKeyedRabbitMQClient("messaging");
        }
        else
        {
            builder.AddRabbitMQClient("messaging");
        }

        using var host = builder.Build();
        var connection = useKeyed ?
            host.Services.GetRequiredKeyedService<IConnection>("messaging") :
            host.Services.GetRequiredService<IConnection>();

        AssertEquals(_containerFixture.GetConnectionString(), connection.Endpoint);
    }

    [TestMethod]
    public void ConnectionFactoryOptionsFromConfig()
    {
        static Stream CreateStreamFromString(string data) => new MemoryStream(Encoding.UTF8.GetBytes(data));

        using var jsonStream = CreateStreamFromString("""
        {
          "Aspire": {
            "RabbitMQ": {
              "Client": {
                "ConnectionFactory": {
                  "AmqpUriSslProtocols": "Tls12",
                  "AutomaticRecoveryEnabled": false,
                  "ConsumerDispatchConcurrency": 2,
                  "SocketReadTimeout": "00:00:03",
                  "Ssl": {
                    "AcceptablePolicyErrors": "RemoteCertificateNameMismatch",
                    "Enabled": true,
                    "Version": "Tls13"
                  },
                  "RequestedFrameMax": 304,
                  "ClientProvidedName": "aspire-app"
                }
              }
            }
          }
        }
        """);

        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Configuration.AddJsonStream(jsonStream);

        builder.AddRabbitMQClient("messaging");

        using var host = builder.Build();
        var connectionFactory = (ConnectionFactory)host.Services.GetRequiredService<IConnectionFactory>();

        Assert.AreEqual(SslProtocols.Tls12, connectionFactory.AmqpUriSslProtocols);
        Assert.IsFalse(connectionFactory.AutomaticRecoveryEnabled);
        Assert.AreEqual(2, connectionFactory.ConsumerDispatchConcurrency);
        Assert.AreEqual(SslPolicyErrors.RemoteCertificateNameMismatch, connectionFactory.Ssl.AcceptablePolicyErrors);
        Assert.IsTrue(connectionFactory.Ssl.Enabled);
        Assert.AreEqual(SslProtocols.Tls13, connectionFactory.Ssl.Version);
        Assert.AreEqual(TimeSpan.FromSeconds(3), connectionFactory.SocketReadTimeout);
        Assert.AreEqual((uint)304, connectionFactory.RequestedFrameMax);
        Assert.AreEqual("aspire-app", connectionFactory.ClientProvidedName);
    }

    [TestMethod]
    [RequiresDocker]
    public async Task CanAddMultipleKeyedServices()
    {
        await using var container2 = await RabbitMQContainerFixture.CreateContainerAsync();
        await using var container3 = await RabbitMQContainerFixture.CreateContainerAsync();

        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:messaging1", _containerFixture.GetConnectionString()),
            new KeyValuePair<string, string?>("ConnectionStrings:messaging2", container2.GetConnectionString()),
            new KeyValuePair<string, string?>("ConnectionStrings:messaging3", container3.GetConnectionString())
        ]);

        builder.AddRabbitMQClient("messaging1");
        builder.AddKeyedRabbitMQClient("messaging2");
        builder.AddKeyedRabbitMQClient("messaging3");

        using var host = builder.Build();

        var connection1 = host.Services.GetRequiredService<IConnection>();
        var connection2 = host.Services.GetRequiredKeyedService<IConnection>("messaging2");
        var connection3 = host.Services.GetRequiredKeyedService<IConnection>("messaging3");

        Assert.AreNotSame(connection1, connection2);
        Assert.AreNotSame(connection1, connection3);
        Assert.AreNotSame(connection2, connection3);

        AssertEquals(_containerFixture.GetConnectionString(), connection1.Endpoint);
        AssertEquals(container2.GetConnectionString(), connection2.Endpoint);
        AssertEquals(container3.GetConnectionString(), connection3.Endpoint);
    }

    private static void AssertEquals(string expectedUri, AmqpTcpEndpoint endpoint)
    {
        var uri = new Uri(expectedUri);
        Assert.AreEqual(uri.Host, endpoint.HostName);
        Assert.AreEqual(uri.Port, endpoint.Port);
    }
}
