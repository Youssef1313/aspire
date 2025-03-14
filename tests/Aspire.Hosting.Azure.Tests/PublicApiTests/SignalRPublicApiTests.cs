// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Tests.PublicApiTests;

[TestClass]
public class SignalRPublicApiTests
{
    [TestMethod]
    public void CtorAzureSignalREmulatorResourceShouldThrowWhenInnerResourceIsNull()
    {
        AzureSignalRResource innerResource = null!;

        var action = () => new AzureSignalREmulatorResource(innerResource);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(innerResource), exception.ParamName);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public void AddAzureSignalRShouldThrowWhenBuilderIsNull(int overrideIndex)
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "emulator";
        var serviceMode = new AzureSignalRServiceMode();

        Action action = overrideIndex switch
        {
            0 => () => builder.AddAzureSignalR(name),
            1 => () => builder.AddAzureSignalR(name, serviceMode),
            _ => throw new InvalidOperationException()
        };

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(0, false)]
    [DataRow(0, true)]
    [DataRow(1, false)]
    [DataRow(1, true)]
    public void AddAzureSignalRShouldThrowWhenBuilderIsNullOrEmpty(int overrideIndex, bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;
        var serviceMode = new AzureSignalRServiceMode();

        Action action = overrideIndex switch
        {
            0 => () => builder.AddAzureSignalR(name),
            1 => () => builder.AddAzureSignalR(name, serviceMode),
            _ => throw new InvalidOperationException()
        };

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void RunAsEmulatorShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<AzureSignalRResource> builder = null!;

        var action = () => builder.RunAsEmulator();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CtorAzureSignalRResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var name = isNull ? null! : string.Empty;
        Action<AzureResourceInfrastructure> configureInfrastructure = (_) => { };

        var action = () => new AzureSignalRResource(name, configureInfrastructure);

        var exception = isNull
           ? Assert.Throws<ArgumentNullException>(action)
           : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void CtorAzureSignalRResourceShouldThrowWhenConfigureInfrastructureIsNull()
    {
        const string name = "signal-r";
        Action<AzureResourceInfrastructure> configureInfrastructure = null!;

        var action = () => new AzureSignalRResource(name, configureInfrastructure);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(configureInfrastructure), exception.ParamName);
    }
}
