// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenAI;

namespace Aspire.OpenAI.Tests;

[TestClass]
public class AspireOpenAIExtensionsTests
{
    private const string ConnectionString = "Endpoint=https://api.openai.com/;Key=fake";

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ReadsFromConnectionStringsCorrectly(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:openai", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedOpenAIClient("openai");
        }
        else
        {
            builder.AddOpenAIClient("openai");
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<OpenAIClient>("openai") :
            host.Services.GetRequiredService<OpenAIClient>();

        Assert.IsNotNull(client);
        Assert.IsType<OpenAIClient>(client);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ConnectionStringCanBeSetInCode(bool useKeyed)
    {
        var uri = new Uri("https://api.openai.com/");
        var key = "fake";
        var builder = Host.CreateEmptyApplicationBuilder(null);

        if (useKeyed)
        {
            builder.AddKeyedOpenAIClient("openai", settings => { settings.Endpoint = uri; settings.Key = key; });
        }
        else
        {
            builder.AddOpenAIClient("openai", settings => { settings.Endpoint = uri; settings.Key = key; });
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<OpenAIClient>("openai") :
            host.Services.GetRequiredService<OpenAIClient>();

        Assert.IsNotNull(client);
    }

    [TestMethod]
    [DataRow("Endpoint=http://domain.com:12345;Key=abc123")]
    [DataRow("Key=abc123")]
    public void ReadsFromConnectionStringsFormats(string connectionString)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:openai", connectionString)
        ]);

        builder.AddOpenAIClient("openai");

        using var host = builder.Build();
        var client = host.Services.GetRequiredService<OpenAIClient>();

        Assert.IsNotNull(client);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("Endpoint=http://domain.com:12345")]
    public void ThrowsWhitInvalidConnectionString(string connectionString)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:openai", connectionString)
        ]);

        builder.AddOpenAIClient("openai");

        using var host = builder.Build();

        var exception = Assert.Throws<InvalidOperationException>(host.Services.GetRequiredService<OpenAIClient>);

        Assert.IsNotNull(exception);
        Assert.AreEqual("An OpenAIClient could not be configured. Ensure valid connection information was provided in " +
            "'ConnectionStrings:openai' or specify a Key in the 'Aspire:OpenAI' configuration section.", exception.Message);
    }

    [TestMethod]
    public void CanAddMultipleKeyedServices()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:openai1", ConnectionString),
            new KeyValuePair<string, string?>("ConnectionStrings:openai2", ConnectionString + "2"),
            new KeyValuePair<string, string?>("ConnectionStrings:openai3", ConnectionString + "3")
        ]);

        builder.AddOpenAIClient("openai1");
        builder.AddKeyedOpenAIClient("openai2");
        builder.AddKeyedOpenAIClient("openai3");

        using var host = builder.Build();

        var client1 = host.Services.GetRequiredService<OpenAIClient>();
        var client2 = host.Services.GetRequiredKeyedService<OpenAIClient>("openai2");
        var client3 = host.Services.GetRequiredKeyedService<OpenAIClient>("openai3");

        Assert.AreNotSame(client1, client2);
        Assert.AreNotSame(client1, client3);
        Assert.AreNotSame(client2, client3);
    }

    [TestMethod]
    public void BindsSettingsAndInvokesCallback()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:openai", ConnectionString),
            new KeyValuePair<string, string?>("Aspire:OpenAI:DisableTracing", "true")
        ]);

        OpenAISettings? localSettings = null;

        builder.AddOpenAIClient("openai", settings =>
        {
            settings.DisableMetrics = true;
            localSettings = settings;
        });

        Assert.IsNotNull(localSettings);
        Assert.IsTrue(localSettings.DisableMetrics);
        Assert.IsTrue(localSettings.DisableTracing);
    }

    [TestMethod]
    public void BindsOptionsAndInvokesCallback()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:openai", ConnectionString),
            new KeyValuePair<string, string?>("Aspire:OpenAI:ClientOptions:ProjectId", "myproject")
        ]);

        builder.AddOpenAIClient("openai", configureOptions: options =>
        {
            options.UserAgentApplicationId = "myapplication";
        });

        using var host = builder.Build();

        var options = host.Services.GetRequiredService<IOptions<OpenAIClientOptions>>().Value;

        Assert.IsNotNull(options);
        Assert.AreEqual("myproject", options.ProjectId);
        Assert.AreEqual("myapplication", options.UserAgentApplicationId);
    }

    [TestMethod]
    public void BindsToNamedClientOptions()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("Aspire:OpenAI:ClientOptions:ProjectId", "myproject"),
            new KeyValuePair<string, string?>("Aspire:OpenAI:openai:ClientOptions:ProjectId", "myproject2")
        ]);

        builder.AddOpenAIClient("openai");

        using var host = builder.Build();

        var options = host.Services.GetRequiredService<IOptions<OpenAIClientOptions>>().Value;

        Assert.IsNotNull(options);
        Assert.AreEqual("myproject2", options.ProjectId);
    }
}
