// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.MySql;
using Aspire.MySqlConnector.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MySqlConnector.Logging;
using OpenTelemetry.Trace;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure.Internal;

namespace Aspire.Pomelo.EntityFrameworkCore.MySql.Tests;

[TestClass]
public class EnrichMySqlTests : ConformanceTests
{
    public static readonly MySqlServerVersion DefaultVersion = new(new Version(MySqlContainerImageTags.Tag));

    public EnrichMySqlTests(MySqlContainerFixture containerFixture) : base(containerFixture)
    {
    }

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<PomeloEntityFrameworkCoreMySqlSettings>? configure = null, string? key = null)
    {
        builder.Services.AddDbContextPool<TestDbContext>((serviceProvider, options) =>
        {
            // use the legacy method of setting the ILoggerFactory because Pomelo EF Core doesn't use MySqlDataSource
            if (serviceProvider.GetService<ILoggerFactory>() is { } loggerFactory)
            {
                MySqlConnectorLogManager.Provider = new MicrosoftExtensionsLoggingLoggerProvider(loggerFactory);
            }

            options.UseMySql(ConnectionString, DefaultVersion);
        });
        builder.EnrichMySqlDbContext<TestDbContext>(configure);
    }

    [TestMethod]
    public void ShouldThrowIfDbContextIsNotRegistered()
    {
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(settings: null);

        var exception = Assert.Throws<InvalidOperationException>(() => builder.EnrichMySqlDbContext<TestDbContext>());
        Assert.AreEqual("DbContext<TestDbContext> was not registered. Ensure you have registered the DbContext in DI before calling EnrichMySqlDbContext.", exception.Message);
    }

    [TestMethod]
    public void ShouldNotThrowIfDbContextIsRegistered()
    {
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(settings: null);

        builder.Services.AddDbContext<TestDbContext>(options => options.UseMySql(ConnectionString, DefaultVersion));

        builder.EnrichMySqlDbContext<TestDbContext>();
    }

    protected override void SetupConnectionInformationIsDelayValidated()
    {
        Assert.Inconclusive("Enrich doesn't use ConnectionString");
    }

    [TestMethod]
    public void EnrichCanConfigureDbContextOptions()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("Aspire:Pomelo:EntityFrameworkCore:MySql:DisableRetry", "false")
        ]);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseMySql(ConnectionString, DefaultVersion, builder =>
            {
                builder.CommandTimeout(123);
            });
        });

        builder.EnrichMySqlDbContext<TestDbContext>();

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<MySqlOptionsExtension>();
        Assert.IsNotNull(extension);

        // ensure the command timeout was respected
        Assert.AreEqual(123, extension.CommandTimeout);

        // ensure the retry strategy is enabled and set to its default value
        Assert.IsNotNull(extension.ExecutionStrategyFactory);
        var executionStrategy = extension.ExecutionStrategyFactory(new ExecutionStrategyDependencies(new CurrentDbContext(context), context.Options, null!));
        var retryStrategy = Assert.IsType<MySqlRetryingExecutionStrategy>(executionStrategy);
        Assert.AreEqual(new WorkaroundToReadProtectedField(context).MaxRetryCount, retryStrategy.MaxRetryCount);

#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    [TestMethod]
    public void EnrichWithConflictingCommandTimeoutThrows()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseMySql(ConnectionString, DefaultVersion, builder =>
            {
                builder.CommandTimeout(123);
            });
        });

        builder.EnrichMySqlDbContext<TestDbContext>(settings => settings.CommandTimeout = 456);
        using var host = builder.Build();

        var exception = Assert.Throws<InvalidOperationException>(host.Services.GetRequiredService<TestDbContext>);
        Assert.AreEqual("Conflicting values for 'CommandTimeout' were found in PomeloEntityFrameworkCoreMySqlSettings and set in DbContextOptions<TestDbContext>.", exception.Message);
    }

    [TestMethod]
    public void EnrichEnablesRetryByDefault()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseMySql(ConnectionString, DefaultVersion);
        });

        var oldOptionsDescriptor = builder.Services.FirstOrDefault(sd => sd.ServiceType == typeof(DbContextOptions<TestDbContext>));
        Assert.IsNotNull(oldOptionsDescriptor);

        builder.EnrichMySqlDbContext<TestDbContext>();

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<MySqlOptionsExtension>();
        Assert.IsNotNull(extension);

        // ensure the retry strategy is enabled and set to its default value
        Assert.IsNotNull(extension.ExecutionStrategyFactory);
        var executionStrategy = extension.ExecutionStrategyFactory(new ExecutionStrategyDependencies(new CurrentDbContext(context), context.Options, null!));
        var retryStrategy = Assert.IsType<MySqlRetryingExecutionStrategy>(executionStrategy);
        Assert.AreEqual(new WorkaroundToReadProtectedField(context).MaxRetryCount, retryStrategy.MaxRetryCount);

#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    [TestMethod]
    public void EnrichPreservesDefaultWhenMaxRetryCountNotSet()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("Aspire:Pomelo:EntityFrameworkCore:MySql:DisableRetry", "true")
        ]);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseMySql(ConnectionString, DefaultVersion, builder =>
            {
                builder.EnableRetryOnFailure(456);
            });
        });

        var oldOptionsDescriptor = builder.Services.FirstOrDefault(sd => sd.ServiceType == typeof(DbContextOptions<TestDbContext>));
        Assert.IsNotNull(oldOptionsDescriptor);

        builder.EnrichMySqlDbContext<TestDbContext>();

        // The service descriptor of DbContextOptions<TestDbContext> should not be affected since Retry is false
        var optionsDescriptor = builder.Services.FirstOrDefault(sd => sd.ServiceType == typeof(DbContextOptions<TestDbContext>));
        Assert.IsNotNull(optionsDescriptor);
        Assert.AreSame(oldOptionsDescriptor, optionsDescriptor);

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<MySqlOptionsExtension>();
        Assert.IsNotNull(extension);

        // ensure the retry strategy is enabled and set to the configured value
        Assert.IsNotNull(extension.ExecutionStrategyFactory);
        var executionStrategy = extension.ExecutionStrategyFactory(new ExecutionStrategyDependencies(new CurrentDbContext(context), context.Options, null!));
        var retryStrategy = Assert.IsType<MySqlRetryingExecutionStrategy>(executionStrategy);
        Assert.AreEqual(456, retryStrategy.MaxRetryCount);

#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    [TestMethod]
    public void EnrichDoesntOverridesCustomRetry()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("Aspire:Pomelo:EntityFrameworkCore:MySql:DisableRetry", "false")
        ]);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseMySql(ConnectionString, DefaultVersion, builder =>
            {
                builder.EnableRetryOnFailure(456);
            });
        });

        var oldOptionsDescriptor = builder.Services.FirstOrDefault(sd => sd.ServiceType == typeof(DbContextOptions<TestDbContext>));
        Assert.IsNotNull(oldOptionsDescriptor);

        builder.EnrichMySqlDbContext<TestDbContext>();

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<MySqlOptionsExtension>();
        Assert.IsNotNull(extension);

        // ensure the retry strategy is enabled and set to its default value
        Assert.IsNotNull(extension.ExecutionStrategyFactory);
        var executionStrategy = extension.ExecutionStrategyFactory(new ExecutionStrategyDependencies(new CurrentDbContext(context), context.Options, null!));
        var retryStrategy = Assert.IsType<MySqlRetryingExecutionStrategy>(executionStrategy);
        Assert.AreEqual(456, retryStrategy.MaxRetryCount);
#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    [TestMethod]
    public void EnrichSupportServiceType()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Services.AddDbContextPool<ITestDbContext, TestDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseMySql(ConnectionString, DefaultVersion);
        });

        builder.EnrichMySqlDbContext<TestDbContext>();

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<ITestDbContext>() as TestDbContext;
        Assert.IsNotNull(context);
    }

    [TestMethod]
    public void EnrichSupportCustomOptionsLifetime()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Services.AddDbContext<ITestDbContext, TestDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseMySql(ConnectionString, DefaultVersion);
        }, contextLifetime: ServiceLifetime.Singleton);

        builder.EnrichMySqlDbContext<TestDbContext>();

        var optionsDescriptor = builder.Services.FirstOrDefault(sd => sd.ServiceType == typeof(DbContextOptions<TestDbContext>));
        Assert.IsNotNull(optionsDescriptor);
        Assert.AreEqual(ServiceLifetime.Singleton, optionsDescriptor.Lifetime);

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<ITestDbContext>() as TestDbContext;
        Assert.IsNotNull(context);
    }

    [TestMethod]
    public void EnrichWithoutRetryPreservesCustomExecutionStrategy()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseMySql(ConnectionString, DefaultVersion, builder => builder.ExecutionStrategy(c => new CustomExecutionStrategy(c)));
        });

        builder.EnrichMySqlDbContext<TestDbContext>(settings => settings.DisableRetry = true);

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<MySqlOptionsExtension>();
        Assert.IsNotNull(extension);

        // ensure the retry strategy is enabled and set to its default value
        Assert.IsNotNull(extension.ExecutionStrategyFactory);
        var executionStrategy = extension.ExecutionStrategyFactory(new ExecutionStrategyDependencies(new CurrentDbContext(context), context.Options, null!));
        Assert.IsType<CustomExecutionStrategy>(executionStrategy);

#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    [TestMethod]
    public void EnrichWithRetryAndCustomExecutionStrategyThrows()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseMySql(ConnectionString, DefaultVersion, builder => builder.ExecutionStrategy(c => new CustomExecutionStrategy(c)));
        });

        builder.EnrichMySqlDbContext<TestDbContext>(settings => settings.DisableRetry = false);
        using var host = builder.Build();

        var exception = Assert.Throws<InvalidOperationException>(host.Services.GetRequiredService<TestDbContext>);
        Assert.AreEqual("PomeloEntityFrameworkCoreMySqlSettings.DisableRetry needs to be set when a custom Execution Strategy is configured.", exception.Message);
    }

    [TestMethod]
    public void EnrichWithRetryAndCustomRetryExecutionStrategy()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseMySql(ConnectionString, DefaultVersion, builder => builder.ExecutionStrategy(c => new CustomRetryExecutionStrategy(c)));
        });

        builder.EnrichMySqlDbContext<TestDbContext>(settings => settings.DisableRetry = false);

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<MySqlOptionsExtension>();
        Assert.IsNotNull(extension);

        // ensure the retry strategy is enabled and set to its default value
        Assert.IsNotNull(extension.ExecutionStrategyFactory);
        var executionStrategy = extension.ExecutionStrategyFactory(new ExecutionStrategyDependencies(new CurrentDbContext(context), context.Options, null!));
        Assert.IsType<CustomRetryExecutionStrategy>(executionStrategy);

#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    [TestMethod]
    public void EnrichWithNamedAndNonNamedUsesBoth()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("Aspire:Pomelo:EntityFrameworkCore:MySql:DisableTracing", "false"),
            new KeyValuePair<string, string?>("Aspire:Pomelo:EntityFrameworkCore:MySql:TestDbContext:DisableTracing", "true")
        ]);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseMySql(ConnectionString, DefaultVersion);
        });

        builder.EnrichMySqlDbContext<TestDbContext>();

        using var host = builder.Build();

        var tracerProvider = host.Services.GetService<TracerProvider>();
        Assert.IsNull(tracerProvider);
    }
}
