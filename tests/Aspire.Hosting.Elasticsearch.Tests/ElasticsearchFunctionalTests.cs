// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Polly;

namespace Aspire.Hosting.Elasticsearch.Tests;

[TestClass]
public class ElasticsearchFunctionalTests
{
    public TestContext TestContext { get; set; }

    private const string IndexName = "people";
    private static readonly Person s_person = new()
    {
        Id = 1,
        FirstName = "Alireza",
        LastName = "Baloochi"
    };

    [TestMethod]
    [RequiresDocker]
    // [ActiveIssue("https://github.com/dotnet/aspire/issues/5821")]
    public async Task VerifyElasticsearchResource()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        var pipeline = new ResiliencePipelineBuilder()
           .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(10) })
           .Build();

        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(TestContext);

        var elasticsearch = builder.AddElasticsearch("elasticsearch");

        using var app = builder.Build();

        await app.StartAsync();

        var rns = app.Services.GetRequiredService<ResourceNotificationService>();
        await rns.WaitForResourceHealthyAsync(elasticsearch.Resource.Name, cts.Token);

        var hb = Host.CreateApplicationBuilder();

        hb.Configuration[$"ConnectionStrings:{elasticsearch.Resource.Name}"] = await elasticsearch.Resource.ConnectionStringExpression.GetValueAsync(default);

        hb.AddElasticsearchClient(elasticsearch.Resource.Name);

        using var host = hb.Build();

        await host.StartAsync();

        await pipeline.ExecuteAsync(
            async token =>
            {

                var elasticsearchClient = host.Services.GetRequiredService<ElasticsearchClient>();

                await CreateTestData(elasticsearchClient, TestContext, token);
            }, cts.Token);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    [RequiresDocker]
    // [ActiveIssue("https://github.com/dotnet/aspire/issues/7276")]
    public async Task WithDataShouldPersistStateBetweenUsages(bool useVolume)
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        var pipeline = new ResiliencePipelineBuilder()
           .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(10) })
           .Build();

        string? volumeName = null;
        string? bindMountPath = null;

        try
        {
            using var builder1 = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(TestContext);

            var elasticsearch1 = builder1.AddElasticsearch("elasticsearch");

            var password = elasticsearch1.Resource.PasswordParameter.Value;

            if (useVolume)
            {
                // Use a deterministic volume name to prevent them from exhausting the machines if deletion fails
                volumeName = VolumeNameGenerator.Generate(elasticsearch1, nameof(WithDataShouldPersistStateBetweenUsages));

                // if the volume already exists (because of a crashing previous run), delete it
                DockerUtils.AttemptDeleteDockerVolume(volumeName, throwOnFailure: true);
                elasticsearch1.WithDataVolume(volumeName);
            }
            else
            {
                bindMountPath = Directory.CreateTempSubdirectory().FullName;
                elasticsearch1.WithDataBindMount(bindMountPath);
            }

            using (var app = builder1.Build())
            {
                await app.StartAsync(cts.Token);

                await app.ResourceNotifications.WaitForResourceHealthyAsync(elasticsearch1.Resource.Name, cts.Token);

                try
                {
                    var hb = Host.CreateApplicationBuilder();

                    hb.Configuration[$"ConnectionStrings:{elasticsearch1.Resource.Name}"] = await elasticsearch1.Resource.ConnectionStringExpression.GetValueAsync(default);

                    hb.AddElasticsearchClient(elasticsearch1.Resource.Name);

                    using (var host = hb.Build())
                    {
                        await host.StartAsync();

                        await pipeline.ExecuteAsync(
                            async token =>
                            {
                                var elasticsearchClient = host.Services.GetRequiredService<ElasticsearchClient>();
                                await CreateTestData(elasticsearchClient, TestContext, token);
                            }, cts.Token);

                        await app.StopAsync();

                        // Wait for the container to be stopped and to release the volume files before continuing
                        await pipeline.ExecuteAsync(
                            async token =>
                            {
                                var elasticsearchClient = host.Services.GetRequiredService<ElasticsearchClient>();
                                var getResponse = await elasticsearchClient.GetAsync<Person>(IndexName, s_person.Id, token);
                                Assert.IsFalse(getResponse.IsSuccess());
                            }, cts.Token);
                    }
                }
                finally
                {
                    // Stops the container, or the Volume would still be in use
                    await app.StopAsync();
                }
            }

            using var builder2 = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(TestContext);
            var passwordParameter2 = builder2.AddParameter("pwd", password);
            var elasticsearch2 = builder2.AddElasticsearch("elasticsearch", passwordParameter2);

            if (useVolume)
            {
                elasticsearch2.WithDataVolume(volumeName);
            }
            else
            {
                elasticsearch2.WithDataBindMount(bindMountPath!);
            }

            using (var app = builder2.Build())
            {
                await app.StartAsync();

                await app.ResourceNotifications.WaitForResourceHealthyAsync(elasticsearch2.Resource.Name, cts.Token);

                try
                {
                    var hb = Host.CreateApplicationBuilder();

                    hb.Configuration[$"ConnectionStrings:{elasticsearch2.Resource.Name}"] = await elasticsearch2.Resource.ConnectionStringExpression.GetValueAsync(default);

                    hb.AddElasticsearchClient(elasticsearch2.Resource.Name);

                    using (var host = hb.Build())
                    {
                        await host.StartAsync();
                        await pipeline.ExecuteAsync(
                            async token =>
                            {
                                var elasticsearchClient = host.Services.GetRequiredService<ElasticsearchClient>();

                                var getResponse = await elasticsearchClient.GetAsync<Person>(IndexName, s_person.Id, token);

                                Assert.IsTrue(getResponse.IsSuccess());
                                Assert.IsNotNull(getResponse.Source);
                                Assert.AreEqual(s_person.Id, getResponse.Source?.Id);
                            }, cts.Token);

                        await app.StopAsync();

                        // Wait for the container to be stopped and to release the volume files before continuing
                        await pipeline.ExecuteAsync(
                            async token =>
                            {
                                var elasticsearchClient = host.Services.GetRequiredService<ElasticsearchClient>();
                                var getResponse = await elasticsearchClient.GetAsync<Person>(IndexName, s_person.Id, token);
                                Assert.IsFalse(getResponse.IsSuccess());
                            }, cts.Token);
                    }
                }
                finally
                {
                    // Stops the container, or the Volume would still be in use
                    await app.StopAsync();
                }
            }

        }
        finally
        {
            if (volumeName is not null)
            {
                DockerUtils.AttemptDeleteDockerVolume(volumeName);
            }

            if (bindMountPath is not null)
            {
                try
                {
                    Directory.Delete(bindMountPath, recursive: true);
                }
                catch
                {
                    // Don't fail test if we can't clean the temporary folder
                }
            }
        }
    }

    [TestMethod]
    [RequiresDocker]
    // [ActiveIssue("https://github.com/dotnet/aspire/issues/5844")]
    public async Task VerifyWaitForOnElasticsearchBlocksDependentResources()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(TestContext);

        var healthCheckTcs = new TaskCompletionSource<HealthCheckResult>();
        builder.Services.AddHealthChecks().AddAsyncCheck("blocking_check", () =>
        {
            return healthCheckTcs.Task;
        });

        var resource = builder.AddElasticsearch("resource")
                              .WithHealthCheck("blocking_check");

        var dependentResource = builder.AddContainer("nginx", "mcr.microsoft.com/cbl-mariner/base/nginx", "1.22")
                                       .WaitFor(resource);

        using var app = builder.Build();

        var pendingStart = app.StartAsync(cts.Token);

        await app.ResourceNotifications.WaitForResourceAsync(resource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await app.ResourceNotifications.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Waiting, cts.Token);

        healthCheckTcs.SetResult(HealthCheckResult.Healthy());

        await app.ResourceNotifications.WaitForResourceHealthyAsync(resource.Resource.Name, cts.Token);

        await app.ResourceNotifications.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await pendingStart;

        await app.StopAsync();
    }

    private static async Task CreateTestData(ElasticsearchClient elasticsearchClient, TestContext testContext, CancellationToken cancellationToken)
    {
        var indexResponse = await elasticsearchClient.IndexAsync<Person>(s_person, IndexName, s_person.Id, cancellationToken);

        var getResponse = await elasticsearchClient.GetAsync<Person>(IndexName, s_person.Id, cancellationToken);

        testContext.WriteLine(indexResponse.DebugInformation);
        testContext.WriteLine(getResponse.DebugInformation);

        Assert.IsTrue(indexResponse.IsSuccess());
        Assert.IsTrue(getResponse.IsSuccess());
        Assert.IsNotNull(getResponse.Source);
        Assert.AreEqual(s_person.Id, getResponse.Source?.Id);
    }

    private sealed class Person
    {
        public int Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
    }

}
