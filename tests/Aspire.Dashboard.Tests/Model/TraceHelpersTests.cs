// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Tests.Shared.Telemetry;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Dashboard.Tests.Model;

[TestClass]
public sealed class TraceHelpersTests
{
    [TestMethod]
    public void GetOrderedApplications_SingleSpan_GroupedResult()
    {
        // Arrange
        var context = new OtlpContext { Logger = NullLogger.Instance, Options = new() };
        var app1 = new OtlpApplication("app1", "instance", context);
        var trace = new OtlpTrace(new byte[] { 1, 2, 3 });
        var scope = new OtlpScope(TelemetryTestHelpers.CreateScope(), context);
        trace.AddSpan(TelemetryTestHelpers.CreateOtlpSpan(app1, trace, scope, spanId: "1", parentSpanId: null, startDate: new DateTime(2001, 1, 1, 1, 1, 1, DateTimeKind.Utc)));

        // Act
        var results = TraceHelpers.GetOrderedApplications(trace);

        // Assert
        Assert.That.Collection(results,
            g =>
            {
                Assert.AreEqual(app1, g.Application);
            });
    }

    [TestMethod]
    public void GetOrderedApplications_MultipleUnparentedSpans_GroupedResult()
    {
        // Arrange
        var context = new OtlpContext { Logger = NullLogger.Instance, Options = new() };
        var app1 = new OtlpApplication("app1", "instance", context);
        var app2 = new OtlpApplication("app2", "instance", context);
        var trace = new OtlpTrace(new byte[] { 1, 2, 3 });
        var scope = new OtlpScope(TelemetryTestHelpers.CreateScope(), context);
        trace.AddSpan(TelemetryTestHelpers.CreateOtlpSpan(app2, trace, scope, spanId: "1-2", parentSpanId: "1", startDate: new DateTime(2001, 1, 1, 1, 1, 2, DateTimeKind.Utc)));
        trace.AddSpan(TelemetryTestHelpers.CreateOtlpSpan(app1, trace, scope, spanId: "1-1", parentSpanId: "1", startDate: new DateTime(2001, 1, 1, 1, 1, 1, DateTimeKind.Utc)));

        // Act
        var results = TraceHelpers.GetOrderedApplications(trace);

        // Assert
        Assert.That.Collection(results,
            g =>
            {
                Assert.AreEqual(app1, g.Application);
            },
            g =>
            {
                Assert.AreEqual(app2, g.Application);
            });
    }

    [TestMethod]
    public void GetOrderedApplications_ChildSpanAfterParentSpan_GroupedResult()
    {
        // Arrange
        var context = new OtlpContext { Logger = NullLogger.Instance, Options = new() };
        var app1 = new OtlpApplication("app1", "instance", context);
        var app2 = new OtlpApplication("app2", "instance", context);
        var trace = new OtlpTrace(new byte[] { 1, 2, 3 });
        var scope = new OtlpScope(TelemetryTestHelpers.CreateScope(), context);
        trace.AddSpan(TelemetryTestHelpers.CreateOtlpSpan(app1, trace, scope, spanId: "1", parentSpanId: null, startDate: new DateTime(2001, 1, 1, 1, 1, 2, DateTimeKind.Utc)));
        trace.AddSpan(TelemetryTestHelpers.CreateOtlpSpan(app2, trace, scope, spanId: "1-1", parentSpanId: "1", startDate: new DateTime(2001, 1, 1, 1, 1, 1, DateTimeKind.Utc)));

        // Act
        var results = TraceHelpers.GetOrderedApplications(trace);

        // Assert
        Assert.That.Collection(results,
            g =>
            {
                Assert.AreEqual(app1, g.Application);
            },
            g =>
            {
                Assert.AreEqual(app2, g.Application);
            });
    }

    [TestMethod]
    public void GetOrderedApplications_ChildSpanDifferentStartTime_GroupedResult()
    {
        // Arrange
        var context = new OtlpContext { Logger = NullLogger.Instance, Options = new() };
        var app1 = new OtlpApplication("app1", "instance", context);
        var app2 = new OtlpApplication("app2", "instance", context);
        var app3 = new OtlpApplication("app3", "instance", context);
        var trace = new OtlpTrace(new byte[] { 1, 2, 3 });
        var scope = new OtlpScope(TelemetryTestHelpers.CreateScope(), context);
        trace.AddSpan(TelemetryTestHelpers.CreateOtlpSpan(app1, trace, scope, spanId: "1", parentSpanId: null, startDate: new DateTime(2001, 1, 1, 1, 1, 2, DateTimeKind.Utc)));
        trace.AddSpan(TelemetryTestHelpers.CreateOtlpSpan(app2, trace, scope, spanId: "1-1", parentSpanId: "1", startDate: new DateTime(2001, 1, 1, 1, 1, 3, DateTimeKind.Utc)));
        trace.AddSpan(TelemetryTestHelpers.CreateOtlpSpan(app3, trace, scope, spanId: "1-1-1", parentSpanId: "1-1", startDate: new DateTime(2001, 1, 1, 1, 1, 2, DateTimeKind.Utc)));
        trace.AddSpan(TelemetryTestHelpers.CreateOtlpSpan(app3, trace, scope, spanId: "1-2", parentSpanId: "1", startDate: new DateTime(2001, 1, 1, 1, 1, 2, DateTimeKind.Utc)));

        // Act
        var results = TraceHelpers.GetOrderedApplications(trace);

        // Assert
        Assert.That.Collection(results,
            g =>
            {
                Assert.AreEqual(app1, g.Application);
            },
            g =>
            {
                Assert.AreEqual(app3, g.Application);
            },
            g =>
            {
                Assert.AreEqual(app2, g.Application);
            });
    }
}
