// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// TODO: Fix this.
#pragma warning disable xUnit1051 // Calls to methods which accept CancellationToken should use TestContext.Current.CancellationToken to allow test cancellation to be more responsive.

using Xunit;

namespace Aspire.Templates.Tests;

public class NewUpAndBuildSupportProjectTemplates(ITestOutputHelper testOutput) : TemplateTestsBase(testOutput)
{
    [Theory]
    // [MemberData(nameof(TestDataForNewAndBuildTemplateTests), arguments: "aspire-apphost")]
    // [MemberData(nameof(TestDataForNewAndBuildTemplateTests), arguments: "aspire-servicedefaults")]
    [MemberData(nameof(TestDataForNewAndBuildTemplateTests), arguments: "aspire-mstest")]
    [MemberData(nameof(TestDataForNewAndBuildTemplateTests), arguments: "aspire-nunit")]
    [MemberData(nameof(TestDataForNewAndBuildTemplateTests), arguments: "aspire-xunit")]
    public async Task CanNewAndBuild(string templateName, TestSdk sdk, TestTargetFramework tfm, string? error)
    {
        var id = GetNewProjectId(prefix: $"new_build_{templateName}_{tfm.ToTFMString()}");
        string config = "Debug";

        var buildEnvToUse = sdk switch
        {
            TestSdk.Current => BuildEnvironment.ForCurrentSdkOnly,
            TestSdk.Previous => BuildEnvironment.ForPreviousSdkOnly,
            TestSdk.CurrentSdkAndPreviousRuntime => BuildEnvironment.ForCurrentSdkAndPreviousRuntime,
            _ => throw new ArgumentOutOfRangeException(nameof(sdk))
        };

        try
        {
            await using var project = await AspireProject.CreateNewTemplateProjectAsync(
                id: id,
                template: "aspire",
                testOutput: _testOutput,
                buildEnvironment: buildEnvToUse,
                targetFramework: tfm);

            var testProjectDir = await CreateAndAddTestTemplateProjectAsync(
                                        id: id,
                                        testTemplateName: templateName,
                                        project: project,
                                        tfm: tfm,
                                        buildEnvironment: buildEnvToUse);

            await project.BuildAsync(extraBuildArgs: [$"-c {config}"], workingDirectory: testProjectDir);
        }
        catch (ToolCommandException tce) when (error is not null)
        {
            Assert.NotNull(tce.Result);
            Assert.Contains(error, tce.Result.Value.Output);
        }
    }
}
