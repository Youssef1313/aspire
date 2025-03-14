// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Kafka.Tests;

[TestClass]
public class AddKafkaTests
{
    [TestMethod]
    public void AddKafkaContainerWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddKafka("kafka");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.ContainsSingle(appModel.Resources.OfType<KafkaServerResource>());
        Assert.AreEqual("kafka", containerResource.Name);

        var endpoints = containerResource.Annotations.OfType<EndpointAnnotation>();
        Assert.AreEqual(2, endpoints.Count());

        var primaryEndpoint = Assert.ContainsSingle(endpoints, e => e.Name == "tcp");
        Assert.AreEqual(9092, primaryEndpoint.TargetPort);
        Assert.IsFalse(primaryEndpoint.IsExternal);
        Assert.AreEqual("tcp", primaryEndpoint.Name);
        Assert.IsNull(primaryEndpoint.Port);
        Assert.AreEqual(ProtocolType.Tcp, primaryEndpoint.Protocol);
        Assert.AreEqual("tcp", primaryEndpoint.Transport);
        Assert.AreEqual("tcp", primaryEndpoint.UriScheme);

        var internalEndpoint = Assert.ContainsSingle(endpoints, e => e.Name == "internal");
        Assert.AreEqual(9093, internalEndpoint.TargetPort);
        Assert.IsFalse(internalEndpoint.IsExternal);
        Assert.AreEqual("internal", internalEndpoint.Name);
        Assert.IsNull(internalEndpoint.Port);
        Assert.AreEqual(ProtocolType.Tcp, internalEndpoint.Protocol);
        Assert.AreEqual("tcp", internalEndpoint.Transport);
        Assert.AreEqual("tcp", internalEndpoint.UriScheme);

        var containerAnnotation = Assert.ContainsSingle(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.AreEqual(KafkaContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.AreEqual(KafkaContainerImageTags.Image, containerAnnotation.Image);
        Assert.AreEqual(KafkaContainerImageTags.Registry, containerAnnotation.Registry);
    }

    [TestMethod]
    public async Task KafkaCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder
            .AddKafka("kafka")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 27017));

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.ContainsSingle(appModel.Resources.OfType<KafkaServerResource>()) as IResourceWithConnectionString;
        var connectionString = await connectionStringResource.GetConnectionStringAsync();

        Assert.AreEqual("localhost:27017", connectionString);
        Assert.AreEqual("{kafka.bindings.tcp.host}:{kafka.bindings.tcp.port}", connectionStringResource.ConnectionStringExpression.ValueExpression);
    }

    [TestMethod]
    public async Task VerifyManifest()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var kafka = appBuilder.AddKafka("kafka");

        var manifest = await ManifestUtils.GetManifest(kafka.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "{kafka.bindings.tcp.host}:{kafka.bindings.tcp.port}",
              "image": "{{KafkaContainerImageTags.Registry}}/{{KafkaContainerImageTags.Image}}:{{KafkaContainerImageTags.Tag}}",
              "env": {
                "KAFKA_LISTENERS": "PLAINTEXT://localhost:29092,CONTROLLER://localhost:29093,PLAINTEXT_HOST://0.0.0.0:9092,PLAINTEXT_INTERNAL://0.0.0.0:9093",
                "KAFKA_LISTENER_SECURITY_PROTOCOL_MAP": "CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT,PLAINTEXT_INTERNAL:PLAINTEXT",
                "KAFKA_ADVERTISED_LISTENERS": "PLAINTEXT://{kafka.bindings.tcp.host}:29092,PLAINTEXT_HOST://{kafka.bindings.tcp.host}:{kafka.bindings.tcp.port},PLAINTEXT_INTERNAL://{kafka.bindings.internal.host}:{kafka.bindings.internal.port}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 9092
                },
                "internal": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 9093
                }
              }
            }
            """;
        Assert.AreEqual(expectedManifest, manifest.ToString());
    }

    [TestMethod]
    public async Task WithDataVolumeConfigureCorrectEnvironment()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var kafka = appBuilder.AddKafka("kafka")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 27017))
            .WithDataVolume("kafka-data");

        var config = await kafka.Resource.GetEnvironmentVariableValuesAsync();

        var volumeAnnotation = kafka.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.AreEqual("kafka-data", volumeAnnotation.Source);
        Assert.AreEqual("/var/lib/kafka/data", volumeAnnotation.Target);
        Assert.Contains(config, kvp => kvp.Key == "KAFKA_LOG_DIRS" && kvp.Value == "/var/lib/kafka/data");
    }

    [TestMethod]
    public async Task WithDataBindConfigureCorrectEnvironment()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var kafka = appBuilder.AddKafka("kafka")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 27017))
            .WithDataBindMount("kafka-data");

        var config = await kafka.Resource.GetEnvironmentVariableValuesAsync();

        var volumeAnnotation = kafka.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.AreEqual(Path.Combine(appBuilder.AppHostDirectory, "kafka-data"), volumeAnnotation.Source);
        Assert.AreEqual("/var/lib/kafka/data", volumeAnnotation.Target);
        Assert.Contains(config, kvp => kvp.Key == "KAFKA_LOG_DIRS" && kvp.Value == "/var/lib/kafka/data");
    }

    public static TheoryData<string?, string, int?> WithKafkaUIAddsAnUniqueContainerSetsItsNameAndInvokesConfigurationCallbackTestVariations()
    {
        return new()
        {
            { "kafka-ui", "kafka-ui", 8081 },
            { null, "kafka1-kafka-ui", 8081 },
            { "kafka-ui", "kafka-ui", null },
            { null, "kafka1-kafka-ui", null },
        };
    }

    [TestMethod]
    [MemberData(nameof(WithKafkaUIAddsAnUniqueContainerSetsItsNameAndInvokesConfigurationCallbackTestVariations))]
    public void WithKafkaUIAddsAnUniqueContainerSetsItsNameAndInvokesConfigurationCallback(string? containerName, string expectedContainerName, int? port)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var configureContainerInvocations = 0;
        Action<IResourceBuilder<KafkaUIContainerResource>> kafkaUIConfigurationCallback = kafkaUi =>
        {
            kafkaUi.WithHostPort(port);
            configureContainerInvocations++;
        };
        builder.AddKafka("kafka1").WithKafkaUI(configureContainer: kafkaUIConfigurationCallback, containerName: containerName);
        builder.AddKafka("kafka2").WithKafkaUI();

        Assert.ContainsSingle(builder.Resources.OfType<KafkaUIContainerResource>());
        var kafkaUiResource = Assert.ContainsSingle(builder.Resources, r => r.Name == expectedContainerName);
        Assert.AreEqual(1, configureContainerInvocations);
        var kafkaUiEndpoint = kafkaUiResource.Annotations.OfType<EndpointAnnotation>().Single();
        Assert.AreEqual(8080, kafkaUiEndpoint.TargetPort);
        Assert.AreEqual(port, kafkaUiEndpoint.Port);
    }
}
