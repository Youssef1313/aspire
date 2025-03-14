// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.InternalTesting;

namespace Aspire.Hosting.Tests;

[TestClass]
public class PublishAsConnectionStringTests
{
    [TestMethod]
    public async Task PublishAsConnectionStringConfiguresManifestAsParameter()
    {
        var builder = DistributedApplication.CreateBuilder();

        var redis = builder.AddRedis("redis").PublishAsConnectionString();

        Assert.IsTrue(redis.Resource.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out _));

        var manifest = await ManifestUtils.GetManifest(redis.Resource).DefaultTimeout();

        var expected =
            """
            {
              "type": "parameter.v0",
              "connectionString": "{redis.value}",
              "value": "{redis.inputs.value}",
              "inputs": {
                "value": {
                  "type": "string",
                  "secret": true
                }
              }
            }
            """;

        var actual = manifest.ToString();

        Assert.AreEqual(expected, actual, ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
    }
}
