// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Confluent.Kafka.Tests;

[TestClass]
public class ProducerConfigurationTests
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ReadsFromConnectionStringsCorrectly(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:messaging", CommonHelpers.TestingEndpoint)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedKafkaProducer<string, string>("messaging");
        }
        else
        {
            builder.AddKafkaProducer<string, string>("messaging");
        }

        using var host = builder.Build();
        var connectionFactory = useKeyed ?
            host.Services.GetRequiredKeyedService(ReflectionHelpers.ProducerConnectionFactoryStringKeyStringValueType.Value, "messaging") :
            host.Services.GetRequiredService(ReflectionHelpers.ProducerConnectionFactoryStringKeyStringValueType.Value);

        var config = GetProducerConfig(connectionFactory)!;

        Assert.AreEqual(CommonHelpers.TestingEndpoint, config.BootstrapServers);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ConnectionStringCanBeSetInCode(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:messaging", "unused")
        ]);

        static void SetConnectionString(KafkaProducerSettings settings) => settings.ConnectionString = CommonHelpers.TestingEndpoint;
        if (useKeyed)
        {
            builder.AddKeyedKafkaProducer<string, string>("messaging", configureSettings: SetConnectionString);
        }
        else
        {
            builder.AddKafkaProducer<string, string>("messaging", configureSettings: SetConnectionString);
        }

        using var host = builder.Build();
        var connectionFactory = useKeyed ?
            host.Services.GetRequiredKeyedService(ReflectionHelpers.ProducerConnectionFactoryStringKeyStringValueType.Value, "messaging") :
            host.Services.GetRequiredService(ReflectionHelpers.ProducerConnectionFactoryStringKeyStringValueType.Value);

        var config = GetProducerConfig(connectionFactory)!;

        Assert.AreEqual(CommonHelpers.TestingEndpoint, config.BootstrapServers);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ConnectionNameWinsOverConfigSection(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var key = useKeyed ? "messaging" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(ProducerConformanceTests.CreateConfigKey("Aspire:Confluent:Kafka:Producer", key, "ConnectionString"), "unused"),
            new KeyValuePair<string, string?>("ConnectionStrings:messaging", CommonHelpers.TestingEndpoint)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedKafkaProducer<string, string>("messaging");
        }
        else
        {
            builder.AddKafkaProducer<string, string>("messaging");
        }

        using var host = builder.Build();
        var connectionFactory = useKeyed ?
            host.Services.GetRequiredKeyedService(ReflectionHelpers.ProducerConnectionFactoryStringKeyStringValueType.Value, "messaging") :
            host.Services.GetRequiredService(ReflectionHelpers.ProducerConnectionFactoryStringKeyStringValueType.Value);

        var config = GetProducerConfig(connectionFactory)!;

        Assert.AreEqual(CommonHelpers.TestingEndpoint, config.BootstrapServers);
    }

    [TestMethod]
    [DataRow(true, true, false, false)]
    [DataRow(true, true, true, false)]
    [DataRow(true, true, false, true)]
    [DataRow(true, false, false, false)]
    [DataRow(true, false, true, false)]
    [DataRow(true, false, false, true)]
    [DataRow(false, true, false, false)]
    [DataRow(false, true, true, false)]
    [DataRow(false, true, false, true)]
    [DataRow(false, false, false, false)]
    [DataRow(false, false, true, false)]
    [DataRow(false, false, false, true)]
    public void ConfigureProducerBuilder(bool useKeyed, bool useConfigureSettings, bool useConfigureBuilder, bool useConfigureBuilderWithServiceProvider)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:messaging", CommonHelpers.TestingEndpoint)
        ]);

        bool configureBuilderIsCalled = false, configureSettingsIsCalled = false;

        Action act =
            (useKeyed, useConfigureSettings, useConfigureBuilder, useConfigureBuilderWithServiceProvider) switch
            {
                (true, false, false, false) => () =>
                    builder.AddKeyedKafkaProducer<string, string>("messaging"),
                (false, false, false, false) => () =>
                    builder.AddKafkaProducer<string, string>("messaging"),

                // only configureSettings
                (true, true, false, false) => () => builder.AddKeyedKafkaProducer<string, string>("messaging",
                    configureSettings: ConfigureSettings),
                (false, true, false, false) => () => builder.AddKafkaProducer<string, string>("messaging",
                    configureSettings: ConfigureSettings),

                // only configureBuilder
                (true, false, true, false) => () => builder.AddKeyedKafkaProducer<string, string>("messaging",
                    configureBuilder: ConfigureBuilder),
                (true, false, false, true) => () => builder.AddKeyedKafkaProducer<string, string>("messaging",
                    configureBuilder: ConfigureBuilderWithServiceProvider),
                (false, false, true, false) => () => builder.AddKafkaProducer<string, string>("messaging",
                    configureBuilder: ConfigureBuilder),
                (false, false, false, true) => () => builder.AddKafkaProducer<string, string>("messaging",
                    configureBuilder: ConfigureBuilderWithServiceProvider),

                // both configureSettings, and configureBuilder
                (true, true, true, false) => () => builder.AddKeyedKafkaProducer<string, string>("messaging",
                    configureSettings: ConfigureSettings,
                    configureBuilder: ConfigureBuilder),
                (false, true, true, false) => () => builder.AddKafkaProducer<string, string>("messaging",
                    configureSettings: ConfigureSettings,
                    configureBuilder: ConfigureBuilder),

                (true, true, false, true) => () => builder.AddKeyedKafkaProducer<string, string>("messaging",
                    configureSettings: ConfigureSettings,
                    configureBuilder: ConfigureBuilderWithServiceProvider),
                (false, true, false, true) => () => builder.AddKafkaProducer<string, string>("messaging",
                    configureSettings: ConfigureSettings,
                    configureBuilder: ConfigureBuilderWithServiceProvider),

                _ => throw new InvalidOperationException()
            };

        act();

        using var host = builder.Build();
        var connectionFactory = useKeyed ?
            host.Services.GetRequiredKeyedService(ReflectionHelpers.ProducerConnectionFactoryStringKeyStringValueType.Value, "messaging") :
            host.Services.GetRequiredService(ReflectionHelpers.ProducerConnectionFactoryStringKeyStringValueType.Value);

        var config = GetProducerConfig(connectionFactory)!;

        if (useConfigureBuilder || useConfigureBuilderWithServiceProvider)
        {
            Assert.IsTrue(configureBuilderIsCalled);
        }

        if (useConfigureSettings)
        {
            Assert.IsTrue(configureSettingsIsCalled);
        }

        Assert.AreEqual(CommonHelpers.TestingEndpoint, config.BootstrapServers);
        return;

        void ConfigureBuilder(ProducerBuilder<string, string> _)
        {
            configureBuilderIsCalled = true;
        }

        void ConfigureBuilderWithServiceProvider(IServiceProvider provider, ProducerBuilder<string, string> _)
        {
            var __ = provider.GetRequiredService<IConfiguration>();
            configureBuilderIsCalled = true;
        }

        void ConfigureSettings(KafkaProducerSettings _)
        {
            configureSettingsIsCalled = true;
        }
    }

    [TestMethod]
    public void ProducerConfigOptionsFromConfig()
    {
        static Stream CreateStreamFromString(string data) => new MemoryStream(Encoding.UTF8.GetBytes(data));

        using var jsonStream = CreateStreamFromString("""
        {
          "Aspire": {
            "Confluent": {
              "Kafka": {
                "Producer": {
                  "Config": {
                    "BootstrapServers": "localhost:9092",
                    "Acks": "All",
                    "SaslUsername": "user",
                    "SaslPassword": "password",
                    "SaslMechanism": "Plain",
                    "SecurityProtocol": "Plaintext"
                  }
                }
              }
            }
          }
        }
        """);

        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Configuration.AddJsonStream(jsonStream);

        builder.AddKafkaProducer<string, string>("messaging");

        using var host = builder.Build();
        var connectionFactory = host.Services.GetRequiredService(ReflectionHelpers.ProducerConnectionFactoryStringKeyStringValueType.Value);

        var config = GetProducerConfig(connectionFactory)!;

        Assert.AreEqual(Acks.All, config.Acks);
        Assert.AreEqual("user", config.SaslUsername);
        Assert.AreEqual("password", config.SaslPassword);
        Assert.AreEqual(SaslMechanism.Plain, config.SaslMechanism);
        Assert.AreEqual(SecurityProtocol.Plaintext, config.SecurityProtocol);
    }

    private static ProducerConfig? GetProducerConfig(object o) => ReflectionHelpers.ProducerConnectionFactoryStringKeyStringValueType.Value.GetProperty("Config")!.GetValue(o) as ProducerConfig;
}
