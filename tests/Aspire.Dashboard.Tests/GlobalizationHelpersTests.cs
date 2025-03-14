// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Utils;

namespace Aspire.Dashboard.Tests;

[TestClass]
public class GlobalizationHelpersTests
{
    [TestMethod]
    public void ExpandedLocalizedCultures_IncludesPopularCultures()
    {
        // Act
        var supportedCultures = GlobalizationHelpers.ExpandedLocalizedCultures
            .SelectMany(kvp => kvp.Value)
            .Select(c => c.Name)
            .ToList();

        // Assert
        foreach (var localizedCulture in GlobalizationHelpers.LocalizedCultures)
        {
            Assert.Contains(localizedCulture.Name, supportedCultures);
        }

        // A few cultures we expect to be available
        Assert.Contains("en-GB", supportedCultures);
        Assert.Contains("fr-CA", supportedCultures);
        Assert.Contains("zh-CN", supportedCultures);
    }

    [TestMethod]
    [DataRow("en", true, "en")]
    [DataRow("en-US", true, "en")]
    [DataRow("fr", true, "fr")]
    [DataRow("zh-Hans", true, "zh-Hans")]
    [DataRow("zh-Hant", true, "zh-Hant")]
    [DataRow("zh-CN", true, "zh-Hans")]
    [DataRow("es", false, null)]
    [DataRow("aa-bb", false, null)]
    public void TryGetKnownParentCulture_VariousCultures_ReturnsExpectedResult(string cultureName, bool expectedResult, string? expectedMatchedCultureName)
    {
        // Arrange
        var cultureOptions = new List<CultureInfo>
        {
            new("en"),
            new("fr"),
            new("zh-Hans"),
            new("zh-Hant")
        };
        var culture = new CultureInfo(cultureName);

        // Act
        var result = GlobalizationHelpers.TryGetKnownParentCulture(cultureOptions, culture, out var matchedCulture);

        // Assert
        Assert.AreEqual(expectedResult, result);
        if (expectedMatchedCultureName is null)
        {
            Assert.IsNull(matchedCulture);
        }
        else
        {
            Assert.AreEqual(new CultureInfo(expectedMatchedCultureName), matchedCulture);
        }
    }

    [TestMethod]
    [DataRow("en", "en-US", "en-US")]
    [DataRow("en", "en-XX", "en")]
    [DataRow("de", "en-US", null)]
    [DataRow("zh-Hans", "en-US,en;q=0.9,zh-CN;q=0.8,zh;q=0.7", "zh-CN")]
    public async Task ResolveSetCultureToAcceptedCultureAsync_MatchRequestToResult(string requestedLanguage, string acceptLanguage, string? result)
    {
        // Arrange
        var englishCultures = GlobalizationHelpers.ExpandedLocalizedCultures[requestedLanguage];

        // Act
        var requestCulture = await GlobalizationHelpers.ResolveSetCultureToAcceptedCultureAsync(acceptLanguage, englishCultures);

        // Assert
        if (result != null)
        {
            Assert.IsNotNull(requestCulture);
            Assert.AreEqual(result, requestCulture.Culture.Name);
            Assert.AreEqual(result, requestCulture.UICulture.Name);
        }
        else
        {
            Assert.IsNull(requestCulture);
        }
    }
}
