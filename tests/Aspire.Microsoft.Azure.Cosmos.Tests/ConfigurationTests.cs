// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Aspire.Microsoft.Azure.Cosmos.Tests;

[TestClass]
public class ConfigurationTests
{
    [TestMethod]
    public void ConnectionStringIsNullByDefault()
        => Assert.IsNull(new MicrosoftAzureCosmosSettings().ConnectionString);

    [TestMethod]
    public void TracingIsEnabledByDefault()
        => Assert.IsFalse(new MicrosoftAzureCosmosSettings().DisableTracing);
}
