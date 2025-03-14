// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Aspire.Npgsql.Tests;

[TestClass]
public class ConfigurationTests
{
    [TestMethod]
    public void ConnectionStringIsNullByDefault()
    => Assert.IsNull(new NpgsqlSettings().ConnectionString);

    [TestMethod]
    public void HealthCheckIsEnabledByDefault()
        => Assert.IsFalse(new NpgsqlSettings().DisableHealthChecks);

    [TestMethod]
    public void TracingIsEnabledByDefault()
        => Assert.IsFalse(new NpgsqlSettings().DisableTracing);

    [TestMethod]
    public void MetricsAreEnabledByDefault()
        => Assert.IsFalse(new NpgsqlSettings().DisableMetrics);
}
