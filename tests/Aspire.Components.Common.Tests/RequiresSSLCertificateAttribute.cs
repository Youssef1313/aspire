// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Components.Common.Tests;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequiresSSLCertificateAttribute(string? reason = null)
    : ConditionBaseAttribute(ConditionMode.Include)
{
    // Not supported on Windows CI
    public static bool IsSupported => !PlatformDetection.IsRunningOnCI || !OperatingSystem.IsWindows();

    public override string? IgnoreMessage { get; } = reason;

    public override string GroupName => nameof(RequiresSSLCertificateAttribute);

    public override bool ShouldRun => IsSupported;
}
