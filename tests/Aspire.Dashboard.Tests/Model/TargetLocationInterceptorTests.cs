// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;

namespace Aspire.Dashboard.Tests.Model;

[TestClass]
public sealed class TargetLocationInterceptorTests
{
    [TestMethod]
    public void InterceptTargetLocation_RelativeRoot_Redirect()
    {
        Assert.IsTrue(TargetLocationInterceptor.InterceptTargetLocation("http://localhost", "/", out var newTargetLocation));
        Assert.AreEqual(TargetLocationInterceptor.StructuredLogsPath, newTargetLocation);
    }

    [TestMethod]
    public void InterceptTargetLocation_Absolute_Redirect()
    {
        Assert.IsTrue(TargetLocationInterceptor.InterceptTargetLocation("http://localhost", "http://localhost/", out var newTargetLocation));
        Assert.AreEqual(TargetLocationInterceptor.StructuredLogsPath, newTargetLocation);
    }

    [TestMethod]
    public void InterceptTargetLocation_Absolute_WithoutTrailingSlash_Redirect()
    {
        Assert.IsTrue(TargetLocationInterceptor.InterceptTargetLocation("http://localhost", "http://localhost", out var newTargetLocation));
        Assert.AreEqual(TargetLocationInterceptor.StructuredLogsPath, newTargetLocation);
    }

    [TestMethod]
    public void InterceptTargetLocation_AbsoluteDifferentCase_Redirect()
    {
        Assert.IsTrue(TargetLocationInterceptor.InterceptTargetLocation("http://LOCALHOST", "http://localhost/", out var newTargetLocation));
        Assert.AreEqual(TargetLocationInterceptor.StructuredLogsPath, newTargetLocation);
    }

    [TestMethod]
    public void InterceptTargetLocation_StructuredLogs_Unchanged()
    {
        Assert.IsFalse(TargetLocationInterceptor.InterceptTargetLocation("http://localhost", TargetLocationInterceptor.StructuredLogsPath, out _));
    }

    [TestMethod]
    public void InterceptTargetLocation_DifferentHost_Unchanged()
    {
        Assert.IsFalse(TargetLocationInterceptor.InterceptTargetLocation("http://localhost", "http://localhost:8888/", out _));
    }

    [TestMethod]
    public void InterceptTargetLocation_DifferentHost_TrailingSlash_Unchanged()
    {
        Assert.IsFalse(TargetLocationInterceptor.InterceptTargetLocation("http://localhost/", "http://localhost:8888/", out _));
    }
}
