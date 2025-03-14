// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Xunit.Abstractions;

namespace Aspire.Workload.Tests;

[RequiresSSLCertificate]
public class StarterTemplateRunTests_PreviousTFM : StarterTemplateRunTestsBase<StarterTemplateFixture_PreviousTFM>
{
    public StarterTemplateRunTests_PreviousTFM(StarterTemplateFixture_PreviousTFM fixture, TestContext testOutput)
        : base(fixture, testOutput)
    {
    }
}
