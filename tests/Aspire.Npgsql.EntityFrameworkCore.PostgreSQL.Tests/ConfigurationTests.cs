// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Aspire.Npgsql.EntityFrameworkCore.PostgreSQL.Tests;

[TestClass]
public class ConfigurationTests
{
    [TestMethod]
    public void ConnectionStringIsNullByDefault()
        => Assert.IsNull(new NpgsqlEntityFrameworkCorePostgreSQLSettings().ConnectionString);

    [TestMethod]
    public void HealthCheckIsEnabledByDefault()
        => Assert.IsFalse(new NpgsqlEntityFrameworkCorePostgreSQLSettings().DisableHealthChecks);

    [TestMethod]
    public void TracingIsEnabledByDefault()
        => Assert.IsFalse(new NpgsqlEntityFrameworkCorePostgreSQLSettings().DisableTracing);

    [TestMethod]
    public void MetricsAreEnabledByDefault()
        => Assert.IsFalse(new NpgsqlEntityFrameworkCorePostgreSQLSettings().DisableMetrics);

    [TestMethod]
    public void RetriesAreEnabledByDefault()
        => Assert.IsFalse(new NpgsqlEntityFrameworkCorePostgreSQLSettings().DisableRetry);
}
