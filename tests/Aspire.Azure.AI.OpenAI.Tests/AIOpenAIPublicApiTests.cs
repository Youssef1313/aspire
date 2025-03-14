// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;

namespace Aspire.Azure.AI.OpenAI.Tests;

[TestClass]
public class AIOpenAIPublicApiTests
{
    [TestMethod]
    public void CtorAspireAzureOpenAIClientBuilderShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder hostBuilder = null!;
        const string connectionName = "openai";
        const string? serviceKey = null;
        const bool disableTracing = false;

        Action action = () => new AspireAzureOpenAIClientBuilder(hostBuilder, connectionName, serviceKey, disableTracing);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(hostBuilder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void CtorAspireAzureOpenAIClientBuilderShouldThrowWhenConnectionNameIsNullOrEmpty(bool isNull)
    {
        var hostBuilder = Host.CreateEmptyApplicationBuilder(null);
        var connectionName = isNull ? null! : string.Empty;
        const string? serviceKey = null;
        const bool disableTracing = false;

        Action action = () => new AspireAzureOpenAIClientBuilder(hostBuilder, connectionName, serviceKey, disableTracing);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(connectionName), exception.ParamName);
    }

    [TestMethod]
    public void AddAzureOpenAIClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string connectionName = "openai";

        Action action = () => builder.AddAzureOpenAIClient(connectionName);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddAzureOpenAIClientShouldThrowWhenConnectionNameIsNullOrEmpty(bool isNull)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var connectionName = isNull ? null! : string.Empty;

        Action action = () => builder.AddAzureOpenAIClient(connectionName);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(connectionName), exception.ParamName);
    }

    [TestMethod]
    public void AddKeyedAzureOpenAIClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string name = "openai";

        Action action = () => builder.AddKeyedAzureOpenAIClient(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddKeyedAzureOpenAIClientShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var name = isNull ? null! : string.Empty;

        Action action = () => builder.AddKeyedAzureOpenAIClient(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void AddOpenAIClientFromConfigurationShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string connectionName = "openai";

        Action action = () => builder.AddOpenAIClientFromConfiguration(connectionName);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddOpenAIClientFromConfigurationShouldThrowWhenConnectionNameIsNullOrEmpty(bool isNull)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var connectionName = isNull ? null! : string.Empty;

        Action action = () => builder.AddOpenAIClientFromConfiguration(connectionName);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action); ;
        Assert.AreEqual(nameof(connectionName), exception.ParamName);
    }

    [TestMethod]
    public void AddKeyedOpenAIClientFromConfigurationShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string name = "openai";

        Action action = () => builder.AddKeyedOpenAIClientFromConfiguration(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddKeyedOpenAIClientFromConfigurationShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var name = isNull ? null! : string.Empty;

        Action action = () => builder.AddKeyedOpenAIClientFromConfiguration(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action); ;
        Assert.AreEqual(nameof(name), exception.ParamName);
    }
}
