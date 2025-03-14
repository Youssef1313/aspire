// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.StackExchange.Redis.OutputCaching.Tests;

[TestClass]
public class AspireRedisOutputCacheExtensionsTests
{
    [TestMethod]
    public void AddsRedisOutputCacheCorrectly()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.AddRedisOutputCache("redis");

        using var host = builder.Build();
        var cacheStore = host.Services.GetRequiredService<IOutputCacheStore>();

        // note the RedisOutputCacheStore is internal
        Assert.StartsWith("Redis", cacheStore.GetType().Name);
    }
}
