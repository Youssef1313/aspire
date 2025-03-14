// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.Tests;

[TestClass]
public class PortAllocatorTest
{
    [TestMethod]
    public void CanAllocatePorts()
    {
        var allocator = new PortAllocator(1000);
        var port1 = allocator.AllocatePort();
        allocator.AddUsedPort(port1);
        var port2 = allocator.AllocatePort();

        Assert.AreEqual(1000, port1);
        Assert.AreEqual(1001, port2);
    }

    [TestMethod]
    public void SkipUsedPorts()
    {
        var allocator = new PortAllocator(1000);
        allocator.AddUsedPort(1000);
        allocator.AddUsedPort(1001);
        allocator.AddUsedPort(1003);
        var port1 = allocator.AllocatePort();
        allocator.AddUsedPort(port1);
        var port2 = allocator.AllocatePort();

        Assert.AreEqual(1002, port1);
        Assert.AreEqual(1004, port2);
    }
}
