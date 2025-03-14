// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Dashboard.Model;
using Aspire.ResourceService.Proto.V1;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging.Abstractions;
using DiagnosticsHealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Aspire.Dashboard.Tests.Model;

[TestClass]
public sealed class ResourceViewModelTests
{
    private static readonly DateTime s_dateTime = new(2000, 12, 30, 23, 59, 59, DateTimeKind.Utc);
    private static readonly BrowserTimeProvider s_timeProvider = new(NullLoggerFactory.Instance);

    [TestMethod]
    [DataRow(KnownResourceState.Starting, null, null)]
    [DataRow(KnownResourceState.Starting, null, new string[]{})]
    [DataRow(KnownResourceState.Starting, null, new string?[]{null})]
    // we don't have a Running + HealthReports null case because that's not a valid state - by this point, we will have received the list of HealthReports
    [DataRow(KnownResourceState.Running, DiagnosticsHealthStatus.Healthy, new string[]{})]
    [DataRow(KnownResourceState.Running, DiagnosticsHealthStatus.Healthy, new string?[] {"Healthy"})]
    [DataRow(KnownResourceState.Running, DiagnosticsHealthStatus.Unhealthy, new string?[] {null})]
    [DataRow(KnownResourceState.Running, DiagnosticsHealthStatus.Degraded, new string?[] {"Healthy", "Degraded"})]
    public void Resource_WithHealthReportAndState_ReturnsCorrectHealthStatus(KnownResourceState? state, DiagnosticsHealthStatus? expectedStatus, string?[]? healthStatusStrings)
    {
        var reports = healthStatusStrings?.Select<string?, HealthReportViewModel>((h, i) => new HealthReportViewModel(i.ToString(), h is null ? null : System.Enum.Parse<DiagnosticsHealthStatus>(h), null, null)).ToImmutableArray() ?? [];
        var actualStatus = ResourceViewModel.ComputeHealthStatus(reports, state);
        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void ToViewModel_EmptyEnvVarName_Success()
    {
        // Arrange
        var resource = new Resource
        {
            Name = "TestName-abc",
            DisplayName = "TestName",
            CreatedAt = Timestamp.FromDateTime(s_dateTime),
            Environment =
            {
                new EnvironmentVariable { Name = string.Empty, Value = "Value!" }
            }
        };

        // Act
        var vm = resource.ToViewModel(s_timeProvider, new MockKnownPropertyLookup());

        // Assert
        Assert.That.Collection(resource.Environment,
            e =>
            {
                Assert.IsEmpty(e.Name);
                Assert.AreEqual("Value!", e.Value);
            });
    }

    [TestMethod]
    public void ToViewModel_MissingRequiredData_FailWithFriendlyError()
    {
        // Arrange
        var resource = new Resource
        {
            Name = "TestName-abc"
        };

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => resource.ToViewModel(s_timeProvider, new MockKnownPropertyLookup()));

        // Assert
        Assert.AreEqual(@"Error converting resource ""TestName-abc"" to ResourceViewModel.", ex.Message);
        Assert.IsNotNull(ex.InnerException);
    }

    [TestMethod]
    public void ToViewModel_CopiesProperties()
    {
        // Arrange
        var resource = new Resource
        {
            Name = "TestName-abc",
            DisplayName = "TestName",
            CreatedAt = Timestamp.FromDateTime(s_dateTime),
            Properties =
            {
                new ResourceProperty { Name = "Property1", Value = Value.ForString("Value1"), IsSensitive = false },
                new ResourceProperty { Name = "Property2", Value = Value.ForString("Value2"), IsSensitive = true }
            }
        };

        var kp = new KnownProperty("foo", "bar");

        // Act
        var viewModel = resource.ToViewModel(s_timeProvider, new MockKnownPropertyLookup(123, kp));

        // Assert
        Assert.That.Collection(
            viewModel.Properties.OrderBy(p => p.Key),
            p =>
            {
                Assert.AreEqual("Property1", p.Key);
                Assert.AreEqual("Property1", p.Value.Name);
                Assert.AreEqual("Value1", p.Value.Value.StringValue);
                Assert.AreEqual(123, p.Value.Priority);
                Assert.AreSame(kp, p.Value.KnownProperty);
                Assert.IsFalse(p.Value.IsValueMasked);
                Assert.IsFalse(p.Value.IsValueSensitive);
            },
            p =>
            {
                Assert.AreEqual("Property2", p.Key);
                Assert.AreEqual("Property2", p.Value.Name);
                Assert.AreEqual("Value2", p.Value.Value.StringValue);
                Assert.AreEqual(123, p.Value.Priority);
                Assert.AreSame(kp, p.Value.KnownProperty);
                Assert.IsTrue(p.Value.IsValueMasked);
                Assert.IsTrue(p.Value.IsValueSensitive);
            });
    }
}
