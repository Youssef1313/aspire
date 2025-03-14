// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Azure.Provisioning.AppContainers;

namespace Aspire.Hosting.Azure.Tests.PublicApiTests;

[TestClass]
public class AppContainersPublicApiTests
{
    [TestMethod]
    public void PublishAsAzureContainerAppShouldThrowWhenContainerIsNull()
    {
        IResourceBuilder<ContainerResource> container = null!;
        Action<AzureResourceInfrastructure, ContainerApp> configure = (r, c) => { };

        var action = () => container.PublishAsAzureContainerApp(configure);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(container), exception.ParamName);
    }

    [TestMethod]
    public void PublishAsAzureContainerAppShouldThrowWhenConfigureIsNull()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("api", "myimage");
        Action<AzureResourceInfrastructure, ContainerApp> configure = null!;

        var action = () => container.PublishAsAzureContainerApp(configure);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(configure), exception.ParamName);
    }

    [TestMethod]
    public void CtorAzureContainerAppCustomizationAnnotationShouldThrowWhenConfigureIsNull()
    {
        Action<AzureResourceInfrastructure, ContainerApp> configure = null!;

        var action = () => new AzureContainerAppCustomizationAnnotation(configure);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(configure), exception.ParamName);
    }

    [TestMethod]
    public void PublishAsAzureContainerAppShouldThrowWhenExecutableIsNull()
    {
        IResourceBuilder<ExecutableResource> executable = null!;
        Action<AzureResourceInfrastructure, ContainerApp> configure = (r, c) => { };

        var action = () => executable.PublishAsAzureContainerApp(configure);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(executable), exception.ParamName);
    }

    [TestMethod]
    public void PublishAsAzureContainerAppForExecutableShouldThrowWhenConfigureIsNull()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var executable = builder.AddExecutable("api", "node.exe", Environment.CurrentDirectory);
        Action<AzureResourceInfrastructure, ContainerApp> configure = null!;

        var action = () => executable.PublishAsAzureContainerApp(configure);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(configure), exception.ParamName);
    }

    [TestMethod]
    public void AddAzureContainerAppsInfrastructureShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;

        var action = () =>
        {
            builder.AddAzureContainerAppsInfrastructure();
        };

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void PublishAsAzureContainerAppShouldThrowWhenProjectIsNull()
    {
        IResourceBuilder<ProjectResource> project = null!;
        Action<AzureResourceInfrastructure, ContainerApp> configure = (r, c) => { };

        var action = () => project.PublishAsAzureContainerApp(configure);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(project), exception.ParamName);
    }

    [TestMethod]
    public void PublishAsAzureContainerAppForProjectShouldThrowWhenConfigureIsNull()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<ProjectA>("serviceA", o => o.ExcludeLaunchProfile = true);
        Action<AzureResourceInfrastructure, ContainerApp> configure = null!;

        var action = () => project.PublishAsAzureContainerApp(configure);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(configure), exception.ParamName);
    }

    [TestMethod]
    [Experimental("ASPIREACADOMAINS001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public void ConfigureCustomDomainShouldThrowWhenAppIsNull()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        ContainerApp app = null!;
        var customDomain = builder.AddParameter("customDomain");
        var certificateName = builder.AddParameter("certificateName");

        var action = () => app.ConfigureCustomDomain(customDomain, certificateName);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(app), exception.ParamName);
    }

    [TestMethod]
    [Experimental("ASPIREACADOMAINS001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public void ConfigureCustomDomainShouldThrowWhenCustomDomainIsNull()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = new ContainerApp("app");
        IResourceBuilder<ParameterResource> customDomain = null!;
        var certificateName = builder.AddParameter("certificateName");

        var action = () => app.ConfigureCustomDomain(customDomain, certificateName);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(customDomain), exception.ParamName);
    }

    [TestMethod]
    [Experimental("ASPIREACADOMAINS001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public void ConfigureCustomDomainShouldThrowWhenCertificateNameIsNull()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var app = new ContainerApp("app");
        var customDomain = builder.AddParameter("customDomain");
        IResourceBuilder<ParameterResource> certificateName = null!;

        var action = () => app.ConfigureCustomDomain(customDomain, certificateName);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(certificateName), exception.ParamName);
    }

    private sealed class ProjectA : IProjectMetadata
    {
        public string ProjectPath => "projectA";
    }
}
