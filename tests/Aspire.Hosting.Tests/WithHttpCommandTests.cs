// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Aspire.Hosting.Tests;

[TestClass]
public class WithHttpCommandTests(TestContext testContext)
{
    [TestMethod]
    public void WithHttpCommand_AddsResourceCommandAnnotation_WithDefaultValues()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(testContext);
        var resourceBuilder = builder.AddContainer("name", "image")
            .WithHttpEndpoint()
            .WithHttpCommand("/some-path", "Do The Thing");

        // Act
        var command = resourceBuilder.Resource.Annotations.OfType<ResourceCommandAnnotation>().FirstOrDefault();

        // Assert
        Assert.IsNotNull(command);
        Assert.AreEqual("Do The Thing", command.DisplayName);
        // Expected name format: "{endpoint.Resource.Name}-{endpoint.EndpointName}-http-{httpMethod}"
        Assert.AreEqual($"{resourceBuilder.Resource.Name}-http-http-post-/some-path", command.Name);
        Assert.IsNull(command.DisplayDescription);
        Assert.IsNull(command.ConfirmationMessage);
        Assert.IsNull(command.IconName);
        Assert.IsNull(command.IconVariant);
        Assert.IsFalse(command.IsHighlighted);
    }

    [TestMethod]
    public void WithHttpCommand_AddsResourceCommandAnnotation_WithCustomValues()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(testContext);
        var resourceBuilder = builder.AddContainer("name", "image")
            .WithHttpEndpoint()
            .WithHttpCommand("/some-path", "Do The Thing",
                commandName: "my-command-name",
                displayDescription: "Command description",
                confirmationMessage: "Are you sure?",
                iconName: "DatabaseLightning",
                iconVariant: IconVariant.Filled,
                isHighlighted: true);

        // Act
        var command = resourceBuilder.Resource.Annotations.OfType<ResourceCommandAnnotation>().FirstOrDefault();

        // Assert
        Assert.IsNotNull(command);
        Assert.AreEqual("Do The Thing", command.DisplayName);
        Assert.AreEqual("my-command-name", command.Name);
        Assert.AreEqual("Command description", command.DisplayDescription);
        Assert.AreEqual("Are you sure?", command.ConfirmationMessage);
        Assert.AreEqual("DatabaseLightning", command.IconName);
        Assert.AreEqual(IconVariant.Filled, command.IconVariant);
        Assert.IsTrue(command.IsHighlighted);
    }

    [TestMethod]
    public void WithHttpCommand_AddsResourceCommandAnnotations_WithUniqueCommandNames()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(testContext);
        var resourceBuilder = builder.AddContainer("name", "image")
            .WithHttpEndpoint()
            .WithHttpEndpoint(name: "custom-endpoint")
            .WithHttpCommand("/some-path", "Do The Thing")
            .WithHttpCommand("/some-path", "Do The Thing", endpointName: "custom-endpoint")
            .WithHttpCommand("/some-path", "Do The Get Thing", method: HttpMethod.Get)
            .WithHttpCommand("/some-path", "Do The Get Thing", method: HttpMethod.Get, endpointName: "custom-endpoint")
            .WithHttpCommand("/some-other-path", "Do The Other Thing")
            // Call it again but just change display name, it should override the previous one with the same path
            .WithHttpCommand("/some-other-path", "Do The Other Thing CHANGED");

        // Act
        var commands = resourceBuilder.Resource.Annotations.OfType<ResourceCommandAnnotation>().ToList();
        var command1 = commands.FirstOrDefault(c => c.DisplayName == "Do The Thing");
        var command2 = commands.FirstOrDefault(c => c.DisplayName == "Do The Thing" && c.Name.Contains("custom-endpoint"));
        var command3 = commands.FirstOrDefault(c => c.DisplayName == "Do The Get Thing");
        var command4 = commands.FirstOrDefault(c => c.DisplayName == "Do The Get Thing" && c.Name.Contains("custom-endpoint"));
        var command5 = commands.FirstOrDefault(c => c.DisplayName == "Do The Other Thing");
        var command6 = commands.FirstOrDefault(c => c.DisplayName == "Do The Other Thing CHANGED");

        // Assert
        Assert.IsTrue(commands.Count >= 5);
        Assert.IsNotNull(command1);
        Assert.IsNotNull(command2);
        Assert.IsNotNull(command3);
        Assert.IsNotNull(command4);
        Assert.IsNull(command5); // This one is overridden by the last one
        Assert.IsNotNull(command6);
    }

    [DataRow(200, true)]
    [DataRow(201, true)]
    [DataRow(400, false)]
    [DataRow(401, false)]
    [DataRow(403, false)]
    [DataRow(404, false)]
    [DataRow(500, false)]
    [TestMethod]
    public async Task WithHttpCommand_ResultsInExpectedResultForStatusCode(int statusCode, bool expectSuccess)
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(testContext);
        var resourceBuilder = builder.AddProject<Projects.ServiceA>("servicea")
            .WithHttpCommand($"/status/{statusCode}", "Do The Thing", commandName: "mycommand");
        var command = resourceBuilder.Resource.Annotations.OfType<ResourceCommandAnnotation>().First(c => c.Name == "mycommand");

        // Act
        var app = builder.Build();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("servicea").DefaultTimeout(TestConstants.LongTimeoutTimeSpan);

        var context = new ExecuteCommandContext
        {
            ResourceName = resourceBuilder.Resource.Name,
            ServiceProvider = app.Services,
            CancellationToken = CancellationToken.None
        };
        var result = await command.ExecuteCommand(context);

        // Assert
        Assert.AreEqual(expectSuccess, result.Success);
    }

    [DataRow(null, false)] // Default method is POST
    [DataRow("get", true)]
    [DataRow("post", false)]
    [TestMethod]
    public async Task WithHttpCommand_ResultsInExpectedResultForHttpMethod(string? httpMethod, bool expectSuccess)
    {
        // Arrange
        var method = httpMethod is not null ? new HttpMethod(httpMethod) : null;
        using var builder = TestDistributedApplicationBuilder.Create(testContext);
        var resourceBuilder = builder.AddProject<Projects.ServiceA>("servicea")
            .WithHttpCommand("/get-only", "Do The Thing", method: method, commandName: "mycommand");
        var command = resourceBuilder.Resource.Annotations.OfType<ResourceCommandAnnotation>().First(c => c.Name == "mycommand");

        // Act
        var app = builder.Build();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("servicea").DefaultTimeout(TestConstants.LongTimeoutTimeSpan);

        var context = new ExecuteCommandContext
        {
            ResourceName = resourceBuilder.Resource.Name,
            ServiceProvider = app.Services,
            CancellationToken = CancellationToken.None
        };
        var result = await command.ExecuteCommand(context);

        // Assert
        Assert.AreEqual(expectSuccess, result.Success);
    }

    [TestMethod]
    public async Task WithHttpCommand_UsesNamedHttpClient()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(testContext);
        var trackingMessageHandler = new TrackingHttpMessageHandler();
        builder.Services.AddHttpClient("commandclient")
            .AddHttpMessageHandler((sp) => trackingMessageHandler);
        var resourceBuilder = builder.AddProject<Projects.ServiceA>("servicea")
            .WithHttpCommand("/get-only", "Do The Thing", commandName: "mycommand", httpClientName: "commandclient");
        var command = resourceBuilder.Resource.Annotations.OfType<ResourceCommandAnnotation>().First(c => c.Name == "mycommand");

        // Act
        var app = builder.Build();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("servicea").DefaultTimeout(TestConstants.LongTimeoutTimeSpan);

        var context = new ExecuteCommandContext
        {
            ResourceName = resourceBuilder.Resource.Name,
            ServiceProvider = app.Services,
            CancellationToken = CancellationToken.None
        };
        var result = await command.ExecuteCommand(context);

        // Assert
        Assert.IsTrue(trackingMessageHandler.Called);
    }

    private sealed class TrackingHttpMessageHandler : DelegatingHandler
    {
        public bool Called { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Called = true;
            return base.SendAsync(request, cancellationToken);
        }
    }

    [TestMethod]
    public async Task WithHttpCommand_UsesEndpointSelector()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(testContext);

        var serviceA = builder.AddProject<Projects.ServiceA>("servicea");
        var callbackCalled = false;
        var serviceB = builder.AddProject<Projects.ServiceA>("serviceb")
            .WithHttpCommand("/status/200", "Do The Thing", commandName: "mycommand",
            endpointSelector: () =>
            {
                callbackCalled = true;
                return serviceA.GetEndpoint("http");
            });
        var command = serviceB.Resource.Annotations.OfType<ResourceCommandAnnotation>().First(c => c.Name == "mycommand");

        // Act
        var app = builder.Build();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("servicea").DefaultTimeout(TestConstants.LongTimeoutTimeSpan);

        var context = new ExecuteCommandContext
        {
            ResourceName = serviceB.Resource.Name,
            ServiceProvider = app.Services,
            CancellationToken = CancellationToken.None
        };
        var result = await command.ExecuteCommand(context);

        // Assert
        Assert.IsTrue(callbackCalled);
    }

    [TestMethod]
    public async Task WithHttpCommand_CallsPrepareRequestCallback_BeforeSendingRequest()
    {
        // Arrange
        var callbackCalled = false;
        using var builder = TestDistributedApplicationBuilder.Create(testContext);
        var resourceBuilder = builder.AddProject<Projects.ServiceA>("servicea")
            .WithHttpCommand("/status/200", "Do The Thing",
                commandName: "mycommand",
                configureRequest: requestContext =>
                {
                    Assert.IsNotNull(requestContext);
                    Assert.IsNotNull(requestContext.ServiceProvider);
                    Assert.AreEqual("servicea", requestContext.ResourceName);
                    Assert.IsNotNull(requestContext.Endpoint);
                    Assert.IsNotNull(requestContext.HttpClient);
                    Assert.IsNotNull(requestContext.Request);
                    
                    callbackCalled = true;
                    return Task.CompletedTask;
                });
        var command = resourceBuilder.Resource.Annotations.OfType<ResourceCommandAnnotation>().First(c => c.Name == "mycommand");

        // Act
        var app = builder.Build();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("servicea").DefaultTimeout(TestConstants.LongTimeoutTimeSpan);

        var context = new ExecuteCommandContext
        {
            ResourceName = resourceBuilder.Resource.Name,
            ServiceProvider = app.Services,
            CancellationToken = CancellationToken.None
        };
        var result = await command.ExecuteCommand(context);

        // Assert
        Assert.IsTrue(callbackCalled);
        Assert.IsTrue(result.Success);
    }

    [TestMethod]
    public async Task WithHttpCommand_CallsGetResponseCallback_AfterSendingRequest()
    {
        // Arrange
        var callbackCalled = false;
        using var builder = TestDistributedApplicationBuilder.Create(testContext);
        var resourceBuilder = builder.AddProject<Projects.ServiceA>("servicea")
            .WithHttpCommand("/status/200", "Do The Thing",
                commandName: "mycommand",
                getCommandResult: resultContext =>
                {
                    Assert.IsNotNull(resultContext);
                    Assert.IsNotNull(resultContext.ServiceProvider);
                    Assert.AreEqual("servicea", resultContext.ResourceName);
                    Assert.IsNotNull(resultContext.Endpoint);
                    Assert.IsNotNull(resultContext.HttpClient);
                    Assert.IsNotNull(resultContext.Response);

                    callbackCalled = true;
                    return Task.FromResult(CommandResults.Failure("A test error message"));
                });
        var command = resourceBuilder.Resource.Annotations.OfType<ResourceCommandAnnotation>().First(c => c.Name == "mycommand");

        // Act
        var app = builder.Build();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("servicea").DefaultTimeout(TestConstants.LongTimeoutTimeSpan);

        var context = new ExecuteCommandContext
        {
            ResourceName = resourceBuilder.Resource.Name,
            ServiceProvider = app.Services,
            CancellationToken = CancellationToken.None
        };
        var result = await command.ExecuteCommand(context);

        // Assert
        Assert.IsTrue(callbackCalled);
        Assert.IsFalse(result.Success);
        Assert.AreEqual("A test error message", result.ErrorMessage);
    }

    [TestMethod]
    public async Task WithHttpCommand_EnablesCommandOnceResourceIsRunning()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(testContext);

        builder.Configuration["CODESPACES"] = "false";

        var service = builder.AddResource(new CustomResource("service"))
            .WithHttpEndpoint()
            .WithHttpCommand("/dothing", "Do The Thing", commandName: "mycommand");

        using var app = builder.Build();
        ResourceCommandState? commandState = null;
        var watchTcs = new TaskCompletionSource();
        var watchCts = new CancellationTokenSource();
        var watchTask = Task.Run(async () =>
        {
            await foreach (var resourceEvent in app.ResourceNotifications.WatchAsync(watchCts.Token).WithCancellation(watchCts.Token))
            {
                var commandSnapshot = resourceEvent.Snapshot.Commands.First(c => c.Name == "mycommand");
                commandState = commandSnapshot.State;
                if (commandState == ResourceCommandState.Enabled)
                {
                    watchTcs.TrySetResult();
                }
            }
        }, watchCts.Token);

        // Act/Assert
        await app.StartAsync();

        // Move the resource to the starting state
        await app.ResourceNotifications.PublishUpdateAsync(service.Resource, s => s with
        {
            State = KnownResourceStates.Starting
        });
        await app.ResourceNotifications.WaitForResourceAsync(service.Resource.Name, KnownResourceStates.Starting).DefaultTimeout(TestConstants.LongTimeoutTimeSpan);

        // Veriy the command is disabled
        Assert.AreEqual(ResourceCommandState.Disabled, commandState);

        // Move the resource to the running state
        await app.ResourceNotifications.PublishUpdateAsync(service.Resource, s => s with
        {
            State = KnownResourceStates.Running
        });
        await app.ResourceNotifications.WaitForResourceAsync(service.Resource.Name, KnownResourceStates.Running).DefaultTimeout(TestConstants.LongTimeoutTimeSpan);
        await watchTcs.Task.DefaultTimeout(TestConstants.LongTimeoutTimeSpan);

        // Verify the command is enabled
        Assert.AreEqual(ResourceCommandState.Enabled, commandState);

        // Clean up
        watchCts.Cancel();
        await app.StopAsync();
    }

    [TestMethod]
    public async Task WithHttpCommand_EnablesCommandUsingCustomUpdateStateCallback()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(testContext);
        
        builder.Configuration["CODESPACES"] = "false";

        var enableCommand = false;
        var callbackCalled = false;
        var service = builder.AddResource(new CustomResource("service"))
            .WithHttpEndpoint()
            .WithHttpCommand("/dothing", "Do The Thing", commandName: "mycommand",
                updateState: usc =>
                {
                    callbackCalled = true;
                    return enableCommand ? ResourceCommandState.Enabled : ResourceCommandState.Hidden;
                });

        using var app = builder.Build();
        ResourceCommandState? commandState = null;
        var watchTcs = new TaskCompletionSource();
        var watchCts = new CancellationTokenSource();
        var watchTask = Task.Run(async () =>
        {
            await foreach (var resourceEvent in app.ResourceNotifications.WatchAsync(watchCts.Token).WithCancellation(watchCts.Token))
            {
                var commandSnapshot = resourceEvent.Snapshot.Commands.First(c => c.Name == "mycommand");
                commandState = commandSnapshot.State;
                if (commandState == ResourceCommandState.Enabled)
                {
                    watchTcs.TrySetResult();
                }
            }
        }, watchCts.Token);

        // Act/Assert
        await app.StartAsync();

        // Move the resource to the running state
        await app.ResourceNotifications.PublishUpdateAsync(service.Resource, s => s with
        {
            State = KnownResourceStates.Running
        });
        await app.ResourceNotifications.WaitForResourceAsync(service.Resource.Name, KnownResourceStates.Running).DefaultTimeout(TestConstants.LongTimeoutTimeSpan);

        // Veriy the command is hidden despite the resource being running
        Assert.AreEqual(ResourceCommandState.Hidden, commandState);

        // Publish an update to force reevaluation of the command state
        enableCommand = true;
        await app.ResourceNotifications.PublishUpdateAsync(service.Resource, s => s with
        {
            State = KnownResourceStates.Running
        });
        await watchTcs.Task.DefaultTimeout(TestConstants.LongTimeoutTimeSpan);

        // Verify the callback was called and the command is enabled
        Assert.IsTrue(callbackCalled);
        Assert.AreEqual(ResourceCommandState.Enabled, commandState);

        // Clean up
        watchCts.Cancel();
        await app.StopAsync();
    }

    private sealed class CustomResource(string name) : Resource(name), IResourceWithEndpoints, IResourceWithWaitSupport
    {

    }
}
