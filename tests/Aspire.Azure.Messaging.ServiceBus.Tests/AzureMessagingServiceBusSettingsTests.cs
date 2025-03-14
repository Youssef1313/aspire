// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;

namespace Aspire.Azure.Messaging.ServiceBus.Tests;

[TestClass]
public class AzureMessagingServiceBusSettingsTests
{
    [TestMethod]
    public void TracingIsEnabledWhenAzureSwitchIsSet()
    {
        RemoteExecutor.Invoke(() => EnsureTracingIsEnabledWhenAzureSwitchIsSet(true)).Dispose();
        RemoteExecutor.Invoke(() => EnsureTracingIsEnabledWhenAzureSwitchIsSet(false), ConformanceTests.EnableTracingForAzureSdk()).Dispose();
    }

    private static void EnsureTracingIsEnabledWhenAzureSwitchIsSet(bool expectedValue)
    {
        Assert.AreEqual(expectedValue, new AzureMessagingServiceBusSettings().DisableTracing);
    }
}
