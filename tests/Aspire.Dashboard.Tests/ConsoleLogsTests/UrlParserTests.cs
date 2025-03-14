// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Aspire.Dashboard.ConsoleLogs;

namespace Aspire.Dashboard.Tests.ConsoleLogsTests;

[TestClass]
public class UrlParserTests
{
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("This is some text without any urls")]
    public void TryParse_NoUrl_ReturnsFalse(string? input)
    {
        var result = UrlParser.TryParse(input, WebUtility.HtmlEncode, out var _);

        Assert.IsFalse(result);
    }

    [TestMethod]
    [DataRow("This is some text with a URL at the end: https://bing.com/", true, "This is some text with a URL at the end: <a target=\"_blank\" href=\"https://bing.com/\" rel=\"noopener noreferrer nofollow\">https://bing.com/</a>")]
    [DataRow("https://bing.com/ This is some text with a URL at the beginning", true, "<a target=\"_blank\" href=\"https://bing.com/\" rel=\"noopener noreferrer nofollow\">https://bing.com/</a> This is some text with a URL at the beginning")]
    [DataRow("This is some text with a https://bing.com/ in the middle", true, "This is some text with a <a target=\"_blank\" href=\"https://bing.com/\" rel=\"noopener noreferrer nofollow\">https://bing.com/</a> in the middle")]
    public void TryParse_ReturnsCorrectResult(string input, bool expectedResult, string? expectedOutput)
    {
        var result = UrlParser.TryParse(input, WebUtility.HtmlEncode, out var modifiedText);

        Assert.AreEqual(expectedResult, result);
        Assert.AreEqual(expectedOutput, modifiedText);
    }

    [TestMethod]
    [DataRow("http://bing.com", "<a target=\"_blank\" href=\"http://bing.com\" rel=\"noopener noreferrer nofollow\">http://bing.com</a>")]
    [DataRow("https://bing.com", "<a target=\"_blank\" href=\"https://bing.com\" rel=\"noopener noreferrer nofollow\">https://bing.com</a>")]
    [DataRow("http://www.bing.com", "<a target=\"_blank\" href=\"http://www.bing.com\" rel=\"noopener noreferrer nofollow\">http://www.bing.com</a>")]
    [DataRow("http://bing.com/", "<a target=\"_blank\" href=\"http://bing.com/\" rel=\"noopener noreferrer nofollow\">http://bing.com/</a>")]
    [DataRow("http://bing.com/dir", "<a target=\"_blank\" href=\"http://bing.com/dir\" rel=\"noopener noreferrer nofollow\">http://bing.com/dir</a>")]
    [DataRow("http://bing.com/index.aspx", "<a target=\"_blank\" href=\"http://bing.com/index.aspx\" rel=\"noopener noreferrer nofollow\">http://bing.com/index.aspx</a>")]
    [DataRow("http://localhost", "<a target=\"_blank\" href=\"http://localhost\" rel=\"noopener noreferrer nofollow\">http://localhost</a>")]
    public void TryParse_SupportedUrlFormats(string input, string? expectedOutput)
    {
        var result = UrlParser.TryParse(input, WebUtility.HtmlEncode, out var modifiedText);

        Assert.IsTrue(result);
        Assert.AreEqual(expectedOutput, modifiedText);
    }

    [TestMethod]
    [DataRow("file:///c:/windows/system32/calc.exe")]
    [DataRow("ftp://ftp.localhost.com/")]
    [DataRow("ftp://user:pass@ftp.localhost.com/")]
    public void TryParse_UnsupportedUrlFormats(string input)
    {
        var result = UrlParser.TryParse(input, WebUtility.HtmlEncode, out var _);

        Assert.IsFalse(result);
    }

    [TestMethod]
    [DataRow("script:alert('hi')")]
    [DataRow("http://script:alert('hi')")]
    public void TryParse_AttemptedScriptInjection(string input)
    {
        var result = UrlParser.TryParse(input, WebUtility.HtmlEncode, out var _);

        Assert.IsFalse(result);
    }

    [TestMethod]
    [DataRow("http://localhost:8080</url>", "<a target=\"_blank\" href=\"http://localhost:8080\" rel=\"noopener noreferrer nofollow\">http://localhost:8080</a>&lt;/url&gt;")]
    [DataRow("http://localhost:8080\"", "<a target=\"_blank\" href=\"http://localhost:8080\" rel=\"noopener noreferrer nofollow\">http://localhost:8080</a>&quot;")]
    public void TryParse_ExcludeInvalidTrailingChars(string input, string? expectedOutput)
    {
        var result = UrlParser.TryParse(input, WebUtility.HtmlEncode, out var modifiedText);
        Assert.IsTrue(result);

        Assert.AreEqual(expectedOutput, modifiedText);
    }

    [TestMethod]
    public void TryParse_QueryString()
    {
        var result = UrlParser.TryParse("https://www.example.com?query=string&param=value", WebUtility.HtmlEncode, out var modifiedText);
        Assert.IsTrue(result);

        Assert.AreEqual("<a target=\"_blank\" href=\"https://www.example.com?query=string&param=value\" rel=\"noopener noreferrer nofollow\">https://www.example.com?query=string&amp;param=value</a>", modifiedText);
    }

    [TestMethod]
    [DataRow("http://www.localhost:8080")]
    [DataRow("HTTP://WWW.LOCALHOST:8080")]
    [DataRow("mhttp://www.localhost:8080")]
    [DataRow("httphttp://www.localhost:8080")]
    [DataRow(" http://www.localhost:8080")]
    public void GenerateUrlRegEx_MatchUrlAfterContent(string content)
    {
        var regex = UrlParser.GenerateUrlRegEx();
        var match = regex.Match(content);
        Assert.AreEqual("http://www.localhost:8080", match.Value.ToLowerInvariant());
    }

    [TestMethod]
    [DataRow("http://www.localhost:8080!", "http://www.localhost:8080!")]
    [DataRow("http://www.localhost:8080/path!", "http://www.localhost:8080/path!")]
    [DataRow("http://www.localhost:8080/path;", "http://www.localhost:8080/path")]
    [DataRow("http://www.localhost:8080;", "http://www.localhost:8080")]
    [DataRow("http://www.local;host:8080;", "http://www.local")]
    public void GenerateUrlRegEx_MatchUrlBeforeContent(string content, string expected)
    {
        var regex = UrlParser.GenerateUrlRegEx();
        var match = regex.Match(content);
        Assert.AreEqual(expected, match.Value.ToLowerInvariant());
    }
}
