// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.ServiceDiscovery.Tests;

#pragma warning disable IDE0200

public class ExtensionsServicePublicApiTests
{
    [TestMethod]
    public void AddServiceDiscoveryShouldThrowWhenHttpClientBuilderIsNull()
    {
        IHttpClientBuilder httpClientBuilder = null!;

        var action = () => httpClientBuilder.AddServiceDiscovery();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(httpClientBuilder), exception.ParamName);
    }

    [TestMethod]
    public void AddServiceDiscoveryShouldThrowWhenServicesIsNull()
    {
        IServiceCollection services = null!;

        var action = () => services.AddServiceDiscovery();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(services), exception.ParamName);
    }

    [TestMethod]
    public void AddServiceDiscoveryWithConfigureOptionsShouldThrowWhenServicesIsNull()
    {
        IServiceCollection services = null!;
        Action<ServiceDiscoveryOptions> configureOptions = (_) => { };

        var action = () => services.AddServiceDiscovery(configureOptions);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(services), exception.ParamName);
    }

    [TestMethod]
    public void AddServiceDiscoveryWithConfigureOptionsShouldThrowWhenConfigureOptionsIsNull()
    {
        var services = new ServiceCollection();
        Action<ServiceDiscoveryOptions> configureOptions = null!;

        var action = () => services.AddServiceDiscovery(configureOptions);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(configureOptions), exception.ParamName);
    }

    [TestMethod]
    public void AddServiceDiscoveryCoreShouldThrowWhenServicesIsNull()
    {
        IServiceCollection services = null!;

        var action = () => services.AddServiceDiscoveryCore();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(services), exception.ParamName);
    }

    [TestMethod]
    public void AddServiceDiscoveryCoreWithConfigureOptionsShouldThrowWhenServicesIsNull()
    {
        IServiceCollection services = null!;
        Action<ServiceDiscoveryOptions> configureOptions = (_) => { };

        var action = () => services.AddServiceDiscoveryCore(configureOptions);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(services), exception.ParamName);
    }

    [TestMethod]
    public void AddServiceDiscoveryCoreWithConfigureOptionsShouldThrowWhenConfigureOptionsIsNull()
    {
        var services = new ServiceCollection();
        Action<ServiceDiscoveryOptions> configureOptions = null!;

        var action = () => services.AddServiceDiscoveryCore(configureOptions);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(configureOptions), exception.ParamName);
    }

    [TestMethod]
    public void AddConfigurationServiceEndpointProviderShouldThrowWhenServicesIsNull()
    {
        IServiceCollection services = null!;

        var action = () => services.AddConfigurationServiceEndpointProvider();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(services), exception.ParamName);
    }

    [TestMethod]
    public void AddConfigurationServiceEndpointProviderWithConfigureOptionsShouldThrowWhenServicesIsNull()
    {
        IServiceCollection services = null!;
        Action<ConfigurationServiceEndpointProviderOptions> configureOptions = (_) => { };

        var action = () => services.AddConfigurationServiceEndpointProvider(configureOptions);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(services), exception.ParamName);
    }

    [TestMethod]
    public void AddConfigurationServiceEndpointProviderWithConfigureOptionsShouldThrowWhenConfigureOptionsIsNull()
    {
        var services = new ServiceCollection();
        Action<ConfigurationServiceEndpointProviderOptions> configureOptions = null!;

        var action = () => services.AddConfigurationServiceEndpointProvider(configureOptions);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(configureOptions), exception.ParamName);
    }

    [TestMethod]
    public void AddPassThroughServiceEndpointProviderShouldThrowWhenServicesIsNull()
    {
        IServiceCollection services = null!;

        var action = () => services.AddPassThroughServiceEndpointProvider();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(services), exception.ParamName);
    }

    [TestMethod]
    public async Task GetEndpointsAsyncShouldThrowWhenServiceNameIsNull()
    {
        var serviceEndpointWatcherFactory = new ServiceEndpointWatcherFactory(
            new List<IServiceEndpointProviderFactory>(),
            new Logger<ServiceEndpointWatcher>(new NullLoggerFactory()),
            Options.Options.Create(new ServiceDiscoveryOptions()),
            TimeProvider.System);

        var serviceEndpointResolver = new ServiceEndpointResolver(serviceEndpointWatcherFactory, TimeProvider.System);
        string serviceName = null!;

        var action = async () => await serviceEndpointResolver.GetEndpointsAsync(serviceName, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(action);
        Assert.AreEqual(nameof(serviceName), exception.ParamName);
    }

    [TestMethod]
    public void CreateShouldThrowWhenEndPointIsNull()
    {
        EndPoint endPoint = null!;

        var action = () => ServiceEndpoint.Create(endPoint);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(endPoint), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void TryParseShouldThrowWhenEndPointIsNullOrEmpty(bool isNull)
    {
        var input = isNull ? null! : string.Empty;

        var action = () =>
        {
            _ = ServiceEndpointQuery.TryParse(input, out _);
        };

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(input), exception.ParamName);
    }

    [TestMethod]
    public void CtorServiceEndpointSourceShouldThrowWhenChangeTokenIsNull()
    {
        IChangeToken changeToken = null!;
        var features = new FeatureCollection();
        List<ServiceEndpoint>? endpoints = null;

        var action = () => new ServiceEndpointSource(endpoints, changeToken, features);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(changeToken), exception.ParamName);
    }

    [TestMethod]
    public void CtorServiceEndpointSourceShouldThrowWhenFeaturesIsNull()
    {
        var changeToken = NullChangeToken.Singleton;
        IFeatureCollection features = null!;
        List<ServiceEndpoint>? endpoints = null;

        var action = () => new ServiceEndpointSource(endpoints, changeToken, features);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(features), exception.ParamName);
    }
}
