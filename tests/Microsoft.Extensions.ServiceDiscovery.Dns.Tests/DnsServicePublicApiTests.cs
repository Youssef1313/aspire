// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Tests;

public class DnsServicePublicApiTests
{
    [TestMethod]
    public void AddDnsSrvServiceEndpointProviderShouldThrowWhenServicesIsNull()
    {
        IServiceCollection services = null!;

        var action = () => services.AddDnsSrvServiceEndpointProvider();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(services), exception.ParamName);
    }

    [TestMethod]
    public void AddDnsSrvServiceEndpointProviderWithConfigureOptionsShouldThrowWhenServicesIsNull()
    {
        IServiceCollection services = null!;
        Action<DnsSrvServiceEndpointProviderOptions> configureOptions = (_) => { };

        var action = () => services.AddDnsSrvServiceEndpointProvider(configureOptions);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(services), exception.ParamName);
    }

    [TestMethod]
    public void AddDnsSrvServiceEndpointProviderWithConfigureOptionsShouldThrowWhenConfigureOptionsIsNull()
    {
        IServiceCollection services = new ServiceCollection();
        Action<DnsSrvServiceEndpointProviderOptions> configureOptions = null!;

        var action = () => services.AddDnsSrvServiceEndpointProvider(configureOptions);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(configureOptions), exception.ParamName);
    }

    [TestMethod]
    public void AddDnsServiceEndpointProviderShouldThrowWhenServicesIsNull()
    {
        IServiceCollection services = null!;

        var action = () => services.AddDnsServiceEndpointProvider();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(services), exception.ParamName);
    }

    [TestMethod]
    public void AddDnsServiceEndpointProviderWithConfigureOptionsShouldThrowWhenServicesIsNull()
    {
        IServiceCollection services = null!;
        Action<DnsServiceEndpointProviderOptions> configureOptions = (_) => { };

        var action = () => services.AddDnsServiceEndpointProvider(configureOptions);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(services), exception.ParamName);
    }

    [TestMethod]
    public void AddDnsServiceEndpointProviderWithConfigureOptionsShouldThrowWhenConfigureOptionsIsNull()
    {
        IServiceCollection services = new ServiceCollection();
        Action<DnsServiceEndpointProviderOptions> configureOptions = null!;

        var action = () => services.AddDnsServiceEndpointProvider(configureOptions);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(configureOptions), exception.ParamName);
    }
}
