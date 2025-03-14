// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Aspire.Pomelo.EntityFrameworkCore.MySql.Tests;

[TestClass]
public class ConfigurationTests
{
    [TestMethod]
    public void ConnectionStringIsNullByDefault()
        => Assert.IsNull(new PomeloEntityFrameworkCoreMySqlSettings().ConnectionString);

    [TestMethod]
    public void HealthCheckIsEnabledByDefault()
        => Assert.IsFalse(new PomeloEntityFrameworkCoreMySqlSettings().DisableHealthChecks);

    [TestMethod]
    public void TracingIsEnabledByDefault()
        => Assert.IsFalse(new PomeloEntityFrameworkCoreMySqlSettings().DisableTracing);

    [TestMethod]
    public void MetricsAreEnabledByDefault()
        => Assert.IsFalse(new PomeloEntityFrameworkCoreMySqlSettings().DisableMetrics);

    [TestMethod]
    public void RetriesAreEnabledByDefault()
        => Assert.IsFalse(new PomeloEntityFrameworkCoreMySqlSettings().DisableRetry);
}
