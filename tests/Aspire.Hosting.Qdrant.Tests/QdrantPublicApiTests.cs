// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Qdrant.Tests;

[TestClass]
public class QdrantPublicApiTests
{
    [TestMethod]
    public void AddQdrantShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "Qdrant";

        Action action = () => builder.AddQdrant(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddQdrantShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;

        Action action = () => builder.AddQdrant(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void WithDataVolumeShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<QdrantServerResource> builder = null!;

        Action action = () => builder.WithDataVolume();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void WithDataBindMountShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<QdrantServerResource> builder = null!;
        const string source = "/qdrant/storage";

        Action action = () => builder.WithDataBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void WithDataBindMountShouldThrowWhenSourceIsNullOrEmpty(bool isNull)
    {
        var builderResource = TestDistributedApplicationBuilder.Create();
        var qdrant = builderResource.AddQdrant("Qdrant");
        var source = isNull ? null! : string.Empty;

        Action action = () => qdrant.WithDataBindMount(source);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(source), exception.ParamName);
    }

    [TestMethod]
    public void WithReferenceShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<IResourceWithEnvironment> builder = null!;
        var builderResource = TestDistributedApplicationBuilder.Create();
        var qdrantResource = builderResource.AddQdrant("Qdrant");

        Action action = () => builder.WithReference(qdrantResource);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void WithReferenceShouldThrowWhenQdrantResourceIsNull()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var qdrant = builder.AddQdrant("Qdrant");
        IResourceBuilder<QdrantServerResource> qdrantResource = null!;

        Action action = () => qdrant.WithReference(qdrantResource);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(qdrantResource), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void CtorQdrantServerResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var distributedApplicationBuilder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;
        const string key = nameof(key);
        var apiKey = new ParameterResource(key, (ParameterDefault? parameterDefault) => key);

        Action action = () => new QdrantServerResource(name, apiKey);

        var exception = isNull
             ? Assert.Throws<ArgumentNullException>(action)
             : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void CtorQdrantServerResourceShouldThrowWhenApiKeyIsNull()
    {
        const string name = "Qdrant";
        ParameterResource apiKey = null!;

        Action action = () => new QdrantServerResource(name, apiKey);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(apiKey), exception.ParamName);
    }
}
