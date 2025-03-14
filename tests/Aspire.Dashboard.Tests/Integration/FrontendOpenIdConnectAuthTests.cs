// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Web;
using Aspire.Dashboard.Authentication;
using Aspire.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Testing;

namespace Aspire.Dashboard.Tests.Integration;

[TestClass]
public class FrontendOpenIdConnectAuthTests(TestContext testContext)
{
    [TestMethod]
    public async Task Get_Unauthenticated_RedirectsToAuthority()
    {
        await using var authority = await MockOpenIdAuthority.CreateAsync().DefaultTimeout();

        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(
            testContext,
            additionalConfiguration: config =>
            {
                ConfigureOpenIdConnect(config, authority);
            });

        await app.StartAsync().DefaultTimeout();

        var handler = new HttpClientHandler()
        {
            // Don't follow redirects. We want to validate where the redirect would take us.
            AllowAutoRedirect = false
        };

        using var client = new HttpClient(handler) { BaseAddress = new Uri($"http://{app.FrontendSingleEndPointAccessor().EndPoint}") };

        // Act
        var response = await client.GetAsync("/").DefaultTimeout();

        // Assert
        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);

        var redirectedTo = response.Headers.Location;

        Assert.IsNotNull(redirectedTo);
        Assert.IsTrue(redirectedTo.IsAbsoluteUri);
        Assert.AreEqual("localhost", redirectedTo.Host);
        Assert.AreEqual("/authorize", redirectedTo.AbsolutePath);

        var query = HttpUtility.ParseQueryString(redirectedTo.Query);
        Assert.AreEqual("MyClientId", query.Get("client_id"));
        Assert.AreEqual("code", query.Get("response_type"));
        Assert.AreEqual("openid profile", query.Get("scope"));

        await app.StopAsync().DefaultTimeout();
    }

    [TestMethod]
    public async Task Get_Unauthenticated_OtlpHttpConnection_Denied()
    {
        await using var authority = await MockOpenIdAuthority.CreateAsync().DefaultTimeout();

        var testSink = new TestSink();
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(
            testContext,
            additionalConfiguration: config =>
            {
                ConfigureOpenIdConnect(config, authority);
            },
            testSink: testSink);

        await app.StartAsync().DefaultTimeout();

        var handler = new HttpClientHandler()
        {
            // Don't follow redirects. We want to validate where the redirect would take us.
            AllowAutoRedirect = false
        };

        using var client = new HttpClient(handler) { BaseAddress = new Uri($"http://{app.OtlpServiceHttpEndPointAccessor().EndPoint}") };

        // Act
        var response = await client.GetAsync("/").DefaultTimeout();

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        var log = testSink.Writes.Single(s => s.LoggerName == typeof(FrontendCompositeAuthenticationHandler).FullName && s.EventId.Name == "AuthenticationSchemeNotAuthenticatedWithFailure");
        Assert.AreEqual("FrontendComposite was not authenticated. Failure message: Connection type Frontend is not enabled on this connection.", log.Message);

        await app.StopAsync().DefaultTimeout();
    }

    private static void ConfigureOpenIdConnect(Dictionary<string, string?> config, MockOpenIdAuthority.Authority authority)
    {
        // Configure the resource service, as otherwise HTTP requests are redirected to /structuredlogs before OIDC
        config[DashboardConfigNames.ResourceServiceClientAuthModeName.ConfigKey] = "Unsecured";
        config[DashboardConfigNames.ResourceServiceUrlName.ConfigKey] = "https://localhost:1234"; // won't actually exist

        // Configure OIDC. It (the RP) will communicate with the mock authority (IdP) spun up for this test.
        config[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = "OpenIdConnect";
        config["Authentication:Schemes:OpenIdConnect:Authority"] = authority.Url;
        config["Authentication:Schemes:OpenIdConnect:ClientId"] = "MyClientId";
        config["Authentication:Schemes:OpenIdConnect:ClientSecret"] = "MyClientSecret";
        // Allow the requirement of HTTPS communication with the OpenIdConnect authority to be relaxed during tests.
        config["Authentication:Schemes:OpenIdConnect:RequireHttpsMetadata"] = "false";
    }
}
