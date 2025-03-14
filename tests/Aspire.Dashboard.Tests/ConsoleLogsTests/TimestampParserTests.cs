// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.ConsoleLogs;

namespace Aspire.Dashboard.Tests.ConsoleLogsTests;

[TestClass]
public class TimestampParserTests
{
    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("This is some text without any timestamp")]
    public void TryColorizeTimestamp_DoesNotStartWithTimestamp_ReturnsFalse(string input)
    {
        var result = TimestampParser.TryParseConsoleTimestamp(input, out var _);

        Assert.IsFalse(result);
    }

    [TestMethod]
    [DataRow("2023-10-10T15:05:30.123456789Z", true, "", "2023-10-10T15:05:30.123456789Z")]
    [DataRow("2023-10-10T15:05:30.123456789Z ", true, "", "2023-10-10T15:05:30.123456789Z")]
    [DataRow("2023-10-10T15:05:30.123456789Z with some text after it", true, "with some text after it", "2023-10-10T15:05:30.123456789Z")]
    [DataRow("With some text before it 2023-10-10T15:05:30.123456789Z", false, null, null)]
    public void TryColorizeTimestamp_ReturnsCorrectResult(string input, bool expectedResult, string? expectedOutput, string? expectedTimestamp)
    {
        var result = TimestampParser.TryParseConsoleTimestamp(input, out var parseResult);

        Assert.AreEqual(expectedResult, result);

        if (result)
        {
            Assert.IsNotNull(parseResult);
            Assert.AreEqual(expectedOutput, parseResult.Value.ModifiedText);
            Assert.AreEqual(expectedTimestamp != null ? (DateTimeOffset?)DateTimeOffset.Parse(expectedTimestamp, CultureInfo.InvariantCulture) : null, parseResult.Value.Timestamp);
        }
        else
        {
            Assert.IsNull(parseResult);
        }
    }

    [TestMethod]
    [DataRow("2023-10-10T15:05:30.123456789Z")]
    [DataRow("2023-10-10T15:05:30.12345678Z")]
    [DataRow("2023-10-10T15:05:30.1234567Z")]
    [DataRow("2023-10-10T15:05:30.123456Z")]
    [DataRow("2023-10-10T15:05:30.12345Z")]
    [DataRow("2023-10-10T15:05:30.1234Z")]
    [DataRow("2023-10-10T15:05:30.123Z")]
    [DataRow("2023-10-10T15:05:30.12Z")]
    [DataRow("2023-10-10T15:05:30.1Z")]
    [DataRow("2023-10-10T15:05:30Z")]
    [DataRow("2023-10-10T15:05:30.123456789+12:59")]
    [DataRow("2023-10-10T15:05:30.123456789-12:59")]
    [DataRow("2023-10-10T15:05:30.123456789")]
    public void TryColorizeTimestamp_SupportedTimestampFormats(string input)
    {
        var result = TimestampParser.TryParseConsoleTimestamp(input, out var _);

        Assert.IsTrue(result);
    }
}
