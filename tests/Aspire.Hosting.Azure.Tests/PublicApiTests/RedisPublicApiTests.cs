// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Tests.PublicApiTests;

[TestClass]
public class RedisPublicApiTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CtorAzureRedisCacheResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var name = isNull ? null! : string.Empty;
        Action<AzureResourceInfrastructure> configureInfrastructure = (_) => { };

        var action = () => new AzureRedisCacheResource(name, configureInfrastructure);

        var exception = isNull
           ? Assert.Throws<ArgumentNullException>(action)
           : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void CtorAzureRedisCacheResourceShouldThrowWhenConfigureInfrastructureIsNull()
    {
        const string name = "redis";
        Action<AzureResourceInfrastructure> configureInfrastructure = null!;

        var action = () => new AzureRedisCacheResource(name, configureInfrastructure);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(configureInfrastructure), exception.ParamName);
    }

    [TestMethod]
    [Obsolete($"This method is obsolete and will be removed in a future version. Use AddAzureRedis instead to add an Azure Cache for Redis resource.")]
    public void PublishAsAzureRedisShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<RedisResource> builder = null!;

        var action = () =>
        {
            builder.PublishAsAzureRedis();
        };

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [Obsolete($"This method is obsolete and will be removed in a future version. Use AddAzureRedis instead to add an Azure Cache for Redis resource.")]
    public void AsAzureRedisShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<RedisResource> builder = null!;

        var action = () =>
        {
            builder.AsAzureRedis();
        };

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void AddAzureRedisShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "redis";

        var action = () => builder.AddAzureRedis(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AddAzureRedisShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddAzureRedis(name);

        var exception = isNull
           ? Assert.Throws<ArgumentNullException>(action)
           : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void RunAsContainerShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<AzureRedisCacheResource> builder = null!;

        var action = () => builder.RunAsContainer();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void WithAccessKeyAuthenticationShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<AzureRedisCacheResource> builder = null!;

        var action = () =>
        {
            builder.WithAccessKeyAuthentication();
        };

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [Obsolete($"This class is obsolete and will be removed in a future version. Use {nameof(AzureRedisExtensions.AddAzureRedis)} instead to add an Azure Cache for Redis resource.")]
    public void CtorAzureRedisResourceShouldThrowWhenInnerResourceIsNull()
    {
        RedisResource innerResource = null!;
        Action<AzureResourceInfrastructure> configureInfrastructure = (_) => { };

        var action = () => new AzureRedisResource(innerResource, configureInfrastructure);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(innerResource), exception.ParamName);
    }

    [TestMethod]
    [Obsolete($"This class is obsolete and will be removed in a future version. Use {nameof(AzureRedisExtensions.AddAzureRedis)} instead to add an Azure Cache for Redis resource.")]
    public void CtorAzureRedisResourceShouldThrowWhenConfigureInfrastructureIsNull()
    {
        var innerResource = new RedisResource("redis");
        Action<AzureResourceInfrastructure> configureInfrastructure = null!;

        var action = () => new AzureRedisResource(innerResource, configureInfrastructure);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(configureInfrastructure), exception.ParamName);
    }
}
