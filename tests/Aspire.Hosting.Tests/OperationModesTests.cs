// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Aspire.Hosting.Tests;

[TestClass]
public class OperationModesTests(TestContext outputHelper)
{
    [TestMethod]
    public async Task VerifyBackwardsCompatableRunModeInvocation()
    {
        // The purpose of this test is to verify that the apphost executable will continue
        // to enter run mode if executed without any arguments.

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        
        var tcs = new TaskCompletionSource<DistributedApplicationExecutionContext>();
        builder.Eventing.Subscribe<AfterResourcesCreatedEvent>((e, ct) => {
            var context = e.Services.GetRequiredService<DistributedApplicationExecutionContext>();
            tcs.SetResult(context);
            return Task.CompletedTask;
        });

        using var app = builder.Build();
        
        await app.StartAsync().WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        var context = await tcs.Task.WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        await app.StopAsync().WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        Assert.AreEqual(DistributedApplicationOperation.Run, context.Operation);
        Assert.IsTrue(context.IsRunMode);
    }

    [TestMethod]
    public async Task VerifyExplicitRunModeInvocation()
    {
        // The purpose of this test is to verify that the apphost executable will enter
        // run mode if executed with the "--operation run" argument.

        using var builder = TestDistributedApplicationBuilder
            .Create(["--operation", "run"])
            .WithTestAndResourceLogging(outputHelper);
        
        var tcs = new TaskCompletionSource<DistributedApplicationExecutionContext>();
        builder.Eventing.Subscribe<AfterResourcesCreatedEvent>((e, ct) => {
            var context = e.Services.GetRequiredService<DistributedApplicationExecutionContext>();
            tcs.SetResult(context);
            return Task.CompletedTask;
        });

        using var app = builder.Build();
        
        await app.StartAsync().WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        var context = await tcs.Task.WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        await app.StopAsync().WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        Assert.AreEqual(DistributedApplicationOperation.Run, context.Operation);
        Assert.IsTrue(context.IsRunMode);
    }

    [TestMethod]
    public async Task VerifyExplicitRunModeWithPublisherInvocation()
    {
        // The purpose of this test is to verify that the apphost executable will enter
        // run mode if executed with the "--operation run" argument.

        using var builder = TestDistributedApplicationBuilder
            .Create(["--operation", "run", "--publisher", "manifest"])
            .WithTestAndResourceLogging(outputHelper);
        
        var tcs = new TaskCompletionSource<DistributedApplicationExecutionContext>();
        builder.Eventing.Subscribe<AfterResourcesCreatedEvent>((e, ct) => {
            var context = e.Services.GetRequiredService<DistributedApplicationExecutionContext>();
            tcs.SetResult(context);
            return Task.CompletedTask;
        });

        using var app = builder.Build();
        
        await app.StartAsync().WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        var context = await tcs.Task.WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        await app.StopAsync().WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        Assert.AreEqual(DistributedApplicationOperation.Run, context.Operation);
        Assert.IsTrue(context.IsRunMode);
    }

    [TestMethod]
    public async Task VerifyBackwardsCompatablePublishModeInvocation()
    {
        // The purpose of this test is to verify that the apphost executable will continue
        // to enter publish mode if the --publisher argument is specified.

        using var builder = TestDistributedApplicationBuilder
            .Create(["--publisher", "manifest", "--output-path", "test-output-path"])
            .WithTestAndResourceLogging(outputHelper);

        // TOOD: This won't work because this event does not fire in publish mode. We need
        //       another way to get at this internal state.
        var tcs = new TaskCompletionSource<DistributedApplicationExecutionContext>();
        builder.Eventing.Subscribe<BeforeStartEvent>((e, ct) => {
            var context = e.Services.GetRequiredService<DistributedApplicationExecutionContext>();
            tcs.SetResult(context);
            return Task.CompletedTask;
        });

        using var app = builder.Build();
        
        await app.StartAsync().WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        var context = await tcs.Task.WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        await app.StopAsync().WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        Assert.AreEqual(DistributedApplicationOperation.Publish, context.Operation);
        Assert.IsTrue(context.IsPublishMode);
    }

    [TestMethod]
    public async Task VerifyExplicitPublishModeInvocation()
    {
        // The purpose of this test is to verify that the apphost executable will continue
        // to enter publish mode if the --publisher argument is specified.

        using var builder = TestDistributedApplicationBuilder
            .Create(["--operation", "publish", "--publisher", "manifest", "--output-path", "test-output-path"])
            .WithTestAndResourceLogging(outputHelper);

        // TOOD: This won't work because this event does not fire in publish mode. We need
        //       another way to get at this internal state.
        var tcs = new TaskCompletionSource<DistributedApplicationExecutionContext>();
        builder.Eventing.Subscribe<BeforeStartEvent>((e, ct) => {
            var context = e.Services.GetRequiredService<DistributedApplicationExecutionContext>();
            tcs.SetResult(context);
            return Task.CompletedTask;
        });

        using var app = builder.Build();
        
        await app.StartAsync().WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        var context = await tcs.Task.WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        await app.StopAsync().WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        Assert.AreEqual(DistributedApplicationOperation.Publish, context.Operation);
        Assert.IsTrue(context.IsPublishMode);
    }

    [TestMethod]
    public async Task VerifyExplicitInspectModeInvocation()
    {
        // The purpose of this test is to verify that the apphost executable will continue
        // to enter publish mode if the --publisher argument is specified.

        using var builder = TestDistributedApplicationBuilder
            .Create(["--operation", "inspect"])
            .WithTestAndResourceLogging(outputHelper);

        // TOOD: This won't work because this event does not fire in publish mode. We need
        //       another way to get at this internal state.
        var tcs = new TaskCompletionSource<DistributedApplicationExecutionContext>();
        builder.Eventing.Subscribe<BeforeStartEvent>((e, ct) => {
            var context = e.Services.GetRequiredService<DistributedApplicationExecutionContext>();
            tcs.SetResult(context);
            return Task.CompletedTask;
        });

        using var app = builder.Build();
        
        await app.StartAsync().WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        var context = await tcs.Task.WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        await app.StopAsync().WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        Assert.AreEqual(DistributedApplicationOperation.Inspect, context.Operation);
        Assert.IsTrue(context.IsInspectMode);
    }

    [TestMethod]
    public async Task VerifyExplicitInspectModeWithPublisherSpecifiedInvocation()
    {
        // The purpose of this test is to verify that the apphost executable will continue
        // to enter publish mode if the --publisher argument is specified.

        using var builder = TestDistributedApplicationBuilder
            .Create(["--operation", "inspect", "--publisher", "manifest"])
            .WithTestAndResourceLogging(outputHelper);

        // TOOD: This won't work because this event does not fire in publish mode. We need
        //       another way to get at this internal state.
        var tcs = new TaskCompletionSource<DistributedApplicationExecutionContext>();
        builder.Eventing.Subscribe<BeforeStartEvent>((e, ct) => {
            var context = e.Services.GetRequiredService<DistributedApplicationExecutionContext>();
            tcs.SetResult(context);
            return Task.CompletedTask;
        });

        using var app = builder.Build();
        
        await app.StartAsync().WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        var context = await tcs.Task.WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        await app.StopAsync().WaitAsync(TestConstants.DefaultTimeoutTimeSpan);

        Assert.AreEqual(DistributedApplicationOperation.Inspect, context.Operation);
        Assert.IsTrue(context.IsInspectMode);
    }
}