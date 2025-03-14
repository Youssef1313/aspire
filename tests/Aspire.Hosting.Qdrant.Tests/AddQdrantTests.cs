// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Qdrant.Tests;

[TestClass]
public class AddQdrantTests
{
    private const int QdrantPortGrpc = 6334;
    private const int QdrantPortHttp = 6333;

    [TestMethod]
    public void AddQdrantAddsGeneratedApiKeyParameterWithUserSecretsParameterDefaultInRunMode()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var qd = appBuilder.AddQdrant("qd");

        Assert.AreEqual("Aspire.Hosting.ApplicationModel.UserSecretsParameterDefault", qd.Resource.ApiKeyParameter.Default?.GetType().FullName);
    }

    [TestMethod]
    public void AddQdrantDoesNotAddGeneratedPasswordParameterWithUserSecretsParameterDefaultInPublishMode()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var qd = appBuilder.AddQdrant("qd");

        Assert.AreNotEqual("Aspire.Hosting.ApplicationModel.UserSecretsParameterDefault", qd.Resource.ApiKeyParameter.Default?.GetType().FullName);
    }

    [TestMethod]
    public async Task AddQdrantWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddQdrant("my-qdrant");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.ContainsSingle(appModel.GetContainerResources());
        Assert.AreEqual("my-qdrant", containerResource.Name);

        var containerAnnotation = Assert.ContainsSingle(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.AreEqual(QdrantContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.AreEqual(QdrantContainerImageTags.Image, containerAnnotation.Image);
        Assert.AreEqual(QdrantContainerImageTags.Registry, containerAnnotation.Registry);

        var endpoint = containerResource.Annotations.OfType<EndpointAnnotation>()
            .FirstOrDefault(e => e.Name == "grpc");
        Assert.IsNotNull(endpoint);
        Assert.AreEqual(QdrantPortGrpc, endpoint.TargetPort);
        Assert.IsFalse(endpoint.IsExternal);
        Assert.AreEqual("grpc", endpoint.Name);
        Assert.IsNull(endpoint.Port);
        Assert.AreEqual(ProtocolType.Tcp, endpoint.Protocol);
        Assert.AreEqual("http2", endpoint.Transport);
        Assert.AreEqual("http", endpoint.UriScheme);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(containerResource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.That.Collection(config,
            env =>
            {
                Assert.AreEqual("QDRANT__SERVICE__API_KEY", env.Key);
                Assert.IsFalse(string.IsNullOrEmpty(env.Value));
            });
    }

    [TestMethod]
    public void AddQdrantWithDefaultsAndDashboardAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddQdrant("my-qdrant");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.ContainsSingle(appModel.GetContainerResources());
        Assert.AreEqual("my-qdrant", containerResource.Name);

        var containerAnnotation = Assert.ContainsSingle(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.AreEqual(QdrantContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.AreEqual(QdrantContainerImageTags.Image, containerAnnotation.Image);
        Assert.AreEqual(QdrantContainerImageTags.Registry, containerAnnotation.Registry);

        var endpoint = containerResource.Annotations.OfType<EndpointAnnotation>()
            .FirstOrDefault(e => e.Name == "http");

        Assert.IsNotNull(endpoint);
        Assert.AreEqual(QdrantPortHttp, endpoint.TargetPort);
        Assert.IsFalse(endpoint.IsExternal);
        Assert.AreEqual("http", endpoint.Name);
        Assert.IsNull(endpoint.Port);
        Assert.AreEqual(ProtocolType.Tcp, endpoint.Protocol);
        Assert.AreEqual("http", endpoint.Transport);
        Assert.AreEqual("http", endpoint.UriScheme);
    }

    [TestMethod]
    public async Task AddQdrantAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var pass = appBuilder.AddParameter("pass", "pass");
        appBuilder.AddQdrant("my-qdrant", apiKey: pass);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.ContainsSingle(appModel.GetContainerResources());
        Assert.AreEqual("my-qdrant", containerResource.Name);

        var containerAnnotation = Assert.ContainsSingle(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.AreEqual(QdrantContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.AreEqual(QdrantContainerImageTags.Image, containerAnnotation.Image);
        Assert.AreEqual(QdrantContainerImageTags.Registry, containerAnnotation.Registry);

        var endpoint = containerResource.Annotations.OfType<EndpointAnnotation>()
            .FirstOrDefault(e => e.Name == "grpc");
        Assert.IsNotNull(endpoint);
        Assert.AreEqual(QdrantPortGrpc, endpoint.TargetPort);
        Assert.IsFalse(endpoint.IsExternal);
        Assert.AreEqual("grpc", endpoint.Name);
        Assert.IsNull(endpoint.Port);
        Assert.AreEqual(ProtocolType.Tcp, endpoint.Protocol);
        Assert.AreEqual("http2", endpoint.Transport);
        Assert.AreEqual("http", endpoint.UriScheme);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(containerResource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.That.Collection(config,
            env =>
            {
                Assert.AreEqual("QDRANT__SERVICE__API_KEY", env.Key);
                Assert.AreEqual("pass", env.Value);
            });
    }

    [TestMethod]
    public async Task QdrantCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var pass = appBuilder.AddParameter("pass", "pass");

        var qdrant = appBuilder.AddQdrant("my-qdrant", pass)
                                 .WithEndpoint("grpc", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 6334));

        var connectionStringResource = qdrant.Resource as IResourceWithConnectionString;

        var connectionString = await connectionStringResource.GetConnectionStringAsync();
        Assert.AreEqual($"Endpoint=http://localhost:6334;Key=pass", connectionString);
    }

    [TestMethod]
    public async Task QdrantClientAppWithReferenceContainsConnectionStrings()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var pass = appBuilder.AddParameter("pass", "pass");

        var qdrant = appBuilder.AddQdrant("my-qdrant", pass)
            .WithEndpoint("grpc", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 6334))
            .WithEndpoint("http", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 6333));

        var projectA = appBuilder.AddProject<ProjectA>("projecta", o => o.ExcludeLaunchProfile = true)
            .WithReference(qdrant);

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(projectA.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("ConnectionStrings__"));
        Assert.AreEqual(2, servicesKeysCount);

        Assert.Contains(config, kvp => kvp.Key == "ConnectionStrings__my-qdrant" && kvp.Value == "Endpoint=http://localhost:6334;Key=pass");
        Assert.Contains(config, kvp => kvp.Key == "ConnectionStrings__my-qdrant_http" && kvp.Value == "Endpoint=http://localhost:6333;Key=pass");

        var container1 = appBuilder.AddContainer("container1", "fake")
            .WithReference(qdrant);

        // Call environment variable callbacks.
        var containerConfig = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(container1.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        var containerServicesKeysCount = containerConfig.Keys.Count(k => k.StartsWith("ConnectionStrings__"));
        Assert.AreEqual(2, containerServicesKeysCount);

        Assert.Contains(containerConfig, kvp => kvp.Key == "ConnectionStrings__my-qdrant" && kvp.Value == "Endpoint=http://my-qdrant:6334;Key=pass");
        Assert.Contains(containerConfig, kvp => kvp.Key == "ConnectionStrings__my-qdrant_http" && kvp.Value == "Endpoint=http://my-qdrant:6333;Key=pass");
    }

    [TestMethod]
    public async Task VerifyManifest()
    {
        var appBuilder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions() { Args = new string[] { "--publisher", "manifest" } } );
        var qdrant = appBuilder.AddQdrant("qdrant");

        var serverManifest = await ManifestUtils.GetManifest(qdrant.Resource); // using this method does not get any ExecutionContext.IsPublishMode changes

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "Endpoint={qdrant.bindings.grpc.url};Key={qdrant-Key.value}",
              "image": "{{QdrantContainerImageTags.Registry}}/{{QdrantContainerImageTags.Image}}:{{QdrantContainerImageTags.Tag}}",
              "env": {
                "QDRANT__SERVICE__API_KEY": "{qdrant-Key.value}",
                "QDRANT__SERVICE__ENABLE_STATIC_CONTENT": "0"
              },
              "bindings": {
                "grpc": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http2",
                  "targetPort": 6334
                },
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 6333
                }
              }
            }
            """;
        Assert.AreEqual(expectedManifest, serverManifest.ToString());
    }

    [TestMethod]
    public async Task VerifyManifestWithParameters()
    {
        var appBuilder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions() { Args = new string[] { "--publisher", "manifest" } });

        var apiKeyParameter = appBuilder.AddParameter("QdrantApiKey");
        var qdrant = appBuilder.AddQdrant("qdrant", apiKeyParameter);

        var serverManifest = await ManifestUtils.GetManifest(qdrant.Resource); // using this method does not get any ExecutionContext.IsPublishMode changes

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "Endpoint={qdrant.bindings.grpc.url};Key={QdrantApiKey.value}",
              "image": "{{QdrantContainerImageTags.Registry}}/{{QdrantContainerImageTags.Image}}:{{QdrantContainerImageTags.Tag}}",
              "env": {
                "QDRANT__SERVICE__API_KEY": "{QdrantApiKey.value}",
                "QDRANT__SERVICE__ENABLE_STATIC_CONTENT": "0"
              },
              "bindings": {
                "grpc": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http2",
                  "targetPort": 6334
                },
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 6333
                }
              }
            }
            """;
        Assert.AreEqual(expectedManifest, serverManifest.ToString());
    }

    [TestMethod]
    public void AddQdrantWithSpecifyingPorts()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var qdrant = builder.AddQdrant("my-qdrant", grpcPort: 5503, httpPort: 5504);

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var qdrantResource = Assert.ContainsSingle(appModel.Resources.OfType<QdrantServerResource>());
        Assert.AreEqual("my-qdrant", qdrantResource.Name);

        Assert.AreEqual(2, qdrantResource.Annotations.OfType<EndpointAnnotation>().Count());

        var grpcEndpoint = qdrantResource.Annotations.OfType<EndpointAnnotation>().Single(e => e.Name == "grpc");
        Assert.AreEqual(6334, grpcEndpoint.TargetPort);
        Assert.IsFalse(grpcEndpoint.IsExternal);
        Assert.AreEqual(5503, grpcEndpoint.Port);
        Assert.AreEqual(ProtocolType.Tcp, grpcEndpoint.Protocol);
        Assert.AreEqual("http2", grpcEndpoint.Transport);
        Assert.AreEqual("http", grpcEndpoint.UriScheme);

        var httpEndpoint = qdrantResource.Annotations.OfType<EndpointAnnotation>().Single(e => e.Name == "http");
        Assert.AreEqual(6333, httpEndpoint.TargetPort);
        Assert.IsFalse(httpEndpoint.IsExternal);
        Assert.AreEqual(5504, httpEndpoint.Port);
        Assert.AreEqual(ProtocolType.Tcp, httpEndpoint.Protocol);
        Assert.AreEqual("http", httpEndpoint.Transport);
        Assert.AreEqual("http", httpEndpoint.UriScheme);
    }

    private sealed class ProjectA : IProjectMetadata
    {
        public string ProjectPath => "projectA";
    }
}
