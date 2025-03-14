// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Sockets;

namespace Aspire.Hosting.RabbitMQ.Tests;

[TestClass]
public class AddRabbitMQTests
{
    [TestMethod]
    public void AddRabbitMQAddsGeneratedPasswordParameterWithUserSecretsParameterDefaultInRunMode()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var rmq = appBuilder.AddRabbitMQ("rmq");

        Assert.AreEqual("Aspire.Hosting.ApplicationModel.UserSecretsParameterDefault", rmq.Resource.PasswordParameter.Default?.GetType().FullName);
    }

    [TestMethod]
    public void AddRabbitMQDoesNotAddGeneratedPasswordParameterWithUserSecretsParameterDefaultInPublishMode()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var rmq = appBuilder.AddRabbitMQ("rmq");

        Assert.AreNotEqual("Aspire.Hosting.ApplicationModel.UserSecretsParameterDefault", rmq.Resource.PasswordParameter.Default?.GetType().FullName);
    }

    [TestMethod]
    [DataRow(false, null)]
    [DataRow(true, null)]
    [DataRow(true, 15672)]
    public void AddRabbitMQContainerWithDefaultsAddsAnnotationMetadata(bool withManagementPlugin, int? withManagementPluginPort)
    {
        var appBuilder = TestDistributedApplicationBuilder.Create();

        var rabbitmq = appBuilder.AddRabbitMQ("rabbit");

        if (withManagementPlugin)
        {
            rabbitmq.WithManagementPlugin(withManagementPluginPort);
        }

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.ContainsSingle(appModel.Resources.OfType<RabbitMQServerResource>());
        Assert.AreEqual("rabbit", containerResource.Name);

        var primaryEndpoint = Assert.ContainsSingle(containerResource.Annotations.OfType<EndpointAnnotation>().Where(e => e.Name == "tcp"));
        Assert.AreEqual(5672, primaryEndpoint.TargetPort);
        Assert.IsFalse(primaryEndpoint.IsExternal);
        Assert.AreEqual("tcp", primaryEndpoint.Name);
        Assert.IsNull(primaryEndpoint.Port);
        Assert.AreEqual(ProtocolType.Tcp, primaryEndpoint.Protocol);
        Assert.AreEqual("tcp", primaryEndpoint.Transport);
        Assert.AreEqual("tcp", primaryEndpoint.UriScheme);

        if (withManagementPlugin)
        {
            var mangementEndpoint = Assert.ContainsSingle(containerResource.Annotations.OfType<EndpointAnnotation>().Where(e => e.Name == "management"));
            Assert.AreEqual(15672, mangementEndpoint.TargetPort);
            Assert.IsFalse(primaryEndpoint.IsExternal);
            Assert.AreEqual("management", mangementEndpoint.Name);
            Assert.AreEqual(ProtocolType.Tcp, mangementEndpoint.Protocol);
            Assert.AreEqual("http", mangementEndpoint.Transport);
            Assert.AreEqual("http", mangementEndpoint.UriScheme);

            if (!withManagementPluginPort.HasValue)
            {
                Assert.IsNull(mangementEndpoint.Port);
            }
            else
            {
                Assert.AreEqual(withManagementPluginPort.Value, mangementEndpoint.Port);
            }
        }

        var containerAnnotation = Assert.ContainsSingle(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.AreEqual(RabbitMQContainerImageTags.Image, containerAnnotation.Image);
        Assert.AreEqual(withManagementPlugin ? RabbitMQContainerImageTags.ManagementTag : RabbitMQContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.AreEqual(RabbitMQContainerImageTags.Registry, containerAnnotation.Registry);
    }

    [TestMethod]
    public async Task RabbitMQCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var pass = appBuilder.AddParameter("pass", "p@ssw0rd1");
        appBuilder
            .AddRabbitMQ("rabbit", password: pass)
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 27011));

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var rabbitMqResource = Assert.ContainsSingle(appModel.Resources.OfType<RabbitMQServerResource>());
        var connectionStringResource = rabbitMqResource as IResourceWithConnectionString;
        var connectionString = await connectionStringResource.GetConnectionStringAsync(default);

        Assert.AreEqual("amqp://guest:p@ssw0rd1@localhost:27011", connectionString);
        Assert.AreEqual("amqp://guest:{pass.value}@{rabbit.bindings.tcp.host}:{rabbit.bindings.tcp.port}", connectionStringResource.ConnectionStringExpression.ValueExpression);
    }

    [TestMethod]
    [DataRow(null, RabbitMQContainerImageTags.ManagementTag)]
    [DataRow("3", "3-management")]
    [DataRow("3.12", "3.12-management")]
    [DataRow("3.12.0", "3.12.0-management")]
    [DataRow("3-alpine", "3-management-alpine")]
    [DataRow("3.12-alpine", "3.12-management-alpine")]
    [DataRow("3.12.0-alpine", "3.12.0-management-alpine")]
    [DataRow("999", "999-management")]
    [DataRow("12345", "12345-management")]
    [DataRow("12345.00.12", "12345.00.12-management")]
    public void WithManagementPluginUpdatesContainerImageTagToEnableManagementPlugin(string? imageTag, string expectedTag)
    {
        var appBuilder = TestDistributedApplicationBuilder.Create();

        var rabbitmq = appBuilder.AddRabbitMQ("rabbit");
        if (imageTag is not null)
        {
            rabbitmq.WithImageTag(imageTag);
        }
        rabbitmq.WithManagementPlugin();

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.ContainsSingle(appModel.Resources.OfType<RabbitMQServerResource>());
        var containerAnnotation = Assert.ContainsSingle(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.AreEqual(expectedTag, containerAnnotation.Tag);
    }

    [TestMethod]
    [DataRow(" ")]
    [DataRow("test")]
    [DataRow(".123")]
    [DataRow(".")]
    [DataRow(".1.2")]
    [DataRow("1.2.")]
    [DataRow("1.٩.3")]
    [DataRow("1.2..3")]
    [DataRow("not-supported")]
    public void WithManagementPluginThrowsForUnsupportedContainerImageTag(string imageTag)
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var rabbitmq = appBuilder.AddRabbitMQ("rabbit");
        rabbitmq.WithImageTag(imageTag);

        Assert.Throws<DistributedApplicationException>(() => _ = rabbitmq.WithManagementPlugin());
    }

    [TestMethod]
    [DataRow("notrabbitmq")]
    [DataRow("not-supported")]
    public void WithManagementPluginThrowsForUnsupportedContainerImageName(string imageName)
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var rabbitmq = appBuilder.AddRabbitMQ("rabbit");
        rabbitmq.WithImage(imageName);

        Assert.Throws<DistributedApplicationException>(() => _ = rabbitmq.WithManagementPlugin());
    }

    [TestMethod]
    [DataRow(" ")]
    [DataRow("custom.url")]
    [DataRow("not.the.default")]
    public void WithManagementPluginThrowsForUnsupportedContainerImageRegistry(string registry)
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var rabbitmq = appBuilder.AddRabbitMQ("rabbit");
        rabbitmq.WithImageRegistry(registry);

        Assert.Throws<DistributedApplicationException>(() => _ = rabbitmq.WithManagementPlugin());
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task VerifyManifest(bool withManagementPlugin)
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var rabbit = builder.AddRabbitMQ("rabbit");
        if (withManagementPlugin)
        {
            rabbit.WithManagementPlugin();
        }
        var manifest = await ManifestUtils.GetManifest(rabbit.Resource);

        var expectedTag = withManagementPlugin ? RabbitMQContainerImageTags.ManagementTag : RabbitMQContainerImageTags.Tag;
        var managementBinding = withManagementPlugin
            ? """
            ,
                "management": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 15672
                }
            """
            : "";
        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "amqp://guest:{rabbit-password.value}@{rabbit.bindings.tcp.host}:{rabbit.bindings.tcp.port}",
              "image": "{{RabbitMQContainerImageTags.Registry}}/{{RabbitMQContainerImageTags.Image}}:{{expectedTag}}",
              "env": {
                "RABBITMQ_DEFAULT_USER": "guest",
                "RABBITMQ_DEFAULT_PASS": "{rabbit-password.value}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 5672
                }{{managementBinding}}
              }
            }
            """;

        Assert.AreEqual(expectedManifest, manifest.ToString());
    }

    [TestMethod]
    public async Task VerifyManifestWithParameters()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var userNameParameter = builder.AddParameter("user");
        var passwordParameter = builder.AddParameter("pass");

        var rabbit = builder.AddRabbitMQ("rabbit", userNameParameter, passwordParameter);
        var manifest = await ManifestUtils.GetManifest(rabbit.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "amqp://{user.value}:{pass.value}@{rabbit.bindings.tcp.host}:{rabbit.bindings.tcp.port}",
              "image": "{{RabbitMQContainerImageTags.Registry}}/{{RabbitMQContainerImageTags.Image}}:{{RabbitMQContainerImageTags.Tag}}",
              "env": {
                "RABBITMQ_DEFAULT_USER": "{user.value}",
                "RABBITMQ_DEFAULT_PASS": "{pass.value}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 5672
                }
              }
            }
            """;
        Assert.AreEqual(expectedManifest, manifest.ToString());

        rabbit = builder.AddRabbitMQ("rabbit2", userNameParameter);
        manifest = await ManifestUtils.GetManifest(rabbit.Resource);

        expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "amqp://{user.value}:{rabbit2-password.value}@{rabbit2.bindings.tcp.host}:{rabbit2.bindings.tcp.port}",
              "image": "{{RabbitMQContainerImageTags.Registry}}/{{RabbitMQContainerImageTags.Image}}:{{RabbitMQContainerImageTags.Tag}}",
              "env": {
                "RABBITMQ_DEFAULT_USER": "{user.value}",
                "RABBITMQ_DEFAULT_PASS": "{rabbit2-password.value}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 5672
                }
              }
            }
            """;
        Assert.AreEqual(expectedManifest, manifest.ToString());

        rabbit = builder.AddRabbitMQ("rabbit3", password: passwordParameter);
        manifest = await ManifestUtils.GetManifest(rabbit.Resource);

        expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "amqp://guest:{pass.value}@{rabbit3.bindings.tcp.host}:{rabbit3.bindings.tcp.port}",
              "image": "{{RabbitMQContainerImageTags.Registry}}/{{RabbitMQContainerImageTags.Image}}:{{RabbitMQContainerImageTags.Tag}}",
              "env": {
                "RABBITMQ_DEFAULT_USER": "guest",
                "RABBITMQ_DEFAULT_PASS": "{pass.value}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 5672
                }
              }
            }
            """;
        Assert.AreEqual(expectedManifest, manifest.ToString());
    }
}
