// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Containers.Tests;

[TestClass]
public class ContainerImageAnnotationTests
{
    [TestMethod]
    public void SettingTagNullsSha()
    {
        var annotation = new ContainerImageAnnotation()
        {
            Image = "grafana/grafana",
            SHA256 = "pretendthisisasha"
        };

        Assert.IsNull(annotation.Tag);
        annotation.Tag = "latest";
        Assert.AreEqual("latest", annotation.Tag);
        Assert.IsNull(annotation.SHA256);
    }

    [TestMethod]
    public void SettingShaNullsTag()
    {
        var annotation = new ContainerImageAnnotation()
        {
            Image = "grafana/grafana",
            Tag = "latest"
        };

        Assert.IsNull(annotation.SHA256);
        annotation.SHA256 = "pretendthisisasha";
        Assert.AreEqual("pretendthisisasha", annotation.SHA256);
        Assert.IsNull(annotation.Tag);
    }

}
