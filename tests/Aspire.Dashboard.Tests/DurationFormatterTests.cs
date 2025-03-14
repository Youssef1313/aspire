// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Otlp.Model;

namespace Aspire.Dashboard.Tests;

[TestClass]
public class DurationFormatterTests
{
    [TestMethod]
    [DataRow(0, "μs")]
    [DataRow(1, "μs")]
    [DataRow(1_000, "μs")]
    [DataRow(1_000_000, "ms")]
    [DataRow(1_000_000_000, "s")]
    [DataRow(1_000_000_000_000, "h")]
    [DataRow(1_000_000_000_000_000, "h")]
    [DataRow(1_000_000_000_000_000_000, "h")]
    public void GetUnit(long ticks, string unit)
    {
        Assert.AreEqual(unit, DurationFormatter.GetUnit(TimeSpan.FromTicks(ticks)));
    }

    [TestMethod]
    public void KeepsMicrosecondsTheSame()
    {
        Assert.AreEqual("1μs", DurationFormatter.FormatDuration(TimeSpan.FromTicks(1 * TimeSpan.TicksPerMicrosecond)));
    }

    [TestMethod]
    public void DisplaysMaximumOf2UnitsAndRoundsLastOne()
    {
        var input = 10 * TimeSpan.TicksPerDay + 13 * TimeSpan.TicksPerHour + 30 * TimeSpan.TicksPerMinute;
        Assert.AreEqual("10d 14h", DurationFormatter.FormatDuration(TimeSpan.FromTicks(input)));
    }

    [TestMethod]
    public void SkipsUnitsThatAreEmpty()
    {
        var input = 2 * TimeSpan.TicksPerDay + 5 * TimeSpan.TicksPerMinute;
        Assert.AreEqual("2d", DurationFormatter.FormatDuration(TimeSpan.FromTicks(input)));
    }

    [TestMethod]
    public void DisplaysMillisecondsInDecimals()
    {
        var input = 2 * TimeSpan.TicksPerMillisecond + 357 * TimeSpan.TicksPerMicrosecond;
        Assert.AreEqual(2.36m.ToString("0.##ms", CultureInfo.CurrentCulture), DurationFormatter.FormatDuration(TimeSpan.FromTicks(input)));
    }

    [TestMethod]
    public void DisplaysSecondsInDecimals()
    {
        var input = 2 * TimeSpan.TicksPerSecond + 357 * TimeSpan.TicksPerMillisecond;
        Assert.AreEqual(2.36m.ToString("0.##s", CultureInfo.CurrentCulture), DurationFormatter.FormatDuration(TimeSpan.FromTicks(input)));
    }

    [TestMethod]
    public void DisplaysMinutesInSplitUnits()
    {
        var input = 2 * TimeSpan.TicksPerMinute + 30 * TimeSpan.TicksPerSecond + 555 * TimeSpan.TicksPerMillisecond;
        Assert.AreEqual("2m 31s", DurationFormatter.FormatDuration(TimeSpan.FromTicks(input)));
    }

    [TestMethod]
    public void DisplaysHoursInSplitUnits()
    {
        var input = 2 * TimeSpan.TicksPerHour + 30 * TimeSpan.TicksPerMinute + 30 * TimeSpan.TicksPerSecond;
        Assert.AreEqual("2h 31m", DurationFormatter.FormatDuration(TimeSpan.FromTicks(input)));
    }

    [TestMethod]
    public void DisplaysTimesLessThanMicroseconds()
    {
        var input = (double)TimeSpan.TicksPerMicrosecond / 10;
        Assert.AreEqual(0.1m.ToString("0.##μs", CultureInfo.CurrentCulture), DurationFormatter.FormatDuration(TimeSpan.FromTicks((long)input)));
    }

    [TestMethod]
    public void DisplaysTimesOf0()
    {
        var input = 0;
        Assert.AreEqual("0μs", DurationFormatter.FormatDuration(TimeSpan.FromTicks(input)));
    }
}
