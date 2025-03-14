// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Elasticsearch;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Tests.Elasticsearch;

[TestClass]
public class AddElasticsearchTests
{
    [TestMethod]
    public async Task AddElasticsearchContainerWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddElasticsearch("elasticsearch");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.ContainsSingle(appModel.Resources.OfType<ElasticsearchResource>());
        Assert.AreEqual("elasticsearch", containerResource.Name);

        var endpoints = containerResource.Annotations.OfType<EndpointAnnotation>();
        Assert.AreEqual(2, endpoints.Count());

        var primaryEndpoint = Assert.ContainsSingle(endpoints, e => e.Name == "http");
        Assert.AreEqual(9200, primaryEndpoint.TargetPort);
        Assert.IsFalse(primaryEndpoint.IsExternal);
        Assert.AreEqual("http", primaryEndpoint.Name);
        Assert.IsNull(primaryEndpoint.Port);
        Assert.AreEqual(ProtocolType.Tcp, primaryEndpoint.Protocol);
        Assert.AreEqual("http", primaryEndpoint.Transport);
        Assert.AreEqual("http", primaryEndpoint.UriScheme);

        var internalEndpoint = Assert.ContainsSingle(endpoints, e => e.Name == "internal");
        Assert.AreEqual(9300, internalEndpoint.TargetPort);
        Assert.IsFalse(internalEndpoint.IsExternal);
        Assert.AreEqual("internal", internalEndpoint.Name);
        Assert.IsNull(internalEndpoint.Port);
        Assert.AreEqual(ProtocolType.Tcp, internalEndpoint.Protocol);
        Assert.AreEqual("tcp", internalEndpoint.Transport);
        Assert.AreEqual("tcp", internalEndpoint.UriScheme);

        var containerAnnotation = Assert.ContainsSingle(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.AreEqual(ElasticsearchContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.AreEqual(ElasticsearchContainerImageTags.Image, containerAnnotation.Image);
        Assert.AreEqual(ElasticsearchContainerImageTags.Registry, containerAnnotation.Registry);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(containerResource);

        Assert.That.Collection(config,
            env =>
            {
                Assert.AreEqual("discovery.type", env.Key);
                Assert.AreEqual("single-node", env.Value);
            },
            env =>
            {
                Assert.AreEqual("xpack.security.enabled", env.Key);
                Assert.AreEqual("true", env.Value);
            },
            env =>
            {
                Assert.AreEqual("ELASTIC_PASSWORD", env.Key);
                Assert.IsFalse(string.IsNullOrEmpty(env.Value));
            });
    }

    [TestMethod]
    public async Task AddElasticsearchContainerAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var pass = appBuilder.AddParameter("pass", "pass");
        appBuilder.AddElasticsearch("elasticsearch",pass);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.ContainsSingle(appModel.Resources.OfType<ElasticsearchResource>());
        Assert.AreEqual("elasticsearch", containerResource.Name);

        var endpoints = containerResource.Annotations.OfType<EndpointAnnotation>();
        Assert.AreEqual(2, endpoints.Count());

        var primaryEndpoint = Assert.ContainsSingle(endpoints, e => e.Name == "http");
        Assert.AreEqual(9200, primaryEndpoint.TargetPort);
        Assert.IsFalse(primaryEndpoint.IsExternal);
        Assert.AreEqual("http", primaryEndpoint.Name);
        Assert.IsNull(primaryEndpoint.Port);
        Assert.AreEqual(ProtocolType.Tcp, primaryEndpoint.Protocol);
        Assert.AreEqual("http", primaryEndpoint.Transport);
        Assert.AreEqual("http", primaryEndpoint.UriScheme);

        var internalEndpoint = Assert.ContainsSingle(endpoints, e => e.Name == "internal");
        Assert.AreEqual(9300, internalEndpoint.TargetPort);
        Assert.IsFalse(internalEndpoint.IsExternal);
        Assert.AreEqual("internal", internalEndpoint.Name);
        Assert.IsNull(internalEndpoint.Port);
        Assert.AreEqual(ProtocolType.Tcp, internalEndpoint.Protocol);
        Assert.AreEqual("tcp", internalEndpoint.Transport);
        Assert.AreEqual("tcp", internalEndpoint.UriScheme);

        var containerAnnotation = Assert.ContainsSingle(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.AreEqual(ElasticsearchContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.AreEqual(ElasticsearchContainerImageTags.Image, containerAnnotation.Image);
        Assert.AreEqual(ElasticsearchContainerImageTags.Registry, containerAnnotation.Registry);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(containerResource);

        Assert.That.Collection(config,
            env =>
            {
                Assert.AreEqual("discovery.type", env.Key);
                Assert.AreEqual("single-node", env.Value);
            },
            env =>
            {
                Assert.AreEqual("xpack.security.enabled", env.Key);
                Assert.AreEqual("true", env.Value);
            },
            env =>
            {
                Assert.AreEqual("ELASTIC_PASSWORD", env.Key);
                Assert.AreEqual("pass", env.Value);
            });
    }

    [TestMethod]
    public async Task ElasticsearchCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var elasticsearch = appBuilder
            .AddElasticsearch("elasticsearch")
            .WithEndpoint("http", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 27020));

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.ContainsSingle(appModel.Resources.OfType<ElasticsearchResource>()) as IResourceWithConnectionString;
        var connectionString = await connectionStringResource.GetConnectionStringAsync();

        Assert.AreEqual($"http://elastic:{elasticsearch.Resource.PasswordParameter.Value}@localhost:27020", connectionString);
        Assert.AreEqual("http://elastic:{elasticsearch-password.value}@{elasticsearch.bindings.http.host}:{elasticsearch.bindings.http.port}", connectionStringResource.ConnectionStringExpression.ValueExpression);
    }

    [TestMethod]
    public async Task VerifyManifestWithDefaultsAddsAnnotationMetadata()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var elasticsearch = appBuilder.AddElasticsearch("elasticsearch");

        var manifest = await ManifestUtils.GetManifest(elasticsearch.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "http://elastic:{elasticsearch-password.value}@{elasticsearch.bindings.http.host}:{elasticsearch.bindings.http.port}",
              "image": "{{ElasticsearchContainerImageTags.Registry}}/{{ElasticsearchContainerImageTags.Image}}:{{ElasticsearchContainerImageTags.Tag}}",
              "env": {
                "discovery.type": "single-node",
                "xpack.security.enabled": "true",
                "ELASTIC_PASSWORD": "{elasticsearch-password.value}"
              },
              "bindings": {
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 9200
                },
                "internal": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 9300
                }
              }
            }
            """;
        Assert.AreEqual(expectedManifest, manifest.ToString());
    }

    [TestMethod]
    public async Task VerifyManifestWithDataVolumeAddsAnnotationMetadata()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var elasticsearch = appBuilder.AddElasticsearch("elasticsearch")
            .WithDataVolume("data");

        var manifest = await ManifestUtils.GetManifest(elasticsearch.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "http://elastic:{elasticsearch-password.value}@{elasticsearch.bindings.http.host}:{elasticsearch.bindings.http.port}",
              "image": "{{ElasticsearchContainerImageTags.Registry}}/{{ElasticsearchContainerImageTags.Image}}:{{ElasticsearchContainerImageTags.Tag}}",
              "volumes": [
                {
                  "name": "data",
                  "target": "/usr/share/elasticsearch/data",
                  "readOnly": false
                }
              ],
              "env": {
                "discovery.type": "single-node",
                "xpack.security.enabled": "true",
                "ELASTIC_PASSWORD": "{elasticsearch-password.value}"
              },
              "bindings": {
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 9200
                },
                "internal": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 9300
                }
              }
            }
            """;
        Assert.AreEqual(expectedManifest, manifest.ToString());
    }
}
