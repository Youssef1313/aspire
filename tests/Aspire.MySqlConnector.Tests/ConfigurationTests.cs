// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Aspire.MySqlConnector.Tests;

[TestClass]
public class ConfigurationTests
{
    [TestMethod]
    public void ConnectionStringIsNullByDefault()
    => Assert.IsNull(new MySqlConnectorSettings().ConnectionString);

    [TestMethod]
    public void HealthCheckIsEnabledByDefault()
        => Assert.IsFalse(new MySqlConnectorSettings().DisableHealthChecks);

    [TestMethod]
    public void TracingIsEnabledByDefault()
        => Assert.IsFalse(new MySqlConnectorSettings().DisableTracing);

    [TestMethod]
    public void MetricsAreEnabledByDefault()
        => Assert.IsFalse(new MySqlConnectorSettings().DisableMetrics);
}
