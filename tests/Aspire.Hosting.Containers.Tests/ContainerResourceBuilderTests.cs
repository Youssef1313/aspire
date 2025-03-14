// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Containers.Tests;

[TestClass]
public class ContainerResourceBuilderTests
{
    [TestMethod]
    public void WithImageMutatesImageName()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddContainer("redis", "redis").WithImage("redis-stack");
        Assert.AreEqual("redis-stack", redis.Resource.Annotations.OfType<ContainerImageAnnotation>().Single().Image);
    }

    [TestMethod]
    public void WithImageMutatesImageNameAndTag()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddContainer("redis", "redis").WithImage("redis-stack", "1.0.0");
        Assert.AreEqual("redis-stack", redis.Resource.Annotations.OfType<ContainerImageAnnotation>().Single().Image);
        Assert.AreEqual("1.0.0", redis.Resource.Annotations.OfType<ContainerImageAnnotation>().Single().Tag);
    }

    [TestMethod]
    public void WithImageAddsAnnotationIfNotExistingAndMutatesImageName()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("app", "some-image");
        container.Resource.Annotations.RemoveAt(0);

        container.WithImage("new-image");
        Assert.AreEqual("new-image", container.Resource.Annotations.OfType<ContainerImageAnnotation>().Single().Image);
        Assert.AreEqual("latest", container.Resource.Annotations.OfType<ContainerImageAnnotation>().Single().Tag);
    }

    [TestMethod]
    public void WithImageMutatesImageNameOfLastAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("app", "some-image");
        container.Resource.Annotations.Add(new ContainerImageAnnotation { Image = "another-image" });

        container.WithImage("new-image");
        Assert.AreEqual("new-image", container.Resource.Annotations.OfType<ContainerImageAnnotation>().Last().Image);
        Assert.AreEqual("latest", container.Resource.Annotations.OfType<ContainerImageAnnotation>().Last().Tag);
    }

    [TestMethod]
    public void WithImageTagMutatesImageTag()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddContainer("redis", "redis").WithImageTag("7.1");
        Assert.AreEqual("7.1", redis.Resource.Annotations.OfType<ContainerImageAnnotation>().Single().Tag);
    }

    [TestMethod]
    public void WithImageRegistryMutatesImageRegistry()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddContainer("redis", "redis").WithImageRegistry("myregistry.azurecr.io");
        Assert.AreEqual("myregistry.azurecr.io", redis.Resource.Annotations.OfType<ContainerImageAnnotation>().Single().Registry);
    }

    [TestMethod]
    public void WithImageSHA256MutatesImageSHA256()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddContainer("redis", "redis").WithImageSHA256("42b5c726e719639fcc1e9dbc13dd843f567dcd37911d0e1abb9f47f2cc1c95cd");
        Assert.AreEqual("42b5c726e719639fcc1e9dbc13dd843f567dcd37911d0e1abb9f47f2cc1c95cd", redis.Resource.Annotations.OfType<ContainerImageAnnotation>().Single().SHA256);
    }

    [TestMethod]
    public void WithImageTagThrowsIfNoImageAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddResource(new TestContainerResource("testcontainer"));

        var exception = Assert.Throws<InvalidOperationException>(() => container.WithImageTag("7.1"));
        Assert.AreEqual("The resource 'testcontainer' does not have a container image specified. Use WithImage to specify the container image and tag.", exception.Message);
    }

    [TestMethod]
    public void WithImageRegistryThrowsIfNoImageAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddResource(new TestContainerResource("testcontainer"));

        var exception = Assert.Throws<InvalidOperationException>(() => container.WithImageRegistry("myregistry.azurecr.io"));
        Assert.AreEqual("The resource 'testcontainer' does not have a container image specified. Use WithImage to specify the container image and tag.", exception.Message);
    }

    [TestMethod]
    public void WithImageSHA256ThrowsIfNoImageAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddResource(new TestContainerResource("testcontainer"));

        var exception = Assert.Throws<InvalidOperationException>(() => container.WithImageSHA256("42b5c726e719639fcc1e9dbc13dd843f567dcd37911d0e1abb9f47f2cc1c95cd"));
        Assert.AreEqual("The resource 'testcontainer' does not have a container image specified. Use WithImage to specify the container image and tag.", exception.Message);
    }

    [TestMethod]
    [DataRow("redis", "redis", "latest", null)]
    [DataRow("redis:latest", "redis", "latest", null)]
    [DataRow("registry.io/library/rabbitmq", "registry.io/library/rabbitmq", "latest", null)]
    [DataRow("postgres:tag", "postgres", "tag", null)]
    [DataRow("kafka@sha256:01234567890abcdef01234567890abcdef01234567890abcdef01234567890ab", "kafka", null, "01234567890abcdef01234567890abcdef01234567890abcdef01234567890ab")]
    [DataRow("registry.io/image:tag", "registry.io/image", "tag", null)]
    [DataRow("host.com/path/to/image@sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "host.com/path/to/image", null, "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [DataRow("another.org/path/to/another/image:tag@sha256:9999999999999999999999999999999999999999999999999999999999999999", "another.org/path/to/another/image", null, "9999999999999999999999999999999999999999999999999999999999999999")]
    public void WithImageMutatesContainerImageAnnotation(string reference, string expectedImage, string? expectedTag, string? expectedSha256)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddResource(new TestContainerResource("testcontainer"));

        container.WithImage(reference);

        AssertImageComponents(container, null, expectedImage, expectedTag, expectedSha256);
    }

    [TestMethod]
    public void WithImageThrowsWithConflictingTag()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddResource(new TestContainerResource("testcontainer"));

        Assert.Throws<InvalidOperationException>(() => container.WithImage("image:tag", "anothertag"));
    }

    [TestMethod]
    public void WithImageThrowsWithConflictingTagAndDigest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddResource(new TestContainerResource("testcontainer"));

        Assert.Throws<ArgumentOutOfRangeException>(() => container.WithImage("image@sha246:abcdabcdabcdabcdabcdabcdabcdabcdabcdabcdabcdabcdabcdabcdabcdabcd", "tag"));
    }

    [TestMethod]
    public void WithImageOverridesExistingImageAndTag()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder
            .AddContainer("container", "image", "original-tag")
            .WithImage("yet-another-image:new-tag");

        var annotation = redis.Resource.Annotations.OfType<ContainerImageAnnotation>().Single();
        AssertImageComponents(redis, null, "yet-another-image", "new-tag", null);
    }

    [TestMethod]
    public void WithImageOverridesExistingImageAndSha()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder
            .AddContainer("container", "image", "original-tag")
            .WithImage("yet-another-image@sha256:421c76d77563afa1914846b010bd164f395bd34c2102e5e99e0cb9cf173c1d87");

        var annotation = redis.Resource.Annotations.OfType<ContainerImageAnnotation>().Single();
        AssertImageComponents(redis, null, "yet-another-image", null, "421c76d77563afa1914846b010bd164f395bd34c2102e5e99e0cb9cf173c1d87");
    }

    [TestMethod]
    public void WithImageWithoutRegistryShouldKeepExistingRegistryButOverwriteTagWithLatest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder
            .AddContainer("container", "image", "original-tag")
            .WithImageRegistry("foobar.io")
            .WithImage("different-image");

        var annotation = redis.Resource.Annotations.OfType<ContainerImageAnnotation>().Single();
        AssertImageComponents(redis, "foobar.io", "different-image", "latest", null);
    }

    [TestMethod]
    public void WithImageWithoutTagShouldReplaceExistingTagWithLatest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder
            .AddContainer("container", "redis-stack", "original-tag")
            .WithImage("redis-stack");

        AssertImageComponents(redis, null, "redis-stack", "latest", null);
    }

    [TestMethod]
    public void WithImageOverwritesSha256WithLatestTag()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder
            .AddContainer("redis", "image")
            .WithImageSHA256("ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")
            .WithImage("redis-stack");

        AssertImageComponents(redis, null, "redis-stack", "latest", null);
    }

    [TestMethod]
    public void WithImagePullPolicyMutatesImagePullPolicyOfLastAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder
            .AddContainer("redis", "image")
            .WithImagePullPolicy(ImagePullPolicy.Missing)
            .WithImagePullPolicy(ImagePullPolicy.Always);

        var annotation = redis.Resource.Annotations.OfType<ContainerImagePullPolicyAnnotation>().Single();

        Assert.AreEqual(ImagePullPolicy.Always, annotation.ImagePullPolicy);
    }

    private static void AssertImageComponents<T>(IResourceBuilder<T> builder, string? expectedRegistry, string expectedImage, string? expectedTag, string? expectedSha256)
        where T: IResource
    {
        var containerImage = builder.Resource.Annotations.OfType<ContainerImageAnnotation>().Single();
        Assert.Multiple(() =>
        {
            Assert.AreEqual(expectedRegistry, containerImage.Registry);
            Assert.AreEqual(expectedImage, containerImage.Image);
            Assert.AreEqual(expectedTag, containerImage.Tag);
            Assert.AreEqual(expectedSha256, containerImage.SHA256);
        });
    }

    private sealed class TestContainerResource(string name) : ContainerResource(name)
    {
    }
}
