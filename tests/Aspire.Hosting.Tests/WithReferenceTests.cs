// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.InternalTesting;

namespace Aspire.Hosting.Tests;

[TestClass]
public class WithReferenceTests
{
    [TestMethod]
    [DataRow("mybinding")]
    [DataRow("MYbinding")]
    public async Task ResourceWithSingleEndpointProducesSimplifiedEnvironmentVariables(string endpointName)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        // Create a binding and its matching annotation (simulating DCP behavior)
        var projectA = builder.AddProject<ProjectA>("projecta")
                .WithHttpsEndpoint(1000, 2000, "mybinding")
                .WithEndpoint("mybinding", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000));

        // Get the service provider.
        var projectB = builder.AddProject<ProjectB>("b").WithReference(projectA.GetEndpoint(endpointName));

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(projectB.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

        Assert.AreEqual("https://localhost:2000", config["services__projecta__mybinding__0"]);
    }

    [TestMethod]
    public async Task ResourceWithConflictingEndpointsProducesFullyScopedEnvironmentVariables()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var projectA = builder.AddProject<ProjectA>("projecta")
                              .WithHttpsEndpoint(1000, 2000, "mybinding")
                              .WithEndpoint("mybinding", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000))
                              .WithHttpsEndpoint(1000, 3000, "myconflictingbinding")
                              // Create a binding and its matching annotation (simulating DCP behavior) - HOWEVER
                              // this binding conflicts with the earlier because they have the same scheme.
                              .WithEndpoint("myconflictingbinding", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 3000));

        var projectB = builder.AddProject<ProjectB>("projectb")
               .WithReference(projectA.GetEndpoint("mybinding"))
               .WithReference(projectA.GetEndpoint("myconflictingbinding"));

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(projectB.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

        Assert.AreEqual("https://localhost:2000", config["services__projecta__mybinding__0"]);
        Assert.AreEqual("https://localhost:3000", config["services__projecta__myconflictingbinding__0"]);
    }

    [TestMethod]
    public async Task ResourceWithNonConflictingEndpointsProducesAllVariantsOfEnvironmentVariables()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        // Create a binding and its matching annotation (simulating DCP behavior)
        var projectA = builder.AddProject<ProjectA>("projecta")
                              .WithHttpsEndpoint(1000, 2000, "mybinding")
                              .WithEndpoint("mybinding", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000))
                              // Create a binding and its matching annotation (simulating DCP behavior) - not
                              // conflicting because the scheme is different to the first binding.
                              .WithHttpEndpoint(1000, 3000, "mynonconflictingbinding")
                              .WithEndpoint("mynonconflictingbinding", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 3000));

        var projectB = builder.AddProject<ProjectB>("projectb")
                              .WithReference(projectA.GetEndpoint("mybinding"))
                              .WithReference(projectA.GetEndpoint("mynonconflictingbinding"));

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(projectB.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

        Assert.AreEqual("https://localhost:2000", config["services__projecta__mybinding__0"]);
        Assert.AreEqual("http://localhost:3000", config["services__projecta__mynonconflictingbinding__0"]);
    }

    [TestMethod]
    public async Task ResourceWithConflictingEndpointsProducesAllEnvironmentVariables()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        // Create a binding and its matching annotation (simulating DCP behavior)
        var projectA = builder.AddProject<ProjectA>("projecta")
                              .WithHttpsEndpoint(1000, 2000, "mybinding")
                              .WithEndpoint("mybinding", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000))
                              .WithHttpsEndpoint(1000, 3000, "mybinding2")
                              .WithEndpoint("mybinding2", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 3000));

        // Get the service provider.
        var projectB = builder.AddProject<ProjectB>("projectb")
                              .WithReference(projectA);

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(projectB.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

        Assert.AreEqual("https://localhost:2000", config["services__projecta__mybinding__0"]);
        Assert.AreEqual("https://localhost:3000", config["services__projecta__mybinding2__0"]);
    }

    [TestMethod]
    public async Task ResourceWithEndpointsProducesAllEnvironmentVariables()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var projectA = builder.AddProject<ProjectA>("projecta")
                              .WithHttpsEndpoint(1000, 2000, "mybinding")
                              .WithEndpoint("mybinding", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000))
                              .WithHttpEndpoint(1000, 3000, "mybinding2")
                              .WithEndpoint("mybinding2", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 3000));

        // Get the service provider.
        var projectB = builder.AddProject<ProjectB>("projectb")
                              .WithReference(projectA);
        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(projectB.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

        Assert.AreEqual("https://localhost:2000", config["services__projecta__mybinding__0"]);
        Assert.AreEqual("http://localhost:3000", config["services__projecta__mybinding2__0"]);
    }

    [TestMethod]
    public async Task ConnectionStringResourceThrowsWhenMissingConnectionString()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        // Get the service provider.
        var resource = builder.AddResource(new TestResource("resource"));
        var projectB = builder.AddProject<ProjectB>("projectb").WithReference(resource, optional: false);

        // Call environment variable callbacks.
        await Assert.ThrowsAsync<DistributedApplicationException>(async () =>
        {
            await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(projectB.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);
        }).DefaultTimeout();
    }

    [TestMethod]
    public async Task ConnectionStringResourceOptionalWithMissingConnectionString()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        // Get the service provider.
        var resource = builder.AddResource(new TestResource("resource"));
        var projectB = builder.AddProject<ProjectB>("projectB")
                              .WithReference(resource, optional: true);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(projectB.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("ConnectionStrings__"));
        Assert.AreEqual(0, servicesKeysCount);
    }

    [TestMethod]
    public async Task ParameterAsConnectionStringResourceThrowsWhenConnectionStringSectionMissing()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        // Get the service provider.
        var missingResource = builder.AddConnectionString("missingresource");
        var projectB = builder.AddProject<ProjectB>("projectb")
                              .WithReference(missingResource);

        // Call environment variable callbacks.
        var exception = await Assert.ThrowsAsync<DistributedApplicationException>(async () =>
        {
            var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(projectB.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);
        }).DefaultTimeout();

        Assert.AreEqual("Connection string parameter resource could not be used because connection string 'missingresource' is missing.", exception.Message);
    }

    [TestMethod]
    public async Task ParameterAsConnectionStringResourceInjectsConnectionStringWhenPresent()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.Configuration["ConnectionStrings:resource"] = "test connection string";

        // Get the service provider.
        var resource = builder.AddConnectionString("resource");
        var projectB = builder.AddProject<ProjectB>("projectb")
                             .WithReference(resource);

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(projectB.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

        Assert.AreEqual("test connection string", config["ConnectionStrings__resource"]);
    }

    [TestMethod]
    public async Task ParameterAsConnectionStringResourceInjectsExpressionWhenPublishingManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        // Get the service provider.
        var resource = builder.AddConnectionString("resource");
        var projectB = builder.AddProject<ProjectB>("projectb")
                       .WithReference(resource);

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(projectB.Resource, DistributedApplicationOperation.Publish).DefaultTimeout();

        Assert.AreEqual("{resource.connectionString}", config["ConnectionStrings__resource"]);
    }

    [TestMethod]
    public async Task ParameterAsConnectionStringResourceInjectsCorrectEnvWhenPublishingManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        // Get the service provider.
        var resource = builder.AddConnectionString("resource", "MY_ENV");
        var projectB = builder.AddProject<ProjectB>("projectb")
                              .WithReference(resource);

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(projectB.Resource, DistributedApplicationOperation.Publish).DefaultTimeout();

        Assert.AreEqual("{resource.connectionString}", config["MY_ENV"]);
    }

    [TestMethod]
    public async Task ConnectionStringResourceWithConnectionString()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        // Get the service provider.
        var resource = builder.AddResource(new TestResource("resource")
        {
            ConnectionString = "123"
        });
        var projectB = builder.AddProject<ProjectB>("projectb")
                              .WithReference(resource);

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(projectB.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("ConnectionStrings__"));
        Assert.AreEqual(1, servicesKeysCount);
        Assert.Contains(config, kvp => kvp.Key == "ConnectionStrings__resource" && kvp.Value == "123");
    }

    [TestMethod]
    public async Task ConnectionStringResourceWithExpressiononnectionString()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var endpoint = builder.AddParameter("endpoint", "http://localhost:3452");
        var key = builder.AddParameter("key", "secretKey", secret: true);

        var cs = ReferenceExpression.Create($"Endpoint={endpoint};Key={key}");
        
        // Get the service provider.
        var resource = builder.AddConnectionString("cs", cs);

        var projectB = builder.AddProject<ProjectB>("projectb")
                              .WithReference(resource);

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(projectB.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("ConnectionStrings__"));
        Assert.AreEqual(1, servicesKeysCount);
        Assert.AreEqual("Endpoint=http://localhost:3452;Key=secretKey", config["ConnectionStrings__cs"]);
    }

    [TestMethod]
    public async Task ConnectionStringResourceWithConnectionStringOverwriteName()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        // Get the service provider.
        var resource = builder.AddResource(new TestResource("resource")
        {
            ConnectionString = "123"
        });

        var projectB = builder.AddProject<ProjectB>("projectb")
                              .WithReference(resource, connectionName: "bob");

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(projectB.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("ConnectionStrings__"));
        Assert.AreEqual(1, servicesKeysCount);
        Assert.Contains(config, kvp => kvp.Key == "ConnectionStrings__bob" && kvp.Value == "123");
    }

    [TestMethod]
    public void WithReferenceHttpRelativeUriThrowsException()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        Assert.Throws<InvalidOperationException>(() => builder.AddProject<ProjectA>("projecta").WithReference("petstore", new Uri("petstore.swagger.io", UriKind.Relative)));
    }

    [TestMethod]
    public void WithReferenceHttpUriThrowsException()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        Assert.Throws<InvalidOperationException>(() => builder.AddProject<ProjectA>("projecta").WithReference("petstore", new Uri("https://petstore.swagger.io/v2")));
    }

    [TestMethod]
    public async Task WithReferenceHttpProduceEnvironmentVariables()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var projectA = builder.AddProject<ProjectA>("projecta")
                               .WithReference("petstore", new Uri("https://petstore.swagger.io/"));

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(projectA.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("services__"));
        Assert.AreEqual(1, servicesKeysCount);
        Assert.Contains(config, kvp => kvp.Key == "services__petstore__default__0" && kvp.Value == "https://petstore.swagger.io/");
    }

    private sealed class TestResource(string name) : Resource(name), IResourceWithConnectionString
    {
        public string? ConnectionString { get; set; }

        public ReferenceExpression ConnectionStringExpression =>
            ReferenceExpression.Create($"{ConnectionString}");
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
