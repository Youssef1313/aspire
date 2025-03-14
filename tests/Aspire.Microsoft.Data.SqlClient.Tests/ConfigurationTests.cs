// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Aspire.Microsoft.Data.SqlClient.Tests;

[TestClass]
public class ConfigurationTests
{
    [TestMethod]
    public void ConnectionStringIsNullByDefault()
        => Assert.IsNull(new MicrosoftDataSqlClientSettings().ConnectionString);

    [TestMethod]
    public void HealthCheckIsEnabledByDefault()
        => Assert.IsFalse(new MicrosoftDataSqlClientSettings().DisableHealthChecks);

    [TestMethod]
    public void TracingIsEnabledByDefault()
        => Assert.IsFalse(new MicrosoftDataSqlClientSettings().DisableTracing);
}
