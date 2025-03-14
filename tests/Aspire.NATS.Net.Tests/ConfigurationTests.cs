// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Aspire.NATS.Net.Tests;

[TestClass]
public class ConfigurationTests
{
    [TestMethod]
    public void ConnectionStringIsNullByDefault()
        => Assert.IsNull(new NatsClientSettings().ConnectionString);

    [TestMethod]
    public void HealthCheckIsEnabledByDefault()
        => Assert.IsFalse(new NatsClientSettings().DisableHealthChecks);

    [TestMethod]
    public void TracingIsEnabledByDefault()
        => Assert.IsFalse(new NatsClientSettings().DisableTracing);
}
