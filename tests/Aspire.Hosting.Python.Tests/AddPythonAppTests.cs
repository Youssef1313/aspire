// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Aspire.Hosting.Utils;
using Aspire.Hosting.Tests.Utils;
using System.Diagnostics;
using Aspire.Components.Common.Tests;
using Aspire.Hosting.ApplicationModel;
using System.Runtime.CompilerServices;

namespace Aspire.Hosting.Python.Tests;

[TestClass]
public class AddPythonAppTests
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    [RequiresTools(["python"])]
    public async Task AddPythonAppProducesDockerfileResourceInManifest()
    {
        var (projectDirectory, pythonExecutable, scriptName) = CreateTempPythonProject(TestContext);

        var manifestPath = Path.Combine(projectDirectory, "aspire-manifest.json");

        using var builder = TestDistributedApplicationBuilder.Create(options =>
        {
            GetProjectDirectoryRef(options) = Path.GetFullPath(projectDirectory);
            options.Args = ["--publisher", "manifest", "--output-path", manifestPath];
        }, TestContext);

        var pyproj = builder.AddPythonApp("pyproj", projectDirectory, scriptName);

        var manifest = await ManifestUtils.GetManifest(pyproj.Resource, manifestDirectory: projectDirectory);
        var expectedManifest = $$"""
            {
              "type": "container.v1",
              "build": {
                "context": ".",
                "dockerfile": "Dockerfile"
              }
            }
            """;
        Assert.AreEqual(expectedManifest, manifest.ToString(), ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);

        // If we don't throw, clean up the directories.
        Directory.Delete(projectDirectory, true);
    }

    [TestMethod]
    [RequiresTools(["python"])]
    public async Task AddInstrumentedPythonProjectProducesDockerfileResourceInManifest()
    {
        var (projectDirectory, pythonExecutable, scriptName) = CreateTempPythonProject(TestContext, instrument: true);

        var manifestPath = Path.Combine(projectDirectory, "aspire-manifest.json");

        using var builder = TestDistributedApplicationBuilder.Create(options =>
        {
            GetProjectDirectoryRef(options) = Path.GetFullPath(projectDirectory);
            options.Args = ["--publisher", "manifest", "--output-path", manifestPath];
        }, TestContext);

        var pyproj = builder.AddPythonApp("pyproj", projectDirectory, scriptName);

        var manifest = await ManifestUtils.GetManifest(pyproj.Resource, manifestDirectory: projectDirectory);
        var expectedManifest = $$"""
            {
              "type": "container.v1",
              "build": {
                "context": ".",
                "dockerfile": "Dockerfile"
              },
              "env": {
                "OTEL_PYTHON_LOGGING_AUTO_INSTRUMENTATION_ENABLED": "true"
              }
            }
            """;

        Assert.AreEqual(expectedManifest, manifest.ToString(), ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);

        // If we don't throw, clean up the directories.
        Directory.Delete(projectDirectory, true);
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_projectDirectory")]
    static extern ref string? GetProjectDirectoryRef(DistributedApplicationOptions? @this);

    [TestMethod]
    [RequiresTools(["python"])]
    public async Task PythonResourceFinishesSuccessfully()
    {
        var (projectDirectory, _, scriptName) = CreateTempPythonProject(TestContext);

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(TestContext);
        builder.AddPythonApp("pyproj", projectDirectory, scriptName);

        using var app = builder.Build();

        await app.StartAsync();

        await app.ResourceNotifications.WaitForResourceAsync("pyproj", "Finished").WaitAsync(TimeSpan.FromSeconds(30));

        await app.StopAsync();

        // If we don't throw, clean up the directories.
        Directory.Delete(projectDirectory, true);
    }

    [TestMethod]
    [RequiresTools(["python"])]
    public async Task PythonResourceSupportsWithReference()
    {
        var (projectDirectory, _, scriptName) = CreateTempPythonProject(TestContext);

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(TestContext);

        var externalResource = builder.AddConnectionString("connectionString");
        builder.Configuration["ConnectionStrings:connectionString"] = "test";

        var pyproj = builder.AddPythonApp("pyproj", projectDirectory, scriptName)
                            .WithReference(externalResource);

        var environmentVariables = await pyproj.Resource.GetEnvironmentVariableValuesAsync(DistributedApplicationOperation.Run);

        Assert.AreEqual("test", environmentVariables["ConnectionStrings__connectionString"]);

        // If we don't throw, clean up the directories.
        Directory.Delete(projectDirectory, true);
    }

    [TestMethod]
    [RequiresTools(["python"])]
    public async Task AddPythonApp_SetsResourcePropertiesCorrectly()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(TestContext);

        var (projectDirectory, pythonExecutable, scriptName) = CreateTempPythonProject(TestContext);

        builder.AddPythonApp("pythonProject", projectDirectory, scriptName);

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var executableResources = appModel.GetExecutableResources();

        var pythonProjectResource = Assert.ContainsSingle(executableResources);

        Assert.AreEqual("pythonProject", pythonProjectResource.Name);
        Assert.AreEqual(projectDirectory, pythonProjectResource.WorkingDirectory);

        if (OperatingSystem.IsWindows())
        {
            Assert.AreEqual(Path.Join(projectDirectory, ".venv", "Scripts", "python.exe"), pythonProjectResource.Command);
        }
        else
        {
            Assert.AreEqual(Path.Join(projectDirectory, ".venv", "bin", "python"), pythonProjectResource.Command);
        }

        var commandArguments = await ArgumentEvaluator.GetArgumentListAsync(pythonProjectResource);

        Assert.AreEqual(scriptName, commandArguments[0]);

        // If we don't throw, clean up the directories.
        Directory.Delete(projectDirectory, true);
    }

    [TestMethod]
    [RequiresTools(["python"])]
    public async Task AddPythonAppWithInstrumentation_SwitchesExecutableToInstrumentationExecutable()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(TestContext);

        var (projectDirectory, pythonExecutable, scriptName) = CreateTempPythonProject(TestContext, instrument: true);

        builder.AddPythonApp("pythonProject", projectDirectory, scriptName, virtualEnvironmentPath: ".venv");

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var executableResources = appModel.GetExecutableResources();

        var pythonProjectResource = Assert.ContainsSingle(executableResources);
        var commandArguments = await ArgumentEvaluator.GetArgumentListAsync(pythonProjectResource);

        if (OperatingSystem.IsWindows())
        {
            Assert.AreEqual(Path.Join(projectDirectory, ".venv", "Scripts", "opentelemetry-instrument.exe"), pythonProjectResource.Command);
        }
        else
        {
            Assert.AreEqual(Path.Join(projectDirectory, ".venv", "bin", "opentelemetry-instrument"), pythonProjectResource.Command);
        }

        Assert.AreEqual("--traces_exporter", commandArguments[0]);
        Assert.AreEqual("otlp", commandArguments[1]);
        Assert.AreEqual("--logs_exporter", commandArguments[2]);
        Assert.AreEqual("console,otlp", commandArguments[3]);
        Assert.AreEqual("--metrics_exporter", commandArguments[4]);
        Assert.AreEqual("otlp", commandArguments[5]);
        Assert.AreEqual(pythonExecutable, commandArguments[6]);
        Assert.AreEqual(scriptName, commandArguments[7]);

        // If we don't throw, clean up the directories.
        Directory.Delete(projectDirectory, true);
    }

    [TestMethod]
    [RequiresTools(["python"])]
    public async Task AddPythonAppWithScriptArgs_IncludesTheArguments()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(TestContext);

        var (projectDirectory, pythonExecutable, scriptName) = CreateTempPythonProject(TestContext);

        builder.AddPythonApp("pythonProject", projectDirectory, scriptName, scriptArgs: "test");

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var executableResources = appModel.GetExecutableResources();

        var pythonProjectResource = Assert.ContainsSingle(executableResources);

        Assert.AreEqual("pythonProject", pythonProjectResource.Name);
        Assert.AreEqual(projectDirectory, pythonProjectResource.WorkingDirectory);

        if (OperatingSystem.IsWindows())
        {
            Assert.AreEqual(Path.Join(projectDirectory, ".venv", "Scripts", "python.exe"), pythonProjectResource.Command);
        }
        else
        {
            Assert.AreEqual(Path.Join(projectDirectory, ".venv", "bin", "python"), pythonProjectResource.Command);
        }

        var commandArguments = await ArgumentEvaluator.GetArgumentListAsync(pythonProjectResource);

        Assert.AreEqual(scriptName, commandArguments[0]);
        Assert.AreEqual("test", commandArguments[1]);

        // If we don't throw, clean up the directories.
        Directory.Delete(projectDirectory, true);
    }

    private static (string projectDirectory, string pythonExecutable, string scriptName) CreateTempPythonProject(TestContext testContext, bool instrument = false)
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(projectDirectory);

        if (instrument)
        {
            PreparePythonProject(testContext, projectDirectory, PythonApp, InstrumentedPythonAppRequirements);
        }
        else
        {
            PreparePythonProject(testContext, projectDirectory, PythonApp);
        }

        var pythonExecutable = Path.Combine(projectDirectory,
            ".venv",
            OperatingSystem.IsWindows() ? "Scripts" : "bin",
            OperatingSystem.IsWindows() ? "python.exe" : "python"
            );

        return (projectDirectory, pythonExecutable, "main.py");
    }

    private static void PreparePythonProject(TestContext testContext, string projectDirectory, string scriptContent, string? requirementsContent = null)
    {
        var scriptPath = Path.Combine(projectDirectory, "main.py");
        File.WriteAllText(scriptPath, scriptContent);

        var requirementsPath = Path.Combine(projectDirectory, "requirements.txt");
        File.WriteAllText(requirementsPath, requirementsContent);

        // This dockerfile doesn't *need* to work but it's a good sanity check.
        var dockerFilePath = Path.Combine(projectDirectory, "Dockerfile");
        File.WriteAllText(dockerFilePath,
            """
            FROM python:3.9
            WORKDIR /app
            COPY requirements.txt .
            RUN pip install --no-cache-dir -r requirements.txt
            COPY . .
            CMD ["python", "main.py"]
            """);

        var prepareVirtualEnvironmentStartInfo = new ProcessStartInfo()
        {
            FileName = "python",
            Arguments = $"-m venv .venv",
            WorkingDirectory = projectDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        var createVirtualEnvironmentProcess = Process.Start(prepareVirtualEnvironmentStartInfo);
        var createVirtualEnvironmentProcessResult = createVirtualEnvironmentProcess!.WaitForExit(TimeSpan.FromMinutes(2));

        testContext.WriteLine("Create Virtual Environment Standard Output:");

        CopyStreamToTestOutput("python -m venv .venv (Standard Output)", createVirtualEnvironmentProcess.StandardOutput, testContext);
        CopyStreamToTestOutput("python -m venv .venv (Standard Error)", createVirtualEnvironmentProcess.StandardError, testContext);

        if (!createVirtualEnvironmentProcessResult)
        {
            createVirtualEnvironmentProcess.Kill(true);
            throw new InvalidOperationException("Failed to create virtual environment.");
        }

        var relativePipPath = Path.Combine(
            ".venv",
            OperatingSystem.IsWindows() ? "Scripts" : "bin",
            OperatingSystem.IsWindows() ? "pip.exe" : "pip"
            );
        var pipPath = Path.GetFullPath(relativePipPath, projectDirectory);

        var installRequirementsStartInfo = new ProcessStartInfo()
        {
            FileName = pipPath,
            Arguments = $"install -q -r requirements.txt",
            WorkingDirectory = projectDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        var installRequirementsProcess = Process.Start(installRequirementsStartInfo);
        var installRequirementsProcessResult = installRequirementsProcess!.WaitForExit(TimeSpan.FromMinutes(2));

        CopyStreamToTestOutput("pip install -r requirements.txt (Standard Output)", installRequirementsProcess.StandardOutput, testContext);
        CopyStreamToTestOutput("pip install -r requirements.txt (Standard Error)", installRequirementsProcess.StandardError, testContext);

        if (!installRequirementsProcessResult)
        {
            installRequirementsProcess.Kill(true);
            throw new InvalidOperationException("Failed to install requirements.");
        }
    }

    private static void CopyStreamToTestOutput(string label, StreamReader reader, TestContext testContext)
    {
        var output = reader.ReadToEnd();
        testContext.WriteLine($"{label}:\n\n{output}");
    }

    private const string PythonApp = """"
        import logging

        # Reset the logging configuration to a sensible default.
        logging.basicConfig()
        logging.getLogger().setLevel(logging.NOTSET)

        # Write a basic log message.
        logging.getLogger(__name__).info("Hello world!")
        """";

    private const string InstrumentedPythonAppRequirements = """"
        opentelemetry-distro[otlp]
        """";
}
