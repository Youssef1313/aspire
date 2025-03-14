// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestProject;
using Aspire.Workload.Tests;

namespace Aspire.EndToEnd.Tests;

public class IntegrationServicesTests : IClassFixture<IntegrationServicesFixture>
{
    private readonly IntegrationServicesFixture _integrationServicesFixture;
    private readonly TestOutputWrapper _testOutput;

    public IntegrationServicesTests(TestContext testOutput, IntegrationServicesFixture integrationServicesFixture)
    {
        _integrationServicesFixture = integrationServicesFixture;
        _testOutput = new TestOutputWrapper(testOutput);
    }

    [TestMethod]
    [Trait("scenario", "basicservices")]
    [DataRow(TestResourceNames.postgres)]
    [DataRow(TestResourceNames.efnpgsql)]
    [DataRow(TestResourceNames.redis)]
    public Task VerifyComponentWorks(TestResourceNames resourceName)
        => RunTestAsync(async () =>
        {
            _integrationServicesFixture.EnsureAppHasResources(resourceName);
            try
            {
                var response = await _integrationServicesFixture.IntegrationServiceA.HttpGetAsync("http", $"/{resourceName}/verify");
                var responseContent = await response.Content.ReadAsStringAsync();

                Assert.IsTrue(response.IsSuccessStatusCode, responseContent);
            }
            catch
            {
                await _integrationServicesFixture.DumpComponentLogsAsync(resourceName, _testOutput);
                throw;
            }
        });

    [TestMethod]
    [Trait("scenario", "basicservices")]
    public Task VerifyHealthyOnIntegrationServiceA()
        => RunTestAsync(async () =>
        {
            // We wait until timeout for the /health endpoint to return successfully. We assume
            // that components wired up into this project have health checks enabled.
            await _integrationServicesFixture.IntegrationServiceA.WaitForHealthyStatusAsync("http", _testOutput);
        });

    private async Task RunTestAsync(Func<Task> test)
    {
        _integrationServicesFixture.Project.EnsureAppHostRunning();
        try
        {
            await test();
        }
        catch
        {
            await _integrationServicesFixture.Project.DumpDockerInfoAsync();
            throw;
        }
    }
}
