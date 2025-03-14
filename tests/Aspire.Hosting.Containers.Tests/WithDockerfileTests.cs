// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Dcp.Model;
using Aspire.Hosting.Testing;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Containers.Tests;

[TestClass]
public class WithDockerfileTests
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    [RequiresDocker]
    public async Task WithBuildSecretPopulatesSecretFilesCorrectly()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(b => b.AddMSTest(TestContext));

        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync(includeSecrets: true);

        builder.Configuration["Parameters:secret"] = "open sesame from env";
        var parameter = builder.AddParameter("secret", secret: true);

        builder.AddContainer("testcontainer", "testimage")
               .WithHttpEndpoint(targetPort: 80)
               .WithDockerfile(tempContextPath, tempDockerfilePath)
               .WithBuildSecret("ENV_SECRET", parameter);

        using var app = builder.Build();
        await app.StartAsync();

        await WaitForResourceAsync(app, "testcontainer", "Running");

        using var client = app.CreateHttpClient("testcontainer", "http");

        var envSecretMessage = await client.GetStringAsync("/ENV_SECRET.txt");
        Assert.AreEqual("open sesame from env", envSecretMessage);

        await app.StopAsync();
    }

    [TestMethod]
    [RequiresDocker]
    public async Task ContainerBuildLogsAreStreamedToAppHost()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(logging =>
        {
            logging.AddFakeLogging();
            logging.AddMSTest(TestContext);
        });

        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();

        builder.AddContainer("testcontainer", "testimage")
               .WithHttpEndpoint(targetPort: 80)
               .WithDockerfile(tempContextPath, tempDockerfilePath);

        using var app = builder.Build();

        await app.StartAsync();

        // Wait for the resource to come online.
        await WaitForResourceAsync(app, "testcontainer", "Running");
        using var client = app.CreateHttpClient("testcontainer", "http");
        var message = await client.GetStringAsync("/aspire.html");

        // By the time we can make a request to the service the logs
        // should be streamed back to the app host.
        var collector = app.Services.GetFakeLogCollector();
        var logs = collector.GetSnapshot();

        // Just looking for a common message in Docker build output.
        Assert.Contains(logs, log => log.Message.Contains("load build definition from Dockerfile"));

        await app.StopAsync();
    }

    [TestMethod]
    [DataRow("testcontainer")]
    [DataRow("TestContainer")]
    [DataRow("test-Container")]
    [DataRow("TEST-234-CONTAINER")]
    public async Task AddDockerfileUsesLowercaseResourceNameAsImageName(string resourceName)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(b => b.AddMSTest(TestContext));

        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();

        var dockerFile = builder.AddDockerfile(resourceName, tempContextPath, tempDockerfilePath);

        Assert.IsTrue(dockerFile.Resource.TryGetLastAnnotation<ContainerImageAnnotation>(out var containerImageAnnotation));
        Assert.AreEqual(resourceName.ToLowerInvariant(), containerImageAnnotation.Image);
    }

    [TestMethod]
    [DataRow("testcontainer")]
    [DataRow("TestContainer")]
    [DataRow("test-Container")]
    [DataRow("TEST-234-CONTAINER")]
    public async Task WithDockerfileUsesLowercaseResourceNameAsImageName(string resourceName)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(b => b.AddMSTest(TestContext));

        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();

        var dockerFile = builder.AddContainer(resourceName, "someimagename")
            .WithDockerfile(tempContextPath, tempDockerfilePath);

        Assert.IsTrue(dockerFile.Resource.TryGetLastAnnotation<ContainerImageAnnotation>(out var containerImageAnnotation));
        Assert.AreEqual(resourceName.ToLowerInvariant(), containerImageAnnotation.Image);
    }

    [TestMethod]
    public async Task WithDockerfileUsesGeneratesDifferentHashForImageTagOnEachCall()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(b => b.AddMSTest(TestContext));

        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();

        var dockerFile = builder.AddContainer("testcontainer", "someimagename")
            .WithDockerfile(tempContextPath, tempDockerfilePath);
        Assert.IsTrue(dockerFile.Resource.TryGetLastAnnotation<ContainerImageAnnotation>(out var containerImageAnnotation1));
        var tag1 = containerImageAnnotation1.Tag;

        dockerFile.WithDockerfile(tempContextPath, tempDockerfilePath);
        Assert.IsTrue(dockerFile.Resource.TryGetLastAnnotation<ContainerImageAnnotation>(out var containerImageAnnotation2));
        var tag2 = containerImageAnnotation2.Tag;

        Assert.AreNotEqual(tag1, tag2);
    }

    [TestMethod]
    public async Task WithDockerfileGeneratedImageTagCanBeOverridden()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(b => b.AddMSTest(TestContext));

        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();

        var dockerFile = builder.AddContainer("testcontainer", "someimagename")
            .WithDockerfile(tempContextPath, tempDockerfilePath);

        Assert.IsTrue(dockerFile.Resource.TryGetLastAnnotation<ContainerImageAnnotation>(out var containerImageAnnotation1));
        var generatedTag = containerImageAnnotation1.Tag;

        dockerFile.WithImageTag("latest");
        Assert.IsTrue(dockerFile.Resource.TryGetLastAnnotation<ContainerImageAnnotation>(out var containerImageAnnotation2));
        var overriddenTag = containerImageAnnotation2.Tag;

        Assert.AreNotEqual(generatedTag, overriddenTag);
        Assert.AreEqual("latest", overriddenTag);
    }

    [TestMethod]
    [RequiresDocker]
    public async Task WithDockerfileLaunchesContainerSuccessfully()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(b => b.AddMSTest(TestContext));

        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();

        builder.AddContainer("testcontainer", "testimage")
               .WithHttpEndpoint(targetPort: 80)
               .WithDockerfile(tempContextPath, tempDockerfilePath);

        using var app = builder.Build();
        await app.StartAsync();

        await WaitForResourceAsync(app, "testcontainer", "Running");

        using var client = app.CreateHttpClient("testcontainer", "http");

        var message = await client.GetStringAsync("/aspire.html");

        Assert.AreEqual($"{DefaultMessage}\n", message);

        var kubernetes = app.Services.GetRequiredService<IKubernetesService>();
        var containers = await kubernetes.ListAsync<Container>();

        var container = Assert.ContainsSingle(containers);
        Assert.AreEqual(tempContextPath, container!.Spec!.Build!.Context);
        Assert.AreEqual(tempDockerfilePath, container!.Spec!.Build!.Dockerfile);

        await app.StopAsync();
    }

    [TestMethod]
    [RequiresDocker]
    public async Task AddDockerfileLaunchesContainerSuccessfully()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(b => b.AddMSTest(TestContext));

        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();

        builder.AddDockerfile("testcontainer", tempContextPath, tempDockerfilePath)
               .WithHttpEndpoint(targetPort: 80);

        using var app = builder.Build();
        await app.StartAsync();

        await WaitForResourceAsync(app, "testcontainer", "Running");

        using var client = app.CreateHttpClient("testcontainer", "http");
        var message = await client.GetStringAsync("/aspire.html");

        Assert.AreEqual($"{DefaultMessage}\n", message);

        var kubernetes = app.Services.GetRequiredService<IKubernetesService>();
        var containers = await kubernetes.ListAsync<Container>();

        var container = Assert.ContainsSingle<Container>(containers);
        Assert.AreEqual(tempContextPath, container!.Spec!.Build!.Context);
        Assert.AreEqual(tempDockerfilePath, container!.Spec!.Build!.Dockerfile);

        await app.StopAsync();
    }

    [TestMethod]
    public async Task WithDockerfileResultsInBuildAttributeBeingAddedToManifest()
    {
        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();
        var manifestOutputPath = Path.Combine(tempContextPath, "aspire-manifest.json");
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = ["--publisher", "manifest", "--output-path", manifestOutputPath],
        });
        builder.Services.AddLogging(b => b.AddMSTest(TestContext));

        builder.Configuration["Parameters:message"] = "hello";
        var parameter = builder.AddParameter("message");

        var container = builder.AddContainer("testcontainer", "testimage")
                               .WithHttpEndpoint(targetPort: 80)
                               .WithDockerfile(tempContextPath, tempDockerfilePath, "runner")
                               .WithBuildArg("MESSAGE", parameter)
                               .WithBuildArg("stringParam", "a string")
                               .WithBuildArg("intParam", 42);

        var manifest = await ManifestUtils.GetManifest(container.Resource, manifestDirectory: tempContextPath);
        var expectedManifest = $$$$"""
            {
              "type": "container.v1",
              "build": {
                "context": ".",
                "dockerfile": "Dockerfile",
                "stage": "runner",
                "args": {
                  "MESSAGE": "{message.value}",
                  "stringParam": "a string",
                  "intParam": "42"
                }
              },
              "bindings": {
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 80
                }
              }
            }
            """;
        Assert.AreEqual(expectedManifest, manifest.ToString());
    }

    [TestMethod]
    public async Task AddDockerfileResultsInBuildAttributeBeingAddedToManifest()
    {
        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();
        var manifestOutputPath = Path.Combine(tempContextPath, "aspire-manifest.json");
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = ["--publisher", "manifest", "--output-path", manifestOutputPath],
        });
        builder.Services.AddLogging(b => b.AddMSTest(TestContext));

        builder.Configuration["Parameters:message"] = "hello";
        var parameter = builder.AddParameter("message");

        var container = builder.AddDockerfile("testcontainer", tempContextPath, tempDockerfilePath, "runner")
                               .WithHttpEndpoint(targetPort: 80)
                               .WithBuildArg("MESSAGE", parameter)
                               .WithBuildArg("stringParam", "a string")
                               .WithBuildArg("intParam", 42);

        var manifest = await ManifestUtils.GetManifest(container.Resource, manifestDirectory: tempContextPath);
        var expectedManifest = $$$$"""
            {
              "type": "container.v1",
              "build": {
                "context": ".",
                "dockerfile": "Dockerfile",
                "stage": "runner",
                "args": {
                  "MESSAGE": "{message.value}",
                  "stringParam": "a string",
                  "intParam": "42"
                }
              },
              "bindings": {
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 80
                }
              }
            }
            """;
        Assert.AreEqual(expectedManifest, manifest.ToString());
    }

    [TestMethod]
    public async Task WithDockerfileWithBuildSecretResultsInManifestReferencingSecretParameter()
    {
        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();
        var manifestOutputPath = Path.Combine(tempContextPath, "aspire-manifest.json");
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = ["--publisher", "manifest", "--output-path", manifestOutputPath],
        });
        builder.Services.AddLogging(b => b.AddMSTest(TestContext));

        builder.Configuration["Parameters:secret"] = "open sesame";
        var parameter = builder.AddParameter("secret", secret: true);

        var container = builder.AddContainer("testcontainer", "testimage")
                               .WithHttpEndpoint(targetPort: 80)
                               .WithDockerfile(tempContextPath, tempDockerfilePath)
                               .WithBuildSecret("SECRET", parameter);

        var manifest = await ManifestUtils.GetManifest(container.Resource, manifestDirectory: tempContextPath);
        var expectedManifest = $$$$"""
            {
              "type": "container.v1",
              "build": {
                "context": ".",
                "dockerfile": "Dockerfile",
                "secrets": {
                  "SECRET": {
                    "type": "env",
                    "value": "{secret.value}"
                  }
                }
              },
              "bindings": {
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 80
                }
              }
            }
            """;
        Assert.AreEqual(expectedManifest, manifest.ToString());
    }

    [TestMethod]
    public async Task AddDockerfileWithBuildSecretResultsInManifestReferencingSecretParameter()
    {
        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();
        var manifestOutputPath = Path.Combine(tempContextPath, "aspire-manifest.json");
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = ["--publisher", "manifest", "--output-path", manifestOutputPath],
        });
        builder.Services.AddLogging(b => b.AddMSTest(TestContext));

        builder.Configuration["Parameters:secret"] = "open sesame";
        var parameter = builder.AddParameter("secret", secret: true);

        var container = builder.AddDockerfile("testcontainer", tempContextPath, tempDockerfilePath)
                               .WithHttpEndpoint(targetPort: 80)
                               .WithBuildSecret("SECRET", parameter);

        var manifest = await ManifestUtils.GetManifest(container.Resource, manifestDirectory: tempContextPath);
        var expectedManifest = $$$$"""
            {
              "type": "container.v1",
              "build": {
                "context": ".",
                "dockerfile": "Dockerfile",
                "secrets": {
                  "SECRET": {
                    "type": "env",
                    "value": "{secret.value}"
                  }
                }
              },
              "bindings": {
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 80
                }
              }
            }
            """;
        Assert.AreEqual(expectedManifest, manifest.ToString());
    }

    [TestMethod]
    [RequiresDocker]
    public async Task WithDockerfileWithParameterLaunchesContainerSuccessfully()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(b => b.AddMSTest(TestContext));

        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();

        builder.Configuration["Parameters:message"] = "hello";
        var parameter = builder.AddParameter("message");

        builder.AddContainer("testcontainer", "testimage")
               .WithHttpEndpoint(targetPort: 80)
               .WithDockerfile(tempContextPath, tempDockerfilePath)
               .WithBuildArg("MESSAGE", parameter)
               .WithBuildArg("stringParam", "a string")
               .WithBuildArg("intParam", 42)
               .WithBuildArg("boolParamTrue", true)
               .WithBuildArg("boolParamFalse", false);

        using var app = builder.Build();
        await app.StartAsync();

        await WaitForResourceAsync(app, "testcontainer", "Running");

        using var client = app.CreateHttpClient("testcontainer", "http");

        var message = await client.GetStringAsync("/aspire.html");

        Assert.AreEqual($"hello\n", message);

        var kubernetes = app.Services.GetRequiredService<IKubernetesService>();
        var containers = await kubernetes.ListAsync<Container>();

        var container = Assert.ContainsSingle<Container>(containers);
        Assert.AreEqual(tempContextPath, container!.Spec!.Build!.Context);
        Assert.AreEqual(tempDockerfilePath, container!.Spec!.Build!.Dockerfile);
        Assert.IsNull(container!.Spec!.Build!.Stage);
        Assert.That.Collection(
            container!.Spec!.Build!.Args!,
            arg =>
            {
                Assert.AreEqual("MESSAGE", arg.Name);
                Assert.AreEqual("hello", arg.Value);
            },
            arg =>
            {
                Assert.AreEqual("stringParam", arg.Name);
                Assert.AreEqual("a string", arg.Value);
            },
            arg =>
            {
                Assert.AreEqual("intParam", arg.Name);
                Assert.AreEqual("42", arg.Value);
            },
            arg =>
            {
                Assert.AreEqual("boolParamTrue", arg.Name);
                Assert.AreEqual("true", arg.Value);
            },
            arg =>
            {
                Assert.AreEqual("boolParamFalse", arg.Name);
                Assert.AreEqual("false", arg.Value);
            }
            );

        await app.StopAsync();
    }

    [TestMethod]
    [RequiresDocker]
    public async Task AddDockerfileWithParameterLaunchesContainerSuccessfully()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(b => b.AddMSTest(TestContext));

        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();

        builder.Configuration["Parameters:message"] = "hello";
        var parameter = builder.AddParameter("message");

        builder.AddDockerfile("testcontainer", tempContextPath, tempDockerfilePath)
               .WithHttpEndpoint(targetPort: 80)
               .WithBuildArg("MESSAGE", parameter)
               .WithBuildArg("stringParam", "a string")
               .WithBuildArg("intParam", 42)
               .WithBuildArg("boolParamTrue", true)
               .WithBuildArg("boolParamFalse", false);

        using var app = builder.Build();
        await app.StartAsync();

        await WaitForResourceAsync(app, "testcontainer", "Running");

        using var client = app.CreateHttpClient("testcontainer", "http");

        var message = await client.GetStringAsync("/aspire.html");

        Assert.AreEqual($"hello\n", message);

        var kubernetes = app.Services.GetRequiredService<IKubernetesService>();
        var containers = await kubernetes.ListAsync<Container>();

        var container = Assert.ContainsSingle<Container>(containers);
        Assert.AreEqual(tempContextPath, container!.Spec!.Build!.Context);
        Assert.AreEqual(tempDockerfilePath, container!.Spec!.Build!.Dockerfile);
        Assert.IsNull(container!.Spec!.Build!.Stage);
        Assert.That.Collection(
            container!.Spec!.Build!.Args!,
            arg =>
            {
                Assert.AreEqual("MESSAGE", arg.Name);
                Assert.AreEqual("hello", arg.Value);
            },
            arg =>
            {
                Assert.AreEqual("stringParam", arg.Name);
                Assert.AreEqual("a string", arg.Value);
            },
            arg =>
            {
                Assert.AreEqual("intParam", arg.Name);
                Assert.AreEqual("42", arg.Value);
            },
            arg =>
            {
                Assert.AreEqual("boolParamTrue", arg.Name);
                Assert.AreEqual("true", arg.Value);
            },
            arg =>
            {
                Assert.AreEqual("boolParamFalse", arg.Name);
                Assert.AreEqual("false", arg.Value);
            }
            );

        await app.StopAsync();
    }

    [TestMethod]
    public void WithDockerfileWithEmptyContextPathThrows()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(b => b.AddMSTest(TestContext));

        var ex = Assert.Throws<ArgumentException>(() =>
        {
            builder.AddContainer("mycontainer", "myimage")
                   .WithDockerfile(string.Empty);
        });

        Assert.AreEqual("contextPath", ex.ParamName);
    }

    [TestMethod]
    public void AddDockerfileWithEmptyContextPathThrows()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var ex = Assert.Throws<ArgumentException>(() =>
        {
            builder.AddDockerfile("mycontainer", string.Empty)
                   .WithDockerfile(string.Empty);
        });

        Assert.AreEqual("contextPath", ex.ParamName);
    }

    [TestMethod]
    public void WithBuildArgsBeforeWithDockerfileThrows()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(b => b.AddMSTest(TestContext));

        var container = builder.AddContainer("mycontainer", "myimage");

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            container.WithBuildArg("MESSAGE", "hello");
        });

        Assert.AreEqual(
            "The resource does not have a Dockerfile build annotation. Call WithDockerfile before calling WithBuildArg.",
            ex.Message
            );
    }

    [TestMethod]
    public async Task WithDockerfileWithValidContextPathValidDockerfileWithImplicitDefaultNameSucceeds()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(b => b.AddMSTest(TestContext));

        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();

        var container = builder.AddContainer("mycontainer", "myimage")
                               .WithDockerfile(tempContextPath);

        var annotation = Assert.ContainsSingle(container.Resource.Annotations.OfType<DockerfileBuildAnnotation>());
        Assert.AreEqual(tempContextPath, annotation.ContextPath);
        Assert.AreEqual(tempDockerfilePath, annotation.DockerfilePath);
    }

    [TestMethod]
    public async Task AddDockerfileWithValidContextPathValidDockerfileWithImplicitDefaultNameSucceeds()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(b => b.AddMSTest(TestContext));

        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();

        var container = builder.AddDockerfile("mycontainer", tempContextPath);

        var annotation = Assert.ContainsSingle(container.Resource.Annotations.OfType<DockerfileBuildAnnotation>());
        Assert.AreEqual(tempContextPath, annotation.ContextPath);
        Assert.AreEqual(tempDockerfilePath, annotation.DockerfilePath);
    }

    [TestMethod]
    public async Task WithDockerfileWithValidContextPathValidDockerfileWithExplicitDefaultNameSucceeds()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(b => b.AddMSTest(TestContext));

        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();

        var container = builder.AddContainer("mycontainer", "myimage")
                               .WithDockerfile(tempContextPath, "Dockerfile");

        var annotation = Assert.ContainsSingle(container.Resource.Annotations.OfType<DockerfileBuildAnnotation>());
        Assert.AreEqual(tempContextPath, annotation.ContextPath);
        Assert.AreEqual(tempDockerfilePath, annotation.DockerfilePath);
    }

    [TestMethod]
    public async Task AddDockerfileWithValidContextPathValidDockerfileWithExplicitDefaultNameSucceeds()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(b => b.AddMSTest(TestContext));

        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();

        var container = builder.AddDockerfile("mycontainer", tempContextPath, "Dockerfile");

        var annotation = Assert.ContainsSingle(container.Resource.Annotations.OfType<DockerfileBuildAnnotation>());
        Assert.AreEqual(tempContextPath, annotation.ContextPath);
        Assert.AreEqual(tempDockerfilePath, annotation.DockerfilePath);
    }

    [TestMethod]
    public async Task WithDockerfileWithValidContextPathValidDockerfileWithExplicitNonDefaultNameSucceeds()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(b => b.AddMSTest(TestContext));

        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync("Otherdockerfile");

        var container = builder.AddContainer("mycontainer", "myimage")
                               .WithDockerfile(tempContextPath, "Otherdockerfile");

        var annotation = Assert.ContainsSingle(container.Resource.Annotations.OfType<DockerfileBuildAnnotation>());
        Assert.AreEqual(tempContextPath, annotation.ContextPath);
        Assert.AreEqual(tempDockerfilePath, annotation.DockerfilePath);
    }

    [TestMethod]
    public async Task AddDockerfileWithValidContextPathValidDockerfileWithExplicitNonDefaultNameSucceeds()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(b => b.AddMSTest(TestContext));

        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync("Otherdockerfile");

        var container = builder.AddDockerfile("mycontainer", tempContextPath, "Otherdockerfile");

        var annotation = Assert.ContainsSingle(container.Resource.Annotations.OfType<DockerfileBuildAnnotation>());
        Assert.AreEqual(tempContextPath, annotation.ContextPath);
        Assert.AreEqual(tempDockerfilePath, annotation.DockerfilePath);
    }

    [TestMethod]
    public async Task WithDockerfileWithValidContextPathValidDockerfileWithExplicitAbsoluteDefaultNameSucceeds()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(b => b.AddMSTest(TestContext));

        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();

        var container = builder.AddContainer("mycontainer", "myimage")
                               .WithDockerfile(tempContextPath, tempDockerfilePath);

        var annotation = Assert.ContainsSingle(container.Resource.Annotations.OfType<DockerfileBuildAnnotation>());
        Assert.AreEqual(tempContextPath, annotation.ContextPath);
        Assert.AreEqual(tempDockerfilePath, annotation.DockerfilePath);
    }

    [TestMethod]
    public async Task AddDockerfileWithValidContextPathValidDockerfileWithExplicitAbsoluteDefaultNameSucceeds()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(b => b.AddMSTest(TestContext));

        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();

        var container = builder.AddDockerfile("mycontainer", tempContextPath, tempDockerfilePath);

        var annotation = Assert.ContainsSingle(container.Resource.Annotations.OfType<DockerfileBuildAnnotation>());
        Assert.AreEqual(tempContextPath, annotation.ContextPath);
        Assert.AreEqual(tempDockerfilePath, annotation.DockerfilePath);
    }

    private static async Task<(string ContextPath, string DockerfilePath)> CreateTemporaryDockerfileAsync(string dockerfileName = "Dockerfile", bool createDockerfile = true, bool includeSecrets = false)
    {
        var tempContextPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempContextPath);

        var tempDockerfilePath = Path.Combine(tempContextPath, dockerfileName);

        if (createDockerfile)
        {
            var dockerfileTemplate = includeSecrets ? HelloWorldDockerfileWithSecrets : HelloWorldDockerfile;
            // We apply this random value to the Dockerfile to make sure that we get a clean
            // build each time with no possible caching.
            var cacheBuster = Guid.NewGuid();
            var dockerfileContent = dockerfileTemplate.Replace("!!!CACHEBUSTER!!!", cacheBuster.ToString());

            await File.WriteAllTextAsync(tempDockerfilePath, dockerfileContent);
        }

        return (tempContextPath, tempDockerfilePath);
    }

    private static async Task WaitForResourceAsync(DistributedApplication app, string resourceName, string resourceState, TimeSpan? timeout = null)
    {
        await app.ResourceNotifications.WaitForResourceAsync(resourceName, resourceState).WaitAsync(timeout ?? TimeSpan.FromMinutes(3));
    }

    private const string DefaultMessage = "aspire!";

    private const string HelloWorldDockerfile = $$"""
        FROM mcr.microsoft.com/cbl-mariner/base/nginx:1.22 AS builder
        ARG MESSAGE=aspire!
        RUN mkdir -p /usr/share/nginx/html
        RUN echo !!!CACHEBUSTER!!! > /usr/share/nginx/html/cachebuster.txt
        RUN echo ${MESSAGE} > /usr/share/nginx/html/aspire.html

        FROM mcr.microsoft.com/cbl-mariner/base/nginx:1.22 AS runner
        ARG MESSAGE
        RUN mkdir -p /usr/share/nginx/html
        COPY --from=builder /usr/share/nginx/html/cachebuster.txt /usr/share/nginx/html
        COPY --from=builder /usr/share/nginx/html/aspire.html /usr/share/nginx/html
        """;

    private const string HelloWorldDockerfileWithSecrets = $$"""
        FROM mcr.microsoft.com/cbl-mariner/base/nginx:1.22 AS builder
        ARG MESSAGE=aspire!
        RUN mkdir -p /usr/share/nginx/html
        RUN echo !!!CACHEBUSTER!!! > /usr/share/nginx/html/cachebuster.txt
        RUN echo ${MESSAGE} > /usr/share/nginx/html/aspire.html

        FROM mcr.microsoft.com/cbl-mariner/base/nginx:1.22 AS runner
        ARG MESSAGE
        RUN mkdir -p /usr/share/nginx/html
        COPY --from=builder /usr/share/nginx/html/cachebuster.txt /usr/share/nginx/html
        COPY --from=builder /usr/share/nginx/html/aspire.html /usr/share/nginx/html
        RUN --mount=type=secret,id=ENV_SECRET cp /run/secrets/ENV_SECRET /usr/share/nginx/html/ENV_SECRET.txt
        RUN chmod -R 777 /usr/share/nginx/html
        """;
}
