// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Oracle.Tests;

[TestClass]
public class AddOracleTests
{
    [TestMethod]
    public void AddOracleAddsGeneratedPasswordParameterWithUserSecretsParameterDefaultInRunMode()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var orcl = appBuilder.AddOracle("orcl");

        Assert.AreEqual("Aspire.Hosting.ApplicationModel.UserSecretsParameterDefault", orcl.Resource.PasswordParameter.Default?.GetType().FullName);
    }

    [TestMethod]
    public void AddOracleDoesNotAddGeneratedPasswordParameterWithUserSecretsParameterDefaultInPublishMode()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var orcl = appBuilder.AddOracle("orcl");

        Assert.AreNotEqual("Aspire.Hosting.ApplicationModel.UserSecretsParameterDefault", orcl.Resource.PasswordParameter.Default?.GetType().FullName);
    }

    [TestMethod]
    public async Task AddOracleWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddOracle("orcl");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.ContainsSingle(appModel.GetContainerResources());
        Assert.AreEqual("orcl", containerResource.Name);

        var containerAnnotation = Assert.ContainsSingle(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.AreEqual(OracleContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.AreEqual(OracleContainerImageTags.Image, containerAnnotation.Image);
        Assert.AreEqual(OracleContainerImageTags.Registry, containerAnnotation.Registry);

        var endpoint = Assert.ContainsSingle(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.AreEqual(1521, endpoint.TargetPort);
        Assert.IsFalse(endpoint.IsExternal);
        Assert.AreEqual("tcp", endpoint.Name);
        Assert.IsNull(endpoint.Port);
        Assert.AreEqual(ProtocolType.Tcp, endpoint.Protocol);
        Assert.AreEqual("tcp", endpoint.Transport);
        Assert.AreEqual("tcp", endpoint.UriScheme);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(containerResource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.That.Collection(config,
            env =>
            {
                Assert.AreEqual("ORACLE_PWD", env.Key);
                Assert.IsFalse(string.IsNullOrEmpty(env.Value));
            });
    }

    [TestMethod]
    public async Task AddOracleAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var pass = appBuilder.AddParameter("pass", "pass");
        appBuilder.AddOracle("orcl", pass, 1234);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.ContainsSingle(appModel.GetContainerResources());
        Assert.AreEqual("orcl", containerResource.Name);

        var containerAnnotation = Assert.ContainsSingle(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.AreEqual(OracleContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.AreEqual(OracleContainerImageTags.Image, containerAnnotation.Image);
        Assert.AreEqual(OracleContainerImageTags.Registry, containerAnnotation.Registry);

        var endpoint = Assert.ContainsSingle(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.AreEqual(1521, endpoint.TargetPort);
        Assert.IsFalse(endpoint.IsExternal);
        Assert.AreEqual("tcp", endpoint.Name);
        Assert.AreEqual(1234, endpoint.Port);
        Assert.AreEqual(ProtocolType.Tcp, endpoint.Protocol);
        Assert.AreEqual("tcp", endpoint.Transport);
        Assert.AreEqual("tcp", endpoint.UriScheme);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(containerResource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.That.Collection(config,
            env =>
            {
                Assert.AreEqual("ORACLE_PWD", env.Key);
                Assert.AreEqual("pass", env.Value);
            });
    }

    [TestMethod]
    public async Task OracleCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddOracle("orcl")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000));

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.ContainsSingle(appModel.Resources.OfType<IResourceWithConnectionString>());
        var connectionString = await connectionStringResource.GetConnectionStringAsync(default);

        Assert.AreEqual("user id=system;password={orcl-password.value};data source={orcl.bindings.tcp.host}:{orcl.bindings.tcp.port}", connectionStringResource.ConnectionStringExpression.ValueExpression);
        Assert.StartsWith("user id=system;password=", connectionString);
        Assert.EndsWith(";data source=localhost:2000", connectionString);
    }

    [TestMethod]
    public async Task OracleCreatesConnectionStringWithDatabase()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddOracle("orcl")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000))
            .AddDatabase("db");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var oracleResource = Assert.ContainsSingle(appModel.Resources.OfType<OracleDatabaseServerResource>());
        var oracleConnectionStringResource = (IResourceWithConnectionString)oracleResource;
        var oracleConnectionString = oracleConnectionStringResource.GetConnectionStringAsync();
        var oracleDatabaseResource = Assert.ContainsSingle(appModel.Resources.OfType<OracleDatabaseResource>());
        var oracleDatabaseConnectionStringResource = (IResourceWithConnectionString)oracleDatabaseResource;
        var dbConnectionString = await oracleDatabaseConnectionStringResource.GetConnectionStringAsync();

        Assert.AreEqual("{orcl.connectionString}/db", oracleDatabaseConnectionStringResource.ConnectionStringExpression.ValueExpression);
        Assert.AreEqual(oracleConnectionString + "/db", dbConnectionString);
    }

    [TestMethod]
    public async Task AddDatabaseToOracleDatabaseAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var pass = appBuilder.AddParameter("pass", "pass");
        appBuilder.AddOracle("oracle", pass, 1234).AddDatabase("db");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var containerResources = appModel.GetContainerResources();

        var containerResource = Assert.ContainsSingle(containerResources);
        Assert.AreEqual("oracle", containerResource.Name);

        var containerAnnotation = Assert.ContainsSingle(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.AreEqual(OracleContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.AreEqual(OracleContainerImageTags.Image, containerAnnotation.Image);
        Assert.AreEqual(OracleContainerImageTags.Registry, containerAnnotation.Registry);

        var endpoint = Assert.ContainsSingle(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.AreEqual(1521, endpoint.TargetPort);
        Assert.IsFalse(endpoint.IsExternal);
        Assert.AreEqual("tcp", endpoint.Name);
        Assert.AreEqual(1234, endpoint.Port);
        Assert.AreEqual(ProtocolType.Tcp, endpoint.Protocol);
        Assert.AreEqual("tcp", endpoint.Transport);
        Assert.AreEqual("tcp", endpoint.UriScheme);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(containerResource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.That.Collection(config,
            env =>
            {
                Assert.AreEqual("ORACLE_PWD", env.Key);
                Assert.AreEqual("pass", env.Value);
            });
    }

    [TestMethod]
    public async Task VerifyManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var oracleServer = builder.AddOracle("oracle");
        var db = oracleServer.AddDatabase("db");

        var serverManifest = await ManifestUtils.GetManifest(oracleServer.Resource);
        var dbManifest = await ManifestUtils.GetManifest(db.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "user id=system;password={oracle-password.value};data source={oracle.bindings.tcp.host}:{oracle.bindings.tcp.port}",
              "image": "{{OracleContainerImageTags.Registry}}/{{OracleContainerImageTags.Image}}:{{OracleContainerImageTags.Tag}}",
              "env": {
                "ORACLE_PWD": "{oracle-password.value}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 1521
                }
              }
            }
            """;
        Assert.AreEqual(expectedManifest, serverManifest.ToString());

        expectedManifest = """
            {
              "type": "value.v0",
              "connectionString": "{oracle.connectionString}/db"
            }
            """;
        Assert.AreEqual(expectedManifest, dbManifest.ToString());
    }

    [TestMethod]
    public async Task VerifyManifestWithPasswordParameter()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var pass = builder.AddParameter("pass");

        var oracleServer = builder.AddOracle("oracle", pass);
        var serverManifest = await ManifestUtils.GetManifest(oracleServer.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "user id=system;password={pass.value};data source={oracle.bindings.tcp.host}:{oracle.bindings.tcp.port}",
              "image": "{{OracleContainerImageTags.Registry}}/{{OracleContainerImageTags.Image}}:{{OracleContainerImageTags.Tag}}",
              "env": {
                "ORACLE_PWD": "{pass.value}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 1521
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

        var db = builder.AddOracle("oracle1");
        db.AddDatabase("db");

        Assert.Throws<DistributedApplicationException>(() => db.AddDatabase("db"));
    }

    [TestMethod]
    public void ThrowsWithIdenticalChildResourceNamesDifferentParents()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.AddOracle("oracle1")
            .AddDatabase("db");

        var db = builder.AddOracle("oracle2");
        Assert.Throws<DistributedApplicationException>(() => db.AddDatabase("db"));
    }

    [TestMethod]
    public void CanAddDatabasesWithDifferentNamesOnSingleServer()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var oracle1 = builder.AddOracle("oracle1");

        var db1 = oracle1.AddDatabase("db1", "customers1");
        var db2 = oracle1.AddDatabase("db2", "customers2");

        Assert.AreEqual("customers1", db1.Resource.DatabaseName);
        Assert.AreEqual("customers2", db2.Resource.DatabaseName);

        Assert.AreEqual("{oracle1.connectionString}/customers1", db1.Resource.ConnectionStringExpression.ValueExpression);
        Assert.AreEqual("{oracle1.connectionString}/customers2", db2.Resource.ConnectionStringExpression.ValueExpression);
    }

    [TestMethod]
    public void CanAddDatabasesWithTheSameNameOnMultipleServers()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var db1 = builder.AddOracle("oracle1")
            .AddDatabase("db1", "imports");

        var db2 = builder.AddOracle("oracle2")
            .AddDatabase("db2", "imports");

        Assert.AreEqual("imports", db1.Resource.DatabaseName);
        Assert.AreEqual("imports", db2.Resource.DatabaseName);

        Assert.AreEqual("{oracle1.connectionString}/imports", db1.Resource.ConnectionStringExpression.ValueExpression);
        Assert.AreEqual("{oracle2.connectionString}/imports", db2.Resource.ConnectionStringExpression.ValueExpression);
    }
}
