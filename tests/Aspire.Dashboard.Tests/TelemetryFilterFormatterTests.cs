// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Model.Otlp;

namespace Aspire.Dashboard.Tests;

[TestClass]
public class TelemetryFilterFormatterTests
{
    [TestMethod]
    public void RoundTripFilterWithColon()
    {
        var serializedFilters = TelemetryFilterFormatter.SerializeFiltersToString([
            new TelemetryFilter
            {
                Field = "test:name",
                Condition = FilterCondition.Equals,
                Value = "test:value"
            }
        ]);

        var filters = TelemetryFilterFormatter.DeserializeFiltersFromString(serializedFilters);

        var filter = Assert.ContainsSingle(filters);

        Assert.AreEqual("test:name", filter.Field);
        Assert.AreEqual("test:value", filter.Value);
    }

    [TestMethod]
    public void RoundTripFiltersWithPluses()
    {
        var serializedFilters = TelemetryFilterFormatter.SerializeFiltersToString([
            new TelemetryFilter
            {
                Field = "test+name",
                Condition = FilterCondition.Equals,
                Value = "test+value"
            }
        ]);

        var filters = TelemetryFilterFormatter.DeserializeFiltersFromString(serializedFilters);

        var filter = Assert.ContainsSingle(filters);

        Assert.AreEqual("test+name", filter.Field);
        Assert.AreEqual("test+value", filter.Value);
    }
}
