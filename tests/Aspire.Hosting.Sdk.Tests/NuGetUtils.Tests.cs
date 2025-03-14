// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NuGet.RuntimeModel;

namespace Aspire.Hosting.Sdk.Tests;

[TestClass]
public class NuGetUtilsTests
{
    [TestMethod]
    // Matching RID cases
    [DataRow("win-x64", "win-x64")]
    [DataRow("win-x86", "win-x86")]
    [DataRow("win-arm64", "win-arm64")]
    [DataRow("linux-x64", "linux-x64")]
    [DataRow("linux-arm64", "linux-arm64")]
    [DataRow("osx-x64", "osx-x64")]
    [DataRow("osx-arm64", "osx-arm64")]

    //Compatible RID cases
    [DataRow("rhel.8-x64", "linux-x64")] // https://github.com/dotnet/aspire/issues/5486
    [DataRow("ubuntu.23.04-x64", "linux-x64")]
    [DataRow("fedora.39-x64", "linux-x64")]
    [DataRow("linux-musl-x64", "linux-x64")]
    public void RightRIDIsSelected(string inputRID, string expectedRID)
    {
        RuntimeGraph graph = JsonRuntimeFormat.ReadRuntimeGraph("RuntimeIdentifierGraph.json");

        var result = NuGetUtils.GetBestMatchingRid(graph, inputRID, new[] { "win-x64", "win-arm64", "win-x86",
            "linux-x64", "linux-arm64",
            "osx-x64", "osx-arm64"}, out bool wasInGraph);

        Assert.AreEqual(expectedRID, result);
        Assert.IsTrue(wasInGraph);
    }
}
