// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Tests.PublicApiTests;

[TestClass]
public class CognitiveServicesPublicApiTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CtorAzureOpenAIDeploymentShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var name = isNull ? null! : string.Empty;
        const string modelName = "ai";
        const string modelVersion = "1.0";

        var action = () => new AzureOpenAIDeployment(name, modelName, modelVersion);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CtorAzureOpenAIDeploymentShouldThrowWhenModelNameIsNullOrEmpty(bool isNull)
    {
        const string name = "open-ai";
        var modelName = isNull ? null! : string.Empty;
        const string modelVersion = "1.0";

        var action = () => new AzureOpenAIDeployment(name, modelName, modelVersion);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(modelName), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CtorAzureOpenAIDeploymentShouldThrowWhenModelVersionIsNullOrEmpty(bool isNull)
    {
        const string name = "open-ai";
        const string modelName = "ai";
        var modelVersion = isNull ? null! : string.Empty;

        var action = () => new AzureOpenAIDeployment(name, modelName, modelVersion);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(modelVersion), exception.ParamName);
    }

    [TestMethod]
    public void AddAzureOpenAIShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "open-ai";

        var action = () => builder.AddAzureOpenAI(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AddAzureOpenAIShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddAzureOpenAI(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void AddDeploymentShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<AzureOpenAIResource> builder = null!;
        var deployment = new AzureOpenAIDeployment("open-ai", "ai", "1.0");

        var action = () => builder.AddDeployment(deployment);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void AddDeploymentShouldThrowWhenDeploymentIsNull()
    {
        using var testBuilder = TestDistributedApplicationBuilder.Create();
        var builder = testBuilder.AddAzureOpenAI("open-ai");
        AzureOpenAIDeployment deployment = null!;

        var action = () => builder.AddDeployment(deployment);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(deployment), exception.ParamName);
    }
}
