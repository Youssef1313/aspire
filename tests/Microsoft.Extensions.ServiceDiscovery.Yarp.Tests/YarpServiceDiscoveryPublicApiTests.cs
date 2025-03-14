// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.ServiceDiscovery.Yarp.Tests;

#pragma warning disable IDE0200

public class YarpServiceDiscoveryPublicApiTests
{
    [TestMethod]
    public void AddServiceDiscoveryDestinationResolverShouldThrowWhenBuilderIsNull()
    {
        IReverseProxyBuilder builder = null!;

        var action = () => builder.AddServiceDiscoveryDestinationResolver();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void AddHttpForwarderWithServiceDiscoveryShouldThrowWhenServicesIsNull()
    {
        IServiceCollection services = null!;

        var action = () => services.AddHttpForwarderWithServiceDiscovery();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(services), exception.ParamName);
    }

    [TestMethod]
    public void AddServiceDiscoveryForwarderFactoryShouldThrowWhenServicesIsNull()
    {
        IServiceCollection services = null!;

        var action = () => services.AddServiceDiscoveryForwarderFactory();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(services), exception.ParamName);
    }
}
