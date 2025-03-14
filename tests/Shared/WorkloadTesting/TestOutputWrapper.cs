// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace Aspire.Workload.Tests;

public class TestOutputWrapper(TestContext? testContext = null, bool forceShowBuildOutput = false) : TestContext
{
    public override IDictionary Properties => throw new NotImplementedException();

    public override void AddResultFile(string fileName)
        => testContext?.AddResultFile(fileName);

    public override void DisplayMessage(MessageLevel messageLevel, string message)
        => testContext?.DisplayMessage(messageLevel, message);

    public override void Write(string? message)
    {
        testContext?.Write(message);

        if (forceShowBuildOutput || EnvironmentVariables.ShowBuildOutput)
        {
            Console.Write(message);
        }
    }

    public override void Write(string format, params object?[] args)
    {
        testContext?.Write(format, args);
        if (forceShowBuildOutput || EnvironmentVariables.ShowBuildOutput)
        {
            Console.Write(format, args);
        }
    }

    public override void WriteLine(string? message)
    {
        testContext?.WriteLine(message);

        if (forceShowBuildOutput || EnvironmentVariables.ShowBuildOutput)
        {
            Console.WriteLine(message);
        }
    }

    public override void WriteLine(string format, params object?[] args)
    {
        testContext?.WriteLine(format, args);
        if (forceShowBuildOutput || EnvironmentVariables.ShowBuildOutput)
        {
            Console.WriteLine(format, args);
        }
    }
}
