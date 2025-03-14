// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;
using Oracle.EntityFrameworkCore;
using Oracle.EntityFrameworkCore.Infrastructure.Internal;
using Xunit.Abstractions;

namespace Aspire.Oracle.EntityFrameworkCore.Tests;

[TestClass]
public class EnrichOracleDatabaseTests : ConformanceTests
{
    public EnrichOracleDatabaseTests(OracleContainerFixture? containerFixture, TestContext testContext) : base(containerFixture, testContext)
    {
    }

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<OracleEntityFrameworkCoreSettings>? configure = null, string? key = null)
    {
        builder.Services.AddDbContextPool<TestDbContext>(options => options.UseOracle(ConnectionString));
        builder.EnrichOracleDatabaseDbContext<TestDbContext>(configure);
    }

    [TestMethod]
    public void ShouldThrowIfDbContextIsNotRegistered()
    {
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(settings: null);

        var exception = Assert.Throws<InvalidOperationException>(() => builder.EnrichOracleDatabaseDbContext<TestDbContext>());
        Assert.AreEqual("DbContext<TestDbContext> was not registered. Ensure you have registered the DbContext in DI before calling EnrichOracleDatabaseDbContext.", exception.Message);
    }

    [TestMethod]
    public void ShouldNotThrowIfDbContextIsRegistered()
    {
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(settings: null);

        builder.Services.AddDbContext<TestDbContext>(options => options.UseOracle(ConnectionString));

        builder.EnrichOracleDatabaseDbContext<TestDbContext>();
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
            new KeyValuePair<string, string?>("Aspire:Oracle:EntityFrameworkCore:DisableRetry", "false")
        ]);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseOracle(ConnectionString, builder =>
            {
                builder.CommandTimeout(123);
            });
        });

        builder.EnrichOracleDatabaseDbContext<TestDbContext>();

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<OracleOptionsExtension>();
        Assert.IsNotNull(extension);

        // ensure the command timeout was respected
        Assert.AreEqual(123, extension.CommandTimeout);

        // ensure the retry strategy is enabled and set to its default value
        Assert.IsNotNull(extension.ExecutionStrategyFactory);
        var executionStrategy = extension.ExecutionStrategyFactory(new ExecutionStrategyDependencies(new CurrentDbContext(context), context.Options, null!));
        var retryStrategy = Assert.IsType<OracleRetryingExecutionStrategy>(executionStrategy);
        Assert.AreEqual(new WorkaroundToReadProtectedField(context).MaxRetryCount, retryStrategy.MaxRetryCount);

#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    [TestMethod]
    public void EnrichWithConflictingCommandTimeoutThrows()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseOracle(ConnectionString, builder =>
            {
                builder.CommandTimeout(123);
            });
        });

        builder.EnrichOracleDatabaseDbContext<TestDbContext>(settings => settings.CommandTimeout = 456);
        using var host = builder.Build();

        var exception = Assert.Throws<InvalidOperationException>(host.Services.GetRequiredService<TestDbContext>);
        Assert.AreEqual("Conflicting values for 'CommandTimeout' were found in OracleEntityFrameworkCoreSettings and set in DbContextOptions<TestDbContext>.", exception.Message);
    }

    [TestMethod]
    public void EnrichEnablesRetryByDefault()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseOracle(ConnectionString);
        });

        var oldOptionsDescriptor = builder.Services.FirstOrDefault(sd => sd.ServiceType == typeof(DbContextOptions<TestDbContext>));
        Assert.IsNotNull(oldOptionsDescriptor);

        builder.EnrichOracleDatabaseDbContext<TestDbContext>();

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<OracleOptionsExtension>();
        Assert.IsNotNull(extension);

        // ensure the retry strategy is enabled and set to its default value
        Assert.IsNotNull(extension.ExecutionStrategyFactory);
        var executionStrategy = extension.ExecutionStrategyFactory(new ExecutionStrategyDependencies(new CurrentDbContext(context), context.Options, null!));
        var retryStrategy = Assert.IsType<OracleRetryingExecutionStrategy>(executionStrategy);
        Assert.AreEqual(new WorkaroundToReadProtectedField(context).MaxRetryCount, retryStrategy.MaxRetryCount);

#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    [TestMethod]
    public void EnrichPreservesDefaultWhenMaxRetryCountNotSet()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("Aspire:Oracle:EntityFrameworkCore:DisableRetry", "true")
        ]);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseOracle(ConnectionString, builder =>
            {
                builder.ExecutionStrategy(context => new OracleRetryingExecutionStrategy(context, 456));
            });
        });

        var oldOptionsDescriptor = builder.Services.FirstOrDefault(sd => sd.ServiceType == typeof(DbContextOptions<TestDbContext>));
        Assert.IsNotNull(oldOptionsDescriptor);

        builder.EnrichOracleDatabaseDbContext<TestDbContext>();

        // The service descriptor of DbContextOptions<TestDbContext> should not be affected since Retry is false
        var optionsDescriptor = builder.Services.FirstOrDefault(sd => sd.ServiceType == typeof(DbContextOptions<TestDbContext>));
        Assert.IsNotNull(optionsDescriptor);
        Assert.AreSame(oldOptionsDescriptor, optionsDescriptor);

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<OracleOptionsExtension>();
        Assert.IsNotNull(extension);

        // ensure the retry strategy is enabled and set to the configured value
        Assert.IsNotNull(extension.ExecutionStrategyFactory);
        var executionStrategy = extension.ExecutionStrategyFactory(new ExecutionStrategyDependencies(new CurrentDbContext(context), context.Options, null!));
        var retryStrategy = Assert.IsType<OracleRetryingExecutionStrategy>(executionStrategy);
        Assert.AreEqual(456, retryStrategy.MaxRetryCount);

#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    [TestMethod]
    public void EnrichDoesntOverridesCustomRetry()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("Aspire:Oracle:EntityFrameworkCore:DisableRetry", "false")
        ]);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseOracle(ConnectionString, builder =>
            {
                builder.ExecutionStrategy(context => new OracleRetryingExecutionStrategy(context, 456));
            });
        });

        var oldOptionsDescriptor = builder.Services.FirstOrDefault(sd => sd.ServiceType == typeof(DbContextOptions<TestDbContext>));
        Assert.IsNotNull(oldOptionsDescriptor);

        builder.EnrichOracleDatabaseDbContext<TestDbContext>();

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<OracleOptionsExtension>();
        Assert.IsNotNull(extension);

        // ensure the retry strategy is enabled and set to its default value
        Assert.IsNotNull(extension.ExecutionStrategyFactory);
        var executionStrategy = extension.ExecutionStrategyFactory(new ExecutionStrategyDependencies(new CurrentDbContext(context), context.Options, null!));
        var retryStrategy = Assert.IsType<OracleRetryingExecutionStrategy>(executionStrategy);
        Assert.AreEqual(456, retryStrategy.MaxRetryCount);
#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    [TestMethod]
    public void EnrichSupportServiceType()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Services.AddDbContextPool<ITestDbContext, TestDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseOracle(ConnectionString);
        });

        builder.EnrichOracleDatabaseDbContext<TestDbContext>();

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
            optionsBuilder.UseOracle(ConnectionString);
        }, contextLifetime: ServiceLifetime.Singleton);

        builder.EnrichOracleDatabaseDbContext<TestDbContext>();

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
            optionsBuilder.UseOracle(ConnectionString, builder => builder.ExecutionStrategy(c => new CustomExecutionStrategy(c)));
        });

        builder.EnrichOracleDatabaseDbContext<TestDbContext>(settings => settings.DisableRetry = true);

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<OracleOptionsExtension>();
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
            optionsBuilder.UseOracle(ConnectionString, builder => builder.ExecutionStrategy(c => new CustomExecutionStrategy(c)));
        });

        builder.EnrichOracleDatabaseDbContext<TestDbContext>(settings => settings.DisableRetry = false);
        using var host = builder.Build();

        var exception = Assert.Throws<InvalidOperationException>(host.Services.GetRequiredService<TestDbContext>);
        Assert.AreEqual("OracleEntityFrameworkCoreSettings.DisableRetry needs to be set when a custom Execution Strategy is configured.", exception.Message);
    }

    [TestMethod]
    public void EnrichWithRetryAndCustomRetryExecutionStrategy()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseOracle(ConnectionString, builder => builder.ExecutionStrategy(c => new CustomRetryExecutionStrategy(c)));
        });

        builder.EnrichOracleDatabaseDbContext<TestDbContext>(settings => settings.DisableRetry = false);

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<OracleOptionsExtension>();
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
            new KeyValuePair<string, string?>("Aspire:Oracle:EntityFrameworkCore:DisableTracing", "false"),
            new KeyValuePair<string, string?>("Aspire:Oracle:EntityFrameworkCore:TestDbContext:DisableTracing", "true")
        ]);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseOracle(ConnectionString);
        });

        builder.EnrichOracleDatabaseDbContext<TestDbContext>();

        using var host = builder.Build();

        var tracerProvider = host.Services.GetService<TracerProvider>();
        Assert.IsNull(tracerProvider);
    }
}
