// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Containers.Tests;

[TestClass]
public class ContainerMountAnnotationTests
{
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public void CtorThrowsArgumentNullExceptionIfSourceIsMissingForBindMount(string? source)
    {
#pragma warning disable CA1507 // Use nameof to express symbol names: false positive here, the parameter name being tested isn't the parameter to the test method
        Assert.Throws<ArgumentNullException>("source", () => new ContainerMountAnnotation(source, "/usr/foo", ContainerMountType.BindMount, false));
#pragma warning restore CA1507
    }

    [TestMethod]
    public void CtorThrowsArgumentExceptionIfBindMountSourceIsNotRooted()
    {
        Assert.Throws<ArgumentException>("source", () => new ContainerMountAnnotation("usr/foo", "/usr/foo", ContainerMountType.BindMount, false));
    }

    [TestMethod]
    public void CtorThrowsArgumentExceptionIfAnonymousVolumeIsReadOnly()
    {
        Assert.Throws<ArgumentException>("isReadOnly", () => new ContainerMountAnnotation(null, "/usr/foo", ContainerMountType.Volume, true));
    }
}
