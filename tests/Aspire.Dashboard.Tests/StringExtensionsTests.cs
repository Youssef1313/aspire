// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Extensions;

namespace Aspire.Dashboard.Tests;

[TestClass]
public class StringExtensionsTests
{
    [TestMethod]
    [DataRow("", "DefaultValue", "DefaultValue")]
    [DataRow("   ", "DefaultValue", "DefaultValue")]
    [DataRow("\t", "DefaultValue", "DefaultValue")]
    [DataRow("SingleNameOnly", null, "S")]
    [DataRow("singleNameOnly", null, "S")]
    [DataRow("Two Names", null, "TN")]
    [DataRow("two Names", null, "TN")]
    [DataRow("Two names", null, "TN")]
    [DataRow("two names", null, "TN")]
    [DataRow("With Three Names", null, "WN")]
    [DataRow("with Three Names", null, "WN")]
    [DataRow("With Three names", null, "WN")]
    [DataRow("with Three names", null, "WN")]
    [DataRow("With Hyphenated-Name", null, "WH")]
    [DataRow("with Hyphenated-Name", null, "WH")]
    [DataRow("With hyphenated-Name", null, "WH")]
    [DataRow("With Hyphenated-name", null, "WH")]
    [DataRow("with hyphenated-Name", null, "WH")]
    [DataRow("with Hyphenated-name", null, "WH")]
    [DataRow("with hyphenated-name", null, "WH")]
    public void GetInitials(string name, string? defaultValue, string expectedResult)
    {
        var actual = StringExtensions.GetInitials(name, defaultValue);

        Assert.AreEqual(expectedResult, actual);
    }
}
