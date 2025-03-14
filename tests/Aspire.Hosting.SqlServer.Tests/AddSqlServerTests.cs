// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Sockets;

namespace Aspire.Hosting.SqlServer.Tests;

[TestClass]
public class AddSqlServerTests
{
    [TestMethod]
    public void AddSqlServerAddsGeneratedPasswordParameterWithUserSecretsParameterDefaultInRunMode()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var sql = appBuilder.AddSqlServer("sql");

        Assert.AreEqual("Aspire.Hosting.ApplicationModel.UserSecretsParameterDefault", sql.Resource.PasswordParameter.Default?.GetType().FullName);
    }

    [TestMethod]
    public void AddSqlServerDoesNotAddGeneratedPasswordParameterWithUserSecretsParameterDefaultInPublishMode()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var sql = appBuilder.AddSqlServer("sql");

        Assert.AreNotEqual("Aspire.Hosting.ApplicationModel.UserSecretsParameterDefault", sql.Resource.PasswordParameter.Default?.GetType().FullName);
    }

    [TestMethod]
    public async Task AddSqlServerContainerWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddSqlServer("sqlserver");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.ContainsSingle(appModel.Resources.OfType<SqlServerServerResource>());
        Assert.AreEqual("sqlserver", containerResource.Name);

        var endpoint = Assert.ContainsSingle(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.AreEqual(1433, endpoint.TargetPort);
        Assert.IsFalse(endpoint.IsExternal);
        Assert.AreEqual("tcp", endpoint.Name);
        Assert.IsNull(endpoint.Port);
        Assert.AreEqual(ProtocolType.Tcp, endpoint.Protocol);
        Assert.AreEqual("tcp", endpoint.Transport);
        Assert.AreEqual("tcp", endpoint.UriScheme);

        var containerAnnotation = Assert.ContainsSingle(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.AreEqual(SqlServerContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.AreEqual(SqlServerContainerImageTags.Image, containerAnnotation.Image);
        Assert.AreEqual(SqlServerContainerImageTags.Registry, containerAnnotation.Registry);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(containerResource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.That.Collection(config,
            env =>
            {
                Assert.AreEqual("ACCEPT_EULA", env.Key);
                Assert.AreEqual("Y", env.Value);
            },
            env =>
            {
                Assert.AreEqual("MSSQL_SA_PASSWORD", env.Key);
                Assert.IsNotNull(env.Value);
                Assert.IsTrue(env.Value.Length >= 8);
            });
    }

    [TestMethod]
    public async Task SqlServerCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var pass = appBuilder.AddParameter("pass", "p@ssw0rd1");
        appBuilder
            .AddSqlServer("sqlserver", pass)
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 1433));

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.ContainsSingle(appModel.Resources.OfType<SqlServerServerResource>());
        var connectionString = await connectionStringResource.GetConnectionStringAsync(default);

        Assert.AreEqual("Server=127.0.0.1,1433;User ID=sa;Password=p@ssw0rd1;TrustServerCertificate=true", connectionString);
        Assert.AreEqual("Server={sqlserver.bindings.tcp.host},{sqlserver.bindings.tcp.port};User ID=sa;Password={pass.value};TrustServerCertificate=true", connectionStringResource.ConnectionStringExpression.ValueExpression);
    }

    [TestMethod]
    public async Task SqlServerDatabaseCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var pass = appBuilder.AddParameter("pass", "p@ssw0rd1");
        appBuilder
            .AddSqlServer("sqlserver", pass)
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 1433))
            .AddDatabase("mydb");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var sqlResource = Assert.ContainsSingle(appModel.Resources.OfType<SqlServerDatabaseResource>());
        var connectionStringResource = (IResourceWithConnectionString)sqlResource;
        var connectionString = await connectionStringResource.GetConnectionStringAsync();

        Assert.AreEqual("Server=127.0.0.1,1433;User ID=sa;Password=p@ssw0rd1;TrustServerCertificate=true;Database=mydb", connectionString);
        Assert.AreEqual("{sqlserver.connectionString};Database=mydb", connectionStringResource.ConnectionStringExpression.ValueExpression);
    }

    [TestMethod]
    public async Task VerifyManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var sqlServer = builder.AddSqlServer("sqlserver");
        var db = sqlServer.AddDatabase("db");

        var serverManifest = await ManifestUtils.GetManifest(sqlServer.Resource);
        var dbManifest = await ManifestUtils.GetManifest(db.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "Server={sqlserver.bindings.tcp.host},{sqlserver.bindings.tcp.port};User ID=sa;Password={sqlserver-password.value};TrustServerCertificate=true",
              "image": "{{SqlServerContainerImageTags.Registry}}/{{SqlServerContainerImageTags.Image}}:{{SqlServerContainerImageTags.Tag}}",
              "env": {
                "ACCEPT_EULA": "Y",
                "MSSQL_SA_PASSWORD": "{sqlserver-password.value}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 1433
                }
              }
            }
            """;
        Assert.AreEqual(expectedManifest, serverManifest.ToString());

        expectedManifest = """
            {
              "type": "value.v0",
              "connectionString": "{sqlserver.connectionString};Database=db"
            }
            """;
        Assert.AreEqual(expectedManifest, dbManifest.ToString());
    }

    [TestMethod]
    public async Task VerifyManifestWithPasswordParameter()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var pass = builder.AddParameter("pass");

        var sqlServer = builder.AddSqlServer("sqlserver", pass);
        var serverManifest = await ManifestUtils.GetManifest(sqlServer.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "Server={sqlserver.bindings.tcp.host},{sqlserver.bindings.tcp.port};User ID=sa;Password={pass.value};TrustServerCertificate=true",
              "image": "{{SqlServerContainerImageTags.Registry}}/{{SqlServerContainerImageTags.Image}}:{{SqlServerContainerImageTags.Tag}}",
              "env": {
                "ACCEPT_EULA": "Y",
                "MSSQL_SA_PASSWORD": "{pass.value}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 1433
                }
              }
            }
            """;
        Assert.AreEqual(expectedManifest, serverManifest.ToString());
    }

    [TestMethod]
    public void ThrowsWithIdenticalChildResourceNames()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var db = builder.AddSqlServer("sqlserver1");
        db.AddDatabase("db");

        Assert.Throws<DistributedApplicationException>(() => db.AddDatabase("db"));
    }

    [TestMethod]
    public void ThrowsWithIdenticalChildResourceNamesDifferentParents()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.AddSqlServer("sqlserver1")
            .AddDatabase("db");

        var db = builder.AddSqlServer("sqlserver2");
        Assert.Throws<DistributedApplicationException>(() => db.AddDatabase("db"));
    }

    [TestMethod]
    public void CanAddDatabasesWithDifferentNamesOnSingleServer()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var sqlserver1 = builder.AddSqlServer("sqlserver1");

        var db1 = sqlserver1.AddDatabase("db1", "customers1");
        var db2 = sqlserver1.AddDatabase("db2", "customers2");

        Assert.AreEqual("customers1", db1.Resource.DatabaseName);
        Assert.AreEqual("customers2", db2.Resource.DatabaseName);

        Assert.AreEqual("{sqlserver1.connectionString};Database=customers1", db1.Resource.ConnectionStringExpression.ValueExpression);
        Assert.AreEqual("{sqlserver1.connectionString};Database=customers2", db2.Resource.ConnectionStringExpression.ValueExpression);
    }

    [TestMethod]
    public void CanAddDatabasesWithTheSameNameOnMultipleServers()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var db1 = builder.AddSqlServer("sqlserver1")
            .AddDatabase("db1", "imports");

        var db2 = builder.AddSqlServer("sqlserver2")
            .AddDatabase("db2", "imports");

        Assert.AreEqual("imports", db1.Resource.DatabaseName);
        Assert.AreEqual("imports", db2.Resource.DatabaseName);

        Assert.AreEqual("{sqlserver1.connectionString};Database=imports", db1.Resource.ConnectionStringExpression.ValueExpression);
        Assert.AreEqual("{sqlserver2.connectionString};Database=imports", db2.Resource.ConnectionStringExpression.ValueExpression);
    }
}
