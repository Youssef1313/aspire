// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace Aspire.MongoDB.Driver.Tests;

[TestClass]
public class AspireMongoDBDriverExtensionsTests : IClassFixture<MongoDbContainerFixture>
{
    private const string DefaultConnectionName = "mongodb";

    private readonly MongoDbContainerFixture _containerFixture;

    public AspireMongoDBDriverExtensionsTests(MongoDbContainerFixture containerFixture)
    {
        _containerFixture = containerFixture;
    }

    private string DefaultConnectionString => _containerFixture.GetConnectionString();

    [TestMethod]
    [DataRow("mongodb://localhost:27017/mydatabase", true, false)]
    [DataRow("mongodb://localhost:27017", false, false)]
    [DataRow("mongodb://admin:pass@localhost:27017/mydatabase?authSource=admin&authMechanism=SCRAM-SHA-256", true, true)]
    [DataRow("mongodb://admin:pass@localhost:27017?authSource=admin&authMechanism=SCRAM-SHA-256", false, true)]
    public void AddMongoDBDataSource_ReadsFromConnectionStringsCorrectly(string connectionString, bool shouldRegisterDatabase, bool shouldUseAuthentication)
    {
        var builder = CreateBuilder(connectionString);

        builder.AddMongoDBClient(DefaultConnectionName);

        using var host = builder.Build();

        var mongoClient = host.Services.GetRequiredService<IMongoClient>();

        var uri = MongoUrl.Create(connectionString);

        Assert.AreEqual(uri.Server.Host, mongoClient.Settings.Server.Host);
        Assert.AreEqual(uri.Server.Port, mongoClient.Settings.Server.Port);

        if (shouldUseAuthentication)
        {
            Assert.IsNotNull(mongoClient.Settings.Credential);
            Assert.AreEqual(uri.AuthenticationSource, mongoClient.Settings.Credential.Source);
            Assert.AreEqual(uri.AuthenticationMechanism, mongoClient.Settings.Credential.Mechanism);
            Assert.AreEqual(uri.Username, mongoClient.Settings.Credential.Username);
        }
        else
        {
            Assert.IsNull(mongoClient.Settings.Credential);
        }
        var mongoDatabase = host.Services.GetService<IMongoDatabase>();

        if (shouldRegisterDatabase)
        {
            Assert.IsNotNull(mongoDatabase);
            Assert.AreEqual(uri.DatabaseName, mongoDatabase.DatabaseNamespace.DatabaseName);
        }
        else
        {
            Assert.IsNull(mongoDatabase);
        }
    }

    [TestMethod]
    [DataRow("mongodb://localhost:27017/mydatabase", true, false)]
    [DataRow("mongodb://localhost:27017", false, false)]
    [DataRow("mongodb://admin:pass@localhost:27017/mydatabase?authSource=admin&authMechanism=SCRAM-SHA-256", true, true)]
    [DataRow("mongodb://admin:pass@localhost:27017?authSource=admin&authMechanism=SCRAM-SHA-256", false, true)]
    public void AddKeyedMongoDBDataSource_ReadsFromConnectionStringsCorrectly(string connectionString, bool shouldRegisterDatabase, bool shouldUseAuthentication)
    {
        var key = DefaultConnectionName;

        var builder = CreateBuilder(connectionString);

        builder.AddKeyedMongoDBClient(key);

        using var host = builder.Build();

        var mongoClient = host.Services.GetRequiredKeyedService<IMongoClient>(key);

        var uri = MongoUrl.Create(connectionString);

        Assert.AreEqual(uri.Server.Host, mongoClient.Settings.Server.Host);
        Assert.AreEqual(uri.Server.Port, mongoClient.Settings.Server.Port);

        if (shouldUseAuthentication)
        {
            Assert.IsNotNull(mongoClient.Settings.Credential);
            Assert.AreEqual(uri.AuthenticationSource, mongoClient.Settings.Credential.Source);
            Assert.AreEqual(uri.AuthenticationMechanism, mongoClient.Settings.Credential.Mechanism);
            Assert.AreEqual(uri.Username, mongoClient.Settings.Credential.Username);
        }
        else
        {
            Assert.IsNull(mongoClient.Settings.Credential);
        }

        var mongoDatabase = host.Services.GetKeyedService<IMongoDatabase>(key);

        if (shouldRegisterDatabase)
        {
            Assert.IsNotNull(mongoDatabase);
            Assert.AreEqual(uri.DatabaseName, mongoDatabase.DatabaseNamespace.DatabaseName);
        }
        else
        {
            Assert.IsNull(mongoDatabase);
        }
    }

    [TestMethod]
    [RequiresDocker]
    public async Task AddMongoDBDataSource_HealthCheckShouldBeRegisteredWhenEnabled()
    {
        var builder = CreateBuilder(DefaultConnectionString);

        builder.AddMongoDBClient(DefaultConnectionName, settings =>
        {
            settings.DisableHealthChecks = false;
            settings.HealthCheckTimeout = 1;
        });

        using var host = builder.Build();

        var healthCheckService = host.Services.GetRequiredService<HealthCheckService>();

        var healthCheckReport = await healthCheckService.CheckHealthAsync();

        var healthCheckName = "MongoDB.Driver";

        Assert.Contains(healthCheckReport.Entries, x => x.Key == healthCheckName);
    }

    [TestMethod]
    [RequiresDocker]
    public void AddKeyedMongoDBDataSource_HealthCheckShouldNotBeRegisteredWhenDisabled()
    {
        var builder = CreateBuilder(DefaultConnectionString);

        builder.AddKeyedMongoDBClient(DefaultConnectionName, settings =>
        {
            settings.DisableHealthChecks = true;
        });

        using var host = builder.Build();

        var healthCheckService = host.Services.GetService<HealthCheckService>();

        Assert.IsNull(healthCheckService);

    }

    [TestMethod]
    [RequiresDocker]
    public async Task AddKeyedMongoDBDataSource_HealthCheckShouldBeRegisteredWhenEnabled()
    {
        var key = DefaultConnectionName;

        var builder = CreateBuilder(DefaultConnectionString);

        builder.AddKeyedMongoDBClient(key, settings =>
        {
            settings.DisableHealthChecks = false;
            settings.HealthCheckTimeout = 1;
        });

        using var host = builder.Build();

        var healthCheckService = host.Services.GetRequiredService<HealthCheckService>();

        var healthCheckReport = await healthCheckService.CheckHealthAsync();

        var healthCheckName = $"MongoDB.Driver_{key}";

        Assert.Contains(healthCheckReport.Entries, x => x.Key == healthCheckName);
    }

    [TestMethod]
    [RequiresDocker]
    public void AddMongoDBDataSource_HealthCheckShouldNotBeRegisteredWhenDisabled()
    {
        var builder = CreateBuilder(DefaultConnectionString);

        builder.AddMongoDBClient(DefaultConnectionName, settings =>
        {
            settings.DisableHealthChecks = true;
        });

        using var host = builder.Build();

        var healthCheckService = host.Services.GetService<HealthCheckService>();

        Assert.IsNull(healthCheckService);
    }

    [TestMethod]
    public void CanAddMultipleKeyedServices()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:mongodb1", "mongodb://localhost:27011/mydatabase1"),
            new KeyValuePair<string, string?>("ConnectionStrings:mongodb2", "mongodb://localhost:27012/mydatabase2"),
            new KeyValuePair<string, string?>("ConnectionStrings:mongodb3", "mongodb://localhost:27013/mydatabase3"),
        ]);

        builder.AddMongoDBClient("mongodb1");
        builder.AddKeyedMongoDBClient("mongodb2");
        builder.AddKeyedMongoDBClient("mongodb3");

        using var host = builder.Build();

        var connection1 = host.Services.GetRequiredService<IMongoDatabase>();
        var connection2 = host.Services.GetRequiredKeyedService<IMongoDatabase>("mongodb2");
        var connection3 = host.Services.GetRequiredKeyedService<IMongoDatabase>("mongodb3");

        Assert.AreNotSame(connection1, connection2);
        Assert.AreNotSame(connection1, connection3);
        Assert.AreNotSame(connection2, connection3);

        Assert.AreEqual("mydatabase1", connection1.DatabaseNamespace.DatabaseName);
        Assert.AreEqual("mydatabase2", connection2.DatabaseNamespace.DatabaseName);
        Assert.AreEqual("mydatabase3", connection3.DatabaseNamespace.DatabaseName);
    }

    private static HostApplicationBuilder CreateBuilder(string connectionString)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>($"ConnectionStrings:{DefaultConnectionName}", connectionString)
        ]);
        return builder;
    }
}
