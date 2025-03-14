// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Tests.Shared.Telemetry;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Dashboard.Tests.TelemetryRepositoryTests;

[TestClass]
public class OtlpSpanTests
{
    private static readonly DateTime s_testTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void AllProperties()
    {
        // Arrange
        var context = new OtlpContext { Logger = NullLogger.Instance, Options = new() };
        var app1 = new OtlpApplication("app1", "instance", context);
        var trace = new OtlpTrace(new byte[] { 1, 2, 3 });
        var scope = new OtlpScope(TelemetryTestHelpers.CreateScope(), context);

        var span = TelemetryTestHelpers.CreateOtlpSpan(app1, trace, scope, spanId: "abc", parentSpanId: null, startDate: s_testTime,
            statusCode: OtlpSpanStatusCode.Ok, statusMessage: "Status message!", attributes: [new KeyValuePair<string, string>(KnownTraceFields.StatusMessageField, "value")]);

        // Act
        var properties = span.AllProperties();

        // Assert
        Assert.That.Collection(properties,
            a =>
            {
                Assert.AreEqual("trace.spanid", a.Key);
                Assert.AreEqual("abc", a.Value);
            },
            a =>
            {
                Assert.AreEqual("trace.name", a.Key);
                Assert.AreEqual("Test", a.Value);
            },
            a =>
            {
                Assert.AreEqual("trace.kind", a.Key);
                Assert.AreEqual("Unspecified", a.Value);
            },
            a =>
            {
                Assert.AreEqual("trace.status", a.Key);
                Assert.AreEqual("Ok", a.Value);
            },
            a =>
            {
                Assert.AreEqual("trace.statusmessage", a.Key);
                Assert.AreEqual("Status message!", a.Value);
            },
            a =>
            {
                Assert.AreEqual("unknown-trace.statusmessage", a.Key);
                Assert.AreEqual("value", a.Value);
            });
    }
}
