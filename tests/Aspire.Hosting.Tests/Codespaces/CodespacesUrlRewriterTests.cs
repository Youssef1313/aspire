// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Tests.Codespaces;

[TestClass]
public class CodespacesUrlRewriterTests(TestContext testContext)
{
    [TestMethod]
    public async Task VerifyUrlsRewriterStopsWhenNotInCodespaces()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testContext);

        // Explicitly disable codespace behavior for this test.
        builder.Configuration["CODESPACES"] = "false";

        builder.Services.AddLogging(logging =>
        {
            logging.AddFakeLogging();
            logging.AddMSTest(testContext);
        });

        var resource = builder.AddResource(new CustomResource("resource"));

        var abortToken = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        using var app = builder.Build();

        await app.StartAsync(abortToken.Token);

        var collector = app.Services.GetFakeLogCollector();

        var urlRewriterStopped = false;

        while (!abortToken.Token.IsCancellationRequested)
        {
            var logs = collector.GetSnapshot();
            urlRewriterStopped = logs.Any(l => l.Message.Contains("Not running in Codespaces, skipping URL rewriting."));
            if (urlRewriterStopped)
            {
                break;
            }
        }

        Assert.IsTrue(urlRewriterStopped);

        await app.StopAsync(abortToken.Token);
    }

    [TestMethod]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/6648")]
    public async Task VerifyUrlsRewrittenWhenInCodespaces()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testContext);

        builder.Configuration["CODESPACES"] = "true";
        builder.Configuration["GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN"] = "app.github.dev";
        builder.Configuration["CODESPACE_NAME"] = "test-codespace";

        var resource = builder.AddResource(new CustomResource("resource"));

        var abortToken = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        using var app = builder.Build();

        await app.StartAsync(abortToken.Token);

        // Push the URL to the resource state.
        var localhostUrlSnapshot = new UrlSnapshot("Test", "http://localhost:1234", false);
        await app.ResourceNotifications.PublishUpdateAsync(resource.Resource, s => s with
        {
            State = KnownResourceStates.Running,
            Urls = [localhostUrlSnapshot]
        });

        // Wait until
        var resourceEvent = await app.ResourceNotifications.WaitForResourceAsync(
            resource.Resource.Name,
            (re) => {
                var match = re.Snapshot.Urls.Length > 0 && re.Snapshot.Urls[0].Url.Contains("app.github.dev");
                return match;
            },
            abortToken.Token);

        Assert.That.Collection(
            resourceEvent.Snapshot.Urls,
            u =>
            {
                Assert.AreEqual("Test", u.Name);
                Assert.AreEqual("http://test-codespace-1234.app.github.dev/", u.Url);
                Assert.IsFalse(u.IsInternal);
            }
            );

        await app.StopAsync(abortToken.Token);
    }

    private sealed class CustomResource(string name) : Resource(name)
    {
    }
}
