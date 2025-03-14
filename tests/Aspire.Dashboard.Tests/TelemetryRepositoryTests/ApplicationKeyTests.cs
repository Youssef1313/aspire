// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Storage;

namespace Aspire.Dashboard.Tests.TelemetryRepositoryTests;

[TestClass]
public class ApplicationKeyTests
{
    [TestMethod]
    [DataRow("name", "instanceid", "name-instanceid")]
    [DataRow("name", "instanceid", "NAME-INSTANCEID")]
    [DataRow("name", "752e1688-ca3c-45da-b48b-b2163296ac91", "name-752e1688-ca3c-45da-b48b-b2163296ac91")]
    public void EqualsCompositeName_Success(string name, string instanceId, string compositeName)
    {
        // Arrange
        var key = new ApplicationKey(name, instanceId);

        // Act
        var result = key.EqualsCompositeName(compositeName);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    [DataRow("name", "instanceid", null)]
    [DataRow("name", "instanceid", "")]
    [DataRow("name", "instanceid", "name")]
    [DataRow("name", "instanceid", "instanceid")]
    [DataRow("name", "instanceid", "name_instanceid")]
    [DataRow("name", "instanceid", "instanceid-name")]
    public void EqualsCompositeName_Failure(string name, string instanceId, string? compositeName)
    {
        // Arrange
        var key = new ApplicationKey(name, instanceId);

        // Act
        var result = key.EqualsCompositeName(compositeName!);

        // Assert
        Assert.IsFalse(result);
    }
}
