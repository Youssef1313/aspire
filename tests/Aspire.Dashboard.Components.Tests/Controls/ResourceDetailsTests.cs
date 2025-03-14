// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Dashboard.Components.Controls;
using Aspire.Dashboard.Components.Tests.Shared;
using Aspire.Dashboard.Model;
using Aspire.Tests.Shared.DashboardModel;
using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace Aspire.Dashboard.Components.Tests.Controls;

[UseCulture("en-US")]
[TestClass]
public class ResourceDetailsTests : Bunit.TestContext
{
    [TestMethod]
    public async Task ClickMaskAllSwitch_UpdatedResource_MaskChanged()
    {
        // Arrange
        ResourceSetupHelpers.SetupResourceDetails(this);

        var resource1 = ModelTestHelpers.CreateResource(
            "app1",
            environment: new List<EnvironmentVariableViewModel>
            {
                new EnvironmentVariableViewModel("envvar1", "value!", fromSpec: true),
                new EnvironmentVariableViewModel("envvar2", "value!", fromSpec: true)
            }.ToImmutableArray());

        // Act
        var cut = RenderComponent<ResourceDetails>(builder =>
        {
            builder.Add(p => p.ShowSpecOnlyToggle, true);
            builder.Add(p => p.Resource, resource1);
        });

        // Assert
        Assert.That.Collection(cut.Instance.FilteredEnvironmentVariables,
            e =>
            {
                Assert.AreEqual("envvar1", e.Name);
                Assert.AreEqual("value!", e.Value);
                Assert.IsTrue(e.IsValueMasked);
            },
            e =>
            {
                Assert.AreEqual("envvar2", e.Name);
                Assert.AreEqual("value!", e.Value);
                Assert.IsTrue(e.IsValueMasked);
            });

        var maskAllSwitch = cut.Find(".mask-all-switch");
        await maskAllSwitch.ClickAsync(new MouseEventArgs());

        Assert.That.Collection(cut.Instance.FilteredEnvironmentVariables,
            e =>
            {
                Assert.AreEqual("envvar1", e.Name);
                Assert.IsFalse(e.IsValueMasked);
            },
            e =>
            {
                Assert.AreEqual("envvar2", e.Name);
                Assert.IsFalse(e.IsValueMasked);
            });

        var resource2 = ModelTestHelpers.CreateResource(
            "app1",
            environment: new List<EnvironmentVariableViewModel>
            {
                new EnvironmentVariableViewModel("envvar1", "value!", fromSpec: true),
                new EnvironmentVariableViewModel("envvar2", "value!", fromSpec: true),
                new EnvironmentVariableViewModel("envvar3", "value!", fromSpec: true)
            }.ToImmutableArray());

        cut.SetParametersAndRender(builder =>
        {
            builder.Add(p => p.Resource, resource2);
        });

        Assert.That.Collection(cut.Instance.FilteredEnvironmentVariables,
            e =>
            {
                Assert.AreEqual("envvar1", e.Name);
                Assert.IsFalse(e.IsValueMasked);
            },
            e =>
            {
                Assert.AreEqual("envvar2", e.Name);
                Assert.IsFalse(e.IsValueMasked);
            },
            e =>
            {
                Assert.AreEqual("envvar3", e.Name);
                Assert.IsFalse(e.IsValueMasked);
            });
    }

    [TestMethod]
    public async Task ClickMaskAllSwitch_NewResource_MaskChanged()
    {
        // Arrange
        ResourceSetupHelpers.SetupResourceDetails(this);

        var resource1 = ModelTestHelpers.CreateResource(
            "app1",
            environment: new List<EnvironmentVariableViewModel>
            {
                new EnvironmentVariableViewModel("envvar1", "value!", fromSpec: true),
                new EnvironmentVariableViewModel("envvar2", "value!", fromSpec: true)
            }.ToImmutableArray());

        // Act
        var cut = RenderComponent<ResourceDetails>(builder =>
        {
            builder.Add(p => p.ShowSpecOnlyToggle, true);
            builder.Add(p => p.Resource, resource1);
        });

        // Assert
        Assert.That.Collection(cut.Instance.FilteredEnvironmentVariables,
            e =>
            {
                Assert.AreEqual("envvar1", e.Name);
                Assert.AreEqual("value!", e.Value);
                Assert.IsTrue(e.IsValueMasked);
            },
            e =>
            {
                Assert.AreEqual("envvar2", e.Name);
                Assert.AreEqual("value!", e.Value);
                Assert.IsTrue(e.IsValueMasked);
            });

        var maskAllSwitch = cut.Find(".mask-all-switch");
        await maskAllSwitch.ClickAsync(new MouseEventArgs());

        Assert.That.Collection(cut.Instance.FilteredEnvironmentVariables,
            e =>
            {
                Assert.AreEqual("envvar1", e.Name);
                Assert.IsFalse(e.IsValueMasked);
            },
            e =>
            {
                Assert.AreEqual("envvar2", e.Name);
                Assert.IsFalse(e.IsValueMasked);
            });

        var resource2 = ModelTestHelpers.CreateResource(
            "app2",
            environment: new List<EnvironmentVariableViewModel>
            {
                new EnvironmentVariableViewModel("envvar1", "value!", fromSpec: true),
                new EnvironmentVariableViewModel("envvar2", "value!", fromSpec: true),
                new EnvironmentVariableViewModel("envvar3", "value!", fromSpec: true)
            }.ToImmutableArray());

        cut.SetParametersAndRender(builder =>
        {
            builder.Add(p => p.Resource, resource2);
        });

        Assert.That.Collection(cut.Instance.FilteredEnvironmentVariables,
            e =>
            {
                Assert.AreEqual("envvar1", e.Name);
                Assert.IsTrue(e.IsValueMasked);
            },
            e =>
            {
                Assert.AreEqual("envvar2", e.Name);
                Assert.IsTrue(e.IsValueMasked);
            },
            e =>
            {
                Assert.AreEqual("envvar3", e.Name);
                Assert.IsTrue(e.IsValueMasked);
            });
    }

    [TestMethod]
    public async Task ClickMaskEnvVarSwitch_UpdatedResource_MaskChanged()
    {
        // Arrange
        ResourceSetupHelpers.SetupResourceDetails(this);

        var resource1 = ModelTestHelpers.CreateResource(
            "app1",
            environment: new List<EnvironmentVariableViewModel>
            {
                new EnvironmentVariableViewModel("envvar1", "value!", fromSpec: true),
                new EnvironmentVariableViewModel("envvar2", "value!", fromSpec: true)
            }.ToImmutableArray());

        // Act
        var cut = RenderComponent<ResourceDetails>(builder =>
        {
            builder.Add(p => p.ShowSpecOnlyToggle, true);
            builder.Add(p => p.Resource, resource1);
        });

        // Assert
        Assert.That.Collection(cut.Instance.FilteredEnvironmentVariables,
            e =>
            {
                Assert.AreEqual("envvar1", e.Name);
                Assert.AreEqual("value!", e.Value);
                Assert.IsTrue(e.IsValueMasked);
            },
            e =>
            {
                Assert.AreEqual("envvar2", e.Name);
                Assert.AreEqual("value!", e.Value);
                Assert.IsTrue(e.IsValueMasked);
            });

        var maskValueButton = cut.Find(".env-var-properties .grid-value-mask-button");
        await maskValueButton.ClickAsync(new MouseEventArgs());

        Assert.That.Collection(cut.Instance.FilteredEnvironmentVariables,
            e =>
            {
                Assert.AreEqual("envvar1", e.Name);
                Assert.IsFalse(e.IsValueMasked);
            },
            e =>
            {
                Assert.AreEqual("envvar2", e.Name);
                Assert.IsTrue(e.IsValueMasked);
            });

        var resource2 = ModelTestHelpers.CreateResource(
            "app1",
            environment: new List<EnvironmentVariableViewModel>
            {
                new EnvironmentVariableViewModel("envvar1", "value!", fromSpec: true),
                new EnvironmentVariableViewModel("envvar2", "value!", fromSpec: true),
                new EnvironmentVariableViewModel("envvar3", "value!", fromSpec: true)
            }.ToImmutableArray());

        cut.SetParametersAndRender(builder =>
        {
            builder.Add(p => p.Resource, resource2);
        });

        Assert.That.Collection(cut.Instance.FilteredEnvironmentVariables,
            e =>
            {
                Assert.AreEqual("envvar1", e.Name);
                Assert.IsFalse(e.IsValueMasked);
            },
            e =>
            {
                Assert.AreEqual("envvar2", e.Name);
                Assert.IsTrue(e.IsValueMasked);
            },
            e =>
            {
                Assert.AreEqual("envvar3", e.Name);
                Assert.IsTrue(e.IsValueMasked);
            });
    }

    [TestMethod]
    public async Task ClickMaskEnvVarSwitch_NewResource_MaskChanged()
    {
        // Arrange
        ResourceSetupHelpers.SetupResourceDetails(this);

        var resource1 = ModelTestHelpers.CreateResource(
            "app1",
            environment: new List<EnvironmentVariableViewModel>
            {
                new EnvironmentVariableViewModel("envvar1", "value!", fromSpec: true),
                new EnvironmentVariableViewModel("envvar2", "value!", fromSpec: true)
            }.ToImmutableArray());

        // Act
        var cut = RenderComponent<ResourceDetails>(builder =>
        {
            builder.Add(p => p.ShowSpecOnlyToggle, true);
            builder.Add(p => p.Resource, resource1);
        });

        // Assert
        Assert.That.Collection(cut.Instance.FilteredEnvironmentVariables,
            e =>
            {
                Assert.AreEqual("envvar1", e.Name);
                Assert.AreEqual("value!", e.Value);
                Assert.IsTrue(e.IsValueMasked);
            },
            e =>
            {
                Assert.AreEqual("envvar2", e.Name);
                Assert.AreEqual("value!", e.Value);
                Assert.IsTrue(e.IsValueMasked);
            });

        var maskValueButton = cut.Find(".env-var-properties .grid-value-mask-button");
        await maskValueButton.ClickAsync(new MouseEventArgs());

        Assert.That.Collection(cut.Instance.FilteredEnvironmentVariables,
            e =>
            {
                Assert.AreEqual("envvar1", e.Name);
                Assert.IsFalse(e.IsValueMasked);
            },
            e =>
            {
                Assert.AreEqual("envvar2", e.Name);
                Assert.IsTrue(e.IsValueMasked);
            });

        var resource2 = ModelTestHelpers.CreateResource(
            "app2",
            environment: new List<EnvironmentVariableViewModel>
            {
                new EnvironmentVariableViewModel("envvar1", "value!", fromSpec: true),
                new EnvironmentVariableViewModel("envvar2", "value!", fromSpec: true),
                new EnvironmentVariableViewModel("envvar3", "value!", fromSpec: true)
            }.ToImmutableArray());

        cut.SetParametersAndRender(builder =>
        {
            builder.Add(p => p.Resource, resource2);
        });

        Assert.That.Collection(cut.Instance.FilteredEnvironmentVariables,
            e =>
            {
                Assert.AreEqual("envvar1", e.Name);
                Assert.IsTrue(e.IsValueMasked);
            },
            e =>
            {
                Assert.AreEqual("envvar2", e.Name);
                Assert.IsTrue(e.IsValueMasked);
            },
            e =>
            {
                Assert.AreEqual("envvar3", e.Name);
                Assert.IsTrue(e.IsValueMasked);
            });
    }
}
