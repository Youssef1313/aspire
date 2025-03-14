// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Tests;

[TestClass]
public class CliSmokeTests
{
    [TestMethod]
    public async Task NoArgsReturnsExitCode1()
    {
        var exitCode = await Aspire.Cli.Program.Main([]);
        Assert.AreEqual(ExitCodeConstants.InvalidCommand, exitCode);
    }
}
