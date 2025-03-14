// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using static Aspire.Hosting.Utils.CommandLineArgsParser;

namespace Aspire.Hosting.Tests.Utils;

[TestClass]
public class CommandLineArgsParserTests
{
    [TestMethod]
    [DataRow("", new string[] { })]
    [DataRow("single", new[] { "single" })]
    [DataRow("hello world", new[] { "hello", "world" })]
    [DataRow("foo bar baz", new[] { "foo", "bar", "baz" })]
    [DataRow("foo\tbar\tbaz", new[] { "foo", "bar", "baz" })]
    [DataRow("\"quoted string\"", new[] { "quoted string" })]
    [DataRow("\"quoted\tstring\"", new[] { "quoted\tstring" })]
    [DataRow("\"quoted \"\" string\"", new[] { "quoted \" string" })]
    // Single quotes are not treated as string delimiters
    [DataRow("\"hello 'world'\"", new[] { "hello 'world'" })]
    [DataRow("'single quoted'", new[] { "'single", "quoted'" })]
    [DataRow("'foo \"bar\" baz'", new[] { "'foo", "bar", "baz'" })]
    public void TestParse(string commandLine, string[] expectedParsed)
    {
        var actualParsed = Parse(commandLine);

        Assert.AreEqual(expectedParsed, actualParsed.ToArray());
    }
}
