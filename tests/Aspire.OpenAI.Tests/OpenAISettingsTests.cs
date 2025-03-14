// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;

namespace Aspire.OpenAI.Tests;

[TestClass]
public class OpenAISettingsTests
{
    [TestMethod]
    public void MetricsIsEnabledWhenAzureSwitchIsSet()
    {
        RemoteExecutor.Invoke(() => EnsureMetricsIsEnabledWhenAzureSwitchIsSet(true)).Dispose();
        RemoteExecutor.Invoke(() => EnsureMetricsIsEnabledWhenAzureSwitchIsSet(false), EnableTelemetry()).Dispose();
    }

    [TestMethod]
    public void TracingIsEnabledWhenAzureSwitchIsSet()
    {
        RemoteExecutor.Invoke(() => EnsureTracingIsEnabledWhenAzureSwitchIsSet(true)).Dispose();
        RemoteExecutor.Invoke(() => EnsureTracingIsEnabledWhenAzureSwitchIsSet(false), EnableTelemetry()).Dispose();
    }

    private static void EnsureMetricsIsEnabledWhenAzureSwitchIsSet(bool expectedValue)
    {
        Assert.AreEqual(expectedValue, new OpenAISettings().DisableMetrics);
    }

    private static void EnsureTracingIsEnabledWhenAzureSwitchIsSet(bool expectedValue)
    {
        Assert.AreEqual(expectedValue, new OpenAISettings().DisableTracing);
    }

    private static RemoteInvokeOptions EnableTelemetry()
        => new()
        {
            RuntimeConfigurationOptions = { { "OpenAI.Experimental.EnableOpenTelemetry", true } }
        };
}
