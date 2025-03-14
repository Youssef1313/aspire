// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Keycloak.Authentication.Tests;

[TestClass]
public class KeycloakAuthenticationPublicApiTests
{
    [TestMethod]
    public void AddKeycloakJwtBearerShouldThrowWhenBuilderIsNull()
    {
        AuthenticationBuilder builder = null!;
        const string serviceName = "keycloak";
        const string realm = "aspire-realm";

        var action = () => builder.AddKeycloakJwtBearer(serviceName, realm);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddKeycloakJwtBearerShouldThrowWhenServiceNameIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        var serviceName = isNull ? null! : string.Empty;
        const string realm = "aspire-realm";

        var action = () => builder.AddKeycloakJwtBearer(serviceName, realm);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(serviceName), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddKeycloakJwtBearerShouldThrowWhenRealmIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        const string serviceName = "keycloak";
        var realm = isNull ? null! : string.Empty;

        var action = () => builder.AddKeycloakJwtBearer(serviceName, realm);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(realm), exception.ParamName);
    }

    [TestMethod]
    public void AddKeycloakJwtBearerWithAuthenticationSchemeShouldThrowWhenBuilderIsNull()
    {
        AuthenticationBuilder builder = null!;
        const string serviceName = "keycloak";
        const string realm = "aspire-realm";
        const string authenticationScheme = "Bearer";

        var action = () => builder.AddKeycloakJwtBearer(serviceName, realm, authenticationScheme);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddKeycloakJwtBearerWithAuthenticationSchemeShouldThrowWhenServiceNameIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        var serviceName = isNull ? null! : string.Empty;
        const string realm = "aspire-realm";
        const string authenticationScheme = "Bearer";

        var action = () => builder.AddKeycloakJwtBearer(serviceName, realm, authenticationScheme);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(serviceName), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddKeycloakJwtBearerWithAuthenticationSchemeShouldThrowWhenRealmIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        const string serviceName = "keycloak";
        var realm = isNull ? null! : string.Empty;
        const string authenticationScheme = "Bearer";

        var action = () => builder.AddKeycloakJwtBearer(serviceName, realm, authenticationScheme);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(realm), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddKeycloakJwtBearerWithAuthenticationSchemeShouldThrowWhenAuthenticationSchemeIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        const string serviceName = "keycloak";
        const string realm = "aspire-realm";
        var authenticationScheme = isNull ? null! : string.Empty;

        var action = () => builder.AddKeycloakJwtBearer(serviceName, realm, authenticationScheme);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(authenticationScheme), exception.ParamName);
    }

    [TestMethod]
    public void AddKeycloakJwtBearerWithConfigureOptionsShouldThrowWhenBuilderIsNull()
    {
        AuthenticationBuilder builder = null!;
        const string serviceName = "keycloak";
        const string realm = "aspire-realm";
        Action<JwtBearerOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakJwtBearer(serviceName, realm, configureOptions);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddKeycloakJwtBearerWithConfigureOptionsShouldThrowWhenServiceNameIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        var serviceName = isNull ? null! : string.Empty;
        const string realm = "aspire-realm";
        Action<JwtBearerOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakJwtBearer(serviceName, realm, configureOptions);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(serviceName), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddKeycloakJwtBearerWithConfigureOptionsShouldThrowWhenRealmIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        const string serviceName = "keycloak";
        var realm = isNull ? null! : string.Empty;
        Action<JwtBearerOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakJwtBearer(serviceName, realm, configureOptions);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(realm), exception.ParamName);
    }

    [TestMethod]
    public void AddKeycloakJwtBearerWithAuthenticationSchemeAndConfigureOptionsShouldThrowWhenBuilderIsNull()
    {
        AuthenticationBuilder builder = null!;
        const string serviceName = "keycloak";
        const string realm = "aspire-realm";
        const string authenticationScheme = "Bearer";
        Action<JwtBearerOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakJwtBearer(
            serviceName,
            realm,
            authenticationScheme,
            configureOptions);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddKeycloakJwtBearerWithAuthenticationSchemeAndConfigureOptionsShouldThrowWhenServiceNameIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        var serviceName = isNull ? null! : string.Empty;
        const string realm = "aspire-realm";
        const string authenticationScheme = "Bearer";
        Action<JwtBearerOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakJwtBearer(
            serviceName,
            realm,
            authenticationScheme,
            configureOptions);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(serviceName), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddKeycloakJwtBearerWithAuthenticationSchemeAndConfigureOptionsShouldThrowWhenRealmIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        const string serviceName = "keycloak";
        var realm = isNull ? null! : string.Empty;
        const string authenticationScheme = "Bearer";
        Action<JwtBearerOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakJwtBearer(
            serviceName,
            realm,
            authenticationScheme,
            configureOptions);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(realm), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddKeycloakJwtBearerWithAuthenticationSchemeAndConfigureOptionsShouldThrowWhenAuthenticationSchemeIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        const string serviceName = "keycloak";
        const string realm = "aspire-realm";
        var authenticationScheme = isNull ? null! : string.Empty;
        Action<JwtBearerOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakJwtBearer(
            serviceName,
            realm,
            authenticationScheme,
            configureOptions);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(authenticationScheme), exception.ParamName);
    }

    [TestMethod]
    public void AddKeycloakOpenIdConnectShouldThrowWhenBuilderIsNull()
    {
        AuthenticationBuilder builder = null!;
        const string serviceName = "keycloak";
        const string realm = "aspire-realm";

        var action = () => builder.AddKeycloakOpenIdConnect(serviceName, realm);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddKeycloakOpenIdConnectShouldThrowWhenServiceNameIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        var serviceName = isNull ? null! : string.Empty;
        const string realm = "aspire-realm";

        var action = () => builder.AddKeycloakOpenIdConnect(serviceName, realm);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(serviceName), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddKeycloakOpenIdConnectShouldThrowWhenRealmIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        const string serviceName = "keycloak";
        var realm = isNull ? null! : string.Empty;

        var action = () => builder.AddKeycloakOpenIdConnect(serviceName, realm);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(realm), exception.ParamName);
    }

    [TestMethod]
    public void AddKeycloakOpenIdConnectWithAuthenticationSchemeShouldThrowWhenBuilderIsNull()
    {
        AuthenticationBuilder builder = null!;
        const string serviceName = "keycloak";
        const string realm = "aspire-realm";
        const string authenticationScheme = "openId";

        var action = () => builder.AddKeycloakOpenIdConnect(serviceName, realm, authenticationScheme);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddKeycloakOpenIdConnectWithAuthenticationSchemeShouldThrowWhenServiceNameIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        var serviceName = isNull ? null! : string.Empty;
        const string realm = "aspire-realm";
        const string authenticationScheme = "openId";

        var action = () => builder.AddKeycloakOpenIdConnect(serviceName, realm, authenticationScheme);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(serviceName), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddKeycloakOpenIdConnectWithAuthenticationSchemeShouldThrowWhenRealmIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        const string serviceName = "keycloak";
        var realm = isNull ? null! : string.Empty;
        const string authenticationScheme = "openId";

        var action = () => builder.AddKeycloakOpenIdConnect(serviceName, realm, authenticationScheme);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(realm), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddKeycloakOpenIdConnectWithAuthenticationSchemeShouldThrowWhenAuthenticationSchemeIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        const string serviceName = "keycloak";
        const string realm = "aspire-realm";
        var authenticationScheme = isNull ? null! : string.Empty;

        var action = () => builder.AddKeycloakOpenIdConnect(serviceName, realm, authenticationScheme);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(authenticationScheme), exception.ParamName);
    }

    [TestMethod]
    public void AddKeycloakOpenIdConnectWithConfigureOptionsShouldThrowWhenBuilderIsNull()
    {
        AuthenticationBuilder builder = null!;
        const string serviceName = "keycloak";
        const string realm = "aspire-realm";
        Action<OpenIdConnectOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakOpenIdConnect(serviceName, realm, configureOptions);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddKeycloakOpenIdConnectWithConfigureOptionsShouldThrowWhenServiceNameIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        var serviceName = isNull ? null! : string.Empty;
        const string realm = "aspire-realm";
        Action<OpenIdConnectOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakOpenIdConnect(serviceName, realm, configureOptions);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(serviceName), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddKeycloakOpenIdConnectWithConfigureOptionsShouldThrowWhenRealmIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        const string serviceName = "keycloak";
        var realm = isNull ? null! : string.Empty;
        Action<OpenIdConnectOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakOpenIdConnect(serviceName, realm, configureOptions);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(realm), exception.ParamName);
    }

    [TestMethod]
    public void AddKeycloakOpenIdConnectWithAuthenticationSchemeAndConfigureOptionsShouldThrowWhenBuilderIsNull()
    {
        AuthenticationBuilder builder = null!;
        const string serviceName = "keycloak";
        const string realm = "aspire-realm";
        const string authenticationScheme = "openId";
        Action<OpenIdConnectOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakOpenIdConnect(
            serviceName,
            realm,
            authenticationScheme,
            configureOptions);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddKeycloakOpenIdConnectWithAuthenticationSchemeAndConfigureOptionsShouldThrowWhenServiceNameIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        var serviceName = isNull ? null! : string.Empty;
        const string realm = "aspire-realm";
        const string authenticationScheme = "openId";
        Action<OpenIdConnectOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakOpenIdConnect(
            serviceName,
            realm,
            authenticationScheme,
            configureOptions);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(serviceName), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddKeycloakOpenIdConnectWithAuthenticationSchemeAndConfigureOptionsShouldThrowWhenRealmIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        const string serviceName = "keycloak";
        var realm = isNull ? null! : string.Empty;
        const string authenticationScheme = "openId";
        Action<OpenIdConnectOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakOpenIdConnect(
            serviceName,
            realm,
            authenticationScheme,
            configureOptions);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(realm), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddKeycloakOpenIdConnectWithAuthenticationSchemeAndConfigureOptionsShouldThrowWhenAuthenticationSchemeIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        const string serviceName = "keycloak";
        const string realm = "aspire-realm";
        var authenticationScheme = isNull ? null! : string.Empty;
        Action<OpenIdConnectOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakOpenIdConnect(
            serviceName,
            realm,
            authenticationScheme,
            configureOptions);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(authenticationScheme), exception.ParamName);
    }
}
