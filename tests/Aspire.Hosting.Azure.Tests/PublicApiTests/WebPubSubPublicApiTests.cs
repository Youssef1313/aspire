// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using static Aspire.Hosting.ApplicationModel.ReferenceExpression;

namespace Aspire.Hosting.Azure.Tests.PublicApiTests;

[TestClass]
public class WebPubSubPublicApiTests
{
    [TestMethod]
    public void AddAzureWebPubSubShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "web-pub-sub";

        var action = () => builder.AddAzureWebPubSub(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AddAzureWebPubSubShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddAzureWebPubSub(name);

        var exception = isNull
           ? Assert.Throws<ArgumentNullException>(action)
           : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void AddHubShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<AzureWebPubSubResource> builder = null!;
        const string hubName = "hub";

        var action = () => builder.AddHub(hubName);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AddHubShouldThrowWhenHubNameIsNullOrEmpty(bool isNull)
    {
        using var testBuilder = TestDistributedApplicationBuilder.Create();
        var builder = testBuilder.AddAzureWebPubSub("web-pub-sub");
        var hubName = isNull ? null! : string.Empty;

        var action = () => builder.AddHub(hubName);

        var exception = isNull
           ? Assert.Throws<ArgumentNullException>(action)
           : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(hubName), exception.ParamName);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public void AddEventHandlerShouldThrowWhenBuilderIsNull(int overrideIndex)
    {
        IResourceBuilder<AzureWebPubSubHubResource> builder = null!;
        var urlExpression = ReferenceExpression.Create($"host");
        const string userEventPattern = "*";

        Action action = overrideIndex switch
        {
            0 => () => builder.AddEventHandler(new ExpressionInterpolatedStringHandler(1, 1), userEventPattern),
            1 => () => builder.AddEventHandler(urlExpression, userEventPattern),
            _ => throw new InvalidOperationException()
        };

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(0, false)]
    [DataRow(0, true)]
    [DataRow(1, false)]
    [DataRow(1, true)]
    public void AddEventHandlerShouldThrowWhenUserEventPatternIsNull(int overrideIndex, bool isNull)
    {
        using var testBuilder = TestDistributedApplicationBuilder.Create();
        var builder = testBuilder.AddAzureWebPubSub("web-pub-sub").AddHub("hub");
        var urlExpression = ReferenceExpression.Create($"host");
        var userEventPattern = isNull ? null! : string.Empty;

        Action action = overrideIndex switch
        {
            0 => () => builder.AddEventHandler(new ExpressionInterpolatedStringHandler(1, 1), userEventPattern),
            1 => () => builder.AddEventHandler(urlExpression, userEventPattern),
            _ => throw new InvalidOperationException()
        };

        var exception = isNull
           ? Assert.Throws<ArgumentNullException>(action)
           : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(userEventPattern), exception.ParamName);
    }

    [TestMethod]
    public void AddEventHandlerShouldThrowWhenUrlExpressionIsNull()
    {
        using var testBuilder = TestDistributedApplicationBuilder.Create();
        var builder = testBuilder.AddAzureWebPubSub("web-pub-sub").AddHub("hub");
        ReferenceExpression urlExpression = null!;

        var action = () => builder.AddEventHandler(urlExpression);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(urlExpression), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CtorAzureWebPubSubHubResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var name = isNull ? null! : string.Empty;
        var webpubsub = new AzureWebPubSubResource("web-pub-sub", (_) => { });

        var action = () => new AzureWebPubSubHubResource(name, webpubsub);

        var exception = isNull
           ? Assert.Throws<ArgumentNullException>(action)
           : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void CtorAzureWebPubSubHubResourceShouldThrowWhenWebPubSubIsNull()
    {
        const string name = "web-pub-sub";
        AzureWebPubSubResource webpubsub = null!;

        var action = () => new AzureWebPubSubHubResource(name, webpubsub);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(webpubsub), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CtorAzureWebPubSubResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var name = isNull ? null! : string.Empty;
        Action<AzureResourceInfrastructure> configureInfrastructure = (_) => { };

        var action = () => new AzureWebPubSubResource(name, configureInfrastructure);

        var exception = isNull
           ? Assert.Throws<ArgumentNullException>(action)
           : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void CtorAzureWebPubSubResourceShouldThrowWhenConfigureInfrastructureIsNull()
    {
        const string name = "web-pub-sub";
        Action<AzureResourceInfrastructure> configureInfrastructure = null!;

        var action = () => new AzureWebPubSubResource(name, configureInfrastructure);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(configureInfrastructure), exception.ParamName);
    }
}
