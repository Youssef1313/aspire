// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Valkey.Tests;

[TestClass]
public class AddValkeyTests
{
    [TestMethod]
    public void AddValkeyContainerWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddValkey("myValkey").PublishAsContainer();

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.ContainsSingle(appModel.Resources.OfType<ValkeyResource>());
        Assert.AreEqual("myValkey", containerResource.Name);

        var endpoint = Assert.ContainsSingle(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.AreEqual(6379, endpoint.TargetPort);
        Assert.IsFalse(endpoint.IsExternal);
        Assert.AreEqual("tcp", endpoint.Name);
        Assert.IsNull(endpoint.Port);
        Assert.AreEqual(ProtocolType.Tcp, endpoint.Protocol);
        Assert.AreEqual("tcp", endpoint.Transport);
        Assert.AreEqual("tcp", endpoint.UriScheme);

        var containerAnnotation = Assert.ContainsSingle(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.AreEqual(ValkeyContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.AreEqual(ValkeyContainerImageTags.Image, containerAnnotation.Image);
        Assert.AreEqual(ValkeyContainerImageTags.Registry, containerAnnotation.Registry);
    }

    [TestMethod]
    public void AddValkeyContainerAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddValkey("myValkey", port: 8813);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.ContainsSingle(appModel.Resources.OfType<ValkeyResource>());
        Assert.AreEqual("myValkey", containerResource.Name);

        var endpoint = Assert.ContainsSingle(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.AreEqual(6379, endpoint.TargetPort);
        Assert.IsFalse(endpoint.IsExternal);
        Assert.AreEqual("tcp", endpoint.Name);
        Assert.AreEqual(8813, endpoint.Port);
        Assert.AreEqual(ProtocolType.Tcp, endpoint.Protocol);
        Assert.AreEqual("tcp", endpoint.Transport);
        Assert.AreEqual("tcp", endpoint.UriScheme);

        var containerAnnotation = Assert.ContainsSingle(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.AreEqual(ValkeyContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.AreEqual(ValkeyContainerImageTags.Image, containerAnnotation.Image);
        Assert.AreEqual(ValkeyContainerImageTags.Registry, containerAnnotation.Registry);
    }

    [TestMethod]
    public async Task ValkeyCreatesConnectionStringWithDefaultPassword()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddValkey("myValkey")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000));

        await using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.ContainsSingle(appModel.Resources.OfType<IResourceWithConnectionString>());
        var connectionString = await connectionStringResource.GetConnectionStringAsync(default);
        Assert.AreEqual("{myValkey.bindings.tcp.host}:{myValkey.bindings.tcp.port},password={myValkey-password.value}", connectionStringResource.ConnectionStringExpression.ValueExpression);
        Assert.StartsWith("localhost:2000", connectionString);
    }

    [TestMethod]
    public void ValkeyCreatesConnectionStringWithPassword()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var password = "p@ssw0rd1";
        var pass = appBuilder.AddParameter("pass", password);
        appBuilder.AddValkey("myValkey", password: pass);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.ContainsSingle(appModel.Resources.OfType<IResourceWithConnectionString>());
        Assert.AreEqual("{myValkey.bindings.tcp.host}:{myValkey.bindings.tcp.port},password={pass.value}", connectionStringResource.ConnectionStringExpression.ValueExpression);
    }

    [TestMethod]
    public void ValkeyCreatesConnectionStringWithPasswordAndPort()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var password = "p@ssw0rd1";
        var pass = appBuilder.AddParameter("pass", password);
        appBuilder.AddValkey("myValkey", port: 3000, password: pass);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.ContainsSingle(appModel.Resources.OfType<IResourceWithConnectionString>());
        Assert.AreEqual("{myValkey.bindings.tcp.host}:{myValkey.bindings.tcp.port},password={pass.value}", connectionStringResource.ConnectionStringExpression.ValueExpression);
    }

    [TestMethod]
    public async Task VerifyWithoutPasswordManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var valkey = builder.AddValkey("myValkey");

        var manifest = await ManifestUtils.GetManifest(valkey.Resource);

        var expectedManifest = $$"""
                                 {
                                   "type": "container.v0",
                                   "connectionString": "{myValkey.bindings.tcp.host}:{myValkey.bindings.tcp.port},password={myValkey-password.value}",
                                   "image": "{{ValkeyContainerImageTags.Registry}}/{{ValkeyContainerImageTags.Image}}:{{ValkeyContainerImageTags.Tag}}",
                                   "entrypoint": "/bin/sh",
                                   "args": [
                                     "-c",
                                     "valkey-server --requirepass $VALKEY_PASSWORD"
                                   ],
                                   "env": {
                                     "VALKEY_PASSWORD": "{myValkey-password.value}"
                                   },
                                   "bindings": {
                                     "tcp": {
                                       "scheme": "tcp",
                                       "protocol": "tcp",
                                       "transport": "tcp",
                                       "targetPort": 6379
                                     }
                                   }
                                 }
                                 """;
        Assert.AreEqual(expectedManifest, manifest.ToString());
    }

    [TestMethod]
    public async Task VerifyWithPasswordManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var password = "p@ssw0rd1";
        builder.Configuration["Parameters:pass"] = password;

        var pass = builder.AddParameter("pass");
        var valkey = builder.AddValkey("myValkey", password: pass);
        var manifest = await ManifestUtils.GetManifest(valkey.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "{myValkey.bindings.tcp.host}:{myValkey.bindings.tcp.port},password={pass.value}",
              "image": "{{ValkeyContainerImageTags.Registry}}/{{ValkeyContainerImageTags.Image}}:{{ValkeyContainerImageTags.Tag}}",
              "entrypoint": "/bin/sh",
              "args": [
                "-c",
                "valkey-server --requirepass $VALKEY_PASSWORD"
              ],
              "env": {
                "VALKEY_PASSWORD": "{pass.value}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 6379
                }
              }
            }
            """;
        Assert.AreEqual(expectedManifest, manifest.ToString());
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(true)]
    [DataRow(false)]
    public void WithDataVolumeAddsVolumeAnnotation(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var valkey = builder.AddValkey("myValkey");
        if (isReadOnly.HasValue)
        {
            valkey.WithDataVolume(isReadOnly: isReadOnly.Value);
        }
        else
        {
            valkey.WithDataVolume();
        }

        var volumeAnnotation = valkey.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.AreEqual($"{builder.GetVolumePrefix()}-myValkey-data", volumeAnnotation.Source);
        Assert.AreEqual("/data", volumeAnnotation.Target);
        Assert.AreEqual(ContainerMountType.Volume, volumeAnnotation.Type);
        Assert.AreEqual(isReadOnly ?? false, volumeAnnotation.IsReadOnly);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(true)]
    [DataRow(false)]
    public void WithDataBindMountAddsMountAnnotation(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var valkey = builder.AddValkey("myValkeydata");
        if (isReadOnly.HasValue)
        {
            valkey.WithDataBindMount("myValkeydata", isReadOnly: isReadOnly.Value);
        }
        else
        {
            valkey.WithDataBindMount("myValkeydata");
        }

        var volumeAnnotation = valkey.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.AreEqual(Path.Combine(builder.AppHostDirectory, "myValkeydata"), volumeAnnotation.Source);
        Assert.AreEqual("/data", volumeAnnotation.Target);
        Assert.AreEqual(ContainerMountType.BindMount, volumeAnnotation.Type);
        Assert.AreEqual(isReadOnly ?? false, volumeAnnotation.IsReadOnly);
    }

    [TestMethod]
    public async Task WithDataVolumeAddsPersistenceAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var valkey = builder.AddValkey("myValkey")
                              .WithDataVolume();

        Assert.IsTrue(valkey.Resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var argsCallbacks));

        var args = await GetCommandLineArgs(valkey);
        Assert.Contains("--save 60 1", args);
    }

    [TestMethod]
    public async Task WithDataVolumeDoesNotAddPersistenceAnnotationIfIsReadOnly()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var valkey = builder.AddValkey("myValkey")
                           .WithDataVolume(isReadOnly: true);

        var args = await GetCommandLineArgs(valkey);
        Assert.DoesNotContain("--save", args);
    }

    [TestMethod]
    public async Task WithDataBindMountAddsPersistenceAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var valkey = builder.AddValkey("myValkey")
                           .WithDataBindMount("myvalkeydata");

        Assert.IsTrue(valkey.Resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var argsCallbacks));

        var args = await GetCommandLineArgs(valkey);
        Assert.Contains("--save 60 1", args);
    }

    [TestMethod]
    public async Task WithDataBindMountDoesNotAddPersistenceAnnotationIfIsReadOnly()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var valkey = builder.AddValkey("myValkey")
                           .WithDataBindMount("myvalkeydata", isReadOnly: true);

        var args = await GetCommandLineArgs(valkey);
        Assert.DoesNotContain("--save", args);
    }

    [TestMethod]
    public async Task WithPersistenceReplacesPreviousAnnotationInstances()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var valkey = builder.AddValkey("myValkey")
                           .WithDataVolume()
                           .WithPersistence(TimeSpan.FromSeconds(10), 2);

        Assert.IsTrue(valkey.Resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var argsCallbacks));

        var args = await GetCommandLineArgs(valkey);
        Assert.Contains("--save 10 2", args);

        // ensure `--save` is not added twice
        var saveIndex = args.IndexOf("--save");
        Assert.DoesNotContain("--save", args.Substring(saveIndex + 1));
    }

    [TestMethod]
    public void WithPersistenceAddsCommandLineArgsAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var valkey = builder.AddValkey("myValkey")
                           .WithPersistence(TimeSpan.FromSeconds(60));

        Assert.IsTrue(valkey.Resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var argsAnnotations));
        Assert.IsNotNull(argsAnnotations.SingleOrDefault());
    }

    [TestMethod]
    public async Task AddValkeyContainerWithPasswordAnnotationMetadata()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var password = "p@ssw0rd1";
        var pass = builder.AddParameter("pass", password);
        var valkey = builder.
            AddValkey("myValkey", password: pass)
           .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5001));

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.ContainsSingle(appModel.Resources.OfType<ValkeyResource>());

        var connectionStringResource = Assert.ContainsSingle(appModel.Resources.OfType<IResourceWithConnectionString>());
        var connectionString = await connectionStringResource.GetConnectionStringAsync(default);
        Assert.AreEqual("{myValkey.bindings.tcp.host}:{myValkey.bindings.tcp.port},password={pass.value}", connectionStringResource.ConnectionStringExpression.ValueExpression);
        Assert.StartsWith($"localhost:5001,password={password}", connectionString);
    }

    private static async Task<string> GetCommandLineArgs(IResourceBuilder<ValkeyResource> builder)
    {
        var args = await ArgumentEvaluator.GetArgumentListAsync(builder.Resource);
        return string.Join(" ", args);
    }
}
