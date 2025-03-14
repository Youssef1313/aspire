// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dashboard;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Tests;

[TestClass]
public class ResourceCommandAnnotationTests
{
    [TestMethod]
    [DataRow(KnownResourceCommands.StartCommand, "Starting", ResourceCommandState.Disabled)]
    [DataRow(KnownResourceCommands.StartCommand, "Stopping", ResourceCommandState.Hidden)]
    [DataRow(KnownResourceCommands.StartCommand, "Running", ResourceCommandState.Hidden)]
    [DataRow(KnownResourceCommands.StartCommand, "Exited", ResourceCommandState.Enabled)]
    [DataRow(KnownResourceCommands.StartCommand, "Finished", ResourceCommandState.Enabled)]
    [DataRow(KnownResourceCommands.StartCommand, "FailedToStart", ResourceCommandState.Enabled)]
    [DataRow(KnownResourceCommands.StartCommand, "Unknown", ResourceCommandState.Enabled)]
    [DataRow(KnownResourceCommands.StartCommand, "Waiting", ResourceCommandState.Enabled)]
    [DataRow(KnownResourceCommands.StartCommand, "RuntimeUnhealthy", ResourceCommandState.Disabled)]
    [DataRow(KnownResourceCommands.StopCommand, "Starting", ResourceCommandState.Hidden)]
    [DataRow(KnownResourceCommands.StopCommand, "Stopping", ResourceCommandState.Disabled)]
    [DataRow(KnownResourceCommands.StopCommand, "Running", ResourceCommandState.Enabled)]
    [DataRow(KnownResourceCommands.StopCommand, "Exited", ResourceCommandState.Hidden)]
    [DataRow(KnownResourceCommands.StopCommand, "Finished", ResourceCommandState.Hidden)]
    [DataRow(KnownResourceCommands.StopCommand, "FailedToStart", ResourceCommandState.Hidden)]
    [DataRow(KnownResourceCommands.StopCommand, "Unknown", ResourceCommandState.Hidden)]
    [DataRow(KnownResourceCommands.StopCommand, "Waiting", ResourceCommandState.Hidden)]
    [DataRow(KnownResourceCommands.StopCommand, "RuntimeUnhealthy", ResourceCommandState.Hidden)]
    [DataRow(KnownResourceCommands.RestartCommand, "Starting", ResourceCommandState.Disabled)]
    [DataRow(KnownResourceCommands.RestartCommand, "Stopping", ResourceCommandState.Disabled)]
    [DataRow(KnownResourceCommands.RestartCommand, "Running", ResourceCommandState.Enabled)]
    [DataRow(KnownResourceCommands.RestartCommand, "Exited", ResourceCommandState.Disabled)]
    [DataRow(KnownResourceCommands.RestartCommand, "Finished", ResourceCommandState.Disabled)]
    [DataRow(KnownResourceCommands.RestartCommand, "FailedToStart", ResourceCommandState.Disabled)]
    [DataRow(KnownResourceCommands.RestartCommand, "Unknown", ResourceCommandState.Disabled)]
    [DataRow(KnownResourceCommands.RestartCommand, "Waiting", ResourceCommandState.Disabled)]
    [DataRow(KnownResourceCommands.RestartCommand, "RuntimeUnhealthy", ResourceCommandState.Disabled)]
    public void LifeCycleCommands_CommandState(string commandName, string resourceState, ResourceCommandState commandState)
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var resourceBuilder = builder.AddContainer("name", "image");
        resourceBuilder.Resource.AddLifeCycleCommands();

        var startCommand = resourceBuilder.Resource.Annotations.OfType<ResourceCommandAnnotation>().Single(a => a.Name == commandName);

        // Act
        var state = startCommand.UpdateState(new UpdateCommandStateContext
        {
            ResourceSnapshot = new CustomResourceSnapshot
            {
                Properties = [],
                ResourceType = "test",
                State = resourceState
            },
            ServiceProvider = new ServiceCollection().BuildServiceProvider()
        });

        // Assert
        Assert.AreEqual(commandState, state);
    }
}
