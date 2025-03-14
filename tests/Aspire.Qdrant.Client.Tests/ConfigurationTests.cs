// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Aspire.Qdrant.Client.Tests;

[TestClass]
public class ConfigurationTests
{
    [TestMethod]
    public void EndpointIsNullByDefault()
    => Assert.IsNull(new QdrantClientSettings().Endpoint);

    [TestMethod]
    public void HealthChecksEnabledByDefault() =>
     Assert.IsFalse(new QdrantClientSettings().DisableHealthChecks);

    [TestMethod]
    public void HealthCheckTimeoutNullByDefault() =>
     Assert.IsNull(new QdrantClientSettings().HealthCheckTimeout);
}
