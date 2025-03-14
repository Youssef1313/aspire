// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Confluent.Kafka;
using Microsoft.Extensions.Hosting;

namespace Aspire.Confluent.Kafka.Tests;

[TestClass]
public class ConfluentKafkaPublicApiTests
{
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    public void AddKafkaConsumerShouldThrowWhenBuilderIsNull(int overrideIndex)
    {
        IHostApplicationBuilder builder = null!;
        const string connectionName = "Kafka:Consumer";
        Action<KafkaConsumerSettings>? configureSettings = null;
        Action<ConsumerBuilder<string, string>>? configureBuilder = null;
        Action<IServiceProvider, ConsumerBuilder<string, string>>? configureBuilderWithServiceProvider = null;

        Action action = overrideIndex switch
        {
            0 => () => builder.AddKafkaConsumer<string, string>(connectionName),
            1 => () => builder.AddKafkaConsumer<string, string>(connectionName, configureSettings),
            2 => () => builder.AddKafkaConsumer(connectionName, configureBuilder),
            3 => () => builder.AddKafkaConsumer(connectionName, configureBuilderWithServiceProvider),
            4 => () => builder.AddKafkaConsumer(connectionName, configureSettings, configureBuilder),
            5 => () => builder.AddKafkaConsumer(connectionName, configureSettings, configureBuilderWithServiceProvider),
            _ => throw new InvalidOperationException()
        };

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(0, false)]
    [DataRow(0, true)]
    [DataRow(1, false)]
    [DataRow(1, true)]
    [DataRow(2, false)]
    [DataRow(2, true)]
    [DataRow(3, false)]
    [DataRow(3, true)]
    [DataRow(4, false)]
    [DataRow(4, true)]
    [DataRow(5, false)]
    [DataRow(5, true)]
    public void AddKafkaConsumerShouldThrowWhenConnectionNameIsNullOrEmpty(int overrideIndex, bool isNull)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var connectionName = isNull ? null! : string.Empty;
        Action<KafkaConsumerSettings>? configureSettings = null;
        Action<ConsumerBuilder<string, string>>? configureBuilder = null;
        Action<IServiceProvider, ConsumerBuilder<string, string>>? configureBuilderWithServiceProvider = null;

        Action action = overrideIndex switch
        {
            0 => () => builder.AddKafkaConsumer<string, string>(connectionName),
            1 => () => builder.AddKafkaConsumer<string, string>(connectionName, configureSettings),
            2 => () => builder.AddKafkaConsumer(connectionName, configureBuilder),
            3 => () => builder.AddKafkaConsumer(connectionName, configureBuilderWithServiceProvider),
            4 => () => builder.AddKafkaConsumer(connectionName, configureSettings, configureBuilder),
            5 => () => builder.AddKafkaConsumer(connectionName, configureSettings, configureBuilderWithServiceProvider),
            _ => throw new InvalidOperationException()
        };

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(connectionName), exception.ParamName);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    public void AddKeyedKafkaConsumerShouldThrowWhenBuilderIsNull(int overrideIndex)
    {
        IHostApplicationBuilder builder = null!;
        const string name = "Kafka:Consumer";
        Action<KafkaConsumerSettings>? configureSettings = null;
        Action<ConsumerBuilder<string, string>>? configureBuilder = null;
        Action<IServiceProvider, ConsumerBuilder<string, string>>? configureBuilderWithServiceProvider = null;

        Action action = overrideIndex switch
        {
            0 => () => builder.AddKeyedKafkaConsumer<string, string>(name),
            1 => () => builder.AddKeyedKafkaConsumer<string, string>(name, configureSettings),
            2 => () => builder.AddKeyedKafkaConsumer(name, configureBuilder),
            3 => () => builder.AddKeyedKafkaConsumer(name, configureBuilderWithServiceProvider),
            4 => () => builder.AddKeyedKafkaConsumer(name, configureSettings, configureBuilder),
            5 => () => builder.AddKeyedKafkaConsumer(name, configureSettings, configureBuilderWithServiceProvider),
            _ => throw new InvalidOperationException()
        };

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(0, false)]
    [DataRow(0, true)]
    [DataRow(1, false)]
    [DataRow(1, true)]
    [DataRow(2, false)]
    [DataRow(2, true)]
    [DataRow(3, false)]
    [DataRow(3, true)]
    [DataRow(4, false)]
    [DataRow(4, true)]
    [DataRow(5, false)]
    [DataRow(5, true)]
    public void AddKeyedKafkaConsumerShouldThrowWhenConnectionNameIsNullOrEmpty(int overrideIndex, bool isNull)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var name = isNull ? null! : string.Empty;
        Action<KafkaConsumerSettings>? configureSettings = null;
        Action<ConsumerBuilder<string, string>>? configureBuilder = null;
        Action<IServiceProvider, ConsumerBuilder<string, string>>? configureBuilderWithServiceProvider = null;

        Action action = overrideIndex switch
        {
            0 => () => builder.AddKeyedKafkaConsumer<string, string>(name),
            1 => () => builder.AddKeyedKafkaConsumer<string, string>(name, configureSettings),
            2 => () => builder.AddKeyedKafkaConsumer(name, configureBuilder),
            3 => () => builder.AddKeyedKafkaConsumer(name, configureBuilderWithServiceProvider),
            4 => () => builder.AddKeyedKafkaConsumer(name, configureSettings, configureBuilder),
            5 => () => builder.AddKeyedKafkaConsumer(name, configureSettings, configureBuilderWithServiceProvider),
            _ => throw new InvalidOperationException()
        };

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    public void AddKafkaProducerShouldThrowWhenBuilderIsNull(int overrideIndex)
    {
        IHostApplicationBuilder builder = null!;
        const string connectionName = "Kafka:Consumer";
        Action<KafkaProducerSettings>? configureSettings = null;
        Action<ProducerBuilder<string, string>>? configureBuilder = null;
        Action<IServiceProvider, ProducerBuilder<string, string>>? configureBuilderWithServiceProvider = null;

        Action action = overrideIndex switch
        {
            0 => () => builder.AddKafkaProducer<string, string>(connectionName),
            1 => () => builder.AddKafkaProducer<string, string>(connectionName, configureSettings),
            2 => () => builder.AddKafkaProducer(connectionName, configureBuilder),
            3 => () => builder.AddKafkaProducer(connectionName, configureBuilderWithServiceProvider),
            4 => () => builder.AddKafkaProducer(connectionName, configureSettings, configureBuilder),
            5 => () => builder.AddKafkaProducer(connectionName, configureSettings, configureBuilderWithServiceProvider),
            _ => throw new InvalidOperationException()
        };

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(0, false)]
    [DataRow(0, true)]
    [DataRow(1, false)]
    [DataRow(1, true)]
    [DataRow(2, false)]
    [DataRow(2, true)]
    [DataRow(3, false)]
    [DataRow(3, true)]
    [DataRow(4, false)]
    [DataRow(4, true)]
    [DataRow(5, false)]
    [DataRow(5, true)]
    public void AddKafkaProducerShouldThrowWhenConnectionNameIsNullOrEmpty(int overrideIndex, bool isNull)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var connectionName = isNull ? null! : string.Empty;
        Action<KafkaProducerSettings>? configureSettings = null;
        Action<ProducerBuilder<string, string>>? configureBuilder = null;
        Action<IServiceProvider, ProducerBuilder<string, string>>? configureBuilderWithServiceProvider = null;

        Action action = overrideIndex switch
        {
            0 => () => builder.AddKafkaProducer<string, string>(connectionName),
            1 => () => builder.AddKafkaProducer<string, string>(connectionName, configureSettings),
            2 => () => builder.AddKafkaProducer(connectionName, configureBuilder),
            3 => () => builder.AddKafkaProducer(connectionName, configureBuilderWithServiceProvider),
            4 => () => builder.AddKafkaProducer(connectionName, configureSettings, configureBuilder),
            5 => () => builder.AddKafkaProducer(connectionName, configureSettings, configureBuilderWithServiceProvider),
            _ => throw new InvalidOperationException()
        };

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(connectionName), exception.ParamName);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    public void AddKeyedKafkaProducerConsumerShouldThrowWhenBuilderIsNull(int overrideIndex)
    {
        IHostApplicationBuilder builder = null!;
        const string name = "Kafka:Consumer";
        Action<KafkaProducerSettings>? configureSettings = null;
        Action<ProducerBuilder<string, string>>? configureBuilder = null;
        Action<IServiceProvider, ProducerBuilder<string, string>>? configureBuilderWithServiceProvider = null;

        Action action = overrideIndex switch
        {
            0 => () => builder.AddKeyedKafkaProducer<string, string>(name),
            1 => () => builder.AddKeyedKafkaProducer<string, string>(name, configureSettings),
            2 => () => builder.AddKeyedKafkaProducer(name, configureBuilder),
            3 => () => builder.AddKeyedKafkaProducer(name, configureBuilderWithServiceProvider),
            4 => () => builder.AddKeyedKafkaProducer(name, configureSettings, configureBuilder),
            5 => () => builder.AddKeyedKafkaProducer(name, configureSettings, configureBuilderWithServiceProvider),
            _ => throw new InvalidOperationException()
        };

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(0, false)]
    [DataRow(0, true)]
    [DataRow(1, false)]
    [DataRow(1, true)]
    [DataRow(2, false)]
    [DataRow(2, true)]
    [DataRow(3, false)]
    [DataRow(3, true)]
    [DataRow(4, false)]
    [DataRow(4, true)]
    [DataRow(5, false)]
    [DataRow(5, true)]
    public void AddKeyedKafkaProducerShouldThrowWhenConnectionNameIsNullOrEmpty(int overrideIndex, bool isNull)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var name = isNull ? null! : string.Empty;
        Action<KafkaProducerSettings>? configureSettings = null;
        Action<ProducerBuilder<string, string>>? configureBuilder = null;
        Action<IServiceProvider, ProducerBuilder<string, string>>? configureBuilderWithServiceProvider = null;

        Action action = overrideIndex switch
        {
            0 => () => builder.AddKeyedKafkaProducer<string, string>(name),
            1 => () => builder.AddKeyedKafkaProducer<string, string>(name, configureSettings),
            2 => () => builder.AddKeyedKafkaProducer(name, configureBuilder),
            3 => () => builder.AddKeyedKafkaProducer(name, configureBuilderWithServiceProvider),
            4 => () => builder.AddKeyedKafkaProducer(name, configureSettings, configureBuilder),
            5 => () => builder.AddKeyedKafkaProducer(name, configureSettings, configureBuilderWithServiceProvider),
            _ => throw new InvalidOperationException()
        };

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }
}
