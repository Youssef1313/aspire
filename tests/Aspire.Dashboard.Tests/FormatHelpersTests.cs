// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Utils;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Dashboard.Tests;

[TestClass]
public class FormatHelpersTests
{
    [TestMethod]
    [DataRow("9", 9d)]
    [DataRow("9.9", 9.9d)]
    [DataRow("0.9", 0.9d)]
    [DataRow("12,345,678.9", 12345678.9d)]
    [DataRow("1.234568", 1.23456789d)]
    public void FormatNumberWithOptionalDecimalPlaces_InvariantCulture(string expected, double value)
    {
        Assert.AreEqual(expected, FormatHelpers.FormatNumberWithOptionalDecimalPlaces(value, maxDecimalPlaces: 6, CultureInfo.InvariantCulture));
    }

    [TestMethod]
    [DataRow("9", 9d)]
    [DataRow("9,9", 9.9d)]
    [DataRow("0,9", 0.9d)]
    [DataRow("12.345.678,9", 12345678.9d)]
    [DataRow("1,234568", 1.23456789d)]
    public void FormatNumberWithOptionalDecimalPlaces_GermanCulture(string expected, double value)
    {
        Assert.AreEqual(expected, FormatHelpers.FormatNumberWithOptionalDecimalPlaces(value, maxDecimalPlaces: 6, CultureInfo.GetCultureInfo("de-DE")));
    }

    [TestMethod]
    [DataRow("06/15/2009 13:45:30.000", MillisecondsDisplay.Truncated, "2009-06-15T13:45:30.0000000Z")]
    [DataRow("06/15/2009 13:45:30.123", MillisecondsDisplay.Truncated, "2009-06-15T13:45:30.1234567Z")]
    [DataRow("06/15/2009 13:45:30.1234567", MillisecondsDisplay.Full, "2009-06-15T13:45:30.1234567Z")]
    [DataRow("06/15/2009 13:45:30", MillisecondsDisplay.None, "2009-06-15T13:45:30.0000000Z")]
    [DataRow("06/15/2009 13:45:30", MillisecondsDisplay.None, "2009-06-15T13:45:30.1234567Z")]
    public void FormatDateTime_WithMilliseconds_InvariantCulture(string expected, MillisecondsDisplay includeMilliseconds, string value)
    {
        var date = GetLocalDateTime(value);
        Assert.AreEqual(expected, FormatHelpers.FormatDateTime(CreateTimeProvider(), date, includeMilliseconds, cultureInfo: CultureInfo.InvariantCulture));
    }

    [TestMethod]
    [DataRow("15.06.2009 13:45:30,000", MillisecondsDisplay.Truncated, "2009-06-15T13:45:30.0000000Z")]
    [DataRow("15.06.2009 13:45:30,123", MillisecondsDisplay.Truncated, "2009-06-15T13:45:30.1234567Z")]
    [DataRow("15.06.2009 13:45:30,1234567", MillisecondsDisplay.Full, "2009-06-15T13:45:30.1234567Z")]
    [DataRow("15.06.2009 13:45:30", MillisecondsDisplay.None, "2009-06-15T13:45:30.0000000Z")]
    [DataRow("15.06.2009 13:45:30", MillisecondsDisplay.None, "2009-06-15T13:45:30.1234567Z")]
    public void FormatDateTime_WithMilliseconds_GermanCulture(string expected, MillisecondsDisplay includeMilliseconds, string value)
    {
        var date = GetLocalDateTime(value);
        Assert.AreEqual(expected, FormatHelpers.FormatDateTime(CreateTimeProvider(), date, includeMilliseconds, cultureInfo: CultureInfo.GetCultureInfo("de-DE")));
    }

    [TestMethod]
    [DataRow("15.6.2009 13.45.30,000", MillisecondsDisplay.Truncated, "2009-06-15T13:45:30.0000000Z")]
    [DataRow("15.6.2009 13.45.30,123", MillisecondsDisplay.Truncated, "2009-06-15T13:45:30.1234567Z")]
    [DataRow("15.6.2009 13.45.30,1234567", MillisecondsDisplay.Full, "2009-06-15T13:45:30.1234567Z")]
    [DataRow("15.6.2009 13.45.30", MillisecondsDisplay.None, "2009-06-15T13:45:30.0000000Z")]
    [DataRow("15.6.2009 13.45.30", MillisecondsDisplay.None, "2009-06-15T13:45:30.1234567Z")]
    public void FormatDateTime_WithMilliseconds_FinnishCulture(string expected, MillisecondsDisplay includeMilliseconds, string value)
    {
        var date = GetLocalDateTime(value);
        Assert.AreEqual(expected, FormatHelpers.FormatDateTime(CreateTimeProvider(), date, includeMilliseconds, cultureInfo: CultureInfo.GetCultureInfo("fi-FI")));
    }

    [TestMethod]
    [DataRow("15/06/2009 1:45:30.000 pm", MillisecondsDisplay.Truncated, "2009-06-15T13:45:30.0000000Z")]
    [DataRow("15/06/2009 1:45:30.123 pm", MillisecondsDisplay.Truncated, "2009-06-15T13:45:30.1234567Z")]
    [DataRow("15/06/2009 1:45:30.1234567 pm", MillisecondsDisplay.Full, "2009-06-15T13:45:30.1234567Z")]
    [DataRow("15/06/2009 1:45:30 pm", MillisecondsDisplay.None, "2009-06-15T13:45:30.0000000Z")]
    [DataRow("15/06/2009 1:45:30 pm", MillisecondsDisplay.None, "2009-06-15T13:45:30.1234567Z")]
    public void FormatDateTime_WithMilliseconds_NewZealandCulture(string expected, MillisecondsDisplay includeMilliseconds, string value)
    {
        var date = GetLocalDateTime(value);
        Assert.AreEqual(expected, FormatHelpers.FormatDateTime(CreateTimeProvider(), date, includeMilliseconds, cultureInfo: CultureInfo.GetCultureInfo("en-NZ")), ignoreWhiteSpaceDifferences: true);
    }

    private static DateTime GetLocalDateTime(string value)
    {
        Assert.IsTrue(DateTime.TryParseExact(value, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date));
        Assert.AreEqual(DateTimeKind.Utc, date.Kind);
        date = DateTime.SpecifyKind(date, DateTimeKind.Local);
        return date;
    }

    private static BrowserTimeProvider CreateTimeProvider()
    {
        return new BrowserTimeProvider(NullLoggerFactory.Instance);
    }
}
