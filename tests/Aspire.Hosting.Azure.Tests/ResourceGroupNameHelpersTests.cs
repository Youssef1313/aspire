// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Azure.Utils;

namespace Aspire.Hosting.Azure.Tests;

[TestClass]
public class ResourceGroupNameHelpersTests
{
    [TestMethod]
    [DataRow("äæǽåàçéïôùÀÇÉÏÔÙ", "aaaceiouACEIOU")]
    [DataRow("🔥🤔😅🤘", "")]
    [DataRow("こんにちは", "")]
    [DataRow("", "")]
    [DataRow("  ", "")]
    [DataRow("-.()_", "-_")]
    [DataRow("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_", "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_")]
    public void ShouldCreateAzdCompatibleResourceGroupNames(string input, string expected)
    {
        var result = ResourceGroupNameHelpers.NormalizeResourceGroupName(input);

        Assert.AreEqual(expected, result);
    }
}
