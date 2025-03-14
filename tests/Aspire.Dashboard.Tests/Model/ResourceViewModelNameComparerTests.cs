// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Tests.Shared.DashboardModel;

namespace Aspire.Dashboard.Tests.Model;

[TestClass]
public sealed class ResourceViewModelNameComparerTests
{
    [TestMethod]
    public void Compare()
    {
        // Arrange
        var resources = new[]
        {
            ModelTestHelpers.CreateResource(appName: "database-dashboard-abc", displayName: "database-dashboard"),
            ModelTestHelpers.CreateResource(appName: "database-dashboard-xyz", displayName: "database-dashboard"),
            ModelTestHelpers.CreateResource(appName: "database-xyz", displayName: "database"),
            ModelTestHelpers.CreateResource(appName: "database-abc", displayName: "database"),
        };

        // Act
        var result = resources.OrderBy(v => v, ResourceViewModelNameComparer.Instance);

        // Assert
        Assert.That.Collection(result,
            vm => Assert.AreEqual("database-abc", vm.Name),
            vm => Assert.AreEqual("database-xyz", vm.Name),
            vm => Assert.AreEqual("database-dashboard-abc", vm.Name),
            vm => Assert.AreEqual("database-dashboard-xyz", vm.Name));
    }
}
