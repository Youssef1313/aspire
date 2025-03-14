// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;

namespace Aspire.Hosting.Testing.Tests;

[TestClass]

// Tests that DistributedApplicationTestingBuilder throws exceptions at the right times when the app crashes.
public class TestingFactoryCrashTests
{
    [TestMethod]
    [RequiresDocker]
    [DataRow("before-build")]
    [DataRow("after-build")]
    [DataRow("after-start")]
    [DataRow("after-shutdown")]
    public async Task CrashTests(string crashArg)
    {
        var timeout = TimeSpan.FromMinutes(5);
        using var cts = new CancellationTokenSource(timeout);

        var factory = new DistributedApplicationFactory(typeof(Projects.TestingAppHost1_AppHost), [$"--crash-{crashArg}"]);

        if (crashArg is "before-build" or "after-build")
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => factory.StartAsync().WaitAsync(cts.Token));
            Assert.Contains(crashArg, exception.Message);
        }
        else
        {
            await factory.StartAsync().WaitAsync(cts.Token);
        }

        await factory.DisposeAsync().AsTask().WaitAsync(cts.Token);
    }
}
