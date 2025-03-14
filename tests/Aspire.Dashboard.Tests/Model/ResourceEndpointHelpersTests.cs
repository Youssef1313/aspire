// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Tests.Shared.DashboardModel;

namespace Aspire.Dashboard.Tests.Model;

[TestClass]
public sealed class ResourceEndpointHelpersTests
{
    public static List<DisplayedEndpoint> GetEndpoints(ResourceViewModel resource, bool includeInternalUrls = false)
    {
        return ResourceEndpointHelpers.GetEndpoints(resource, includeInternalUrls);
    }

    [TestMethod]
    public void GetEndpoints_Empty_NoResults()
    {
        var endpoints = GetEndpoints(ModelTestHelpers.CreateResource(urls: []));

        Assert.IsEmpty(endpoints);
    }

    [TestMethod]
    public void GetEndpoints_HasServices_Results()
    {
        var endpoints = GetEndpoints(ModelTestHelpers.CreateResource(urls: [new("Test", new("http://localhost:8080"), isInternal: false, isInactive: false)]));

        Assert.That.Collection(endpoints,
            e =>
            {
                Assert.AreEqual("http://localhost:8080", e.Text);
                Assert.AreEqual("Test", e.Name);
                Assert.AreEqual("http://localhost:8080", e.Url);
                Assert.AreEqual("localhost", e.Address);
                Assert.AreEqual(8080, e.Port);
            });
    }

    [TestMethod]
    public void GetEndpoints_HasEndpointAndService_Results()
    {
        var endpoints = GetEndpoints(ModelTestHelpers.CreateResource(urls: [
            new("Test", new("http://localhost:8080"), isInternal: false, isInactive: false),
            new("Test2", new("http://localhost:8081"), isInternal: false, isInactive: false)])
        );

        Assert.That.Collection(endpoints,
            e =>
            {
                Assert.AreEqual("http://localhost:8080", e.Text);
                Assert.AreEqual("Test", e.Name);
                Assert.AreEqual("http://localhost:8080", e.Url);
                Assert.AreEqual("localhost", e.Address);
                Assert.AreEqual(8080, e.Port);
            },
            e =>
            {
                Assert.AreEqual("http://localhost:8081", e.Text);
                Assert.AreEqual("Test2", e.Name);
                Assert.AreEqual("http://localhost:8081", e.Url);
                Assert.AreEqual("localhost", e.Address);
                Assert.AreEqual(8081, e.Port);
            });
    }

    [TestMethod]
    public void GetEndpoints_OnlyHttpAndHttpsEndpointsSetTheUrl()
    {
        var endpoints = GetEndpoints(ModelTestHelpers.CreateResource(urls: [
            new("Test", new("http://localhost:8080"), isInternal: false, isInactive: false),
            new("Test2", new("tcp://localhost:8081"), isInternal: false, isInactive: false)])
        );

        Assert.That.Collection(endpoints,
            e =>
            {
                Assert.AreEqual("http://localhost:8080", e.Text);
                Assert.AreEqual("Test", e.Name);
                Assert.AreEqual("http://localhost:8080", e.Url);
                Assert.AreEqual("localhost", e.Address);
                Assert.AreEqual(8080, e.Port);
            },
            e =>
            {
                Assert.AreEqual("tcp://localhost:8081", e.Text);
                Assert.AreEqual("Test2", e.Name);
                Assert.IsNull(e.Url);
                Assert.AreEqual("localhost", e.Address);
                Assert.AreEqual(8081, e.Port);
            });
    }

    [TestMethod]
    public void GetEndpoints_IncludeEndpointUrl_HasEndpointAndService_Results()
    {
        var endpoints = GetEndpoints(ModelTestHelpers.CreateResource(urls: [
            new("First", new("https://localhost:8080/test"), isInternal: false, isInactive: false),
            new("Test", new("https://localhost:8081/test2"), isInternal: false, isInactive: false)
        ]));

        Assert.That.Collection(endpoints,
            e =>
            {
                Assert.AreEqual("https://localhost:8080/test", e.Text);
                Assert.AreEqual("First", e.Name);
                Assert.AreEqual("https://localhost:8080/test", e.Url);
                Assert.AreEqual("localhost", e.Address);
                Assert.AreEqual(8080, e.Port);
            },
            e =>
            {
                Assert.AreEqual("https://localhost:8081/test2", e.Text);
                Assert.AreEqual("Test", e.Name);
                Assert.AreEqual("https://localhost:8081/test2", e.Url);
                Assert.AreEqual("localhost", e.Address);
                Assert.AreEqual(8081, e.Port);
            });
    }

    [TestMethod]
    public void GetEndpoints_ExcludesInternalUrls()
    {
        var endpoints = GetEndpoints(ModelTestHelpers.CreateResource(urls: [
            new("First", new("https://localhost:8080/test"), isInternal: true, isInactive : false),
            new("Test", new("https://localhost:8081/test2"), isInternal: false, isInactive: false)
        ]));

        Assert.That.Collection(endpoints,
            e =>
            {
                Assert.AreEqual("https://localhost:8081/test2", e.Text);
                Assert.AreEqual("Test", e.Name);
                Assert.AreEqual("https://localhost:8081/test2", e.Url);
                Assert.AreEqual("localhost", e.Address);
                Assert.AreEqual(8081, e.Port);
            });
    }

    [TestMethod]
    public void GetEndpoints_ExcludesInactiveUrls()
    {
        var endpoints = GetEndpoints(ModelTestHelpers.CreateResource(urls: [
            new("First", new("https://localhost:8080/test"), isInternal: false, isInactive : true),
            new("Test", new("https://localhost:8081/test2"), isInternal: false, isInactive: false)
        ]));

        Assert.That.Collection(endpoints,
            e =>
            {
                Assert.AreEqual("https://localhost:8081/test2", e.Text);
                Assert.AreEqual("Test", e.Name);
                Assert.AreEqual("https://localhost:8081/test2", e.Url);
                Assert.AreEqual("localhost", e.Address);
                Assert.AreEqual(8081, e.Port);
            });
    }

    [TestMethod]
    public void GetEndpoints_IncludesIncludeInternalUrls()
    {
        var endpoints = GetEndpoints(ModelTestHelpers.CreateResource(urls: [
            new("First", new("https://localhost:8080/test"), isInternal: true, isInactive: false),
            new("Test", new("https://localhost:8081/test2"), isInternal: false, isInactive: false)
        ]),
        includeInternalUrls: true);

        Assert.That.Collection(endpoints,
            e =>
            {
                Assert.AreEqual("https://localhost:8080/test", e.Text);
                Assert.AreEqual("First", e.Name);
                Assert.AreEqual("https://localhost:8080/test", e.Url);
                Assert.AreEqual("localhost", e.Address);
                Assert.AreEqual(8080, e.Port);
            },
            e =>
            {
                Assert.AreEqual("https://localhost:8081/test2", e.Text);
                Assert.AreEqual("Test", e.Name);
                Assert.AreEqual("https://localhost:8081/test2", e.Url);
                Assert.AreEqual("localhost", e.Address);
                Assert.AreEqual(8081, e.Port);
            });
    }

    [TestMethod]
    public void GetEndpoints_OrderByName()
    {
        var endpoints = GetEndpoints(ModelTestHelpers.CreateResource(urls: [
            new("a", new("http://localhost:8080"), isInternal: false, isInactive: false),
            new("C", new("http://localhost:8080"), isInternal: false, isInactive: false),
            new("D", new("tcp://localhost:8080"), isInternal: false, isInactive: false),
            new("B", new("tcp://localhost:8080"), isInternal: false, isInactive: false),
            new("Z", new("https://localhost:8080"), isInternal: false, isInactive: false)
        ]));

        Assert.That.Collection(endpoints,
            e => Assert.AreEqual("Z", e.Name),
            e => Assert.AreEqual("a", e.Name),
            e => Assert.AreEqual("C", e.Name),
            e => Assert.AreEqual("B", e.Name),
            e => Assert.AreEqual("D", e.Name));
    }
}
