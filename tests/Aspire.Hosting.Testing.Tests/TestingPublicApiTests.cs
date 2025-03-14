
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting.Testing.Tests;

[TestClass]
public class TestingPublicApiTests
{
    [TestMethod]
    public void CtorDistributedApplicationFactoryThrowsWhenEntryPointIsNull()
    {
        Type entryPoint = null!;

        var action = () => new DistributedApplicationFactory(entryPoint);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(entryPoint), exception.ParamName);
    }

    [TestMethod]
    public void CtorDistributedApplicationFactoryWithArgsThrowsWhenEntryPointIsNull()
    {
        Type entryPoint = null!;
        string[] args = [];

        var action = () => new DistributedApplicationFactory(entryPoint, args);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(entryPoint), exception.ParamName);
    }

    [TestMethod]
    public void CtorDistributedApplicationFactoryWithArgsThrowsWhenArgsIsNull()
    {
        Type entryPoint = typeof(Projects.TestingAppHost1_AppHost);
        string[] args = null!;

        var action = () => new DistributedApplicationFactory(entryPoint, args);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(args), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void CtorDistributedApplicationFactoryWithArgsThrowsWhenArgsContainsIsNullOrEmpty(bool isNull)
    {
        Type entryPoint = typeof(Projects.TestingAppHost1_AppHost);
        string[] args = ["arg", isNull ? null! : string.Empty, "arg2"];

        var action = () => new DistributedApplicationFactory(entryPoint, args);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(args), exception.ParamName);
        Assert.AreEqual(isNull
            ? "Array params contains null item: [arg, , arg2] (Parameter 'args')"
            : "Array params contains empty item: [arg, , arg2] (Parameter 'args')",
            exception.Message);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void CreateHttpClientThrowsWhenResourceNameIsNullOrEmpty(bool isNull)
    {
        Type entryPoint = typeof(Projects.TestingAppHost1_AppHost);
        var distributedApplicationFactory = new DistributedApplicationFactory(entryPoint);
        string resourceName = isNull ? null! : string.Empty;

        var action = () => distributedApplicationFactory.CreateHttpClient(resourceName);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(resourceName), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task GetConnectionStringThrowsWhenResourceNameIsNullOrEmpty(bool isNull)
    {
        Type entryPoint = typeof(Projects.TestingAppHost1_AppHost);
        var distributedApplicationFactory = new DistributedApplicationFactory(entryPoint);
        string resourceName = isNull ? null! : string.Empty;

        var action = async () => await distributedApplicationFactory.GetConnectionString(resourceName);

        var exception = isNull
            ? await Assert.ThrowsAsync<ArgumentNullException>(action)
            : await Assert.ThrowsAsync<ArgumentException>(action);
        Assert.AreEqual(nameof(resourceName), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void GetEndpointThrowsWhenResourceNameIsNullOrEmpty(bool isNull)
    {
        Type entryPoint = typeof(Projects.TestingAppHost1_AppHost);
        var distributedApplicationFactory = new DistributedApplicationFactory(entryPoint);
        string resourceName = isNull ? null! : string.Empty;

        var action = () => distributedApplicationFactory.GetEndpoint(resourceName);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(resourceName), exception.ParamName);
    }

    [TestMethod]
    public void CreateHttpClientThrowsWhenDistributedApplicationIsNull()
    {
        DistributedApplication app = null!;
        var resourceName = "application";

        var action = () => app.CreateHttpClient(resourceName);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(app), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void CreateHttpClientWithResourceNameThrowsWhenResourceNameIsNullOrEmpty(bool isNull)
    {
        var distributedApplication = DistributedApplication.CreateBuilder().Build();
        string resourceName = isNull ? null! : string.Empty;

        var action = () => distributedApplication.CreateHttpClient(resourceName);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(resourceName), exception.ParamName);
    }

    [TestMethod]
    public async Task GetConnectionStringAsyncThrowsWhenDistributedApplicationIsNull()
    {
        DistributedApplication app = null!;
        var resourceName = "application";

        var action = async () => await app.GetConnectionStringAsync(resourceName);

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(action);
        Assert.AreEqual(nameof(app), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task GetConnectionStringAsyncWithResourceNameThrowsWhenResourceNameIsNullOrEmpty(bool isNull)
    {
        var distributedApplication = DistributedApplication.CreateBuilder().Build();
        string resourceName = isNull ? null! : string.Empty;

        var action = async () => await distributedApplication.GetConnectionStringAsync(resourceName);

        var exception = isNull
            ? await Assert.ThrowsAsync<ArgumentNullException>(action)
            : await Assert.ThrowsAsync<ArgumentException>(action);
        Assert.AreEqual(nameof(resourceName), exception.ParamName);
    }

    [TestMethod]
    public void GetEndpointThrowsWhenDistributedApplicationIsNull()
    {
        DistributedApplication app = null!;
        var resourceName = "application";

        var action = () => app.GetEndpoint(resourceName);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(app), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void GetEndpointClientWithResourceNameThrowsWhenResourceNameIsNullOrEmpty(bool isNull)
    {
        var distributedApplication = DistributedApplication.CreateBuilder().Build();
        string resourceName = isNull ? null! : string.Empty;

        var action = () => distributedApplication.GetEndpoint(resourceName);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(resourceName), exception.ParamName);
    }

    [TestMethod]
    public async Task CreateAsyncWithEntryPointThrowsWhenEntryPointIsNull()
    {
        Type entryPoint = null!;

        var action = () => DistributedApplicationTestingBuilder.CreateAsync(entryPoint);

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(action);
        Assert.AreEqual(nameof(entryPoint), exception.ParamName);
    }

    [TestMethod]
    public async Task CreateAsyncWithArgsThrowsWhenArgsIsNull()
    {
        string[] args = null!;

        var action = () => DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>(args);

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(action);
        Assert.AreEqual(nameof(args), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task CreateAsyncWithArgsThrowsWhenArgsContainsIsNullOrEmpty(bool isNull)
    {
        string[] args = ["arg", isNull ? null! : string.Empty, "arg2"];

        var action = () => DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>(args);

        var exception = isNull
            ? await Assert.ThrowsAsync<ArgumentNullException>(action)
            : await Assert.ThrowsAsync<ArgumentException>(action);
        Assert.AreEqual(nameof(args), exception.ParamName);
        Assert.AreEqual(isNull
            ? "Array params contains null item: [arg, , arg2] (Parameter 'args')"
            : "Array params contains empty item: [arg, , arg2] (Parameter 'args')",
            exception.Message);
    }

    [TestMethod]
    public async Task CreateAsyncWithEntryPointAndArgsThrowsWhenEntryPointIsNull()
    {
        Type entryPoint = null!;
        string[] args = [];

        var action = () => DistributedApplicationTestingBuilder.CreateAsync(entryPoint, args);

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(action);
        Assert.AreEqual(nameof(entryPoint), exception.ParamName);
    }

    [TestMethod]
    public async Task CreateAsyncWithEntryPointAndArgsThrowsWhenArgsIsNull()
    {
        Type entryPoint = typeof(Projects.TestingAppHost1_AppHost);
        string[] args = null!;

        var action = () => DistributedApplicationTestingBuilder.CreateAsync(entryPoint, args);

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(action);
        Assert.AreEqual(nameof(args), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task CreateAsyncWithEntryPointAndArgsThrowsWhenArgsContainsIsNullOrEmpty(bool isNull)
    {
        Type entryPoint = typeof(Projects.TestingAppHost1_AppHost);
        string[] args = ["arg", isNull ? null! : string.Empty, "arg2"];

        var action = () => DistributedApplicationTestingBuilder.CreateAsync(entryPoint, args);

        var exception = isNull
            ? await Assert.ThrowsAsync<ArgumentNullException>(action)
            : await Assert.ThrowsAsync<ArgumentException>(action);
        Assert.AreEqual(nameof(args), exception.ParamName);
        Assert.AreEqual(isNull
            ? "Array params contains null item: [arg, , arg2] (Parameter 'args')"
            : "Array params contains empty item: [arg, , arg2] (Parameter 'args')",
            exception.Message);
    }

    [TestMethod]
    public async Task CreateAsyncWithArgsAndConfigureBuilderThrowsWhenArgsIsNull()
    {
        string[] args = null!;
        Action<DistributedApplicationOptions, HostApplicationBuilderSettings> configureBuilder = (_, _) => { };

        var action = () => DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>(args, configureBuilder);

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(action);
        Assert.AreEqual(nameof(args), exception.ParamName);
    }

    [TestMethod]
    public async Task CreateAsyncWithArgsAndConfigureBuilderThrowsWhenConfigureBuilderIsNull()
    {
        string[] args = ["arg"];
        Action<DistributedApplicationOptions, HostApplicationBuilderSettings> configureBuilder = null!;

        var action = () => DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>(args, configureBuilder);

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(action);
        Assert.AreEqual(nameof(configureBuilder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task CreateAsyncWithArgsAndConfigureBuilderThrowsWhenArgsContainsIsNullOrEmpty(bool isNull)
    {
        string[] args = ["arg", isNull ? null! : string.Empty, "arg2"];
        Action<DistributedApplicationOptions, HostApplicationBuilderSettings> configureBuilder = (_, _) => { };

        var action = () => DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>(args, configureBuilder);

        var exception = isNull
             ? await Assert.ThrowsAsync<ArgumentNullException>(action)
             : await Assert.ThrowsAsync<ArgumentException>(action);
        Assert.AreEqual(nameof(args), exception.ParamName);
        Assert.AreEqual(isNull
            ? "Array params contains null item: [arg, , arg2] (Parameter 'args')"
            : "Array params contains empty item: [arg, , arg2] (Parameter 'args')",
            exception.Message);
    }

    [TestMethod]
    public async Task CreateAsyncWithEntryPointAndArgsAndConfigureBuilderThrowsWhenEntryPointIsNull()
    {
        Type entryPoint = null!;
        string[] args = [];
        Action<DistributedApplicationOptions, HostApplicationBuilderSettings> configureBuilder = (_, _) => { };

        var action = () => DistributedApplicationTestingBuilder.CreateAsync(entryPoint, args, configureBuilder);

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(action);
        Assert.AreEqual(nameof(entryPoint), exception.ParamName);
    }

    [TestMethod]
    public async Task CreateAsyncWithEntryPointAndArgsAndConfigureBuilderThrowsWhenArgsIsNull()
    {
        Type entryPoint = typeof(Projects.TestingAppHost1_AppHost);
        string[] args = null!;
        Action<DistributedApplicationOptions, HostApplicationBuilderSettings> configureBuilder = (_, _) => { };

        var action = () => DistributedApplicationTestingBuilder.CreateAsync(entryPoint, args, configureBuilder);

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(action);
        Assert.AreEqual(nameof(args), exception.ParamName);
    }

    [TestMethod]
    public async Task CreateAsyncWithEntryPointAndArgsAndConfigureBuilderThrowsWhenConfigureBuilderIsNull()
    {
        Type entryPoint = typeof(Projects.TestingAppHost1_AppHost);
        string[] args = [];
        Action<DistributedApplicationOptions, HostApplicationBuilderSettings> configureBuilder = null!;

        var action = () => DistributedApplicationTestingBuilder.CreateAsync(entryPoint, args, configureBuilder);

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(action);
        Assert.AreEqual(nameof(configureBuilder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task CreateAsyncWithEntryPointAndArgsAndConfigureBuilderThrowsWhenArgsContainsIsNullOrEmpty(bool isNull)
    {
        Type entryPoint = typeof(Projects.TestingAppHost1_AppHost);
        string[] args = ["arg", isNull ? null! : string.Empty, "arg2"];
        Action<DistributedApplicationOptions, HostApplicationBuilderSettings> configureBuilder = (_, _) => { };

        var action = () => DistributedApplicationTestingBuilder.CreateAsync(entryPoint, args, configureBuilder);

        var exception = isNull
             ? await Assert.ThrowsAsync<ArgumentNullException>(action)
             : await Assert.ThrowsAsync<ArgumentException>(action);
        Assert.AreEqual(nameof(args), exception.ParamName);
        Assert.AreEqual(isNull
            ? "Array params contains null item: [arg, , arg2] (Parameter 'args')"
            : "Array params contains empty item: [arg, , arg2] (Parameter 'args')",
            exception.Message);
    }

    [TestMethod]
    public void CreateWithArgsThrowsWhenArgsIsNull()
    {
        string[] args = null!;

        var action = () => DistributedApplicationTestingBuilder.Create(args);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(args), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void CreateWithArgsThrowsWhenArgsContainsIsNullOrEmpty(bool isNull)
    {
        string[] args = ["arg", isNull ? null! : string.Empty, "arg2"];

        var action = () => DistributedApplicationTestingBuilder.Create(args);

        var exception = isNull
             ? Assert.Throws<ArgumentNullException>(action)
             : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(args), exception.ParamName);
        Assert.AreEqual(isNull
            ? "Array params contains null item: [arg, , arg2] (Parameter 'args')"
            : "Array params contains empty item: [arg, , arg2] (Parameter 'args')",
            exception.Message);
    }

    [TestMethod]
    public void CreateWithArgsAndConfigureBuilderThrowsWhenArgsIsNull()
    {
        string[] args = null!;
        Action<DistributedApplicationOptions, HostApplicationBuilderSettings> configureBuilder = (_, _) => { };

        var action = () => DistributedApplicationTestingBuilder.Create(args, configureBuilder);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(args), exception.ParamName);
    }

    [TestMethod]
    public void CreateWithArgsAndConfigureBuilderThrowsWhenConfigureBuilderIsNull()
    {
        string[] args = [];
        Action<DistributedApplicationOptions, HostApplicationBuilderSettings> configureBuilder = null!;

        var action = () => DistributedApplicationTestingBuilder.Create(args, configureBuilder);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(configureBuilder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void CreateWithArgsAndConfigureBuilderThrowsWhenArgsContainsIsNullOrEmpty(bool isNull)
    {
        string[] args = ["arg", isNull ? null! : string.Empty, "arg2"];
        Action<DistributedApplicationOptions, HostApplicationBuilderSettings> configureBuilder = (_, _) => { };

        var action = () => DistributedApplicationTestingBuilder.Create(args, configureBuilder);

        var exception = isNull
             ? Assert.Throws<ArgumentNullException>(action)
             : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(args), exception.ParamName);
        Assert.AreEqual(isNull
            ? "Array params contains null item: [arg, , arg2] (Parameter 'args')"
            : "Array params contains empty item: [arg, , arg2] (Parameter 'args')",
            exception.Message);
    }
}
