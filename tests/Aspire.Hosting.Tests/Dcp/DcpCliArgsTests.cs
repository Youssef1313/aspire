// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Tests.Dcp;

[TestClass]
public class DcpCliArgsTests
{
    [TestMethod]
    public void TestDcpCliPathArgumentPopulatesConfig()
    {
        var builder = DistributedApplication.CreateBuilder([
            "--dcp-cli-path", "/not/a/valid/path",
            ]);

        Assert.AreEqual("/not/a/valid/path", builder.Configuration["DcpPublisher:CliPath"]);
    }

    [TestMethod]
    public void TestDcpDependencyCheckTimeoutPopulatesConfig()
    {
        var builder = DistributedApplication.CreateBuilder([
            "--dcp-dependency-check-timeout", "42",
            ]);

        Assert.AreEqual("42", builder.Configuration["DcpPublisher:DependencyCheckTimeout"]);
    }

    [TestMethod]
    public void TestDcpContainerRuntimePopulatesConfig()
    {
        var builder = DistributedApplication.CreateBuilder([
            "--dcp-container-runtime", "not-a-valid-container-runtime",
            ]);

        Assert.AreEqual("not-a-valid-container-runtime", builder.Configuration["DcpPublisher:ContainerRuntime"]);
    }

    [TestMethod]
    public void TestDcpOptionsPopulated()
    {
        var builder = DistributedApplication.CreateBuilder(
            [
            "--dcp-cli-path", "/not/a/valid/path",
            "--dcp-container-runtime", "not-a-valid-container-runtime",
            "--dcp-dependency-check-timeout", "42",
            "--dcp-dashboard-path", "/not/a/valid/path"
            ]);

        using var app = builder.Build();
        var dcpOptions = app.Services.GetRequiredService<IOptions<DcpOptions>>().Value;

        Assert.AreEqual("not-a-valid-container-runtime", dcpOptions.ContainerRuntime);
        Assert.AreEqual(42, dcpOptions.DependencyCheckTimeout);
        Assert.AreEqual("/not/a/valid/path", dcpOptions.CliPath);
        Assert.AreEqual("/not/a/valid/path", dcpOptions.DashboardPath);
    }
}
