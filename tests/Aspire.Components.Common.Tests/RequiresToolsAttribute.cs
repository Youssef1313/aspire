// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Components.Common.Tests;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequiresToolsAttribute : ConditionBaseAttribute
{
    public RequiresToolsAttribute(string[] executablesOnPath)
        : base(ConditionMode.Include)
    {
        if (executablesOnPath.Length == 0)
        {
            throw new ArgumentException("At least one executable must be provided", nameof(executablesOnPath));
        }

        ShouldRun = executablesOnPath.All(executable => FileUtil.FindFullPathFromPath(executable) is not null);
    }

    public override string? IgnoreMessage => null;

    public override string GroupName => nameof(RequiresToolsAttribute);

    public override bool ShouldRun { get; }
}
