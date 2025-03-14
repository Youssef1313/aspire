// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Aspire.Npgsql.Tests;
using OpenTelemetry.Trace;

namespace Aspire.Npgsql.EntityFrameworkCore.PostgreSQL.Tests;

[TestClass]
public class EnrichNpgsqlTests : ConformanceTests
{
    public EnrichNpgsqlTests(PostgreSQLContainerFixture containerFixture) : base(containerFixture)
    {
    }

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<NpgsqlEntityFrameworkCorePostgreSQLSettings>? configure = null, string? key = null)
    {
        builder.Services.AddDbContextPool<TestDbContext>(options => options.UseNpgsql(ConnectionString));
        builder.EnrichNpgsqlDbContext<TestDbContext>(configure);
    }

    [TestMethod]
    public void ShouldThrowIfDbContextIsNotRegistered()
    {
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(settings: null);

        var exception = Assert.Throws<InvalidOperationException>(() => builder.EnrichNpgsqlDbContext<TestDbContext>());
        Assert.AreEqual("DbContext<TestDbContext> was not registered. Ensure you have registered the DbContext in DI before calling EnrichNpgsqlDbContext.", exception.Message);
    }

    [TestMethod]
    public void ShouldNotThrowIfDbContextIsRegistered()
    {
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(settings: null);

        builder.Services.AddDbContext<TestDbContext>(options => options.UseNpgsql(ConnectionString));

        builder.EnrichNpgsqlDbContext<TestDbContext>();
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
            new KeyValuePair<string, string?>("Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:Retry", "true")
        ]);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            AspireEFPostgreSqlExtensionsTests.ConfigureDbContextOptionsBuilderForTesting(optionsBuilder);
            optionsBuilder.UseNpgsql(ConnectionString, npgsqlBuilder =>
            {
                npgsqlBuilder.CommandTimeout(123);
            });
        });

        builder.EnrichNpgsqlDbContext<TestDbContext>();

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<NpgsqlOptionsExtension>();
        Assert.IsNotNull(extension);

        // ensure the command timeout was respected
        Assert.AreEqual(123, extension.CommandTimeout);

        // ensure the retry strategy is enabled and set to its default value
        Assert.IsNotNull(extension.ExecutionStrategyFactory);
        var executionStrategy = extension.ExecutionStrategyFactory(new ExecutionStrategyDependencies(new CurrentDbContext(context), context.Options, null!));
        var retryStrategy = Assert.IsType<NpgsqlRetryingExecutionStrategy>(executionStrategy);
        Assert.AreEqual(new WorkaroundToReadProtectedField(context).MaxRetryCount, retryStrategy.MaxRetryCount);

#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    [TestMethod]
    public void EnrichWithConflictingCommandTimeoutThrows()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            AspireEFPostgreSqlExtensionsTests.ConfigureDbContextOptionsBuilderForTesting(optionsBuilder);
            optionsBuilder.UseNpgsql(ConnectionString, npgsqlBuilder =>
            {
                npgsqlBuilder.CommandTimeout(123);
            });
        });

        builder.EnrichNpgsqlDbContext<TestDbContext>(settings => settings.CommandTimeout = 456);
        using var host = builder.Build();

        var exception = Assert.Throws<InvalidOperationException>(host.Services.GetRequiredService<TestDbContext>);
        Assert.AreEqual("Conflicting values for 'CommandTimeout' were found in NpgsqlEntityFrameworkCorePostgreSQLSettings and set in DbContextOptions<TestDbContext>.", exception.Message);
    }

    [TestMethod]
    public void EnrichEnablesRetryByDefault()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            AspireEFPostgreSqlExtensionsTests.ConfigureDbContextOptionsBuilderForTesting(optionsBuilder);
            optionsBuilder.UseNpgsql(ConnectionString);
        });

        var oldOptionsDescriptor = builder.Services.FirstOrDefault(sd => sd.ServiceType == typeof(DbContextOptions<TestDbContext>));
        Assert.IsNotNull(oldOptionsDescriptor);

        builder.EnrichNpgsqlDbContext<TestDbContext>();

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<NpgsqlOptionsExtension>();
        Assert.IsNotNull(extension);

        // ensure the retry strategy is enabled and set to its default value
        Assert.IsNotNull(extension.ExecutionStrategyFactory);
        var executionStrategy = extension.ExecutionStrategyFactory(new ExecutionStrategyDependencies(new CurrentDbContext(context), context.Options, null!));
        var retryStrategy = Assert.IsType<NpgsqlRetryingExecutionStrategy>(executionStrategy);
        Assert.AreEqual(new WorkaroundToReadProtectedField(context).MaxRetryCount, retryStrategy.MaxRetryCount);

#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    [TestMethod]
    public void EnrichPreservesDefaultWhenMaxRetryCountNotSet()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:DisableRetry", "true")
        ]);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            AspireEFPostgreSqlExtensionsTests.ConfigureDbContextOptionsBuilderForTesting(optionsBuilder);
            optionsBuilder.UseNpgsql(ConnectionString, npgsqlBuilder =>
            {
                npgsqlBuilder.EnableRetryOnFailure(456);
            });
        });

        var oldOptionsDescriptor = builder.Services.FirstOrDefault(sd => sd.ServiceType == typeof(DbContextOptions<TestDbContext>));
        Assert.IsNotNull(oldOptionsDescriptor);

        builder.EnrichNpgsqlDbContext<TestDbContext>();

        // The service descriptor of DbContextOptions<TestDbContext> should not be affected since Retry is false
        var optionsDescriptor = builder.Services.FirstOrDefault(sd => sd.ServiceType == typeof(DbContextOptions<TestDbContext>));
        Assert.IsNotNull(optionsDescriptor);
        Assert.AreSame(oldOptionsDescriptor, optionsDescriptor);

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<NpgsqlOptionsExtension>();
        Assert.IsNotNull(extension);

        // ensure the retry strategy is enabled and set to the configured value
        Assert.IsNotNull(extension.ExecutionStrategyFactory);
        var executionStrategy = extension.ExecutionStrategyFactory(new ExecutionStrategyDependencies(new CurrentDbContext(context), context.Options, null!));
        var retryStrategy = Assert.IsType<NpgsqlRetryingExecutionStrategy>(executionStrategy);
        Assert.AreEqual(456, retryStrategy.MaxRetryCount);

#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    [TestMethod]
    public void EnrichDoesntOverridesCustomRetry()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:DisableRetry", "false")
        ]);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            AspireEFPostgreSqlExtensionsTests.ConfigureDbContextOptionsBuilderForTesting(optionsBuilder);
            optionsBuilder.UseNpgsql(ConnectionString, npgsqlBuilder =>
            {
                npgsqlBuilder.EnableRetryOnFailure(456);
            });
        });

        var oldOptionsDescriptor = builder.Services.FirstOrDefault(sd => sd.ServiceType == typeof(DbContextOptions<TestDbContext>));
        Assert.IsNotNull(oldOptionsDescriptor);

        builder.EnrichNpgsqlDbContext<TestDbContext>();

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<NpgsqlOptionsExtension>();
        Assert.IsNotNull(extension);

        // ensure the retry strategy is enabled and set to its default value
        Assert.IsNotNull(extension.ExecutionStrategyFactory);
        var executionStrategy = extension.ExecutionStrategyFactory(new ExecutionStrategyDependencies(new CurrentDbContext(context), context.Options, null!));
        var retryStrategy = Assert.IsType<NpgsqlRetryingExecutionStrategy>(executionStrategy);
        Assert.AreEqual(456, retryStrategy.MaxRetryCount);

#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    [TestMethod]
    public void EnrichSupportServiceType()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Services.AddDbContextPool<ITestDbContext, TestDbContext>(optionsBuilder =>
        {
            AspireEFPostgreSqlExtensionsTests.ConfigureDbContextOptionsBuilderForTesting(optionsBuilder);
            optionsBuilder.UseNpgsql(ConnectionString);
        });

        builder.EnrichNpgsqlDbContext<TestDbContext>();

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
            AspireEFPostgreSqlExtensionsTests.ConfigureDbContextOptionsBuilderForTesting(optionsBuilder);
            optionsBuilder.UseNpgsql(ConnectionString);
        }, contextLifetime: ServiceLifetime.Singleton);

        builder.EnrichNpgsqlDbContext<TestDbContext>();

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
            AspireEFPostgreSqlExtensionsTests.ConfigureDbContextOptionsBuilderForTesting(optionsBuilder);
            optionsBuilder.UseNpgsql(ConnectionString, npgsql => npgsql.ExecutionStrategy(c => new CustomExecutionStrategy(c)));
        });

        builder.EnrichNpgsqlDbContext<TestDbContext>(settings => settings.DisableRetry = true);

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<NpgsqlOptionsExtension>();
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
            AspireEFPostgreSqlExtensionsTests.ConfigureDbContextOptionsBuilderForTesting(optionsBuilder);
            optionsBuilder.UseNpgsql(ConnectionString, npgsql => npgsql.ExecutionStrategy(c => new CustomExecutionStrategy(c)));
        });

        builder.EnrichNpgsqlDbContext<TestDbContext>(settings => settings.DisableRetry = false);
        using var host = builder.Build();

        var exception = Assert.Throws<InvalidOperationException>(host.Services.GetRequiredService<TestDbContext>);
        Assert.AreEqual("NpgsqlEntityFrameworkCorePostgreSQLSettings.DisableRetry needs to be set when a custom Execution Strategy is configured.", exception.Message);
    }

    [TestMethod]
    public void EnrichWithRetryAndCustomRetryExecutionStrategy()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            AspireEFPostgreSqlExtensionsTests.ConfigureDbContextOptionsBuilderForTesting(optionsBuilder);
            optionsBuilder.UseNpgsql(ConnectionString, npgsql => npgsql.ExecutionStrategy(c => new CustomRetryExecutionStrategy(c)));
        });

        builder.EnrichNpgsqlDbContext<TestDbContext>(settings => settings.DisableRetry = false);

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<NpgsqlOptionsExtension>();
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
            new KeyValuePair<string, string?>("Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:DisableTracing", "false"),
            new KeyValuePair<string, string?>("Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:TestDbContext:DisableTracing", "true")
        ]);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            AspireEFPostgreSqlExtensionsTests.ConfigureDbContextOptionsBuilderForTesting(optionsBuilder);
            optionsBuilder.UseNpgsql(ConnectionString);
        });

        builder.EnrichNpgsqlDbContext<TestDbContext>();

        using var host = builder.Build();

        var tracerProvider = host.Services.GetService<TracerProvider>();
        Assert.IsNull(tracerProvider);
    }
}
