// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Aspire.Dashboard.Authentication.OtlpApiKey;
using Aspire.Dashboard.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Aspire.Dashboard.Tests;

[TestClass]
public class OtlpApiKeyAuthenticationHandlerTests
{
    [TestMethod]
    public async Task AuthenticateAsync_NoHeader_Failure()
    {
        // Arrange
        var handler = await CreateAuthHandlerAsync(primaryApiKey: "abc", secondaryApiKey: null, otlpApiKeyHeader: null).DefaultTimeout();

        // Act
        var result = await handler.AuthenticateAsync().DefaultTimeout();

        // Assert
        Assert.IsNotNull(result.Failure);
        Assert.AreEqual($"API key from '{OtlpApiKeyAuthenticationHandler.ApiKeyHeaderName}' header is missing.", result.Failure.Message);
    }

    [TestMethod]
    public async Task AuthenticateAsync_BigApiKeys_NoMatch_Failure()
    {
        // Arrange
        var handler = await CreateAuthHandlerAsync(primaryApiKey: new string('!', 1000), secondaryApiKey: null, otlpApiKeyHeader: new string('!', 999)).DefaultTimeout();

        // Act
        var result = await handler.AuthenticateAsync().DefaultTimeout();

        // Assert
        Assert.IsNotNull(result.Failure);
        Assert.AreEqual($"Incoming API key from '{OtlpApiKeyAuthenticationHandler.ApiKeyHeaderName}' header doesn't match configured API key.", result.Failure.Message);
    }

    [TestMethod]
    public async Task AuthenticateAsync_BigApiKeys_Match_Success()
    {
        // Arrange
        var handler = await CreateAuthHandlerAsync(primaryApiKey: new string('!', 1000), secondaryApiKey: null, otlpApiKeyHeader: new string('!', 1000)).DefaultTimeout();

        // Act
        var result = await handler.AuthenticateAsync().DefaultTimeout();

        // Assert
        Assert.IsNull(result.Failure);
    }

    [TestMethod]
    [DataRow("abc", null, "abc", true)]
    [DataRow("abcd", null, "abc", false)]
    [DataRow("abc", null, "abcd", false)]
    [DataRow("abc", "abcd", "abcd", true)]
    public async Task AuthenticateAsync_MatchHeader_Success(string primaryApiKey, string? secondaryApiKey, string otlpApiKeyHeader, bool success)
    {
        // Arrange
        var handler = await CreateAuthHandlerAsync(primaryApiKey, secondaryApiKey, otlpApiKeyHeader).DefaultTimeout();

        // Act
        var result = await handler.AuthenticateAsync().DefaultTimeout();

        // Assert
        Assert.AreEqual(success, result.Failure == null);
    }

    private static async Task<OtlpApiKeyAuthenticationHandler> CreateAuthHandlerAsync(string primaryApiKey, string? secondaryApiKey, string? otlpApiKeyHeader)
    {
        var options = new DashboardOptions
        {
            Otlp =
            {
                GrpcEndpointUrl = "http://localhost",
                PrimaryApiKey = primaryApiKey,
                SecondaryApiKey = secondaryApiKey
            }
        };
        Assert.IsTrue(options.Otlp.TryParseOptions(out _));

        var handler = new OtlpApiKeyAuthenticationHandler(
            new TestOptionsMonitor<DashboardOptions>(options),
            new TestOptionsMonitor<OtlpApiKeyAuthenticationHandlerOptions>(new OtlpApiKeyAuthenticationHandlerOptions()),
            NullLoggerFactory.Instance,
            UrlEncoder.Default);

        var httpContext = new DefaultHttpContext();
        if (otlpApiKeyHeader != null)
        {
            httpContext.Request.Headers[OtlpApiKeyAuthenticationHandler.ApiKeyHeaderName] = otlpApiKeyHeader;
        }
        await handler.InitializeAsync(new AuthenticationScheme("Test", "Test", handler.GetType()), httpContext);
        return handler;
    }

    private sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
    {
        public TestOptionsMonitor(T options) => CurrentValue = options;

        public T CurrentValue { get; }

        public T Get(string? name) => CurrentValue;

        public IDisposable OnChange(Action<T, string> listener) => throw new NotImplementedException();

        public IDisposable OnChange(Action<T> listener) => throw new NotImplementedException();
    }
}
