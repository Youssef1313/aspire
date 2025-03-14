// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http.Json;
using System.Web;
using Aspire.Dashboard.Authentication;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Utils;
using Aspire.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Aspire.Dashboard.Tests.Integration;

[TestClass]
public class FrontendBrowserTokenAuthTests
{
    private readonly TestContext _testContext;

    public FrontendBrowserTokenAuthTests(TestContext testContext)
    {
        _testContext = testContext;
    }

    [TestMethod]
    public async Task Get_Unauthenticated_RedirectToLogin()
    {
        // Arrange
        var apiKey = "TestKey123!";
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testContext, config =>
        {
            config[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = FrontendAuthMode.BrowserToken.ToString();
            config[DashboardConfigNames.DashboardFrontendBrowserTokenName.ConfigKey] = apiKey;
        });
        await app.StartAsync().DefaultTimeout();

        using var client = new HttpClient { BaseAddress = new Uri($"http://{app.FrontendSingleEndPointAccessor().EndPoint}") };

        // Act
        var response = await client.GetAsync("/").DefaultTimeout();

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(DashboardUrls.LoginUrl(returnUrl: DashboardUrls.StructuredLogsUrl()), response.RequestMessage!.RequestUri!.PathAndQuery);
    }

    [TestMethod]
    public async Task Get_LoginPage_ValidToken_RedirectToApp()
    {
        // Arrange
        var apiKey = "TestKey123!";
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testContext, config =>
        {
            config[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = FrontendAuthMode.BrowserToken.ToString();
            config[DashboardConfigNames.DashboardFrontendBrowserTokenName.ConfigKey] = apiKey;
        });
        await app.StartAsync().DefaultTimeout();

        using var client = new HttpClient { BaseAddress = new Uri($"http://{app.FrontendSingleEndPointAccessor().EndPoint}") };

        // Act 1
        var response1 = await client.GetAsync(DashboardUrls.LoginUrl(returnUrl: DashboardUrls.TracesUrl(), token: apiKey)).DefaultTimeout();

        // Assert 1
        Assert.AreEqual(HttpStatusCode.OK, response1.StatusCode);
        Assert.AreEqual(DashboardUrls.TracesUrl(), response1.RequestMessage!.RequestUri!.PathAndQuery);

        // Act 2
        var response2 = await client.GetAsync(DashboardUrls.StructuredLogsUrl()).DefaultTimeout();

        // Assert 2
        Assert.AreEqual(HttpStatusCode.OK, response2.StatusCode);
        Assert.AreEqual(DashboardUrls.StructuredLogsUrl(), response2.RequestMessage!.RequestUri!.PathAndQuery);
    }

    [TestMethod]
    public async Task Get_LoginPage_ValidToken_OtlpHttpConnection_Denied()
    {
        // Arrange
        var testSink = new TestSink();

        var apiKey = "TestKey123!";
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testContext, config =>
        {
            config[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = FrontendAuthMode.BrowserToken.ToString();
            config[DashboardConfigNames.DashboardFrontendBrowserTokenName.ConfigKey] = apiKey;
        }, testSink: testSink);
        await app.StartAsync().DefaultTimeout();

        using var client = new HttpClient { BaseAddress = new Uri($"http://{app.OtlpServiceHttpEndPointAccessor().EndPoint}") };

        // Act
        var response = await client.GetAsync(DashboardUrls.LoginUrl(returnUrl: DashboardUrls.TracesUrl(), token: apiKey)).DefaultTimeout();

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        var log = testSink.Writes.Single(s => s.LoggerName == typeof(FrontendCompositeAuthenticationHandler).FullName && s.EventId.Name == "AuthenticationSchemeNotAuthenticatedWithFailure");
        Assert.AreEqual("FrontendComposite was not authenticated. Failure message: Connection type Frontend is not enabled on this connection.", log.Message);
    }

    [TestMethod]
    public async Task Get_LoginPage_InvalidToken_RedirectToLoginWithoutToken()
    {
        // Arrange
        var apiKey = "TestKey123!";
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testContext, config =>
        {
            config[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = FrontendAuthMode.BrowserToken.ToString();
            config[DashboardConfigNames.DashboardFrontendBrowserTokenName.ConfigKey] = apiKey;
        });
        await app.StartAsync().DefaultTimeout();

        using var client = new HttpClient { BaseAddress = new Uri($"http://{app.FrontendSingleEndPointAccessor().EndPoint}") };

        // Act
        var response = await client.GetAsync(DashboardUrls.LoginUrl(returnUrl: DashboardUrls.TracesUrl(), token: "Wrong!")).DefaultTimeout();

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(DashboardUrls.LoginUrl(returnUrl: DashboardUrls.TracesUrl()), response.RequestMessage!.RequestUri!.PathAndQuery, ignoreCase: true);
    }

    [TestMethod]
    [DataRow(FrontendAuthMode.BrowserToken, "TestKey123!", HttpStatusCode.OK, true)]
    [DataRow(FrontendAuthMode.BrowserToken, "Wrong!", HttpStatusCode.OK, false)]
    [DataRow(FrontendAuthMode.Unsecured, "Wrong!", HttpStatusCode.BadRequest, null)]
    public async Task Post_ValidateTokenApi_AvailableBasedOnOptions(FrontendAuthMode authMode, string requestToken, HttpStatusCode statusCode, bool? result)
    {
        // Arrange
        var apiKey = "TestKey123!";
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testContext, config =>
        {
            config[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = authMode.ToString();
            config[DashboardConfigNames.DashboardFrontendBrowserTokenName.ConfigKey] = apiKey;
        });
        await app.StartAsync().DefaultTimeout();

        using var client = new HttpClient { BaseAddress = new Uri($"http://{app.FrontendSingleEndPointAccessor().EndPoint}") };

        // Act
        var response = await client.PostAsync("/api/validatetoken?token=" + requestToken, content: null).DefaultTimeout();

        // Assert
        Assert.AreEqual(statusCode, response.StatusCode);

        if (result != null)
        {
            var actualResult = await response.Content.ReadFromJsonAsync<bool>();
            Assert.AreEqual(result, actualResult);
        }
    }

    [TestMethod]
    public async Task LogOutput_NoToken_GeneratedTokenLogged()
    {
        // Arrange
        var testSink = new TestSink();
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testContext, config =>
        {
            config[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = FrontendAuthMode.BrowserToken.ToString();
        }, testSink: testSink);

        // Act
        await app.StartAsync().DefaultTimeout();

        // Assert
        var l = testSink.Writes.Where(w => w.LoggerName == typeof(DashboardWebApplication).FullName).ToList();
        Assert.That.Collection(l,
            w =>
            {
                Assert.AreEqual("Aspire version: {Version}", GetValue(w.State, "{OriginalFormat}"));
            },
            w =>
            {
                Assert.AreEqual("Now listening on: {DashboardUri}", GetValue(w.State, "{OriginalFormat}"));

                var uri = new Uri((string)GetValue(w.State, "DashboardUri")!);
                Assert.AreNotEqual(0, uri.Port);
            },
            w =>
            {
                Assert.AreEqual("OTLP/gRPC listening on: {OtlpEndpointUri}", GetValue(w.State, "{OriginalFormat}"));

                var uri = new Uri((string)GetValue(w.State, "OtlpEndpointUri")!);
                Assert.AreNotEqual(0, uri.Port);
            },
            w =>
            {
                Assert.AreEqual("OTLP/HTTP listening on: {OtlpEndpointUri}", GetValue(w.State, "{OriginalFormat}"));

                var uri = new Uri((string)GetValue(w.State, "OtlpEndpointUri")!);
                Assert.AreNotEqual(0, uri.Port);
            },
            w =>
            {
                Assert.AreEqual("OTLP server is unsecured. Untrusted apps can send telemetry to the dashboard. For more information, visit https://go.microsoft.com/fwlink/?linkid=2267030", GetValue(w.State, "{OriginalFormat}"));
                Assert.AreEqual(LogLevel.Warning, w.LogLevel);
            },
            w =>
            {
                Assert.AreEqual("Login to the dashboard at {DashboardLoginUrl}", GetValue(w.State, "{OriginalFormat}"));

                var uri = new Uri((string)GetValue(w.State, "DashboardLoginUrl")!, UriKind.Absolute);
                var queryString = HttpUtility.ParseQueryString(uri.Query);
                Assert.IsNotNull(queryString["t"]);
            });
    }

    [TestMethod]
    [DataRow("http://+:0", "localhost")]
    [DataRow("http://0.0.0.0:0", "localhost")]
    [DataRow("http://127.0.0.1:0", "127.0.0.1")]
    [DataRow("http://aspire-test-hostname:0", "aspire-test-hostname")]
    public async Task LogOutput_AnyIP_LoginLinkLocalhost(string frontendUrl, string linkHost)
    {
        // Arrange
        var testSink = new TestSink();
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testContext, config =>
        {
            config[DashboardConfigNames.DashboardFrontendUrlName.ConfigKey] = frontendUrl;
            config[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = FrontendAuthMode.BrowserToken.ToString();
        }, testSink: testSink);

        // Act
        await app.StartAsync().DefaultTimeout();

        // Assert
        var l = testSink.Writes.Where(w => w.LoggerName == typeof(DashboardWebApplication).FullName).ToList();

        // Testing via the log template is kind of hacky. If this becomes a problem then consider adding proper log definitions and match via ID.
        var loginLinkLog = l.Single(w => "Login to the dashboard at {DashboardLoginUrl}" == (string?)GetValue(w.State, "{OriginalFormat}"));

        var uri = new Uri((string)GetValue(loginLinkLog.State, "DashboardLoginUrl")!, UriKind.Absolute);
        var queryString = HttpUtility.ParseQueryString(uri.Query);
        Assert.IsNotNull(queryString["t"]);

        Assert.AreEqual(linkHost, uri.Host);
    }

    [TestMethod]
    public async Task LogOutput_InContainer_LoginLinkContainerMessage()
    {
        // Arrange
        var testSink = new TestSink();
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testContext, config =>
        {
            config["DOTNET_RUNNING_IN_CONTAINER"] = "true";
            config[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = FrontendAuthMode.BrowserToken.ToString();
        }, testSink: testSink);

        // Act
        await app.StartAsync().DefaultTimeout();

        // Assert
        var l = testSink.Writes.Where(w => w.LoggerName == typeof(DashboardWebApplication).FullName).ToList();

        // Testing via the log template is kind of hacky. If this becomes a problem then consider adding proper log definitions and match via ID.
        Assert.ContainsSingle(l.Where(w => "Login to the dashboard at {DashboardLoginUrl}. The URL may need changes depending on how network access to the container is configured." == (string?)GetValue(w.State, "{OriginalFormat}")));
    }

    private static object? GetValue(object? values, string key)
    {
        var list = values as IReadOnlyList<KeyValuePair<string, object>>;
        return list?.SingleOrDefault(kvp => kvp.Key == key).Value;
    }
}
