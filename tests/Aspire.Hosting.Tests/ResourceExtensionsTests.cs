// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.InternalTesting;

namespace Aspire.Hosting.Tests;

[TestClass]
public class ResourceExtensionsTests
{
    [TestMethod]
    public void TryGetAnnotationsOfTypeReturnsFalseWhenNoAnnotations()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var parent = builder.AddResource(new ParentResource("parent"));

        Assert.IsFalse(parent.Resource.HasAnnotationOfType<DummyAnnotation>());
        Assert.IsFalse(parent.Resource.TryGetAnnotationsOfType<DummyAnnotation>(out var annotations));
        Assert.IsNull(annotations);
    }

    [TestMethod]
    public void TryGetAnnotationsOfTypeReturnsFalseWhenOnlyAnnotationsOfOtherTypes()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var parent = builder.AddResource(new ParentResource("parent"))
                            .WithAnnotation(new AnotherDummyAnnotation());

        Assert.IsFalse(parent.Resource.HasAnnotationOfType<DummyAnnotation>());
        Assert.IsFalse(parent.Resource.TryGetAnnotationsOfType<DummyAnnotation>(out var annotations));
        Assert.IsNull(annotations);
    }

    [TestMethod]
    public void TryGetAnnotationsOfTypeReturnsTrueWhenNoAnnotations()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var parent = builder.AddResource(new ParentResource("parent"))
                            .WithAnnotation(new DummyAnnotation());

        Assert.IsTrue(parent.Resource.HasAnnotationOfType<DummyAnnotation>());
        Assert.IsTrue(parent.Resource.TryGetAnnotationsOfType<DummyAnnotation>(out var annotations));
        Assert.ContainsSingle(annotations);
    }

    [TestMethod]
    public void TryGetAnnotationsIncludingAncestorsOfTypeReturnsAnnotationFromParentDirectly()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var parent = builder.AddResource(new ParentResource("parent"))
                            .WithAnnotation(new DummyAnnotation());

        Assert.IsTrue(parent.Resource.HasAnnotationIncludingAncestorsOfType<DummyAnnotation>());
        Assert.IsTrue(parent.Resource.TryGetAnnotationsIncludingAncestorsOfType<DummyAnnotation>(out var annotations));
        Assert.ContainsSingle(annotations);
    }

    [TestMethod]
    public void TryGetAnnotationIncludingAncestorsOfTypeReturnsFalseWhenNoAnnotations()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var parent = builder.AddResource(new ParentResource("parent"));

        Assert.IsFalse(parent.Resource.HasAnnotationIncludingAncestorsOfType<DummyAnnotation>());
        Assert.IsFalse(parent.Resource.TryGetAnnotationsIncludingAncestorsOfType<DummyAnnotation>(out var annotations));
        Assert.IsNull(annotations);
    }

    [TestMethod]
    public void TryGetAnnotationIncludingAncestorsOfTypeReturnsFalseWhenOnlyAnnotationsOfOtherTypes()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var parent = builder.AddResource(new ParentResource("parent"))
                            .WithAnnotation(new AnotherDummyAnnotation());

        Assert.IsFalse(parent.Resource.HasAnnotationIncludingAncestorsOfType<DummyAnnotation>());
        Assert.IsFalse(parent.Resource.TryGetAnnotationsIncludingAncestorsOfType<DummyAnnotation>(out var annotations));
        Assert.IsNull(annotations);
    }

    [TestMethod]
    public void TryGetAnnotationIncludingAncestorsOfTypeReturnsFalseWhenOnlyAnnotationsOfOtherTypesIncludingParent()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var parent = builder.AddResource(new ParentResource("parent"))
                            .WithAnnotation(new AnotherDummyAnnotation());

        var child = builder.AddResource(new ChildResource("child", parent.Resource))
                           .WithAnnotation(new AnotherDummyAnnotation());

        Assert.IsFalse(parent.Resource.HasAnnotationIncludingAncestorsOfType<DummyAnnotation>());
        Assert.IsFalse(child.Resource.TryGetAnnotationsIncludingAncestorsOfType<DummyAnnotation>(out var annotations));
        Assert.IsNull(annotations);
    }

    [TestMethod]
    public void TryGetAnnotationsIncludingAncestorsOfTypeReturnsAnnotationFromParent()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var parent = builder.AddResource(new ParentResource("parent"))
                            .WithAnnotation(new DummyAnnotation());

        var child = builder.AddResource(new ChildResource("child", parent.Resource));

        Assert.IsTrue(parent.Resource.HasAnnotationIncludingAncestorsOfType<DummyAnnotation>());
        Assert.IsTrue(child.Resource.TryGetAnnotationsIncludingAncestorsOfType<DummyAnnotation>(out var annotations));
        Assert.ContainsSingle(annotations);
    }

    [TestMethod]
    public void TryGetAnnotationsIncludingAncestorsOfTypeCombinesAnnotationsFromParentAndChild()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var parent = builder.AddResource(new ParentResource("parent"))
                            .WithAnnotation(new DummyAnnotation());

        var child = builder.AddResource(new ChildResource("child", parent.Resource))
                           .WithAnnotation(new DummyAnnotation());

        Assert.IsTrue(parent.Resource.HasAnnotationIncludingAncestorsOfType<DummyAnnotation>());
        Assert.IsTrue(child.Resource.TryGetAnnotationsIncludingAncestorsOfType<DummyAnnotation>(out var annotations));
        Assert.AreEqual(2, annotations.Count());
    }

    [TestMethod]
    public void TryGetAnnotationsIncludingAncestorsOfTypeCombinesAnnotationsFromParentAndChildAndGrandchild()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var parent = builder.AddResource(new ParentResource("parent"))
                            .WithAnnotation(new DummyAnnotation());

        var child = builder.AddResource(new ChildResource("child", parent: parent.Resource))
                           .WithAnnotation(new DummyAnnotation());

        var grandchild = builder.AddResource(new ChildResource("grandchild", parent: child.Resource))
                                .WithAnnotation(new DummyAnnotation());

        Assert.IsTrue(parent.Resource.HasAnnotationIncludingAncestorsOfType<DummyAnnotation>());
        Assert.IsTrue(grandchild.Resource.TryGetAnnotationsIncludingAncestorsOfType<DummyAnnotation>(out var annotations));
        Assert.AreEqual(3, annotations.Count());
    }

    [TestMethod]
    public void TryGetContainerImageNameReturnsCorrectFormatWhenShaSupplied()
    {
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("grafana", "grafana/grafana", "latest").WithImageSHA256("1adbcc2df3866ff5ec1d836e9d2220c904c7f98901b918d3cc5e1118ab1af991");

        Assert.IsTrue(container.Resource.TryGetContainerImageName(out var imageName));
        Assert.AreEqual("grafana/grafana@sha256:1adbcc2df3866ff5ec1d836e9d2220c904c7f98901b918d3cc5e1118ab1af991", imageName);
    }

    [TestMethod]
    public void TryGetContainerImageNameReturnsCorrectFormatWhenShaNotSupplied()
    {
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("grafana", "grafana/grafana", "10.3.1");

        Assert.IsTrue(container.Resource.TryGetContainerImageName(out var imageName));
        Assert.AreEqual("grafana/grafana:10.3.1", imageName);
    }

    [TestMethod]
    public async Task GetEnvironmentVariableValuesAsyncReturnCorrectVariablesInRunMode()
    {
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("elasticsearch", "library/elasticsearch", "8.14.0")
         .WithEnvironment("discovery.type", "single-node")
         .WithEnvironment("xpack.security.enabled", "true")
         .WithEnvironment(context =>
         {
             context.EnvironmentVariables["ELASTIC_PASSWORD"] = "p@ssw0rd1";
         });

        var env = await container.Resource.GetEnvironmentVariableValuesAsync().DefaultTimeout();

        Assert.That.Collection(env,
            env =>
            {
                Assert.AreEqual("discovery.type", env.Key);
                Assert.AreEqual("single-node", env.Value);
            },
            env =>
            {
                Assert.AreEqual("xpack.security.enabled", env.Key);
                Assert.AreEqual("true", env.Value);
            },
            env =>
            {
                Assert.AreEqual("ELASTIC_PASSWORD", env.Key);
                Assert.AreEqual("p@ssw0rd1", env.Value);
            });
    }

    [TestMethod]
    public async Task GetEnvironmentVariableValuesAsyncReturnCorrectVariablesUsingValueProviderInRunMode()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.Configuration["Parameters:ElasticPassword"] = "p@ssw0rd1";

        var passwordParameter = builder.AddParameter("ElasticPassword");

        var container = builder.AddContainer("elasticsearch", "library/elasticsearch", "8.14.0")
         .WithEnvironment("discovery.type", "single-node")
         .WithEnvironment("xpack.security.enabled", "true")
         .WithEnvironment("ELASTIC_PASSWORD", passwordParameter);

        var env = await container.Resource.GetEnvironmentVariableValuesAsync().DefaultTimeout();

        Assert.That.Collection(env,
            env =>
            {
                Assert.AreEqual("discovery.type", env.Key);
                Assert.AreEqual("single-node", env.Value);
            },
            env =>
            {
                Assert.AreEqual("xpack.security.enabled", env.Key);
                Assert.AreEqual("true", env.Value);
            },
            env =>
            {
                Assert.AreEqual("ELASTIC_PASSWORD", env.Key);
                Assert.AreEqual("p@ssw0rd1", env.Value);
            });
    }

    [TestMethod]
    public async Task GetEnvironmentVariableValuesAsyncReturnCorrectVariablesUsingManifestExpressionProviderInPublishMode()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.Configuration["Parameters:ElasticPassword"] = "p@ssw0rd1";

        var passwordParameter = builder.AddParameter("ElasticPassword");

        var container = builder.AddContainer("elasticsearch", "library/elasticsearch", "8.14.0")
         .WithEnvironment("discovery.type", "single-node")
         .WithEnvironment("xpack.security.enabled", "true")
         .WithEnvironment("ELASTIC_PASSWORD", passwordParameter);

        var env = await container.Resource.GetEnvironmentVariableValuesAsync(DistributedApplicationOperation.Publish).DefaultTimeout();

        Assert.That.Collection(env,
            env =>
            {
                Assert.AreEqual("discovery.type", env.Key);
                Assert.AreEqual("single-node", env.Value);
            },
            env =>
            {
                Assert.AreEqual("xpack.security.enabled", env.Key);
                Assert.AreEqual("true", env.Value);
            },
            env =>
            {
                Assert.AreEqual("{ElasticPassword.value}", env.Value);
                Assert.IsFalse(string.IsNullOrEmpty(env.Value));
            });
    }

    [TestMethod]
    public async Task GetArgumentValuesAsync_ReturnsCorrectValuesForSpecialCases()
    {
        var builder = DistributedApplication.CreateBuilder();
        var surrogate = builder.AddResource(new ConnectionStringParameterResource("ResourceWithConnectionStringSurrogate", _ => "ConnectionString", null));
        var secretParameter = builder.AddResource(new ParameterResource("SecretParameter", _ => "SecretParameter", true));
        var nonSecretParameter = builder.AddResource(new ParameterResource("NonSecretParameter", _ => "NonSecretParameter"));

        var containerArgs = await builder.AddContainer("elasticsearch", "library/elasticsearch", "8.14.0")
            .WithArgs(surrogate)
            .WithArgs(secretParameter)
            .WithArgs(nonSecretParameter)
            .Resource.GetArgumentValuesAsync().DefaultTimeout();

        Assert.Equal<IEnumerable<string>>(["ConnectionString", "SecretParameter", "NonSecretParameter"], containerArgs);

        // Executables can also have arguments passed in AddExecutable
        var executableArgs = await builder.AddExecutable(
                "ping",
                "ping",
                string.Empty,
                surrogate,
                secretParameter,
                nonSecretParameter)
            .Resource.GetArgumentValuesAsync().DefaultTimeout();

        Assert.Equal<IEnumerable<string>>(["ConnectionString", "SecretParameter", "NonSecretParameter"], executableArgs);
    }

    private sealed class ParentResource(string name) : Resource(name)
    {

    }

    private sealed class ChildResource(string name, Resource parent) : Resource(name), IResourceWithParent<Resource>
    {
        public Resource Parent => parent;
    }

    private sealed class DummyAnnotation : IResourceAnnotation
    {

    }

    private sealed class AnotherDummyAnnotation : IResourceAnnotation
    {

    }
}
