// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Xunit.Abstractions;

namespace Aspire.Workload.Tests;

public abstract class StarterTemplateProjectNamesTests : WorkloadTestsBase
{
    private readonly string _testType;
    public StarterTemplateProjectNamesTests(string testType, TestContext testOutput)
        : base(testOutput)
    {
        _testType = testType;
    }

    public static TheoryData<string> ProjectNamesWithTestType_TestData()
        => new(GetProjectNamesForTest());

    [TestMethod]
    [MemberData(nameof(ProjectNamesWithTestType_TestData))]
    [RequiresSSLCertificate("Needs dashboard, web front end access")]
    public async Task StarterTemplateWithTest_ProjectNames(string prefix)
    {
        string id = $"{prefix}-{_testType}";
        string config = "Debug";

        await using var project = await AspireProject.CreateNewTemplateProjectAsync(
            id,
            "aspire-starter",
            _testOutput,
            BuildEnvironment.ForDefaultFramework,
            $"-t {_testType}");

        await using var context = PlaywrightProvider.HasPlaywrightSupport ? await CreateNewBrowserContextAsync() : null;
        _testOutput.WriteLine($"Checking the starter template project");
        await AssertStarterTemplateRunAsync(context, project, config, _testOutput);

        _testOutput.WriteLine($"Checking the starter template project tests");
        await AssertTestProjectRunAsync(project.TestsProjectDirectory, _testType, _testOutput, config);
    }
}

// Individual class for each test framework so the tests can run in separate helix jobs
public class None_StarterTemplateProjectNamesTests : StarterTemplateProjectNamesTests
{
    public None_StarterTemplateProjectNamesTests(TestContext testOutput) : base("none", testOutput)
    {
    }
}

public class MSTest_StarterTemplateProjectNamesTests : StarterTemplateProjectNamesTests
{
    public MSTest_StarterTemplateProjectNamesTests(TestContext testOutput) : base("mstest", testOutput)
    {
    }
}

public class Xunit_StarterTemplateProjectNamesTests : StarterTemplateProjectNamesTests
{
    public Xunit_StarterTemplateProjectNamesTests(TestContext testOutput) : base("xunit.net", testOutput)
    {
    }
}

public class Nunit_StarterTemplateProjectNamesTests : StarterTemplateProjectNamesTests
{
    public Nunit_StarterTemplateProjectNamesTests(TestContext testOutput) : base("nunit", testOutput)
    {
    }
}
