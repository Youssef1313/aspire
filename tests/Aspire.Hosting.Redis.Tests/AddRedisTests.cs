// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Redis.Tests;

[TestClass]
public class AddRedisTests
{
    [TestMethod]
    public void AddRedisAddsHealthCheckAnnotationToResource()
    {
        var builder = DistributedApplication.CreateBuilder();
        var redis = builder.AddRedis("redis");
        Assert.ContainsSingle(redis.Resource.Annotations, a => a is HealthCheckAnnotation hca && hca.Key == "redis_check");
    }

    [TestMethod]
    public void AddRedisContainerWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddRedis("myRedis").PublishAsContainer();

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.ContainsSingle(appModel.Resources.OfType<RedisResource>());
        Assert.AreEqual("myRedis", containerResource.Name);

        var endpoint = Assert.ContainsSingle(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.AreEqual(6379, endpoint.TargetPort);
        Assert.IsFalse(endpoint.IsExternal);
        Assert.AreEqual("tcp", endpoint.Name);
        Assert.IsNull(endpoint.Port);
        Assert.AreEqual(ProtocolType.Tcp, endpoint.Protocol);
        Assert.AreEqual("tcp", endpoint.Transport);
        Assert.AreEqual("tcp", endpoint.UriScheme);

        var containerAnnotation = Assert.ContainsSingle(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.AreEqual(RedisContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.AreEqual(RedisContainerImageTags.Image, containerAnnotation.Image);
        Assert.AreEqual(RedisContainerImageTags.Registry, containerAnnotation.Registry);
    }

    [TestMethod]
    public void AddRedisContainerAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddRedis("myRedis", port: 9813);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.ContainsSingle(appModel.Resources.OfType<RedisResource>());
        Assert.AreEqual("myRedis", containerResource.Name);

        var endpoint = Assert.ContainsSingle(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.AreEqual(6379, endpoint.TargetPort);
        Assert.IsFalse(endpoint.IsExternal);
        Assert.AreEqual("tcp", endpoint.Name);
        Assert.AreEqual(9813, endpoint.Port);
        Assert.AreEqual(ProtocolType.Tcp, endpoint.Protocol);
        Assert.AreEqual("tcp", endpoint.Transport);
        Assert.AreEqual("tcp", endpoint.UriScheme);

        var containerAnnotation = Assert.ContainsSingle(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.AreEqual(RedisContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.AreEqual(RedisContainerImageTags.Image, containerAnnotation.Image);
        Assert.AreEqual(RedisContainerImageTags.Registry, containerAnnotation.Registry);
    }

    [TestMethod]
    public void RedisCreatesConnectionStringWithPassword()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var password = "p@ssw0rd1";
        var pass = appBuilder.AddParameter("pass", password);
        appBuilder.AddRedis("myRedis", password: pass);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.ContainsSingle(appModel.Resources.OfType<IResourceWithConnectionString>());
        Assert.AreEqual("{myRedis.bindings.tcp.host}:{myRedis.bindings.tcp.port},password={pass.value}", connectionStringResource.ConnectionStringExpression.ValueExpression);
    }

    [TestMethod]
    public void RedisCreatesConnectionStringWithPasswordAndPort()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var password = "p@ssw0rd1";
        var pass = appBuilder.AddParameter("pass", password);
        appBuilder.AddRedis("myRedis", port: 3000, password: pass);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.ContainsSingle(appModel.Resources.OfType<IResourceWithConnectionString>());
        Assert.AreEqual("{myRedis.bindings.tcp.host}:{myRedis.bindings.tcp.port},password={pass.value}", connectionStringResource.ConnectionStringExpression.ValueExpression);
    }

    [TestMethod]
    public async Task RedisCreatesConnectionStringWithDefaultPassword()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddRedis("myRedis")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000));

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.ContainsSingle(appModel.Resources.OfType<IResourceWithConnectionString>());
        var connectionString = await connectionStringResource.GetConnectionStringAsync(default);
        Assert.AreEqual("{myRedis.bindings.tcp.host}:{myRedis.bindings.tcp.port},password={myRedis-password.value}", connectionStringResource.ConnectionStringExpression.ValueExpression);
        StringAssert.StartsWith(connectionString, "localhost:2000");
    }

    [TestMethod]
    public async Task VerifyWithoutPasswordManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddRedis("redis");

        var manifest = await ManifestUtils.GetManifest(redis.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "{redis.bindings.tcp.host}:{redis.bindings.tcp.port},password={redis-password.value}",
              "image": "{{RedisContainerImageTags.Registry}}/{{RedisContainerImageTags.Image}}:{{RedisContainerImageTags.Tag}}",
              "entrypoint": "/bin/sh",
              "args": [
                "-c",
                "redis-server --requirepass $REDIS_PASSWORD"
              ],
              "env": {
                "REDIS_PASSWORD": "{redis-password.value}"
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
        var redis = builder.AddRedis("redis", password: pass);
        var manifest = await ManifestUtils.GetManifest(redis.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "{redis.bindings.tcp.host}:{redis.bindings.tcp.port},password={pass.value}",
              "image": "{{RedisContainerImageTags.Registry}}/{{RedisContainerImageTags.Image}}:{{RedisContainerImageTags.Tag}}",
              "entrypoint": "/bin/sh",
              "args": [
                "-c",
                "redis-server --requirepass $REDIS_PASSWORD"
              ],
              "env": {
                "REDIS_PASSWORD": "{pass.value}"
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
    public async Task VerifyWithPasswordValueNotProvidedManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var pass = builder.AddParameter("pass");
        var redis = builder.AddRedis("redis", password: pass);
        var manifest = await ManifestUtils.GetManifest(redis.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "{redis.bindings.tcp.host}:{redis.bindings.tcp.port},password={pass.value}",
              "image": "{{RedisContainerImageTags.Registry}}/{{RedisContainerImageTags.Image}}:{{RedisContainerImageTags.Tag}}",
              "entrypoint": "/bin/sh",
              "args": [
                "-c",
                "redis-server --requirepass $REDIS_PASSWORD"
              ],
              "env": {
                "REDIS_PASSWORD": "{pass.value}"
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
    public void WithRedisCommanderAddsRedisCommanderResource()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddRedis("myredis1").WithRedisCommander();
        builder.AddRedis("myredis2").WithRedisCommander();

        Assert.ContainsSingle(builder.Resources.OfType<RedisCommanderResource>());
    }

    [TestMethod]
    public void WithRedisInsightAddsWithRedisInsightResource()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddRedis("myredis1").WithRedisInsight();
        builder.AddRedis("myredis2").WithRedisInsight();

        Assert.ContainsSingle(builder.Resources.OfType<RedisInsightResource>());
    }

    [TestMethod]
    public void WithRedisCommanderSupportsChangingContainerImageValues()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddRedis("myredis").WithRedisCommander(c =>
        {
            c.WithImageRegistry("example.mycompany.com");
            c.WithImage("customrediscommander");
            c.WithImageTag("someothertag");
        });

        var resource = Assert.ContainsSingle(builder.Resources.OfType<RedisCommanderResource>());
        var containerAnnotation = Assert.ContainsSingle(resource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.AreEqual("example.mycompany.com", containerAnnotation.Registry);
        Assert.AreEqual("customrediscommander", containerAnnotation.Image);
        Assert.AreEqual("someothertag", containerAnnotation.Tag);
    }

    [TestMethod]
    public void WithRedisInsightSupportsChangingContainerImageValues()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddRedis("myredis").WithRedisInsight(c =>
        {
            c.WithImageRegistry("example.mycompany.com");
            c.WithImage("customrediscommander");
            c.WithImageTag("someothertag");
        });

        var resource = Assert.ContainsSingle(builder.Resources.OfType<RedisInsightResource>());
        var containerAnnotation = Assert.ContainsSingle(resource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.AreEqual("example.mycompany.com", containerAnnotation.Registry);
        Assert.AreEqual("customrediscommander", containerAnnotation.Image);
        Assert.AreEqual("someothertag", containerAnnotation.Tag);
    }

    [TestMethod]
    public void WithRedisCommanderSupportsChangingHostPort()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddRedis("myredis").WithRedisCommander(c =>
        {
            c.WithHostPort(1000);
        });

        var resource = Assert.ContainsSingle(builder.Resources.OfType<RedisCommanderResource>());
        var endpoint = Assert.ContainsSingle(resource.Annotations.OfType<EndpointAnnotation>());
        Assert.AreEqual(1000, endpoint.Port);
    }

    [TestMethod]
    public void WithRedisInsightSupportsChangingHostPort()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddRedis("myredis").WithRedisInsight(c =>
        {
            c.WithHostPort(1000);
        });

        var resource = Assert.ContainsSingle(builder.Resources.OfType<RedisInsightResource>());
        var endpoint = Assert.ContainsSingle(resource.Annotations.OfType<EndpointAnnotation>());
        Assert.AreEqual(1000, endpoint.Port);
    }

    [TestMethod]
    public async Task SingleRedisInstanceWithoutPasswordProducesCorrectRedisHostsVariable()
    {
        var builder = DistributedApplication.CreateBuilder();
        var redis = builder.AddRedis("myredis1").WithRedisCommander();
        using var app = builder.Build();

        // Add fake allocated endpoints.
        redis.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5001));

        await builder.Eventing.PublishAsync<AfterEndpointsAllocatedEvent>(new(app.Services, app.Services.GetRequiredService<DistributedApplicationModel>()));

        var commander = builder.Resources.Single(r => r.Name.EndsWith("-commander"));

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            commander,
            DistributedApplicationOperation.Run,
            TestServiceProvider.Instance);

        Assert.AreEqual($"myredis1:{redis.Resource.Name}:6379:0:{redis.Resource.PasswordParameter?.Value}", config["REDIS_HOSTS"]);
    }

    [TestMethod]
    public async Task SingleRedisInstanceWithPasswordProducesCorrectRedisHostsVariable()
    {
        var builder = DistributedApplication.CreateBuilder();
        var password = "p@ssw0rd1";
        var pass = builder.AddParameter("pass", password);
        var redis = builder.AddRedis("myredis1", password: pass).WithRedisCommander();
        using var app = builder.Build();

        // Add fake allocated endpoints.
        redis.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5001));

        await builder.Eventing.PublishAsync<AfterEndpointsAllocatedEvent>(new(app.Services, app.Services.GetRequiredService<DistributedApplicationModel>()));

        var commander = builder.Resources.Single(r => r.Name.EndsWith("-commander"));

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(commander);

        Assert.AreEqual($"myredis1:{redis.Resource.Name}:6379:0:{password}", config["REDIS_HOSTS"]);
    }

    [TestMethod]
    public async Task MultipleRedisInstanceProducesCorrectRedisHostsVariable()
    {
        var builder = DistributedApplication.CreateBuilder();
        var redis1 = builder.AddRedis("myredis1").WithRedisCommander();
        var redis2 = builder.AddRedis("myredis2").WithRedisCommander();
        using var app = builder.Build();

        // Add fake allocated endpoints.
        redis1.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5001));
        redis2.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5002, "host2"));

        await builder.Eventing.PublishAsync<AfterEndpointsAllocatedEvent>(new (app.Services, app.Services.GetRequiredService<DistributedApplicationModel>()));

        var commander = builder.Resources.Single(r => r.Name.EndsWith("-commander"));

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            commander,
            DistributedApplicationOperation.Run,
            TestServiceProvider.Instance);

        Assert.AreEqual($"myredis1:{redis1.Resource.Name}:6379:0:{redis1.Resource.PasswordParameter?.Value},myredis2:myredis2:6379:0:{redis2.Resource.PasswordParameter?.Value}", config["REDIS_HOSTS"]);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(true)]
    [DataRow(false)]
    public void WithDataVolumeAddsVolumeAnnotation(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddRedis("myRedis");
        if (isReadOnly.HasValue)
        {
            redis.WithDataVolume(isReadOnly: isReadOnly.Value);
        }
        else
        {
            redis.WithDataVolume();
        }

        var volumeAnnotation = redis.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.AreEqual($"{builder.GetVolumePrefix()}-myRedis-data", volumeAnnotation.Source);
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
        var redis = builder.AddRedis("myRedis");
        if (isReadOnly.HasValue)
        {
            redis.WithDataBindMount("mydata", isReadOnly: isReadOnly.Value);
        }
        else
        {
            redis.WithDataBindMount("mydata");
        }

        var volumeAnnotation = redis.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.AreEqual(Path.Combine(builder.AppHostDirectory, "mydata"), volumeAnnotation.Source);
        Assert.AreEqual("/data", volumeAnnotation.Target);
        Assert.AreEqual(ContainerMountType.BindMount, volumeAnnotation.Type);
        Assert.AreEqual(isReadOnly ?? false, volumeAnnotation.IsReadOnly);
    }

    [TestMethod]
    public async Task WithDataVolumeAddsPersistenceAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddRedis("myRedis")
                              .WithDataVolume();

        var args = await GetCommandLineArgs(redis);
        Assert.Contains("--save 60 1", args);
    }

    [TestMethod]
    public async Task WithDataVolumeDoesNotAddPersistenceAnnotationIfIsReadOnly()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddRedis("myRedis")
                           .WithDataVolume(isReadOnly: true);

        var args = await GetCommandLineArgs(redis);
        Assert.DoesNotContain("--save", args);
    }

    [TestMethod]
    public async Task WithDataBindMountAddsPersistenceAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddRedis("myRedis")
                           .WithDataBindMount("myredisdata");

        var args = await GetCommandLineArgs(redis);
        Assert.Contains("--save 60 1", args);
    }

    [TestMethod]
    public async Task WithDataBindMountDoesNotAddPersistenceAnnotationIfIsReadOnly()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddRedis("myRedis")
                           .WithDataBindMount("myredisdata", isReadOnly: true);

        var args = await GetCommandLineArgs(redis);
        Assert.DoesNotContain("--save", args);
    }

    [TestMethod]
    public async Task WithPersistenceReplacesPreviousAnnotationInstances()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddRedis("myRedis")
                           .WithDataVolume()
                           .WithPersistence(TimeSpan.FromSeconds(10), 2);

        var args = await GetCommandLineArgs(redis);
        Assert.Contains("--save 10 2", args);

        // ensure `--save` is not added twice
        var saveIndex = args.IndexOf("--save");
        Assert.DoesNotContain("--save", args.Substring(saveIndex + 1));
    }

    private static async Task<string> GetCommandLineArgs(IResourceBuilder<RedisResource> builder)
    {
        var args = await ArgumentEvaluator.GetArgumentListAsync(builder.Resource);
        return string.Join(" ", args);
    }

    [TestMethod]
    public void WithPersistenceAddsCommandLineArgsAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddRedis("myRedis")
                           .WithPersistence(TimeSpan.FromSeconds(60));

        Assert.IsTrue(redis.Resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var argsAnnotations));
        Assert.IsNotNull(argsAnnotations.SingleOrDefault());
    }

    [TestMethod]
    public async Task AddRedisContainerWithPasswordAnnotationMetadata()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var password = "p@ssw0rd1";
        var pass = builder.AddParameter("pass", password);
        var redis = builder.
            AddRedis("myRedis", password: pass)
           .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5001));

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.ContainsSingle(appModel.Resources.OfType<RedisResource>());

        var connectionStringResource = Assert.ContainsSingle(appModel.Resources.OfType<IResourceWithConnectionString>());
        var connectionString = await connectionStringResource.GetConnectionStringAsync(default);
        Assert.AreEqual("{myRedis.bindings.tcp.host}:{myRedis.bindings.tcp.port},password={pass.value}", connectionStringResource.ConnectionStringExpression.ValueExpression);
        StringAssert.StartsWith(connectionString, $"localhost:5001,password={password}");
    }
}
