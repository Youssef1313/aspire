// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Nats.Tests;

[TestClass]
public class AddNatsTests
{
    [TestMethod]
    public void AddNatsAddsGeneratedPasswordParameterWithUserSecretsParameterDefaultInRunMode()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var nats = appBuilder.AddNats("nats");
        Assert.AreEqual("Aspire.Hosting.ApplicationModel.UserSecretsParameterDefault", nats.Resource.PasswordParameter!.Default?.GetType().FullName);
    }

    [TestMethod]
    public void AddNatsDoesNotAddGeneratedPasswordParameterWithUserSecretsParameterDefaultInPublishMode()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var nats = appBuilder.AddNats("nats");

        Assert.AreNotEqual("Aspire.Hosting.ApplicationModel.UserSecretsParameterDefault", nats.Resource.PasswordParameter!.Default?.GetType().FullName);
    }

    [TestMethod]
    public async Task AddNatsSetsDefaultUserNameAndPasswordAndIncludesThemInConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var nats = appBuilder.AddNats("nats")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 4222));

        Assert.IsNotNull(nats.Resource.PasswordParameter);
        Assert.IsFalse(string.IsNullOrEmpty(nats.Resource.PasswordParameter!.Value));

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var natsResource = Assert.ContainsSingle(appModel.Resources.OfType<NatsServerResource>());
        var connectionStringResource = natsResource as IResourceWithConnectionString;
        Assert.IsNotNull(connectionStringResource);
        var connectionString = await connectionStringResource.GetConnectionStringAsync();

        Assert.AreEqual($"nats://nats:{natsResource.PasswordParameter?.Value}@localhost:4222", connectionString);
        Assert.AreEqual("nats://nats:{nats-password.value}@{nats.bindings.tcp.host}:{nats.bindings.tcp.port}", connectionStringResource.ConnectionStringExpression.ValueExpression);
    }

    [TestMethod]
    public async Task AddNatsSetsUserNameAndPasswordAndIncludesThemInConnection()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var userParameters = appBuilder.AddParameter("user", "usr");
        var passwordParameters = appBuilder.AddParameter("pass", "password");

        var nats = appBuilder.AddNats("nats", userName: userParameters, password: passwordParameters)
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 4222));

        Assert.IsNotNull(nats.Resource.UserNameParameter);
        Assert.IsNotNull(nats.Resource.PasswordParameter);

        Assert.AreEqual("usr", nats.Resource.UserNameParameter!.Value);
        Assert.AreEqual("password", nats.Resource.PasswordParameter!.Value);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.ContainsSingle(appModel.Resources.OfType<NatsServerResource>()) as IResourceWithConnectionString;
        var connectionString = await connectionStringResource.GetConnectionStringAsync();

        Assert.AreEqual("nats://usr:password@localhost:4222", connectionString);
        Assert.AreEqual("nats://{user.value}:{pass.value}@{nats.bindings.tcp.host}:{nats.bindings.tcp.port}", connectionStringResource.ConnectionStringExpression.ValueExpression);
    }

    [TestMethod]
    public async Task AddNatsContainerWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddNats("nats");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.ContainsSingle(appModel.Resources.OfType<NatsServerResource>());
        Assert.AreEqual("nats", containerResource.Name);

        var endpoint = Assert.ContainsSingle(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.AreEqual(4222, endpoint.TargetPort);
        Assert.IsFalse(endpoint.IsExternal);
        Assert.AreEqual("tcp", endpoint.Name);
        Assert.IsNull(endpoint.Port);
        Assert.AreEqual(ProtocolType.Tcp, endpoint.Protocol);
        Assert.AreEqual("tcp", endpoint.Transport);
        Assert.AreEqual("tcp", endpoint.UriScheme);

        var containerAnnotation = Assert.ContainsSingle(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.AreEqual(NatsContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.AreEqual(NatsContainerImageTags.Image, containerAnnotation.Image);
        Assert.AreEqual(NatsContainerImageTags.Registry, containerAnnotation.Registry);

        var args = await ArgumentEvaluator.GetArgumentListAsync(containerResource);

        Assert.That.Collection(args,
            arg => Assert.AreEqual("--user", arg),
            arg => Assert.AreEqual("nats", arg),
            arg => Assert.AreEqual("--pass", arg),
            arg => Assert.IsFalse(string.IsNullOrEmpty(arg))
        );
    }

    [TestMethod]
    public async Task AddNatsContainerAddsAnnotationMetadata()
    {
        var path = OperatingSystem.IsWindows() ? @"C:\tmp\dev-data" : "/tmp/dev-data";

        var appBuilder = DistributedApplication.CreateBuilder();
        var user = appBuilder.AddParameter("user", "usr");
        var pass = appBuilder.AddParameter("pass", "pass");

        appBuilder.AddNats("nats", 1234, user, pass).WithJetStream().WithDataBindMount(path);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.ContainsSingle(appModel.Resources.OfType<NatsServerResource>());
        Assert.AreEqual("nats", containerResource.Name);

        var mountAnnotation = Assert.ContainsSingle(containerResource.Annotations.OfType<ContainerMountAnnotation>());
        Assert.AreEqual(path, mountAnnotation.Source);
        Assert.AreEqual("/var/lib/nats", mountAnnotation.Target);

        var endpoint = Assert.ContainsSingle(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.AreEqual(4222, endpoint.TargetPort);
        Assert.IsFalse(endpoint.IsExternal);
        Assert.AreEqual("tcp", endpoint.Name);
        Assert.AreEqual(1234, endpoint.Port);
        Assert.AreEqual(ProtocolType.Tcp, endpoint.Protocol);
        Assert.AreEqual("tcp", endpoint.Transport);
        Assert.AreEqual("tcp", endpoint.UriScheme);

        var containerAnnotation = Assert.ContainsSingle(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.AreEqual(NatsContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.AreEqual(NatsContainerImageTags.Image, containerAnnotation.Image);
        Assert.AreEqual(NatsContainerImageTags.Registry, containerAnnotation.Registry);

        var args = await ArgumentEvaluator.GetArgumentListAsync(containerResource);

        Assert.That.Collection(args,
            arg => Assert.AreEqual("--user", arg),
            arg => Assert.AreEqual("usr", arg),
            arg => Assert.AreEqual("--pass", arg),
            arg => Assert.AreEqual("pass", arg),
            arg => Assert.AreEqual("-js", arg),
            arg => Assert.AreEqual("-sd", arg),
            arg => Assert.AreEqual("/var/lib/nats", arg)
        );
    }

    [TestMethod]
    public void WithNatsContainerOnMultipleResources()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddNats("nats1");
        builder.AddNats("nats2");

        Assert.AreEqual(2, builder.Resources.OfType<NatsServerResource>().Count());
    }

    [TestMethod]
    public async Task VerifyManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var nats = builder.AddNats("nats");

        var manifest = await ManifestUtils.GetManifest(nats.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "nats://nats:{nats-password.value}@{nats.bindings.tcp.host}:{nats.bindings.tcp.port}",
              "image": "{{NatsContainerImageTags.Registry}}/{{NatsContainerImageTags.Image}}:{{NatsContainerImageTags.Tag}}",
              "args": [
                "--user",
                "nats",
                "--pass",
                "{nats-password.value}"
              ],
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 4222
                }
              }
            }
            """;

        Assert.AreEqual(expectedManifest, manifest.ToString());
    }

    [TestMethod]
    public async Task VerifyManifestWihtParameters()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var userNameParameter = builder.AddParameter("user");
        var passwordParameter = builder.AddParameter("pass");

        var nats = builder.AddNats("nats", userName: userNameParameter, password: passwordParameter)
            .WithJetStream();

        var manifest = await ManifestUtils.GetManifest(nats.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "nats://{user.value}:{pass.value}@{nats.bindings.tcp.host}:{nats.bindings.tcp.port}",
              "image": "{{NatsContainerImageTags.Registry}}/{{NatsContainerImageTags.Image}}:{{NatsContainerImageTags.Tag}}",
              "args": [
                "--user",
                "{user.value}",
                "--pass",
                "{pass.value}",
                "-js"
              ],
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 4222
                }
              }
            }
            """;
        Assert.AreEqual(expectedManifest, manifest.ToString());

        nats = builder.AddNats("nats2", userName: userNameParameter);

        manifest = await ManifestUtils.GetManifest(nats.Resource);

        expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "nats://{user.value}:{nats2-password.value}@{nats2.bindings.tcp.host}:{nats2.bindings.tcp.port}",
              "image": "{{NatsContainerImageTags.Registry}}/{{NatsContainerImageTags.Image}}:{{NatsContainerImageTags.Tag}}",
              "args": [
                "--user",
                "{user.value}",
                "--pass",
                "{nats2-password.value}"
              ],
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 4222
                }
              }
            }
            """;
        Assert.AreEqual(expectedManifest, manifest.ToString());

        nats = builder.AddNats("nats3", password: passwordParameter);

        manifest = await ManifestUtils.GetManifest(nats.Resource);

        expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "nats://nats:{pass.value}@{nats3.bindings.tcp.host}:{nats3.bindings.tcp.port}",
              "image": "{{NatsContainerImageTags.Registry}}/{{NatsContainerImageTags.Image}}:{{NatsContainerImageTags.Tag}}",
              "args": [
                "--user",
                "nats",
                "--pass",
                "{pass.value}"
              ],
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 4222
                }
              }
            }
            """;
        Assert.AreEqual(expectedManifest, manifest.ToString());
    }
}
