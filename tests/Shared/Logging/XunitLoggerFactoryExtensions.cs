// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.Extensions.Logging;

public static class XunitLoggerFactoryExtensions
{
    public static ILoggingBuilder AddMSTest(this ILoggingBuilder builder, TestContext output)
    {
        builder.Services.AddSingleton<ILoggerProvider>(new MSTestLoggerProvider(output));
        return builder;
    }

    public static ILoggingBuilder AddMSTest(this ILoggingBuilder builder, TestContext output, LogLevel minLevel)
    {
        builder.Services.AddSingleton<ILoggerProvider>(new MSTestLoggerProvider(output, minLevel));
        return builder;
    }

    public static ILoggingBuilder AddMSTest(this ILoggingBuilder builder, TestContext output, LogLevel minLevel, DateTimeOffset? logStart)
    {
        builder.Services.AddSingleton<ILoggerProvider>(new MSTestLoggerProvider(output, minLevel, logStart));
        return builder;
    }

    public static ILoggerFactory AddMSTest(this ILoggerFactory loggerFactory, TestContext output)
    {
        loggerFactory.AddProvider(new MSTestLoggerProvider(output));
        return loggerFactory;
    }

    public static ILoggerFactory AddMSTest(this ILoggerFactory loggerFactory, TestContext output, LogLevel minLevel)
    {
        loggerFactory.AddProvider(new MSTestLoggerProvider(output, minLevel));
        return loggerFactory;
    }

    public static ILoggerFactory AddMSTest(this ILoggerFactory loggerFactory, TestContext output, LogLevel minLevel, DateTimeOffset? logStart)
    {
        loggerFactory.AddProvider(new MSTestLoggerProvider(output, minLevel, logStart));
        return loggerFactory;
    }

    public static IServiceCollection AddMSTestLogging(this IServiceCollection services, TestContext output) =>
        services.AddLogging(b => b.AddMSTest(output));
}
