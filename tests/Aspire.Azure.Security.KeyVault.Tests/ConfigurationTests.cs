// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Aspire.Azure.Security.KeyVault.Tests;

[TestClass]
public class ConfigurationTests
{
    [TestMethod]
    public void VaultUriIsNullByDefault()
        => Assert.IsNull(new AzureSecurityKeyVaultSettings().VaultUri);

    [TestMethod]
    public void HealthCheckIsEnabledByDefault()
        => Assert.IsFalse(new AzureSecurityKeyVaultSettings().DisableHealthChecks);

    [TestMethod]
    public void TracingIsEnabledByDefault()
        => Assert.IsFalse(new AzureSecurityKeyVaultSettings().DisableTracing);
}
