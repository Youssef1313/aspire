// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Aspire.Dashboard.Extensions;

namespace Aspire.Dashboard.Tests;

[TestClass]
public class AssemblyExtensionsTests
{
    [TestMethod]
    [DataRow("8.0.0-preview.1", "8.0.0-preview.1")]
    [DataRow("8.0.0-preview.1+asdlkjfdijee", "8.0.0-preview.1")]
    [DataRow("8.0.0-preview.1+asdlkjfdijee+someothersuffix", "8.0.0-preview.1")]
    [DataRow("8.0.0", "8.0.0")]
    [DataRow("Plain old text", "Plain old text")]
    [DataRow("", "")]
    [DataRow(null, null)]
    public void GetDisplayVersionUsesInformationalVersionWhenPresent(string? attributeValue, string? expectedDisplayVersion)
    {
        var assembly = new TestingAssembly();
        if (attributeValue is not null)
        {
            assembly.AddCustomAttribute(new AssemblyInformationalVersionAttribute(attributeValue));
            assembly.AddCustomAttribute(new AssemblyFileVersionAttribute("This should never be used"));
            assembly.AddCustomAttribute(new AssemblyVersionAttribute("1.1.1.1"));
        }

        var actualDisplayVersion = assembly.GetDisplayVersion();

        Assert.AreEqual(expectedDisplayVersion, actualDisplayVersion);
    }

    [TestMethod]
    [DataRow("8.0.0.1", "8.0.0.1")]
    [DataRow("8.0.0.1+asdlkjfdijee", "8.0.0.1+asdlkjfdijee")]
    [DataRow("Plain old text", "Plain old text")]
    [DataRow("", "")]
    [DataRow(null, null)]
    public void GetDisplayVersionUsesFileVersionWhenPresentAndInformationalVersionIsMissing(string? attributeValue, string? expectedDisplayVersion)
    {
        var assembly = new TestingAssembly();
        if (attributeValue is not null)
        {
            assembly.AddCustomAttribute(new AssemblyFileVersionAttribute(attributeValue));
            assembly.AddCustomAttribute(new AssemblyVersionAttribute("1.1.1.1"));
        }

        var actualDisplayVersion = assembly.GetDisplayVersion();

        Assert.AreEqual(expectedDisplayVersion, actualDisplayVersion);
    }

    [TestMethod]
    [DataRow("8.0.0.1", "8.0.0.1")]
    [DataRow("8.0.0.1+asdlkjfdijee", "8.0.0.1+asdlkjfdijee")]
    [DataRow("Plain old text", "Plain old text")]
    [DataRow("", "")]
    [DataRow(null, null)]
    public void GetDisplayVersionUsesAssemblyVersionWhenPresentAndInformationalVersionAndFileVersionAreaMissing(string? attributeValue, string? expectedDisplayVersion)
    {
        var assembly = new TestingAssembly();
        if (attributeValue is not null)
        {
            assembly.AddCustomAttribute(new AssemblyVersionAttribute(attributeValue));
        }

        var actualDisplayVersion = assembly.GetDisplayVersion();

        Assert.AreEqual(expectedDisplayVersion, actualDisplayVersion);
    }
}

internal sealed class TestingAssembly : Assembly
{
    private readonly List<Attribute> _customAttributes = [];

    public void AddCustomAttribute(Attribute attribute)
    {
        _customAttributes.Add(attribute);
    }

    public override object[] GetCustomAttributes(bool inherit)
    {
        return base.GetCustomAttributes(inherit);
    }

    public override object[] GetCustomAttributes(Type attributeType, bool inherit)
    {
        return _customAttributes.Where(a => a.GetType() == attributeType).ToArray();
    }
}
