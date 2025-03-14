// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Tests.Utils;

[TestClass]

// Based on tests at https://github.com/distribution/reference/blob/main/reference_test.go
public class ContainerReferenceParserTests
{
    [TestMethod]
    public void EmptyInput()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => ContainerReferenceParser.Parse(""));
        Assert.StartsWith("repository name must have at least one component", ex.Message);
    }

    [TestMethod]
    [DataRow("  ")]
    [DataRow(":justtag")]
    [DataRow("@sha256:ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")]
    [DataRow("aa/asdf$$^/aa")]
    [DataRow("[2001:db8::1]")]
    [DataRow("[2001:db8::1]:5000")]
    [DataRow("[2001:db8::1]:tag")]
    [DataRow("[fe80::1%eth0]:5000/repo")]
    [DataRow("[fe80::1%@invalidzone]:5000/repo")]
    public void InvalidReferenceFormat(string input)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => ContainerReferenceParser.Parse(input));
        Assert.StartsWith("invalid reference format", ex.Message);
    }

    [TestMethod]
    [DataRow("test_com", "test_com")]
    [DataRow("192.168.1.1", "192.168.1.1")]
    public void ImageTests(string input, string expectedImage)
        => ParserTest(input, null, expectedImage, null, null);

    [TestMethod]
    [DataRow("test_com:tag", "test_com", "tag")]
    [DataRow("test.com:5000", "test.com", "5000")]
    [DataRow("lowercase:Uppercase", "lowercase", "Uppercase")]
    [DataRow("foo_bar.com:8080", "foo_bar.com", "8080")]
    [DataRow("192.168.1.1:tag", "192.168.1.1", "tag")]
    [DataRow("192.168.1.1:5000", "192.168.1.1", "5000")]
    [DataRow("a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a:tag-puts-this-over-max", "a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a/a", "tag-puts-this-over-max")]
    [DataRow("foo/foo_bar.com:8080", "foo/foo_bar.com", "8080")]
    public void ImageAndTagTests(string input, string expectedImage, string expectedTag)
        => ParserTest(input, null, expectedImage, expectedTag: expectedTag, null);

    [TestMethod]
    [DataRow("test:5000/repo", "test:5000", "repo")]
    [DataRow("sub-dom1.foo.com/bar/baz/quux", "sub-dom1.foo.com", "bar/baz/quux")]
    [DataRow("192.168.1.1/repo", "192.168.1.1", "repo")]
    [DataRow("192.168.1.1:5000/repo", "192.168.1.1:5000", "repo")]
    [DataRow("[2001:db8::1]/repo", "[2001:db8::1]", "repo")]
    [DataRow("[2001:db8::1]:5000/repo", "[2001:db8::1]:5000", "repo")]
    [DataRow("[2001:db8::]:5000/repo", "[2001:db8::]:5000", "repo")]
    [DataRow("[::1]:5000/repo", "[::1]:5000", "repo")]
    public void DomainAndImageTests(string input, string expectedRegistry, string expectedImage)
        => ParserTest(input, expectedRegistry, expectedImage, null, null);

    [TestMethod]
    [DataRow("test.com/repo:tag", "test.com", "repo", "tag")]
    [DataRow("test:5000/repo:tag", "test:5000", "repo", "tag")]
    [DataRow("sub-dom1.foo.com/bar/baz/quux:some-long-tag", "sub-dom1.foo.com", "bar/baz/quux", "some-long-tag")]
    [DataRow("b.gcr.io/test.example.com/my-app:test.example.com", "b.gcr.io", "test.example.com/my-app", "test.example.com")]
    [DataRow("xn--n3h.com/myimage:xn--n3h.com", "xn--n3h.com", "myimage", "xn--n3h.com")] // ☃.com in punycode
    [DataRow("192.168.1.1:5000/repo:5050", "192.168.1.1:5000", "repo", "5050")]
    [DataRow("[2001:db8:1:2:3:4:5:6]/repo:tag", "[2001:db8:1:2:3:4:5:6]", "repo", "tag")]
    [DataRow("[2001:db8::1]:5000/repo:tag", "[2001:db8::1]:5000", "repo", "tag")]
    [DataRow("localhost/repo:tag", "localhost", "repo", "tag")]
    public void DomainImageAndTagTests(string input, string expectedRegistry, string expectedImage, string expectedTag)
        => ParserTest(input, expectedRegistry, expectedImage, expectedTag, null);

    [TestMethod]
    [DataRow("test:5000/repo@sha256:ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff", "test:5000", "repo", "sha256:ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")]
    [DataRow("[2001:db8::1]:5000/repo@sha256:ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff", "[2001:db8::1]:5000", "repo", "sha256:ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")]
    [DataRow("whatever:5000/repo@algo:value", "whatever:5000", "repo", "algo:value")]
    [DataRow("localhost/repo@digest", "localhost", "repo", "digest")]
    public void DomainImageAndDigestTests(string input, string expectedRegistry, string expectedImage, string expectedDigest)
        => ParserTest(input, expectedRegistry, expectedImage, null, expectedDigest);

    [TestMethod]
    [DataRow("test:5000/repo:tag@sha256:ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff", "test:5000", "repo", "tag", "sha256:ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")]
    [DataRow("xn--7o8h.com/myimage:xn--7o8h.com@sha256:ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff", "xn--7o8h.com", "myimage", "xn--7o8h.com", "sha256:ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")] // 🐳.com in punycode
    [DataRow("[2001:db8::1]:5000/repo:tag@sha256:ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff", "[2001:db8::1]:5000", "repo", "tag", "sha256:ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")]
    public void DomainImageTagAndDigestTests(string input, string expectedRegistry, string expectedImage, string expectedTag, string expectedDigest)
        => ParserTest(input, expectedRegistry, expectedImage, expectedTag, expectedDigest);

    private static void ParserTest(string input, string? expectedRegistry, string expectedImage, string? expectedTag, string? expectedDigest)
    {
        var result = ContainerReferenceParser.Parse(input);

        Assert.Multiple(() =>
        {
            Assert.AreEqual(expectedRegistry, result.Registry);
            Assert.AreEqual(expectedImage, result.Image);
            Assert.AreEqual(expectedTag, result.Tag);
            Assert.AreEqual(expectedDigest, result.Digest);
        });

    }

}