// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.ConformanceTests;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.OpenAI.Tests;

[TestClass]
public class AspireOpenAIClientBuilderChatClientExtensionsTests
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void CanReadDeploymentNameFromConfig(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new ("Aspire:OpenAI:Endpoint", "https://aspireopenaitests.openai.azure.com/"),
            new ("Aspire:OpenAI:Deployment", "testdeployment1"),
            new ("Aspire:OpenAI:Key", "fake"),
        ]);

        if (useKeyed)
        {
            builder.AddOpenAIClient("openai").AddKeyedChatClient("openai_chatclient");
        }
        else
        {
            builder.AddOpenAIClient("openai").AddChatClient();
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<IChatClient>("openai_chatclient") :
            host.Services.GetRequiredService<IChatClient>();

        Assert.IsNotNull(client);
        Assert.AreEqual("testdeployment1", client.GetService<ChatClientMetadata>()?.ModelId);
    }

    [TestMethod]
    [DataRow(true, "Model")]
    [DataRow(false, "Model")]
    [DataRow(true, "Deployment")]
    [DataRow(false, "Deployment")]
    public void CanReadDeploymentNameFromConnectionString(bool useKeyed, string connectionStringKey)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new("ConnectionStrings:openai", $"Endpoint=https://aspireopenaitests.openai.azure.com/;Key=fake;{connectionStringKey}=testdeployment1")
        ]);

        if (useKeyed)
        {
            builder.AddOpenAIClient("openai").AddKeyedChatClient("openai_chatclient");
        }
        else
        {
            builder.AddOpenAIClient("openai").AddChatClient();
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<IChatClient>("openai_chatclient") :
            host.Services.GetRequiredService<IChatClient>();

        Assert.IsNotNull(client);
        Assert.AreEqual("testdeployment1", client.GetService<ChatClientMetadata>()?.ModelId);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void CanAcceptDeploymentNameAsArgument(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new("ConnectionStrings:openai", $"Endpoint=https://aspireopenaitests.openai.azure.com/;Key=fake")
        ]);

        if (useKeyed)
        {
            builder.AddOpenAIClient("openai").AddKeyedChatClient("openai_chatclient", "testdeployment1");
        }
        else
        {
            builder.AddOpenAIClient("openai").AddChatClient("testdeployment1");
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<IChatClient>("openai_chatclient") :
            host.Services.GetRequiredService<IChatClient>();

        Assert.IsNotNull(client);
        Assert.AreEqual("testdeployment1", client.GetService<ChatClientMetadata>()?.ModelId);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void RejectsConnectionStringWithBothModelAndDeployment(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new("ConnectionStrings:openai", $"Endpoint=https://aspireopenaitests.openai.azure.com/;Key=fake;Deployment=testdeployment1;Model=something")
        ]);

        if (useKeyed)
        {
            builder.AddOpenAIClient("openai").AddKeyedChatClient("openai_chatclient");
        }
        else
        {
            builder.AddOpenAIClient("openai").AddChatClient();
        }

        using var host = builder.Build();

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = useKeyed ?
                host.Services.GetRequiredKeyedService<IChatClient>("openai_chatclient") :
                host.Services.GetRequiredService<IChatClient>();
        });

        Assert.StartsWith("The connection string 'openai' contains both 'Deployment' and 'Model' keys.", ex.Message);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void RejectsDeploymentNameNotSpecified(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new("ConnectionStrings:openai", $"Endpoint=https://aspireopenaitests.openai.azure.com/;Key=fake")
        ]);

        if (useKeyed)
        {
            builder.AddOpenAIClient("openai").AddKeyedChatClient("openai_chatclient");
        }
        else
        {
            builder.AddOpenAIClient("openai").AddChatClient();
        }

        using var host = builder.Build();

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = useKeyed ?
                host.Services.GetRequiredKeyedService<IChatClient>("openai_chatclient") :
                host.Services.GetRequiredService<IChatClient>();
        });

        Assert.StartsWith("The deployment could not be determined", ex.Message);
    }

    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, false)]
    [DataRow(true, true)]
    [DataRow(false, true)]
    public void AddsOpenTelemetry(bool useKeyed, bool disableOpenTelemetry)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new("ConnectionStrings:openai", $"Endpoint=https://aspireopenaitests.openai.azure.com/;Key=fake"),
            new("Aspire:OpenAI:DisableTracing", disableOpenTelemetry.ToString()),
        ]);

        if (useKeyed)
        {
            builder.AddOpenAIClient("openai").AddKeyedChatClient("openai_chatclient", "testdeployment1");
        }
        else
        {
            builder.AddOpenAIClient("openai").AddChatClient("testdeployment1");
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<IChatClient>("openai_chatclient") :
            host.Services.GetRequiredService<IChatClient>();

        Assert.AreEqual(disableOpenTelemetry, client.GetService<OpenTelemetryChatClient>() is null);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task CanConfigurePipelineAsync(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new("ConnectionStrings:openai", $"Endpoint=https://aspireopenaitests.openai.azure.com/;Key=fake")
        ]);

        if (useKeyed)
        {
            builder.AddOpenAIClient("openai").AddKeyedChatClient("openai_chatclient", "testdeployment1").Use(TestMiddleware, null);
        }
        else
        {
            builder.AddOpenAIClient("openai").AddChatClient("testdeployment1").Use(TestMiddleware, null);
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<IChatClient>("openai_chatclient") :
            host.Services.GetRequiredService<IChatClient>();

        var completion = await client.GetResponseAsync("Whatever");
        Assert.AreEqual("Hello from middleware", completion.Text);
    }

    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, false)]
    [DataRow(true, true)]
    [DataRow(false, true)]
    public async Task LogsCorrectly(bool useKeyed, bool disableOpenTelemetry)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new("ConnectionStrings:openai", $"Endpoint=https://aspireopenaitests.openai.azure.com/;Key=fake"),
            new("Aspire:OpenAI:DisableTracing", disableOpenTelemetry.ToString()),
        ]);

        builder.Services.AddSingleton<ILoggerFactory, TestLoggerFactory>();

        if (useKeyed)
        {
            builder.AddOpenAIClient("openai").AddKeyedChatClient("openai_chatclient", "testdeployment1").Use(TestMiddleware, null);
        }
        else
        {
            builder.AddOpenAIClient("openai").AddChatClient("testdeployment1").Use(TestMiddleware, null);
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<IChatClient>("openai_chatclient") :
            host.Services.GetRequiredService<IChatClient>();
        var loggerFactory = (TestLoggerFactory)host.Services.GetRequiredService<ILoggerFactory>();

        var completion = await client.GetResponseAsync("Whatever");
        Assert.AreEqual("Hello from middleware", completion.Text);

        const string category = "Microsoft.Extensions.AI.OpenTelemetryChatClient";
        if (disableOpenTelemetry)
        {
            Assert.DoesNotContain(category, loggerFactory.Categories);
        }
        else
        {
            Assert.Contains(category, loggerFactory.Categories);
        }
    }

    private static Task<ChatResponse> TestMiddleware(IEnumerable<ChatMessage> list, ChatOptions? options, IChatClient client, CancellationToken token)
        => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello from middleware")));
}
