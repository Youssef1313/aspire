// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;


namespace Aspire.Hosting.Tests;

[TestClass]
public class ReferenceExpressionTests
{
    [TestMethod]
    [DataRow("world", "Hello world", "Hello world")]
    [DataRow("{", "Hello {{", "Hello {")]
    [DataRow("}", "Hello }}", "Hello }")]
    [DataRow("{1}", "Hello {{1}}", "Hello {1}")]
    [DataRow("{x}", "Hello {{x}}", "Hello {x}")]
    [DataRow("{{x}}", "Hello {{{{x}}}}", "Hello {{x}}")]
    public void TestReferenceExpressionCreateInputStringTreatedAsLiteral(string input, string expectedFormat, string expectedExpression)
    {
        var refExpression = ReferenceExpression.Create($"Hello {input}");
        Assert.AreEqual(expectedFormat, refExpression.Format);

        // Generally, the input string should end up unchanged in the expression, since it's a literal
        var expr = refExpression.ValueExpression;
        Assert.AreEqual(expectedExpression, expr);
    }

    [TestMethod]
    [DataRow("{x}", "{x}")]
    [DataRow("{x", "{x")]
    [DataRow("x}", "x}")]
    [DataRow("{1 var}", "{1 var}")]
    [DataRow("{var 1}", "{var 1}")]
    [DataRow("{1myVar}", "{1myVar}")]
    [DataRow("{myVar1}", "{myVar1}")]
    public void ReferenceExpressionHandlesValueWithNonParameterBrackets(string input, string expected)
    {
        var expr = ReferenceExpression.Create($"{input}").ValueExpression;
        Assert.AreEqual(expected, expr);
    }

    [TestMethod]
    [DataRow("{0}", new string[] { "abc123" }, "abc123")]
    [DataRow("{0} test", new string[] { "abc123" }, "abc123 test")]
    [DataRow("test {0}", new string[] { "abc123" }, "test abc123")]
    [DataRow("https://{0}:{1}/{2}?key={3}", new string[] { "test.com", "443", "path", "1234" }, "https://test.com:443/path?key=1234")]
    public void ReferenceExpressionHandlesValueWithParameterBrackets(string input, string[] parameters, string expected)
    {
        var expr = ReferenceExpression.Create($"{input}", [new HostUrl("test")], parameters).ValueExpression;
        Assert.AreEqual(expected, expr);
    }

    public static readonly object[][] ValidFormattingInParameterBracketCases = [
        ["{0:D}", new DateTime(2024,05,22), string.Format(CultureInfo.InvariantCulture, "{0:D}", new DateTime(2024, 05, 22).ToString())],
        ["{0:N}", 123456.78, string.Format(CultureInfo.InvariantCulture, "{0:N}", "123456.78")]
    ];

    [Theory, MemberData(nameof(ValidFormattingInParameterBracketCases))]
    public void ReferenceExpressionHandlesValueWithFormattingInParameterBrackets(string input, string parameterValue, string expected)
    {
        var expr = ReferenceExpression.Create($"{input}", [new HostUrl("test")], [parameterValue]).ValueExpression;
        Assert.AreEqual(expected, expr);
    }

    [TestMethod]
    public void ReferenceExpressionHandlesValueWithoutBrackets()
    {
        var s = "Test";
        var expr = ReferenceExpression.Create($"{s}").ValueExpression;
        Assert.AreEqual("Test", expr);
    }
}
