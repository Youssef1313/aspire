// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Configuration;
using Aspire.Hosting;

namespace Aspire.Dashboard.Tests;

[TestClass]
public sealed class DashboardOptionsTests
{
    private static DashboardOptions GetValidOptions()
    {
        // The minimal set of options required to pass validation.
        return new()
        {
            Frontend =
            {
                AuthMode = FrontendAuthMode.Unsecured,
                EndpointUrls = "http://localhost:5000"
            },
            Otlp =
            {
                AuthMode = OtlpAuthMode.Unsecured,
                GrpcEndpointUrl = "http://localhost:4317"
            },
        };
    }

    [TestMethod]
    public void ValidOptions_AreValid()
    {
        var result = new ValidateDashboardOptions().Validate(null, GetValidOptions());

        Assert.IsNull(result.FailureMessage);
        Assert.IsTrue(result.Succeeded);
    }

    #region Frontend options

    [TestMethod]
    public void FrontendOptions_EmptyEndpointUrl()
    {
        var options = GetValidOptions();
        options.Frontend.EndpointUrls = "";

        var result = new ValidateDashboardOptions().Validate(null, options);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("One or more frontend endpoint URLs are not configured. Specify an ASPNETCORE_URLS value.", result.FailureMessage);
    }

    [TestMethod]
    public void FrontendOptions_InvalidUrl()
    {
        var options = GetValidOptions();
        options.Frontend.EndpointUrls = "invalid";

        var result = new ValidateDashboardOptions().Validate(null, options);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("Failed to parse frontend endpoint URLs 'invalid'.", result.FailureMessage);
    }

    [TestMethod]
    public void FrontendOptions_ValidAndInvalidUrl()
    {
        var options = GetValidOptions();
        options.Frontend.EndpointUrls = "http://localhost:5000;invalid";

        var result = new ValidateDashboardOptions().Validate(null, options);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("Failed to parse frontend endpoint URLs 'http://localhost:5000;invalid'.", result.FailureMessage);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void FrontendOptions_MaxConsoleLogCount(int limit)
    {
        var options = GetValidOptions();
        options.Frontend.MaxConsoleLogCount = limit;

        var result = new ValidateDashboardOptions().Validate(null, options);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual($"{DashboardConfigNames.DashboardFrontendMaxConsoleLogCountName.ConfigKey} must be greater than zero.", result.FailureMessage);
    }

    #endregion

    #region Resource service client options

    [TestMethod]
    public void ResourceServiceClientOptions_InvalidUrl()
    {
        var options = GetValidOptions();
        options.ResourceServiceClient.Url = "invalid";

        var result = new ValidateDashboardOptions().Validate(null, options);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("Failed to parse resource service client endpoint URL 'invalid'.", result.FailureMessage);
    }

    [TestMethod]
    public void ResourceServiceClientOptions_ApiKeyMode_Empty()
    {
        var options = GetValidOptions();
        options.ResourceServiceClient.Url = "http://localhost";
        options.ResourceServiceClient.AuthMode = ResourceClientAuthMode.ApiKey;
        options.ResourceServiceClient.ApiKey = "";

        var result = new ValidateDashboardOptions().Validate(null, options);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual($"{DashboardConfigNames.ResourceServiceClientAuthModeName.ConfigKey} is \"{nameof(ResourceClientAuthMode.ApiKey)}\", but no {DashboardConfigNames.ResourceServiceClientApiKeyName.ConfigKey} is configured.", result.FailureMessage);
    }

    [TestMethod]
    public void ResourceServiceClientOptions_CertificateMode_FileSource_FilePathEmpty()
    {
        var options = GetValidOptions();
        options.ResourceServiceClient.Url = "http://localhost";
        options.ResourceServiceClient.AuthMode = ResourceClientAuthMode.Certificate;
        options.ResourceServiceClient.ClientCertificate.Source = DashboardClientCertificateSource.File;
        options.ResourceServiceClient.ClientCertificate.FilePath = "";

        var result = new ValidateDashboardOptions().Validate(null, options);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual($"{DashboardConfigNames.ResourceServiceClientCertificateSourceName.ConfigKey} is \"File\", but no {DashboardConfigNames.ResourceServiceClientCertificateFilePathName.ConfigKey} is configured.", result.FailureMessage);
    }

    [TestMethod]
    public void ResourceServiceClientOptions_CertificateMode_KeyStoreSource_SubjectEmpty()
    {
        var options = GetValidOptions();
        options.ResourceServiceClient.Url = "http://localhost";
        options.ResourceServiceClient.AuthMode = ResourceClientAuthMode.Certificate;
        options.ResourceServiceClient.ClientCertificate.Source = DashboardClientCertificateSource.KeyStore;
        options.ResourceServiceClient.ClientCertificate.Subject = "";

        var result = new ValidateDashboardOptions().Validate(null, options);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual($"{DashboardConfigNames.ResourceServiceClientCertificateSourceName.ConfigKey} is \"KeyStore\", but no {DashboardConfigNames.ResourceServiceClientCertificateSubjectName.ConfigKey} is configured.", result.FailureMessage);
    }

    [TestMethod]
    public void ResourceServiceClientOptions_CertificateMode_NullSource()
    {
        var options = GetValidOptions();
        options.ResourceServiceClient.Url = "http://localhost";
        options.ResourceServiceClient.AuthMode = ResourceClientAuthMode.Certificate;
        options.ResourceServiceClient.ClientCertificate.Source = null;

        var result = new ValidateDashboardOptions().Validate(null, options);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual($"The resource service client is configured to use certificates, but no certificate source is specified. Specify {DashboardConfigNames.ResourceServiceClientCertificateSourceName.ConfigKey}. Possible values: {string.Join(", ", typeof(DashboardClientCertificateSource).GetEnumNames())}", result.FailureMessage);
    }

    [TestMethod]
    public void ResourceServiceClientOptions_CertificateMode_InvalidSource()
    {
        var options = GetValidOptions();
        options.ResourceServiceClient.Url = "http://localhost";
        options.ResourceServiceClient.AuthMode = ResourceClientAuthMode.Certificate;
        options.ResourceServiceClient.ClientCertificate.Source = (DashboardClientCertificateSource)int.MaxValue;

        var result = new ValidateDashboardOptions().Validate(null, options);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual($"Unexpected resource service client certificate source: {options.ResourceServiceClient.ClientCertificate.Source}", result.FailureMessage);
    }

    [TestMethod]
    public void ResourceServiceClientOptions_NullMode()
    {
        var options = GetValidOptions();
        options.ResourceServiceClient.Url = "http://localhost";
        options.ResourceServiceClient.AuthMode = null;

        var result = new ValidateDashboardOptions().Validate(null, options);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual($"Resource service client authentication is not configured. Specify {DashboardConfigNames.ResourceServiceClientAuthModeName.ConfigKey}. Possible values: {string.Join(", ", typeof(ResourceClientAuthMode).GetEnumNames())}", result.FailureMessage);
    }

    [TestMethod]
    public void ResourceServiceClientOptions_InvalidMode()
    {
        var options = GetValidOptions();
        options.ResourceServiceClient.Url = "http://localhost";
        options.ResourceServiceClient.AuthMode = (ResourceClientAuthMode)int.MaxValue;

        var result = new ValidateDashboardOptions().Validate(null, options);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual($"Unexpected resource service client authentication mode: {int.MaxValue}", result.FailureMessage);
    }

    #endregion

    #region OTLP options

    [TestMethod]
    public void OtlpOptions_NeitherEndpointSet()
    {
        var options = GetValidOptions();
        options.Otlp.GrpcEndpointUrl = null;
        options.Otlp.HttpEndpointUrl = null;

        var result = new ValidateDashboardOptions().Validate(null, options);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(
            $"Neither OTLP/gRPC or OTLP/HTTP endpoint URLs are configured. Specify either a {DashboardConfigNames.DashboardOtlpGrpcUrlName.EnvVarName} or {DashboardConfigNames.DashboardOtlpHttpUrlName.EnvVarName} value.",
            result.FailureMessage);
    }

    [TestMethod]
    public void OtlpOptions_gRPC_InvalidUrl()
    {
        var options = GetValidOptions();
        options.Otlp.GrpcEndpointUrl = "invalid";

        var result = new ValidateDashboardOptions().Validate(null, options);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("Failed to parse OTLP gRPC endpoint URL 'invalid'.", result.FailureMessage);
    }

    [TestMethod]
    public void OtlpOptions_HTTP_InvalidUrl()
    {
        var options = GetValidOptions();
        options.Otlp.HttpEndpointUrl = "invalid";

        var result = new ValidateDashboardOptions().Validate(null, options);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("Failed to parse OTLP HTTP endpoint URL 'invalid'.", result.FailureMessage);
    }

    #endregion

    #region OpenIDConnect options

    [TestMethod]
    public void OpenIdConnectOptions_NoNameClaimType()
    {
        var options = GetValidOptions();
        options.Frontend.AuthMode = FrontendAuthMode.OpenIdConnect;
        options.Frontend.OpenIdConnect.NameClaimType = "";

        var result = new ValidateDashboardOptions().Validate(null, options);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("OpenID Connect claim type for name not configured. Specify a Dashboard:Frontend:OpenIdConnect:NameClaimType value.", result.FailureMessage);
    }

    [TestMethod]
    public void OpenIdConnectOptions_NoUserNameClaimType()
    {
        var options = GetValidOptions();
        options.Frontend.AuthMode = FrontendAuthMode.OpenIdConnect;
        options.Frontend.OpenIdConnect.UsernameClaimType = "";

        var result = new ValidateDashboardOptions().Validate(null, options);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("OpenID Connect claim type for username not configured. Specify a Dashboard:Frontend:OpenIdConnect:UsernameClaimType value.", result.FailureMessage);
    }

    #endregion
}
