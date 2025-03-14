// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Json;
using Aspire.Components.Common.Tests;
using Aspire.Hosting.Tests.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting.Testing.Tests;

[TestClass]
public class TestingFactoryTests(DistributedApplicationFixture<Projects.TestingAppHost1_AppHost> fixture) : IClassFixture<DistributedApplicationFixture<Projects.TestingAppHost1_AppHost>>
{
    private readonly DistributedApplication _app = fixture.Application;

    [TestMethod]
    [RequiresDocker]
    public void HasEndPoints()
    {
        // Get an endpoint from a resource
        var workerEndpoint = _app.GetEndpoint("myworker1", "myendpoint1");
        Assert.IsNotNull(workerEndpoint);
        Assert.IsTrue(workerEndpoint.Host.Length > 0);
    }

    [TestMethod]
    [RequiresDocker]
    public async Task CanGetConnectionStringFromAddConnectionString()
    {
        // Get a connection string from a resource
        var connectionString = await _app.GetConnectionStringAsync("cs");
        var connectionString2 = await _app.GetConnectionStringAsync("cs2");
        Assert.AreEqual("testconnection", connectionString);
        Assert.AreEqual("Value=this is a value", connectionString2);
    }

    [TestMethod]
    [RequiresDocker]
    public void CanGetResources()
    {
        var appModel = _app.Services.GetRequiredService<DistributedApplicationModel>();
        Assert.Contains(appModel.GetProjectResources(), p => p.Name == "myworker1");
    }

    [TestMethod]
    [RequiresDocker]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4650", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningOnCI))]
    public async Task HttpClientGetTest()
    {
        // Wait for the application to be ready
        await _app.WaitForTextAsync("Application started.", "mywebapp1").WaitAsync(TimeSpan.FromMinutes(1));

        var httpClient = _app.CreateHttpClientWithResilience("mywebapp1");

        var result1 = await httpClient.GetFromJsonAsync<WeatherForecast[]>("/weatherforecast");
        Assert.IsNotNull(result1);
        Assert.IsTrue(result1.Length > 0);
    }

    [TestMethod]
    [RequiresDocker]
    public void SetsCorrectContentRoot()
    {
        var appModel = _app.Services.GetRequiredService<IHostEnvironment>();
        Assert.Contains("TestingAppHost1", appModel.ContentRootPath);
    }

    [TestMethod]
    [RequiresDocker]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4650")]
    public async Task SelectsFirstLaunchProfile()
    {
        var config = _app.Services.GetRequiredService<IConfiguration>();
        var profileName = config["AppHost:DefaultLaunchProfileName"];
        Assert.AreEqual("https", profileName);

        // Wait for resource to start.
        await _app.ResourceNotifications.WaitForResourceAsync("mywebapp1").WaitAsync(TimeSpan.FromSeconds(60));

        // Explicitly get the HTTPS endpoint - this is only available on the "https" launch profile.
        var httpClient = _app.CreateHttpClientWithResilience("mywebapp1", "https");
        var result = await httpClient.GetFromJsonAsync<WeatherForecast[]>("/weatherforecast");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
    }

    private sealed record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}
