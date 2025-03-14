// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Tests.Helpers;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Tests;

[TestClass]
public class ProjectResourceTests
{
    [TestMethod]
    public async Task AddProjectWithInvalidLaunchSettingsShouldThrowSpecificError()
    {
        var projectDetails = await PrepareProjectWithMalformedLaunchSettingsAsync().DefaultTimeout();

        var ex = Assert.Throws<DistributedApplicationException>(() =>
        {
            var appBuilder = CreateBuilder();
            appBuilder.AddProject("project", projectDetails.ProjectFilePath);
        });

        var expectedMessage = $"Failed to get effective launch profile for project resource 'project'. There is malformed JSON in the project's launch settings file at '{projectDetails.LaunchSettingsFilePath}'.";
        Assert.AreEqual(expectedMessage, ex.Message);

        async static Task<(string ProjectFilePath, string LaunchSettingsFilePath)> PrepareProjectWithMalformedLaunchSettingsAsync()
        {
            var csProjContent = """
                                <Project Sdk="Microsoft.NET.Sdk.Web">
                                <!-- Not a real project, just a stub for testing -->
                                </Project>
                                """;

            var launchSettingsContent = """
                                        this { is } { mal formed! >
                                        """;

            var projectDirectoryPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var projectFilePath = Path.Combine(projectDirectoryPath, "Project.csproj");
            var propertiesDirectoryPath = Path.Combine(projectDirectoryPath, "Properties");
            var launchSettingsFilePath = Path.Combine(propertiesDirectoryPath, "launchSettings.json");

            Directory.CreateDirectory(projectDirectoryPath);
            await File.WriteAllTextAsync(projectFilePath, csProjContent).DefaultTimeout();

            Directory.CreateDirectory(propertiesDirectoryPath);
            await File.WriteAllTextAsync(launchSettingsFilePath, launchSettingsContent).DefaultTimeout();

            return (projectFilePath, launchSettingsFilePath);
        }
    }

    [TestMethod]
    public async Task AddProjectAddsEnvironmentVariablesAndServiceMetadata()
    {
        // Explicitly specify development environment and other config so it is constant.
        var appBuilder = CreateBuilder(args: ["--environment", "Development", "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL=http://localhost:18889"],
            DistributedApplicationOperation.Run);

        appBuilder.AddProject<TestProject>("projectName", launchProfileName: null);
        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var projectResources = appModel.GetProjectResources();

        var resource = Assert.ContainsSingle(projectResources);
        Assert.AreEqual("projectName", resource.Name);

        var serviceMetadata = Assert.ContainsSingle(resource.Annotations.OfType<IProjectMetadata>());
        Assert.IsType<TestProject>(serviceMetadata);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

        Assert.That.Collection(config,
            env =>
            {
                Assert.AreEqual("OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES", env.Key);
                Assert.AreEqual("true", env.Value);
            },
            env =>
            {
                Assert.AreEqual("OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES", env.Key);
                Assert.AreEqual("true", env.Value);
            },
            env =>
            {
                Assert.AreEqual("OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY", env.Key);
                Assert.AreEqual("in_memory", env.Value);
            },
            env =>
            {
                Assert.AreEqual("OTEL_DOTNET_EXPERIMENTAL_ASPNETCORE_DISABLE_URL_QUERY_REDACTION", env.Key);
                Assert.AreEqual("true", env.Value);
            },
            env =>
            {
                Assert.AreEqual("OTEL_DOTNET_EXPERIMENTAL_HTTPCLIENT_DISABLE_URL_QUERY_REDACTION", env.Key);
                Assert.AreEqual("true", env.Value);
            },
            env =>
            {
                Assert.AreEqual("OTEL_EXPORTER_OTLP_ENDPOINT", env.Key);
                Assert.AreEqual("http://localhost:18889", env.Value);
            },
            env =>
            {
                Assert.AreEqual("OTEL_EXPORTER_OTLP_PROTOCOL", env.Key);
                Assert.AreEqual("grpc", env.Value);
            },
            env =>
            {
                Assert.AreEqual("OTEL_RESOURCE_ATTRIBUTES", env.Key);
                Assert.AreEqual("service.instance.id={{- index .Annotations \"otel-service-instance-id\" -}}", env.Value);
            },
            env =>
            {
                Assert.AreEqual("OTEL_SERVICE_NAME", env.Key);
                Assert.AreEqual("{{- index .Annotations \"otel-service-name\" -}}", env.Value);
            },
            env =>
            {
                Assert.AreEqual("OTEL_EXPORTER_OTLP_HEADERS", env.Key);
                var parts = env.Value.Split('=');
                Assert.AreEqual("x-otlp-api-key", parts[0]);
                Assert.IsTrue(Guid.TryParse(parts[1], out _));
            },
            env =>
            {
                Assert.AreEqual("OTEL_BLRP_SCHEDULE_DELAY", env.Key);
                Assert.AreEqual("1000", env.Value);
            },
            env =>
            {
                Assert.AreEqual("OTEL_BSP_SCHEDULE_DELAY", env.Key);
                Assert.AreEqual("1000", env.Value);
            },
            env =>
            {
                Assert.AreEqual("OTEL_METRIC_EXPORT_INTERVAL", env.Key);
                Assert.AreEqual("1000", env.Value);
            },
            env =>
            {
                Assert.AreEqual("OTEL_TRACES_SAMPLER", env.Key);
                Assert.AreEqual("always_on", env.Value);
            },
            env =>
            {
                Assert.AreEqual("OTEL_METRICS_EXEMPLAR_FILTER", env.Key);
                Assert.AreEqual("trace_based", env.Value);
            },
            env =>
            {
                Assert.AreEqual("DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION", env.Key);
                Assert.AreEqual("true", env.Value);
            },
            env =>
            {
                Assert.AreEqual("LOGGING__CONSOLE__FORMATTERNAME", env.Key);
                Assert.AreEqual("simple", env.Value);
            });
    }

    [TestMethod]
    [DataRow("true", false)]
    [DataRow("1", false)]
    [DataRow("false", true)]
    [DataRow("0", true)]
    [DataRow(null, true)]
    public async Task AddProjectAddsEnvironmentVariablesAndServiceMetadata_OtlpAuthDisabledSetting(string? value, bool hasHeader)
    {
        var appBuilder = CreateBuilder(args: [$"DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS={value}"], DistributedApplicationOperation.Run);

        appBuilder.AddProject<TestProject>("projectName", launchProfileName: null);
        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var projectResources = appModel.GetProjectResources();

        var resource = Assert.ContainsSingle(projectResources);
        Assert.AreEqual("projectName", resource.Name);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

        if (hasHeader)
        {
            Assert.IsTrue(config.ContainsKey("OTEL_EXPORTER_OTLP_HEADERS"), "Config should have 'OTEL_EXPORTER_OTLP_HEADERS' header and doesn't.");
        }
        else
        {
            Assert.IsFalse(config.ContainsKey("OTEL_EXPORTER_OTLP_HEADERS"), "Config shouldn't have 'OTEL_EXPORTER_OTLP_HEADERS' header and does.");
        }
    }

    [TestMethod]
    public void WithReplicasAddsAnnotationToProject()
    {
        var appBuilder = CreateBuilder();

        appBuilder.AddProject<TestProject>("projectName", launchProfileName: null)
            .WithReplicas(5);
        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var projectResources = appModel.GetProjectResources();

        var resource = Assert.ContainsSingle(projectResources);
        var replica = Assert.ContainsSingle(resource.Annotations.OfType<ReplicaAnnotation>());

        Assert.AreEqual(5, replica.Replicas);
    }

    [TestMethod]
    public void WithLaunchProfileAddsAnnotationToProject()
    {
        var appBuilder = CreateBuilder();

        appBuilder.AddProject<Projects.ServiceA>("projectName", launchProfileName: "http");
        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var projectResources = appModel.GetProjectResources();

        var resource = Assert.ContainsSingle(projectResources);
        Assert.Contains(resource.Annotations, a => a is LaunchProfileAnnotation);
    }

    [TestMethod]
    public void WithLaunchProfile_ApplicationUrlTrailingSemiColon_Ignore()
    {
        var appBuilder = CreateBuilder(operation: DistributedApplicationOperation.Run);

        appBuilder.AddProject<Projects.ServiceA>("projectName", launchProfileName: "https");
        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var projectResources = appModel.GetProjectResources();

        var resource = Assert.ContainsSingle(projectResources);

        Assert.That.Collection(
            resource.Annotations.OfType<EndpointAnnotation>(),
            a =>
            {
                Assert.AreEqual("https", a.Name);
                Assert.AreEqual("https", a.UriScheme);
                Assert.AreEqual(7123, a.Port);
            },
            a =>
            {
                Assert.AreEqual("http", a.Name);
                Assert.AreEqual("http", a.UriScheme);
                Assert.AreEqual(5156, a.Port);
            });
    }

    [TestMethod]
    public void AddProjectFailsIfFileDoesNotExist()
    {
        var appBuilder = CreateBuilder();

        var ex = Assert.Throws<DistributedApplicationException>(() => appBuilder.AddProject<TestProject>("projectName"));
        Assert.AreEqual("Project file 'another-path' was not found.", ex.Message);
    }

    [TestMethod]
    public void SpecificLaunchProfileFailsIfProfileDoesNotExist()
    {
        var appBuilder = CreateBuilder();

        var ex = Assert.Throws<DistributedApplicationException>(() => appBuilder.AddProject<Projects.ServiceA>("projectName", launchProfileName: "not-exist"));
        Assert.AreEqual("Launch settings file does not contain 'not-exist' profile.", ex.Message);
    }

    [TestMethod]
    public void ExcludeLaunchProfileAddsAnnotationToProject()
    {
        var appBuilder = CreateBuilder();

        appBuilder.AddProject<Projects.ServiceA>("projectName", launchProfileName: null);
        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var projectResources = appModel.GetProjectResources();

        var resource = Assert.ContainsSingle(projectResources);

        Assert.Contains(resource.Annotations, a => a is ExcludeLaunchProfileAnnotation);
    }

    [TestMethod]
    public async Task AspNetCoreUrlsNotInjectedInPublishMode()
    {
        var appBuilder = CreateBuilder(operation: DistributedApplicationOperation.Publish);

        appBuilder.AddProject<Projects.ServiceA>("projectName", launchProfileName: null)
                  .WithHttpEndpoint(port: 5000, name: "http")
                  .WithHttpsEndpoint(port: 5001, name: "https");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var projectResources = appModel.GetProjectResources();

        var resource = Assert.ContainsSingle(projectResources);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(resource, DistributedApplicationOperation.Publish).DefaultTimeout();

        Assert.IsFalse(config.ContainsKey("ASPNETCORE_URLS"));
        Assert.IsFalse(config.ContainsKey("ASPNETCORE_HTTPS_PORT"));
    }

    [TestMethod]
    public async Task ExcludeLaunchProfileAddsHttpOrHttpsEndpointAddsToEnv()
    {
        var appBuilder = CreateBuilder(operation: DistributedApplicationOperation.Run);

        appBuilder.AddProject<Projects.ServiceA>("projectName", launchProfileName: null)
                  .WithHttpEndpoint(port: 5000, name: "http")
                  .WithHttpsEndpoint(port: 5001, name: "https")
                  .WithHttpEndpoint(port: 5002, name: "http2", env: "SOME_ENV")
                  .WithHttpEndpoint(port: 5003, name: "dontinjectme")
                  // Should not be included in ASPNETCORE_URLS
                  .WithEndpointsInEnvironment(filter: e => e.Name != "dontinjectme")
                  .WithEndpoint("http", e =>
                  {
                      e.AllocatedEndpoint = new(e, "localhost", e.Port!.Value, targetPortExpression: "p0");
                  })
                  .WithEndpoint("https", e =>
                  {
                      e.AllocatedEndpoint = new(e, "localhost", e.Port!.Value, targetPortExpression: "p1");
                  })
                  .WithEndpoint("http2", e =>
                   {
                       e.AllocatedEndpoint = new(e, "localhost", e.Port!.Value, targetPortExpression: "p2");
                   })
                  .WithEndpoint("dontinjectme", e =>
                   {
                       e.AllocatedEndpoint = new(e, "localhost", e.Port!.Value, targetPortExpression: "p3");
                   });

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var projectResources = appModel.GetProjectResources();

        var resource = Assert.ContainsSingle(projectResources);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

        Assert.AreEqual("http://localhost:p0;https://localhost:p1", config["ASPNETCORE_URLS"]);
        Assert.AreEqual("5001", config["ASPNETCORE_HTTPS_PORT"]);
        Assert.AreEqual("p2", config["SOME_ENV"]);
    }

    [TestMethod]
    public async Task NoEndpointsDoesNotAddAspNetCoreUrls()
    {
        var appBuilder = CreateBuilder(operation: DistributedApplicationOperation.Run);

        appBuilder.AddProject<Projects.ServiceA>("projectName", launchProfileName: null);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var projectResources = appModel.GetProjectResources();

        var resource = Assert.ContainsSingle(projectResources);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

        Assert.IsFalse(config.ContainsKey("ASPNETCORE_URLS"));
        Assert.IsFalse(config.ContainsKey("ASPNETCORE_HTTPS_PORT"));
    }

    [TestMethod]
    public async Task ProjectWithLaunchProfileAddsHttpOrHttpsEndpointAddsToEnv()
    {
        var appBuilder = CreateBuilder(operation: DistributedApplicationOperation.Run);

        appBuilder.AddProject<TestProjectWithLaunchSettings>("projectName")
                  .WithEndpoint("http", e =>
                  {
                      e.AllocatedEndpoint = new(e, "localhost", e.Port!.Value, targetPortExpression: "p0");
                  });

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var projectResources = appModel.GetProjectResources();

        var resource = Assert.ContainsSingle(projectResources);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

        Assert.AreEqual("http://localhost:p0", config["ASPNETCORE_URLS"]);
        Assert.IsFalse(config.ContainsKey("ASPNETCORE_HTTPS_PORT"));
    }

    [TestMethod]
    public async Task ProjectWithMultipleLaunchProfileAppUrlsGetsAllUrls()
    {
        var appBuilder = CreateBuilder(operation: DistributedApplicationOperation.Run);

        var builder = appBuilder.AddProject<TestProjectWithManyAppUrlsInLaunchSettings>("projectName");

        // Need to allocated all the endpoints we get from the launch profile applicationUrl
        var index = 0;
        foreach (var q in new[] { "http", "http2", "https", "https2", "https3" })
        {
            builder.WithEndpoint(q, e =>
            {
                e.AllocatedEndpoint = new(e, "localhost", e.Port!.Value, targetPortExpression: $"p{index++}");
            });
        }

        using var app = appBuilder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var projectResources = appModel.GetProjectResources();
        var resource = Assert.ContainsSingle(projectResources);
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

        Assert.AreEqual("https://localhost:p2;http://localhost:p0;http://localhost:p1;https://localhost:p3;https://localhost:p4", config["ASPNETCORE_URLS"]);

        // The first https port is the one that should be used for ASPNETCORE_HTTPS_PORT
        Assert.AreEqual("7144", config["ASPNETCORE_HTTPS_PORT"]);
    }

    [TestMethod]
    public void DisabledForwardedHeadersAddsAnnotationToProject()
    {
        var appBuilder = CreateBuilder();

        appBuilder.AddProject<Projects.ServiceA>("projectName").DisableForwardedHeaders();
        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var projectResources = appModel.GetProjectResources();

        var resource = Assert.ContainsSingle(projectResources);

        Assert.Contains(resource.Annotations, a => a is DisableForwardedHeadersAnnotation);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task VerifyManifest(bool disableForwardedHeaders)
    {
        var appBuilder = CreateBuilder();

        var project = appBuilder.AddProject<TestProjectWithLaunchSettings>("projectName");
        if (disableForwardedHeaders)
        {
            project.DisableForwardedHeaders();
        }

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var projectResources = appModel.GetProjectResources();

        var resource = Assert.ContainsSingle(projectResources);

        var manifest = await ManifestUtils.GetManifest(resource).DefaultTimeout();

        var fordwardedHeadersEnvVar = disableForwardedHeaders
            ? ""
            : $",{Environment.NewLine}    \"ASPNETCORE_FORWARDEDHEADERS_ENABLED\": \"true\"";

        var expectedManifest = $$"""
            {
              "type": "project.v0",
              "path": "another-path",
              "env": {
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES": "true",
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES": "true",
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY": "in_memory"{{fordwardedHeadersEnvVar}},
                "HTTP_PORTS": "{projectName.bindings.http.targetPort}"
              },
              "bindings": {
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http"
                },
                "https": {
                  "scheme": "https",
                  "protocol": "tcp",
                  "transport": "http"
                }
              }
            }
            """;

        Assert.AreEqual(expectedManifest, manifest.ToString());
    }

    [TestMethod]
    public async Task VerifyManifestWithArgs()
    {
        var appBuilder = CreateBuilder();

        appBuilder.AddProject<TestProjectWithLaunchSettings>("projectName")
            .WithArgs("one", "two");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var projectResources = appModel.GetProjectResources();

        var resource = Assert.ContainsSingle(projectResources);

        var manifest = await ManifestUtils.GetManifest(resource).DefaultTimeout();

        var expectedManifest = $$"""
            {
              "type": "project.v0",
              "path": "another-path",
              "args": [
                "one",
                "two"
              ],
              "env": {
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES": "true",
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES": "true",
                "OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY": "in_memory",
                "ASPNETCORE_FORWARDEDHEADERS_ENABLED": "true",
                "HTTP_PORTS": "{projectName.bindings.http.targetPort}"
              },
              "bindings": {
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http"
                },
                "https": {
                  "scheme": "https",
                  "protocol": "tcp",
                  "transport": "http"
                }
              }
            }
            """;

        Assert.AreEqual(expectedManifest, manifest.ToString());
    }

    [TestMethod]
    public async Task AddProjectWithArgs()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var c1 = appBuilder.AddContainer("c1", "image2")
            .WithEndpoint("ep", e =>
            {
                e.UriScheme = "http";
                e.AllocatedEndpoint = new(e, "localhost", 1234);
            });

        var project = appBuilder.AddProject<TestProjectWithLaunchSettings>("projectName")
             .WithArgs(context =>
             {
                 context.Args.Add("arg1");
                 context.Args.Add(c1.GetEndpoint("ep"));
             });

        using var app = appBuilder.Build();

        var args = await ArgumentEvaluator.GetArgumentListAsync(project.Resource).DefaultTimeout();

        Assert.That.Collection(args,
            arg => Assert.AreEqual("arg1", arg),
            arg => Assert.AreEqual("http://localhost:1234", arg));
    }

    [TestMethod]
    [DataRow(true, "localhost")]
    [DataRow(false, "*")]
    public async Task AddProjectWithWildcardUrlInLaunchSettings(bool isProxied, string expectedHost)
    {
        var appBuilder = CreateBuilder(operation: DistributedApplicationOperation.Run);

        appBuilder.AddProject<TestProjectWithWildcardUrlInLaunchSettings>("projectName")
            .WithEndpoint("http", e =>
            {
                Assert.AreEqual("*", e.TargetHost);
                e.AllocatedEndpoint = new(e, "localhost", e.Port!.Value, targetPortExpression: "p0");
                e.IsProxied = isProxied;
            })
            .WithEndpoint("https", e =>
            {
                Assert.AreEqual("*", e.TargetHost);
                e.AllocatedEndpoint = new(e, "localhost", e.Port!.Value, targetPortExpression: "p1");
                e.IsProxied = isProxied;
            });

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var projectResources = appModel.GetProjectResources();

        var resource = Assert.ContainsSingle(projectResources);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

        var http = resource.GetEndpoint("http");
        var https = resource.GetEndpoint("https");

        if (isProxied)
        {
            // When the end point is proxied, the host should be localhost and the port should match the targetPortExpression
            Assert.AreEqual($"http://{expectedHost}:p0;https://{expectedHost}:p1", config["ASPNETCORE_URLS"]);
        }
        else
        {
            Assert.AreEqual($"http://{expectedHost}:{http.TargetPort};https://{expectedHost}:{https.TargetPort}", config["ASPNETCORE_URLS"]);
        }

        Assert.AreEqual(https.Port.ToString(), config["ASPNETCORE_HTTPS_PORT"]);
    }

    internal static IDistributedApplicationBuilder CreateBuilder(string[]? args = null, DistributedApplicationOperation operation = DistributedApplicationOperation.Publish)
    {
        var resolvedArgs = new List<string>();
        if (args != null)
        {
            resolvedArgs.AddRange(args);
        }
        if (operation == DistributedApplicationOperation.Publish)
        {
            resolvedArgs.AddRange(["--publisher", "manifest"]);
        }
        var appBuilder = DistributedApplication.CreateBuilder(resolvedArgs.ToArray());
        // Block DCP from actually starting anything up as we don't need it for this test.
        appBuilder.Services.AddKeyedSingleton<IDistributedApplicationPublisher, NoopPublisher>("manifest");

        return appBuilder;
    }

    private sealed class TestProject : IProjectMetadata
    {
        public string ProjectPath => "another-path";

        public LaunchSettings? LaunchSettings { get; set; }
    }

    internal abstract class BaseProjectWithProfileAndConfig : IProjectMetadata
    {
        protected Dictionary<string, LaunchProfile>? Profiles { get; set; } = new();
        protected string? JsonConfigString { get; set; }

        public string ProjectPath => "another-path";
        public LaunchSettings? LaunchSettings => new LaunchSettings { Profiles = Profiles! };
        public IConfiguration? Configuration => JsonConfigString == null ? null : new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(JsonConfigString)))
            .Build();
    }

    private sealed class TestProjectWithLaunchSettings : BaseProjectWithProfileAndConfig
    {
        public TestProjectWithLaunchSettings()
        {
            Profiles = new()
            {
                ["http"] = new()
                {
                    CommandName = "Project",
                    CommandLineArgs = "arg1 arg2",
                    LaunchBrowser = true,
                    ApplicationUrl = "http://localhost:5031",
                    EnvironmentVariables = new()
                    {
                        ["ASPNETCORE_ENVIRONMENT"] = "Development"
                    }
                }
            };
        }
    }

    private sealed class TestProjectWithManyAppUrlsInLaunchSettings : BaseProjectWithProfileAndConfig
    {
        public TestProjectWithManyAppUrlsInLaunchSettings()
        {
            Profiles = new()
            {
                ["https"] = new()
                {
                    CommandName = "Project",
                    CommandLineArgs = "arg1 arg2",
                    LaunchBrowser = true,
                    ApplicationUrl = "https://localhost:7144;http://localhost:5193;http://localhost:5194;https://localhost:7145;https://localhost:7146",
                    EnvironmentVariables = new()
                    {
                        ["ASPNETCORE_ENVIRONMENT"] = "Development"
                    }
                }
            };
        }
    }

    private sealed class TestProjectWithWildcardUrlInLaunchSettings : BaseProjectWithProfileAndConfig
    {
        public TestProjectWithWildcardUrlInLaunchSettings()
        {
            Profiles = new()
            {
                ["https"] = new()
                {
                    CommandName = "Project",
                    CommandLineArgs = "arg1 arg2",
                    LaunchBrowser = true,
                    ApplicationUrl = "http://*:5031;https://*:5033",
                    EnvironmentVariables = new()
                    {
                        ["ASPNETCORE_ENVIRONMENT"] = "Development"
                    }
                }
            };
        }
    }
}
