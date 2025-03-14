// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Aspire.Components.Common.Tests;
using Aspire.Hosting.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;

#if RABBITMQ_V6
using RabbitMQ.Client.Logging;
#else
using System.Reflection;
#endif

namespace Aspire.RabbitMQ.Client.Tests;

[TestClass]
public class AspireRabbitMQLoggingTests
{
    /// <summary>
    /// Tests that the RabbitMQ client logs are forwarded to the M.E.Logging correctly in an end-to-end scenario.
    ///
    /// The easiest way to ensure a log is written is to start the RabbitMQ container, establish the connection,
    /// and then stop the container. This will cause the RabbitMQ client to log an error message.
    /// </summary>
    [TestMethod]
    [RequiresDocker]
    public async Task EndToEndLoggingTest()
    {
        await using var rabbitMqContainer = new RabbitMqBuilder()
            .WithImage($"{ComponentTestConstants.AspireTestContainerRegistry}/{RabbitMQContainerImageTags.Image}:{RabbitMQContainerImageTags.Tag}")
            .Build();
        await rabbitMqContainer.StartAsync();

        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:messaging", rabbitMqContainer.GetConnectionString())
        ]);

        builder.AddRabbitMQClient("messaging");

        var tsc = new TaskCompletionSource();
        var logger = new TestLogger();
        logger.LoggedMessage = () =>
        {
            // wait for at least 2 logs to be written
            if (logger.Logs.Count >= 2)
            {
                tsc.SetResult();
            }
        };

        builder.Services.AddSingleton<ILoggerProvider>(sp => new LoggerProvider(logger));

        using var host = builder.Build();
        using var connection = host.Services.GetRequiredService<IConnection>();

        await rabbitMqContainer.StopAsync();
        await rabbitMqContainer.DisposeAsync();

        await tsc.Task.WaitAsync(TimeSpan.FromMinutes(1));

        var logs = logger.Logs.ToArray();
        Assert.IsTrue(logs.Length >= 2, "Should be at least 2 logs written.");

        Assert.Contains(logs, l => l.Level == LogLevel.Information && l.Message == "Performing automatic recovery");
        Assert.Contains(logs, l => l.Level == LogLevel.Error && l.Message == "Connection recovery exception.");
    }

    [TestMethod]
    public void TestInfoAndWarn()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddSingleton<RabbitMQEventSourceLogForwarder>();

        var logger = new TestLogger();
        builder.Services.AddSingleton<ILoggerProvider>(sp => new LoggerProvider(logger));

        using var host = builder.Build();
        host.Services.GetRequiredService<RabbitMQEventSourceLogForwarder>().Start();

        var message = "This is an informational message.";
        LogInfo(message);

        var logs = logger.Logs.ToArray();
        Assert.ContainsSingle(logs);
        Assert.AreEqual(LogLevel.Information, logs[0].Level);
        Assert.AreEqual(message, logs[0].Message);

        var warningMessage = "This is a warning message.";
        LogWarn(warningMessage);

        logs = logger.Logs.ToArray();
        Assert.AreEqual(2, logs.Length);
        Assert.AreEqual(LogLevel.Warning, logs[1].Level);
        Assert.AreEqual(warningMessage, logs[1].Message);
    }

    [TestMethod]
    public void TestExceptionWithoutInnerException()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddSingleton<RabbitMQEventSourceLogForwarder>();

        var logger = new TestLogger();
        builder.Services.AddSingleton<ILoggerProvider>(sp => new LoggerProvider(logger));

        using var host = builder.Build();
        host.Services.GetRequiredService<RabbitMQEventSourceLogForwarder>().Start();

        var exceptionMessage = "Test exception";
        Exception testException;
        try
        {
            throw new InvalidOperationException(exceptionMessage);
        }
        catch (Exception ex)
        {
            testException = ex;
        }

        Assert.IsNotNull(testException);
        var logMessage = "This is an error message.";
        LogError(logMessage, testException);

        var logs = logger.Logs.ToArray();
        Assert.ContainsSingle(logs);
        Assert.AreEqual(LogLevel.Error, logs[0].Level);
        Assert.AreEqual(logMessage, logs[0].Message);

        var errorEvent = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object?>>>(logs[0].State);
        Assert.AreEqual(3, errorEvent.Count);

        Assert.AreEqual("exception.type", errorEvent[0].Key);
        Assert.AreEqual("System.InvalidOperationException", errorEvent[0].Value);

        Assert.AreEqual("exception.message", errorEvent[1].Key);
        Assert.AreEqual(exceptionMessage, errorEvent[1].Value);

        Assert.AreEqual("exception.stacktrace", errorEvent[2].Key);
        Assert.Contains("AspireRabbitMQLoggingTests.TestException", errorEvent[2].Value?.ToString());
    }

    [TestMethod]
    public void TestExceptionWithInnerException()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddSingleton<RabbitMQEventSourceLogForwarder>();

        var logger = new TestLogger();
        builder.Services.AddSingleton<ILoggerProvider>(sp => new LoggerProvider(logger));

        using var host = builder.Build();
        host.Services.GetRequiredService<RabbitMQEventSourceLogForwarder>().Start();

        var exceptionMessage = "Test exception";
        Exception testException;
        InvalidOperationException innerException = new("Inner exception");
        try
        {
            throw new InvalidOperationException(exceptionMessage, innerException);
        }
        catch (Exception ex)
        {
            testException = ex;
        }

        Assert.IsNotNull(testException);
        var logMessage = "This is an error message.";
        LogError(logMessage, testException);

        var logs = logger.Logs.ToArray();
        Assert.ContainsSingle(logs);
        Assert.AreEqual(LogLevel.Error, logs[0].Level);
        Assert.AreEqual(logMessage, logs[0].Message);

        var errorEvent = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object?>>>(logs[0].State);
        Assert.AreEqual(4, errorEvent.Count);

        Assert.AreEqual("exception.type", errorEvent[0].Key);
        Assert.AreEqual("System.InvalidOperationException", errorEvent[0].Value);

        Assert.AreEqual("exception.message", errorEvent[1].Key);
        Assert.AreEqual(exceptionMessage, errorEvent[1].Value);

        Assert.AreEqual("exception.stacktrace", errorEvent[2].Key);
        Assert.Contains("AspireRabbitMQLoggingTests.TestException", errorEvent[2].Value?.ToString());

        Assert.AreEqual("exception.innerexception", errorEvent[3].Key);
        Assert.AreEqual($"{innerException.GetType()}: {innerException.Message}", errorEvent[3].Value?.ToString());
    }

#if !RABBITMQ_V6
    private static readonly object s_log =
        Type.GetType("RabbitMQ.Client.Logging.RabbitMqClientEventSource, RabbitMQ.Client")!
            .GetField("Log", BindingFlags.Static | BindingFlags.Public)!
            .GetValue(null)!;
#endif

    private static void LogInfo(string message)
    {
#if RABBITMQ_V6
        RabbitMqClientEventSource.Log.Info(message);
#else
        s_log.GetType().GetMethod("Info")!.Invoke(s_log, new object[] { message });
#endif
    }
    private static void LogWarn(string message)
    {
#if RABBITMQ_V6
        RabbitMqClientEventSource.Log.Warn(message);
#else
        s_log.GetType().GetMethod("Warn")!.Invoke(s_log, new object[] { message });
#endif
    }
    private static void LogError(string message, Exception ex)
    {
#if RABBITMQ_V6
        RabbitMqClientEventSource.Log.Error(message, ex);
#else
        s_log.GetType().GetMethod("Error", [typeof(string), typeof(Exception)])!.Invoke(s_log, new object[] { message, ex });
#endif
    }

    private sealed class LoggerProvider(TestLogger logger) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => logger;

        public void Dispose() { }
    }

    private sealed class TestLogger : ILogger
    {
        public BlockingCollection<(LogLevel Level, string Message, object? State)> Logs { get; } = new();
        public Action? LoggedMessage { get; set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
            NullLogger.Instance.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Logs.Add((logLevel, formatter(state, exception), state));
            LoggedMessage?.Invoke();
        }
    }
}
