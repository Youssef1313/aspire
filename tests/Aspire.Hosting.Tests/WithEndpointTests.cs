// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Tests;

[TestClass]
public class WithEndpointTests
{
    // copied from /src/Shared/StringComparers.cs to avoid ambiguous reference since StringComparers exists internally in multiple Hosting assemblies.
    private static StringComparison EndpointAnnotationName => StringComparison.OrdinalIgnoreCase;

    [TestMethod]
    public void WithEndpointInvokesCallback()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var projectA = builder.AddProject<ProjectA>("projecta")
                              .WithEndpoint(3000, 1000, name: "mybinding")
                              .WithEndpoint("mybinding", endpoint =>
                              {
                                  endpoint.Port = 2000;
                              });

        var endpoint = projectA.Resource.Annotations.OfType<EndpointAnnotation>()
            .Where(e => string.Equals(e.Name, "mybinding", EndpointAnnotationName)).Single();
        Assert.AreEqual(2000, endpoint.Port);
    }

    [TestMethod]
    public void WithEndpointMakesTargetPortEqualToPortIfProxyless()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var projectA = builder.AddProject<ProjectA>("projecta")
                              .WithEndpoint("mybinding", endpoint =>
                              {
                                  endpoint.Port = 2000;
                                  endpoint.IsProxied = false;
                              });

        var endpoint = projectA.Resource.Annotations.OfType<EndpointAnnotation>()
            .Where(e => string.Equals(e.Name, "mybinding", EndpointAnnotationName)).Single();

        // It should fall back to the Port value since TargetPort was not set
        Assert.AreEqual(2000, endpoint.TargetPort);

        // In Proxy mode, the fallback should not happen
        endpoint.IsProxied = true;
        Assert.IsNull(endpoint.TargetPort);

        // Back in proxy-less mode, it should fall back again
        endpoint.IsProxied = false;
        Assert.AreEqual(2000, endpoint.TargetPort);

        // Setting it to null explicitly should disable the override mechanism
        endpoint.TargetPort = null;
        Assert.IsNull(endpoint.TargetPort);

        // No fallback when setting TargetPort explicitly
        endpoint.TargetPort = 2001;
        Assert.AreEqual(2001, endpoint.TargetPort);
    }

    [TestMethod]
    public void WithEndpointMakesPortEqualToTargetPortIfProxyless()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var projectA = builder.AddProject<ProjectA>("projecta")
                              .WithEndpoint("mybinding", endpoint =>
                              {
                                  endpoint.TargetPort = 2000;
                                  endpoint.IsProxied = false;
                              });

        var endpoint = projectA.Resource.Annotations.OfType<EndpointAnnotation>()
            .Where(e => string.Equals(e.Name, "mybinding", EndpointAnnotationName)).Single();

        // It should fall back to the TargetPort value since Port was not set
        Assert.AreEqual(2000, endpoint.Port);

        // In Proxy mode, the fallback should not happen
        endpoint.IsProxied = true;
        Assert.IsNull(endpoint.Port);

        // Back in proxy-less mode, it should fall back again
        endpoint.IsProxied = false;
        Assert.AreEqual(2000, endpoint.Port);

        // Setting it to null explicitly should disable the override mechanism
        endpoint.Port = null;
        Assert.IsNull(endpoint.Port);

        // No fallback when setting Port explicitly
        endpoint.Port = 2001;
        Assert.AreEqual(2001, endpoint.Port);
    }

    [TestMethod]
    public void WithEndpointCallbackDoesNotRunIfEndpointDoesntExistAndCreateIfNotExistsIsFalse()
    {
        var executed = false;

        using var builder = TestDistributedApplicationBuilder.Create();

        var projectA = builder.AddProject<ProjectA>("projecta")
                              .WithEndpoint("mybinding", endpoint =>
                              {
                                  executed = true;
                              },
                              createIfNotExists: false);

        Assert.IsFalse(executed);
        Assert.IsFalse(projectA.Resource.TryGetAnnotationsOfType<EndpointAnnotation>(out var annotations));
    }

    [TestMethod]
    public void WithEndpointCallbackRunsIfEndpointDoesntExistAndCreateIfNotExistsIsDefault()
    {
        var executed = false;

        using var builder = TestDistributedApplicationBuilder.Create();

        var projectA = builder.AddProject<ProjectA>("projecta")
                              .WithEndpoint("mybinding", endpoint =>
                              {
                                  executed = true;
                              });

        Assert.IsTrue(executed);
        Assert.IsTrue(projectA.Resource.TryGetAnnotationsOfType<EndpointAnnotation>(out _));
    }

    [TestMethod]
    public void WithEndpointCallbackRunsIfEndpointDoesntExistAndCreateIfNotExistsIsTrue()
    {
        var executed = false;

        using var builder = TestDistributedApplicationBuilder.Create();

        var projectA = builder.AddProject<ProjectA>("projecta").WithEndpoint("mybinding", endpoint =>
        {
            executed = true;
        },
        createIfNotExists: true);

        Assert.IsTrue(executed);
        Assert.IsTrue(projectA.Resource.TryGetAnnotationsOfType<EndpointAnnotation>(out _));
    }

    [TestMethod]
    public void EndpointsWithTwoPortsSameNameThrows()
    {
        var ex = Assert.Throws<DistributedApplicationException>(() =>
        {
            using var builder = TestDistributedApplicationBuilder.Create();

            builder.AddProject<ProjectA>("projecta")
                    .WithHttpsEndpoint(3000, 1000, name: "mybinding")
                    .WithHttpsEndpoint(3000, 2000, name: "mybinding");
        });

        Assert.AreEqual("Endpoint with name 'mybinding' already exists. Endpoint name may not have been explicitly specified and was derived automatically from scheme argument (e.g. 'http', 'https', or 'tcp'). Multiple calls to WithEndpoint (and related methods) may result in a conflict if name argument is not specified. Each endpoint must have a unique name. For more information on networking in .NET Aspire see: https://aka.ms/dotnet/aspire/networking", ex.Message);
    }

    [TestMethod]
    public void AddingTwoEndpointsWithDefaultNames()
    {
        var ex = Assert.Throws<DistributedApplicationException>(() =>
        {
            using var builder = TestDistributedApplicationBuilder.Create();

            builder.AddProject<ProjectA>("projecta")
                    .WithHttpsEndpoint(3000, 1000)
                    .WithHttpsEndpoint(3000, 2000);
        });

        Assert.AreEqual("Endpoint with name 'https' already exists. Endpoint name may not have been explicitly specified and was derived automatically from scheme argument (e.g. 'http', 'https', or 'tcp'). Multiple calls to WithEndpoint (and related methods) may result in a conflict if name argument is not specified. Each endpoint must have a unique name. For more information on networking in .NET Aspire see: https://aka.ms/dotnet/aspire/networking", ex.Message);
    }

    [TestMethod]
    public void EndpointsWithSinglePortSameNameThrows()
    {
        var ex = Assert.Throws<DistributedApplicationException>(() =>
        {
            using var builder = TestDistributedApplicationBuilder.Create();
            builder.AddProject<ProjectB>("projectb")
                   .WithHttpsEndpoint(1000, name: "mybinding")
                   .WithHttpsEndpoint(2000, name: "mybinding");
        });

        Assert.AreEqual("Endpoint with name 'mybinding' already exists. Endpoint name may not have been explicitly specified and was derived automatically from scheme argument (e.g. 'http', 'https', or 'tcp'). Multiple calls to WithEndpoint (and related methods) may result in a conflict if name argument is not specified. Each endpoint must have a unique name. For more information on networking in .NET Aspire see: https://aka.ms/dotnet/aspire/networking", ex.Message);
    }

    [TestMethod]
    public async Task CanAddEndpointsWithContainerPortAndEnv()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.AddExecutable("foo", "foo", ".")
               .WithHttpEndpoint(targetPort: 3001, name: "mybinding", env: "PORT");

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var exeResources = appModel.GetExecutableResources();

        var resource = Assert.ContainsSingle(exeResources);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

        Assert.AreEqual("foo", resource.Name);
        var endpoints = resource.Annotations.OfType<EndpointAnnotation>().ToArray();
        Assert.ContainsSingle(endpoints);
        Assert.AreEqual("mybinding", endpoints[0].Name);
        Assert.AreEqual(3001, endpoints[0].TargetPort);
        Assert.AreEqual("http", endpoints[0].UriScheme);
        Assert.AreEqual("3001", config["PORT"]);
    }

    [TestMethod]
    public void GettingContainerHostNameFailsIfNoContainerHostNameSet()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("app", "image")
            .WithEndpoint("ep", e =>
            {
                e.AllocatedEndpoint = new(e, "localhost", 8031);
            });

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            return container.GetEndpoint("ep").ContainerHost;
        });

        Assert.AreEqual("The endpoint \"ep\" has no associated container host name.", ex.Message);
    }

    [TestMethod]
    public void WithExternalHttpEndpointsMarkExistingHttpEndpointsAsExternal()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("app", "image")
                               .WithEndpoint(name: "ep0")
                               .WithHttpEndpoint(name: "ep1")
                               .WithHttpsEndpoint(name: "ep2")
                               .WithExternalHttpEndpoints();

        var ep0 = container.GetEndpoint("ep0");
        var ep1 = container.GetEndpoint("ep1");
        var ep2 = container.GetEndpoint("ep2");

        Assert.IsFalse(ep0.EndpointAnnotation.IsExternal);
        Assert.IsTrue(ep1.EndpointAnnotation.IsExternal);
        Assert.IsTrue(ep2.EndpointAnnotation.IsExternal);
    }

    // Existing code...

    [TestMethod]
    public async Task VerifyManifestWithBothDifferentPortAndTargetPort()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("app", "image")
                               .WithEndpoint(name: "ep0", port: 8080, targetPort: 3000);

        var manifest = await ManifestUtils.GetManifest(container.Resource).DefaultTimeout();
        var expectedManifest =
            """
            {
              "type": "container.v0",
              "image": "image:latest",
              "bindings": {
                "ep0": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "port": 8080,
                  "targetPort": 3000
                }
              }
            }
            """;

        Assert.AreEqual(expectedManifest, manifest.ToString());
    }

    [TestMethod]
    public async Task VerifyManifestWithHttpPortWithTargetPort()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("app", "image")
                               .WithHttpEndpoint(name: "h1", targetPort: 3001);

        var manifest = await ManifestUtils.GetManifest(container.Resource).DefaultTimeout();
        var expectedManifest =
            """
            {
              "type": "container.v0",
              "image": "image:latest",
              "bindings": {
                "h1": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 3001
                }
              }
            }
            """;

        Assert.AreEqual(expectedManifest, manifest.ToString());
    }

    [TestMethod]
    public async Task VerifyManifestWithHttpsAndTargetPort()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("app", "image")
                               .WithHttpsEndpoint(name: "h2", targetPort: 3001);

        var manifest = await ManifestUtils.GetManifest(container.Resource).DefaultTimeout();
        var expectedManifest =
            """
            {
              "type": "container.v0",
              "image": "image:latest",
              "bindings": {
                "h2": {
                  "scheme": "https",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 3001
                }
              }
            }
            """;

        Assert.AreEqual(expectedManifest, manifest.ToString());
    }

    [TestMethod]
    public async Task VerifyManifestContainerWithHttpEndpointAndNoPortsAllocatesPort()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("app", "image")
                               .WithHttpEndpoint(name: "h3");

        var manifest = await ManifestUtils.GetManifest(container.Resource).DefaultTimeout();
        var expectedManifest =
            """
            {
              "type": "container.v0",
              "image": "image:latest",
              "bindings": {
                "h3": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 8000
                }
              }
            }
            """;

        Assert.AreEqual(expectedManifest, manifest.ToString());
    }

    [TestMethod]
    public async Task VerifyManifestContainerWithHttpsEndpointAllocatesPort()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("app", "image")
                               .WithHttpsEndpoint(name: "h4");

        var manifest = await ManifestUtils.GetManifest(container.Resource).DefaultTimeout();
        var expectedManifest =
            """
            {
              "type": "container.v0",
              "image": "image:latest",
              "bindings": {
                "h4": {
                  "scheme": "https",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 8000
                }
              }
            }
            """;

        Assert.AreEqual(expectedManifest, manifest.ToString());
    }

    [TestMethod]
    public async Task VerifyManifestWithHttpEndpointAndPortOnlySetsTargetPort()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("app", "image")
                               .WithHttpEndpoint(name: "otlp", port: 1004);

        var manifest = await ManifestUtils.GetManifest(container.Resource).DefaultTimeout();
        var expectedManifest =
            """
            {
              "type": "container.v0",
              "image": "image:latest",
              "bindings": {
                "otlp": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 1004
                }
              }
            }
            """;

        Assert.AreEqual(expectedManifest, manifest.ToString());
    }

    [TestMethod]
    public async Task VerifyManifestWithTcpEndpointAndNoPortAllocatesPort()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("app", "image")
                               .WithEndpoint(name: "custom");

        var manifest = await ManifestUtils.GetManifest(container.Resource).DefaultTimeout();
        var expectedManifest =
            """
            {
              "type": "container.v0",
              "image": "image:latest",
              "bindings": {
                "custom": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 8000
                }
              }
            }
            """;

        Assert.AreEqual(expectedManifest, manifest.ToString());
    }

    [TestMethod]
    public async Task VerifyManifestProjectWithDefaultHttpEndpointsDoesNotAllocatePort()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var project = builder.AddProject<TestProject>("proj")
            .WithHttpEndpoint(name: "hp")       // Won't get targetPort since it's the first http
            .WithHttpEndpoint(name: "hp2")      // Will get a targetPort
            .WithHttpsEndpoint(name: "hps")     // Won't get targetPort since it's the first https
            .WithHttpsEndpoint(name: "hps2")   // Will get a targetPort
            .WithEndpoint(scheme: "tcp", name: "tcp0");  // Will get a targetPort

        var manifest = await ManifestUtils.GetManifest(project.Resource).DefaultTimeout();

        var expectedManifest =
            """
            {
              "type": "project.v0",
              "path": "projectpath",
              "env": {
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES": "true",
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES": "true",
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY": "in_memory",
                "ASPNETCORE_FORWARDEDHEADERS_ENABLED": "true",
                "HTTP_PORTS": "{proj.bindings.hp.targetPort};{proj.bindings.hp2.targetPort}",
                "HTTPS_PORTS": "{proj.bindings.hps.targetPort};{proj.bindings.hps2.targetPort}"
              },
              "bindings": {
                "hp": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http"
                },
                "hp2": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 8000
                },
                "hps": {
                  "scheme": "https",
                  "protocol": "tcp",
                  "transport": "http"
                },
                "hps2": {
                  "scheme": "https",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 8001
                },
                "tcp0": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 8002
                }
              }
            }
            """;

        Assert.AreEqual(expectedManifest, manifest.ToString());
    }

    [TestMethod]
    public async Task VerifyManifestProjectWithEndpointsSetsPortsEnvVariables()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var project = builder.AddProject<TestProject>("proj")
            .WithHttpEndpoint()
            .WithHttpEndpoint(name: "hp1", port: 5001)
            .WithHttpEndpoint(name: "hp2", port: 5002, targetPort: 5003)
            .WithHttpEndpoint(name: "hp3", targetPort: 5004)
            .WithHttpEndpoint(name: "hp4")
            .WithHttpEndpoint(name: "dontinjectme")
            .WithHttpsEndpoint()
            .WithHttpsEndpoint(name: "hps1", port: 7001)
            .WithHttpsEndpoint(name: "hps2", port: 7002, targetPort: 7003)
            .WithHttpsEndpoint(name: "hps3", targetPort: 7004)
            .WithHttpsEndpoint(name: "hps4", targetPort: 7005)
            // Should not be included in HTTP_PORTS
            .WithEndpointsInEnvironment(e => e.Name != "dontinjectme");

        var manifest = await ManifestUtils.GetManifest(project.Resource).DefaultTimeout();

        var expectedEnv =
            """
            {
              "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES": "true",
              "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES": "true",
              "OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY": "in_memory",
              "ASPNETCORE_FORWARDEDHEADERS_ENABLED": "true",
              "HTTP_PORTS": "{proj.bindings.http.targetPort};{proj.bindings.hp1.targetPort};{proj.bindings.hp2.targetPort};{proj.bindings.hp3.targetPort};{proj.bindings.hp4.targetPort}",
              "HTTPS_PORTS": "{proj.bindings.https.targetPort};{proj.bindings.hps1.targetPort};{proj.bindings.hps2.targetPort};{proj.bindings.hps3.targetPort};{proj.bindings.hps4.targetPort}"
            }
            """;

        Assert.AreEqual(expectedEnv, manifest["env"]!.ToString());
    }

    [TestMethod]
    public async Task VerifyManifestPortAllocationIsGlobal()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container0 = builder.AddContainer("app0", "image")
                               .WithEndpoint(name: "custom");

        var container1 = builder.AddContainer("app1", "image")
                               .WithEndpoint(name: "custom");

        var manifests = await ManifestUtils.GetManifests([container0.Resource, container1.Resource]).DefaultTimeout();
        var expectedManifest0 =
            """
            {
              "type": "container.v0",
              "image": "image:latest",
              "bindings": {
                "custom": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 8000
                }
              }
            }
            """;

        var expectedManifest1 =
            """
            {
              "type": "container.v0",
              "image": "image:latest",
              "bindings": {
                "custom": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 8001
                }
              }
            }
            """;

        Assert.AreEqual(expectedManifest0, manifests[0].ToString());
        Assert.AreEqual(expectedManifest1, manifests[1].ToString());
    }

    [TestMethod]
    public void WithEndpoint_WithAllArguments_ForwardsAllArguments()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var projectA = builder.AddProject<ProjectA>("projecta")
                              .WithEndpoint(123, 456, "scheme", "mybinding", "env", true, true);

        var endpoint = projectA.Resource.Annotations.OfType<EndpointAnnotation>()
            .Where(e => string.Equals(e.Name, "mybinding", EndpointAnnotationName)).Single();

        Assert.AreEqual(123, endpoint.Port);
        Assert.AreEqual(456, endpoint.TargetPort);
        Assert.AreEqual("scheme", endpoint.UriScheme);
        Assert.AreEqual("env", endpoint.TargetPortEnvironmentVariable);
        Assert.IsTrue(endpoint.IsProxied);
        Assert.IsTrue(endpoint.IsExternal);
        Assert.AreEqual(System.Net.Sockets.ProtocolType.Tcp, endpoint.Protocol);
    }

    private sealed class TestProject : IProjectMetadata
    {
        public string ProjectPath => "projectpath";

        public LaunchSettings? LaunchSettings { get; } = new();
    }
    private sealed class ProjectA : IProjectMetadata
    {
        public string ProjectPath => "projectA";

        public LaunchSettings LaunchSettings { get; } = new();
    }

    private sealed class ProjectB : IProjectMetadata
    {
        public string ProjectPath => "projectB";
        public LaunchSettings LaunchSettings { get; } = new();
    }
}
