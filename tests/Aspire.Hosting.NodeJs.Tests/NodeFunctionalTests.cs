// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.Testing;

namespace Aspire.Hosting.NodeJs.Tests;

[TestClass]
public class NodeFunctionalTests : IClassFixture<NodeAppFixture>
{
    private readonly NodeAppFixture _nodeJsFixture;

    public NodeFunctionalTests(NodeAppFixture nodeJsFixture)
    {
        _nodeJsFixture = nodeJsFixture;
    }

    [TestMethod]
    [RequiresTools(["node"])]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4508", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningOnCI))]
    public async Task VerifyNodeAppWorks()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        using var nodeClient = _nodeJsFixture.App.CreateHttpClient(_nodeJsFixture.NodeAppBuilder!.Resource.Name, "http");
        var response = await nodeClient.GetStringAsync("/", cts.Token);

        Assert.AreEqual("Hello from node!", response);
    }

    [TestMethod]
    [RequiresTools(["npm"])]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4508", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningOnCI))]
    public async Task VerifyNpmAppWorks()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        using var npmClient = _nodeJsFixture.App.CreateHttpClient(_nodeJsFixture.NpmAppBuilder!.Resource.Name, "http");
        var response = await npmClient.GetStringAsync("/", cts.Token);

        Assert.AreEqual("Hello from npm!", response);
    }
}
