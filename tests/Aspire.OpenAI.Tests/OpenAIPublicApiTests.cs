// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;

namespace Aspire.OpenAI.Tests;

[TestClass]
public class OpenAIPublicApiTests
{
    [TestMethod]
    public void CtorAspireOpenAIClientBuilderShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder hostBuilder = null!;
        const string connectionName = "open-ai";
        const string? serviceKey = null;
        bool disableTracing = false;

        var action = () => new AspireOpenAIClientBuilder(hostBuilder, connectionName, serviceKey, disableTracing);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(hostBuilder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void CtorAspireOpenAIClientBuilderShouldThrowWhenConnectionNameIsNullOrEmpty(bool isNull)
    {
        var hostBuilder = Host.CreateEmptyApplicationBuilder(null);
        var connectionName = isNull ? null! : string.Empty;
        const string? serviceKey = null;
        bool disableTracing = false;

        var action = () => new AspireOpenAIClientBuilder(hostBuilder, connectionName, serviceKey, disableTracing);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(connectionName), exception.ParamName);
    }

    [TestMethod]
    public void AddChatClientShouldThrowWhenBuilderIsNull()
    {
        AspireOpenAIClientBuilder builder = null!;

        var action = () => builder.AddChatClient();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void AddKeyedChatClientShouldThrowWhenBuilderIsNull()
    {
        AspireOpenAIClientBuilder builder = null!;
        const string serviceKey = "chat";

        var action = () => builder.AddKeyedChatClient(serviceKey);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddKeyedChatClientShouldThrowWhenServiceKeyIsNullOrEmpty(bool isNull)
    {
        var hostBuilder = Host.CreateEmptyApplicationBuilder(null);
        const string connectionName = "open-ai";
        const string? hostServiceKey = null;
        bool disableTracing = false;
        var builder = new AspireOpenAIClientBuilder(hostBuilder, connectionName, hostServiceKey, disableTracing);
        var serviceKey = isNull ? null! : string.Empty;

        var action = () => builder.AddKeyedChatClient(serviceKey);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(serviceKey), exception.ParamName);
    }

    [TestMethod]
    public void AddEmbeddingGeneratorShouldThrowWhenBuilderIsNull()
    {
        AspireOpenAIClientBuilder builder = null!;

        var action = () => builder.AddEmbeddingGenerator();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void AddKeyedEmbeddingGeneratorShouldThrowWhenBuilderIsNull()
    {
        AspireOpenAIClientBuilder builder = null!;
        const string serviceKey = "generator";

        var action = () => builder.AddKeyedEmbeddingGenerator(serviceKey);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddKeyedEmbeddingGeneratorShouldThrowWhenServiceKeyIsNullOrEmpty(bool isNull)
    {
        var hostBuilder = Host.CreateEmptyApplicationBuilder(null);
        const string connectionName = "open-ai";
        const string? hostServiceKey = null;
        bool disableTracing = false;
        var builder = new AspireOpenAIClientBuilder(hostBuilder, connectionName, hostServiceKey, disableTracing);
        var serviceKey = isNull ? null! : string.Empty;

        var action = () => builder.AddKeyedEmbeddingGenerator(serviceKey);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(serviceKey), exception.ParamName);
    }

    [TestMethod]
    public void AddOpenAIClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string connectionName = "open-ai";

        var action = () => builder.AddOpenAIClient(connectionName);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddOpenAIClientShouldThrowWhenConnectionNameIsNullOrEmpty(bool isNull)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var connectionName = isNull ? null! : string.Empty;

        var action = () => builder.AddOpenAIClient(connectionName);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(connectionName), exception.ParamName);
    }

    [TestMethod]
    public void AddKeyedOpenAIClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string name = "open-ai";

        var action = () => builder.AddKeyedOpenAIClient(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddKeyedOpenAIClientShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddKeyedOpenAIClient(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }
}
