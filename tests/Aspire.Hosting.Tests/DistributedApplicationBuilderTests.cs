// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dashboard;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Devcontainers;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Tests;

[TestClass]
public class DistributedApplicationBuilderTests
{
    [TestMethod]
    [DataRow(new string[0], DistributedApplicationOperation.Run)]
    [DataRow(new string[] { "--publisher", "manifest" }, DistributedApplicationOperation.Publish)]
    public void BuilderExecutionContextExposesCorrectOperation(string[] args, DistributedApplicationOperation operation)
    {
        var builder = DistributedApplication.CreateBuilder(args);
        Assert.AreEqual(operation, builder.ExecutionContext.Operation);
    }

    [TestMethod]
    public void BuilderAddsDefaultServices()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.Services.Configure<DcpOptions>(o =>
        {
            o.DashboardPath = "dashboard";
            o.CliPath = "dcp";
        });

        using var app = appBuilder.Build();

        Assert.IsNotNull(app.Services.GetRequiredKeyedService<IDistributedApplicationPublisher>("manifest"));

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        Assert.IsEmpty(appModel.Resources);

        var lifecycles = app.Services.GetServices<IDistributedApplicationLifecycleHook>();
        Assert.That.Collection(
            lifecycles,
            s => Assert.IsType<DashboardLifecycleHook>(s),
            s => Assert.IsType<DevcontainerPortForwardingLifecycleHook>(s)
        );

        var options = app.Services.GetRequiredService<IOptions<PublishingOptions>>();
        Assert.IsNull(options.Value.Publisher);
        Assert.IsNull(options.Value.OutputPath);
    }

    [TestMethod]
    public void BuilderAddsResourceToAddModel()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddResource(new TestResource());
        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var resource = Assert.ContainsSingle(appModel.Resources);
        Assert.IsType<TestResource>(resource);
    }

    [TestMethod]
    public void BuilderConfiguresPublishingOptionsFromCommandLine()
    {
        var appBuilder = DistributedApplication.CreateBuilder(["--publisher", "manifest", "--output-path", "/tmp/"]);
        using var app = appBuilder.Build();

        var publishOptions = app.Services.GetRequiredService<IOptions<PublishingOptions>>();
        Assert.AreEqual("manifest", publishOptions.Value.Publisher);
        Assert.AreEqual("/tmp/", publishOptions.Value.OutputPath);
    }

    [TestMethod]
    public void BuilderConfiguresPublishingOptionsFromConfig()
    {
        var appBuilder = DistributedApplication.CreateBuilder(["--publisher", "manifest", "--output-path", "/tmp/"]);
        appBuilder.Configuration["Publishing:Publisher"] = "docker";
        appBuilder.Configuration["Publishing:OutputPath"] = "/path/";
        using var app = appBuilder.Build();

        var publishOptions = app.Services.GetRequiredService<IOptions<PublishingOptions>>();
        Assert.AreEqual("docker", publishOptions.Value.Publisher);
        Assert.AreEqual("/path/", publishOptions.Value.OutputPath);
    }

    [TestMethod]
    public void AppHostDirectoryAvailableViaConfig()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var appHostDirectory = appBuilder.AppHostDirectory;
        using var app = appBuilder.Build();

        var config = app.Services.GetRequiredService<IConfiguration>();
        Assert.AreEqual(appHostDirectory, config["AppHost:Directory"]);
    }

    [TestMethod]
    public void ResourceServiceConfig_Secured()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        using var app = appBuilder.Build();

        var config = app.Services.GetRequiredService<IConfiguration>();
        Assert.AreEqual(nameof(ResourceServiceAuthMode.ApiKey), config["AppHost:ResourceService:AuthMode"]);
        Assert.IsFalse(string.IsNullOrEmpty(config["AppHost:ResourceService:ApiKey"]));
    }

    [TestMethod]
    public void ResourceServiceConfig_Unsecured()
    {
        var appBuilder = DistributedApplication.CreateBuilder(args: [$"{KnownConfigNames.DashboardUnsecuredAllowAnonymous}=true"]);
        using var app = appBuilder.Build();

        var config = app.Services.GetRequiredService<IConfiguration>();
        Assert.AreEqual(nameof(ResourceServiceAuthMode.Unsecured), config["AppHost:ResourceService:AuthMode"]);
        Assert.IsTrue(string.IsNullOrEmpty(config["AppHost:ResourceService:ApiKey"]));
    }

    [TestMethod]
    public void AddResource_DuplicateResourceNames_SameCasing_Error()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddResource(new ContainerResource("Test"));

        var ex = Assert.Throws<DistributedApplicationException>(() => appBuilder.AddResource(new ContainerResource("Test")));
        Assert.AreEqual("Cannot add resource of type 'Aspire.Hosting.ApplicationModel.ContainerResource' with name 'Test' because resource of type 'Aspire.Hosting.ApplicationModel.ContainerResource' with that name already exists. Resource names are case-insensitive.", ex.Message);
    }

    [TestMethod]
    public void AddResource_DuplicateResourceNames_MixedCasing_Error()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddResource(new ContainerResource("Test"));

        var ex = Assert.Throws<DistributedApplicationException>(() => appBuilder.AddResource(new ContainerResource("TEST")));
        Assert.AreEqual("Cannot add resource of type 'Aspire.Hosting.ApplicationModel.ContainerResource' with name 'TEST' because resource of type 'Aspire.Hosting.ApplicationModel.ContainerResource' with that name already exists. Resource names are case-insensitive.", ex.Message);
    }

    [TestMethod]
    public void Build_DuplicateResourceNames_MixedCasing_Error()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.Resources.Add(new ContainerResource("Test"));
        appBuilder.Resources.Add(new ContainerResource("Test"));

        var ex = Assert.Throws<DistributedApplicationException>(appBuilder.Build);
        Assert.AreEqual("Multiple resources with the name 'Test'. Resource names are case-insensitive.", ex.Message);
    }

    [TestMethod]
    public void Build_DuplicateResourceNames_SameCasing_Error()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.Resources.Add(new ContainerResource("Test"));
        appBuilder.Resources.Add(new ContainerResource("TEST"));

        var ex = Assert.Throws<DistributedApplicationException>(appBuilder.Build);
        Assert.AreEqual("Multiple resources with the name 'Test'. Resource names are case-insensitive.", ex.Message);
    }

    private sealed class TestResource : IResource
    {
        public string Name => nameof(TestResource);

        public ResourceAnnotationCollection Annotations => throw new NotImplementedException();
    }
}
