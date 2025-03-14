// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading.Channels;
using Aspire.Dashboard.Components.Resize;
using Aspire.Dashboard.Components.Tests.Shared;
using Aspire.Dashboard.Model;
using Bunit;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Tests.Pages;

[UseCulture("en-US")]
[TestClass]
public partial class ResourcesTests : Bunit.TestContext
{
    [TestMethod]
    public void UpdateResources_FiltersUpdated()
    {
        // Arrange
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var initialResources = new List<ResourceViewModel>
        {
            CreateResource(
                "Resource1",
                "Type1",
                "Running",
                ImmutableArray.Create(new HealthReportViewModel("Null", null, "Description1", null))),
        };
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>();
        var dashboardClient = new TestDashboardClient(isEnabled: true, initialResources: initialResources, resourceChannelProvider: () => channel);
        ResourceSetupHelpers.SetupResourcesPage(
            this,
            viewport,
            dashboardClient);

        var cut = RenderComponent<Components.Pages.Resources>(builder =>
        {
            builder.AddCascadingValue(viewport);
        });

        // Assert 1
        Assert.That.Collection(cut.Instance.ResourceTypesToVisibility.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.AreEqual("Type1", kvp.Key);
                Assert.IsTrue(kvp.Value);
            });
        Assert.That.Collection(cut.Instance.ResourceStatesToVisibility.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.AreEqual("Running", kvp.Key);
                Assert.IsTrue(kvp.Value);
            });
        Assert.That.Collection(cut.Instance.ResourceHealthStatusesToVisibility.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.AreEqual("Unhealthy", kvp.Key);
                Assert.IsTrue(kvp.Value);
            });

        // Act
        channel.Writer.TryWrite([
            new ResourceViewModelChange(
                ResourceViewModelChangeType.Upsert,
                CreateResource(
                    "Resource2",
                    "Type2",
                    "Running",
                    ImmutableArray.Create(new HealthReportViewModel("Healthy", HealthStatus.Healthy, "Description2", null))))
            ]);

        cut.WaitForState(() => cut.Instance.GetFilteredResources().Count() == 2, TestConstants.WaitTimeout);

        // Assert 2
        Assert.That.Collection(cut.Instance.ResourceTypesToVisibility.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.AreEqual("Type1", kvp.Key);
                Assert.IsTrue(kvp.Value);
            },
            kvp =>
            {
                Assert.AreEqual("Type2", kvp.Key);
                Assert.IsTrue(kvp.Value);
            });
        Assert.That.Collection(cut.Instance.ResourceStatesToVisibility.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.AreEqual("Running", kvp.Key);
                Assert.IsTrue(kvp.Value);
            });
        Assert.That.Collection(cut.Instance.ResourceHealthStatusesToVisibility.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.AreEqual("Healthy", kvp.Key);
                Assert.IsTrue(kvp.Value);
            },
            kvp =>
            {
                Assert.AreEqual("Unhealthy", kvp.Key);
                Assert.IsTrue(kvp.Value);
            });
    }

    [TestMethod]
    public void FilterResources()
    {
        // Arrange
        var viewport = new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false);
        var initialResources = new List<ResourceViewModel>
        {
            CreateResource(
                "Resource1",
                "Type1",
                "Running",
                ImmutableArray.Create(new HealthReportViewModel("Null", null, "Description1", null))),
            CreateResource(
                "Resource2",
                "Type2",
                "Running",
                ImmutableArray.Create(new HealthReportViewModel("Healthy", HealthStatus.Healthy, "Description2", null))),
            CreateResource(
                "Resource3",
                "Type3",
                "Stopping",
                ImmutableArray.Create(new HealthReportViewModel("Degraded", HealthStatus.Degraded, "Description3", null))),
        };
        var dashboardClient = new TestDashboardClient(isEnabled: true, initialResources: initialResources, resourceChannelProvider: Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>);
        ResourceSetupHelpers.SetupResourcesPage(
            this,
            viewport,
            dashboardClient);

        var cut = RenderComponent<Components.Pages.Resources>(builder =>
        {
            builder.AddCascadingValue(viewport);
        });

        // Open the resource filter
        cut.Find("#resourceFilterButton").Click();

        // Assert 1 (the correct filter options are shown)
        AssertResourceFilterListEquals(cut, [
            new("Type1", true),
            new("Type2", true),
            new("Type3", true),
        ], [
            new("Running", true),
            new("Stopping", true),
        ], [
            new("", true),
            new("Healthy", true),
            new("Unhealthy", true),
        ]);

        // Assert 2 (unselect a resource type, assert that a resource was removed)
        cut.FindComponents<SelectResourceOptions<string>>().First(f => f.Instance.Id == "resource-states")
            .FindComponents<FluentCheckbox>()
            .First(checkbox => checkbox.Instance.Label == "Stopping")
            .Find("fluent-checkbox")
            .TriggerEvent("oncheckedchange", new CheckboxChangeEventArgs { Checked = false });

        // above is triggered asynchronously, so wait for the state to change
        cut.WaitForState(() => cut.Instance.GetFilteredResources().Count() == 2);
    }

    private static void AssertResourceFilterListEquals(IRenderedComponent<Components.Pages.Resources> cut, IEnumerable<KeyValuePair<string, bool>> types, IEnumerable<KeyValuePair<string, bool>> states, IEnumerable<KeyValuePair<string, bool>> healthStates)
    {
        IReadOnlyList<IRenderedComponent<SelectResourceOptions<string>>> filterComponents = null!;

        cut.WaitForState(() =>
        {
            filterComponents = cut.FindComponents<SelectResourceOptions<string>>();
            return filterComponents.Count == 3;
        });

        var typeSelect = filterComponents.First(f => f.Instance.Id == "resource-types");
        Assert.AreEqual(types, typeSelect.Instance.Values.ToImmutableSortedDictionary() /* sort for equality comparison */ );

        var stateSelect = filterComponents.First(f => f.Instance.Id == "resource-states");
        Assert.AreEqual(states, stateSelect.Instance.Values.ToImmutableSortedDictionary() /* sort for equality comparison */);

        var healthSelect = filterComponents.First(f => f.Instance.Id == "resource-health-states");
        Assert.AreEqual(healthStates, healthSelect.Instance.Values.ToImmutableSortedDictionary() /* sort for equality comparison */);
    }

    private static ResourceViewModel CreateResource(string name, string type, string? state, ImmutableArray<HealthReportViewModel>? healthReports)
    {
        return new ResourceViewModel
        {
            Name = name,
            ResourceType = type,
            State = state,
            KnownState = state is not null ? Enum.Parse<KnownResourceState>(state) : null,
            DisplayName = name,
            Uid = name,
            HealthReports = healthReports ?? [],

            // unused properties
            StateStyle = null,
            CreationTimeStamp = null,
            StartTimeStamp = null,
            StopTimeStamp = null,
            Environment = default,
            Urls = [],
            Volumes = default,
            Relationships = default,
            Properties = ImmutableDictionary<string, ResourcePropertyViewModel>.Empty,
            Commands = [],
        };
    }
}
