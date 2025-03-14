// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;

namespace Aspire.Dashboard.Tests;

[TestClass]
public class BrowserLinkOutgoingPeerResolverTests
{
    [TestMethod]
    public void EmptyAttributes_NoMatch()
    {
        // Arrange
        var resolver = new BrowserLinkOutgoingPeerResolver();

        // Act & Assert
        Assert.IsFalse(TryResolvePeerName(resolver, [], out _));
    }

    [TestMethod]
    public void EmptyUrlAttribute_NoMatch()
    {
        // Arrange
        var resolver = new BrowserLinkOutgoingPeerResolver();

        // Act & Assert
        Assert.IsFalse(TryResolvePeerName(resolver, [KeyValuePair.Create("http.url", "")], out _));
    }

    [TestMethod]
    public void NullUrlAttribute_NoMatch()
    {
        // Arrange
        var resolver = new BrowserLinkOutgoingPeerResolver();

        // Act & Assert
        Assert.IsFalse(TryResolvePeerName(resolver, [KeyValuePair.Create<string, string>("http.url", null!)], out _));
    }

    // http://localhost:59267/6eed7c2dedc14419901b813e8fe87a86/getScriptTag

    [TestMethod]
    public void RelativeUrlAttribute_NoMatch()
    {
        // Arrange
        var resolver = new BrowserLinkOutgoingPeerResolver();

        // Act & Assert
        Assert.IsFalse(TryResolvePeerName(resolver, [KeyValuePair.Create("http.url", "/6eed7c2dedc14419901b813e8fe87a86/getScriptTag")], out _));
    }

    [TestMethod]
    public void NonLocalHostUrlAttribute_NoMatch()
    {
        // Arrange
        var resolver = new BrowserLinkOutgoingPeerResolver();

        // Act & Assert
        Assert.IsFalse(TryResolvePeerName(resolver, [KeyValuePair.Create("http.url", "http://dummy:59267/6eed7c2dedc14419901b813e8fe87a86/getScriptTag")], out _));
    }

    [TestMethod]
    public void NoPathGuidUrlAttribute_NoMatch()
    {
        // Arrange
        var resolver = new BrowserLinkOutgoingPeerResolver();

        // Act & Assert
        Assert.IsFalse(TryResolvePeerName(resolver, [KeyValuePair.Create("http.url", "http://localhost:59267/getScriptTag")], out _));
    }

    [TestMethod]
    public void InvalidUrlAttribute_NoMatch()
    {
        // Arrange
        var resolver = new BrowserLinkOutgoingPeerResolver();

        // Act & Assert
        Assert.IsFalse(TryResolvePeerName(resolver, [KeyValuePair.Create("http.url", "ht$tp://localhost:59267/6eed7c2dedc14419901b813e8fe87a86/getScriptTag")], out _));
    }

    [TestMethod]
    public void NoPathUrlAttribute_Match()
    {
        // Arrange
        var resolver = new BrowserLinkOutgoingPeerResolver();

        // Act & Assert
        Assert.IsFalse(TryResolvePeerName(resolver, [KeyValuePair.Create("http.url", "http://localhost:59267/")], out _));
    }

    [TestMethod]
    public void GuidPathUrlAttribute_NoMatch()
    {
        // Arrange
        var resolver = new BrowserLinkOutgoingPeerResolver();

        // Act & Assert
        Assert.IsFalse(TryResolvePeerName(resolver, [KeyValuePair.Create("http.url", "http://localhost:59267/not-a-guid/getScriptTag")], out _));
    }

    [TestMethod]
    public void LocalHostAndPathUrlAttribute_Match()
    {
        // Arrange
        var resolver = new BrowserLinkOutgoingPeerResolver();

        // Act & Assert
        Assert.IsTrue(TryResolvePeerName(resolver, [KeyValuePair.Create("http.url", "http://localhost:59267/6eed7c2dedc14419901b813e8fe87a86/getScriptTag")], out var name));
        Assert.AreEqual("Browser Link", name);
    }

    private static bool TryResolvePeerName(IOutgoingPeerResolver resolver, KeyValuePair<string, string>[] attributes, out string? peerName)
    {
        return resolver.TryResolvePeerName(attributes, out peerName);
    }
}
