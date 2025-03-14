// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text.RegularExpressions;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.MySql.Tests;

[TestClass]
public class AddMySqlTests
{
    [TestMethod]
    public void AddMySqlAddsGeneratedPasswordParameterWithUserSecretsParameterDefaultInRunMode()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var mysql = appBuilder.AddMySql("mysql");

        Assert.AreEqual("Aspire.Hosting.ApplicationModel.UserSecretsParameterDefault", mysql.Resource.PasswordParameter.Default?.GetType().FullName);
    }

    [TestMethod]
    public void AddMySqlDoesNotAddGeneratedPasswordParameterWithUserSecretsParameterDefaultInPublishMode()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var mysql = appBuilder.AddMySql("mysql");

        Assert.AreNotEqual("Aspire.Hosting.ApplicationModel.UserSecretsParameterDefault", mysql.Resource.PasswordParameter.Default?.GetType().FullName);
    }

    [TestMethod]
    public async Task AddMySqlContainerWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddMySql("mysql");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.ContainsSingle(appModel.Resources.OfType<MySqlServerResource>());
        Assert.AreEqual("mysql", containerResource.Name);

        var containerAnnotation = Assert.ContainsSingle(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.AreEqual(MySqlContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.AreEqual(MySqlContainerImageTags.Image, containerAnnotation.Image);
        Assert.AreEqual(MySqlContainerImageTags.Registry, containerAnnotation.Registry);

        var endpoint = Assert.ContainsSingle(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.AreEqual(3306, endpoint.TargetPort);
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
                Assert.AreEqual("MYSQL_ROOT_PASSWORD", env.Key);
                Assert.IsFalse(string.IsNullOrEmpty(env.Value));
            });
    }

    [TestMethod]
    public async Task AddMySqlAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var pass = appBuilder.AddParameter("pass", "pass");
        appBuilder.AddMySql("mysql", pass, 1234);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.ContainsSingle(appModel.GetContainerResources());
        Assert.AreEqual("mysql", containerResource.Name);

        var containerAnnotation = Assert.ContainsSingle(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.AreEqual(MySqlContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.AreEqual(MySqlContainerImageTags.Image, containerAnnotation.Image);
        Assert.AreEqual(MySqlContainerImageTags.Registry, containerAnnotation.Registry);

        var endpoint = Assert.ContainsSingle(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.AreEqual(3306, endpoint.TargetPort);
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
                Assert.AreEqual("MYSQL_ROOT_PASSWORD", env.Key);
                Assert.AreEqual("pass", env.Value);
            });
    }

    [TestMethod]
    public async Task MySqlCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddMySql("mysql")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000));

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.ContainsSingle(appModel.Resources.OfType<IResourceWithConnectionString>());
        var connectionString = await connectionStringResource.GetConnectionStringAsync();

        Assert.AreEqual("Server={mysql.bindings.tcp.host};Port={mysql.bindings.tcp.port};User ID=root;Password={mysql-password.value}", connectionStringResource.ConnectionStringExpression.ValueExpression);
        StringAssert.StartsWith(connectionString, "Server=localhost;Port=2000;User ID=root;Password=");
    }

    [TestMethod]
    public async Task MySqlCreatesConnectionStringWithDatabase()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddMySql("mysql")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000))
            .AddDatabase("db");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var mySqlResource = Assert.ContainsSingle(appModel.Resources.OfType<MySqlServerResource>());
        var mySqlConnectionStringResource = (IResourceWithConnectionString)mySqlResource;
        var mySqlConnectionString = await mySqlConnectionStringResource.GetConnectionStringAsync();
        var mySqlDatabaseResource = Assert.ContainsSingle(appModel.Resources.OfType<MySqlDatabaseResource>());
        var mySqlDatabaseConnectionStringResource = (IResourceWithConnectionString)mySqlDatabaseResource;
        var dbConnectionString = await mySqlDatabaseConnectionStringResource.GetConnectionStringAsync();

        Assert.AreEqual(mySqlConnectionString + ";Database=db", dbConnectionString);
        Assert.AreEqual("{mysql.connectionString};Database=db", mySqlDatabaseResource.ConnectionStringExpression.ValueExpression);
    }

    [TestMethod]
    public async Task VerifyManifest()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();
        var mysql = appBuilder.AddMySql("mysql");
        var db = mysql.AddDatabase("db");

        var mySqlManifest = await ManifestUtils.GetManifest(mysql.Resource);
        var dbManifest = await ManifestUtils.GetManifest(db.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "Server={mysql.bindings.tcp.host};Port={mysql.bindings.tcp.port};User ID=root;Password={mysql-password.value}",
              "image": "{{MySqlContainerImageTags.Registry}}/{{MySqlContainerImageTags.Image}}:{{MySqlContainerImageTags.Tag}}",
              "env": {
                "MYSQL_ROOT_PASSWORD": "{mysql-password.value}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 3306
                }
              }
            }
            """;
        Assert.AreEqual(expectedManifest, mySqlManifest.ToString());

        expectedManifest = """
            {
              "type": "value.v0",
              "connectionString": "{mysql.connectionString};Database=db"
            }
            """;
        Assert.AreEqual(expectedManifest, dbManifest.ToString());
    }

    [TestMethod]
    public async Task VerifyManifestWithPasswordParameter()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();
        var pass = appBuilder.AddParameter("pass");

        var mysql = appBuilder.AddMySql("mysql", pass);
        var serverManifest = await ManifestUtils.GetManifest(mysql.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "Server={mysql.bindings.tcp.host};Port={mysql.bindings.tcp.port};User ID=root;Password={pass.value}",
              "image": "{{MySqlContainerImageTags.Registry}}/{{MySqlContainerImageTags.Image}}:{{MySqlContainerImageTags.Tag}}",
              "env": {
                "MYSQL_ROOT_PASSWORD": "{pass.value}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 3306
                }
              }
            }
            """;
        Assert.AreEqual(expectedManifest, serverManifest.ToString());
    }

    [TestMethod]
    public void WithMySqlTwiceEndsUpWithOneAdminContainer()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddMySql("mySql").WithPhpMyAdmin();
        builder.AddMySql("mySql2").WithPhpMyAdmin();

        Assert.ContainsSingle(builder.Resources.OfType<ContainerResource>().Where(resource => resource.Name is "mySql-phpmyadmin"));
    }

    [TestMethod]
    public async Task SingleMySqlInstanceProducesCorrectMySqlHostsVariable()
    {
        var builder = DistributedApplication.CreateBuilder();
        var mysql = builder.AddMySql("mySql").WithPhpMyAdmin();
        using var app = builder.Build();

        // Add fake allocated endpoints.
        mysql.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5001));

        await builder.Eventing.PublishAsync<AfterEndpointsAllocatedEvent>(new(app.Services, app.Services.GetRequiredService<DistributedApplicationModel>()));

        var myAdmin = builder.Resources.Single(r => r.Name.EndsWith("-phpmyadmin"));

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(myAdmin, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        var container = builder.Resources.Single(r => r.Name == "mySql-phpmyadmin");
        Assert.IsEmpty(container.Annotations.OfType<ContainerMountAnnotation>());

        Assert.AreEqual($"{mysql.Resource.Name}:{mysql.Resource.PrimaryEndpoint.TargetPort}", config["PMA_HOST"]);
#pragma warning disable MSTEST0032 // Assertion condition is always true - https://github.com/microsoft/testfx/issues/5241
        Assert.IsNotNull(config["PMA_USER"]);
        Assert.IsNotNull(config["PMA_PASSWORD"]);
#pragma warning restore MSTEST0032 // Assertion condition is always true
    }

    [TestMethod]
    public void WithPhpMyAdminProducesValidServerConfigFile()
    {
        var builder = DistributedApplication.CreateBuilder();

        var tempStorePath = Directory.CreateTempSubdirectory().FullName;
        builder.Configuration["Aspire:Store:Path"] = tempStorePath;

        var mysql1 = builder.AddMySql("mysql1").WithPhpMyAdmin(c => c.WithHostPort(8081));
        var mysql2 = builder.AddMySql("mysql2").WithPhpMyAdmin(c => c.WithHostPort(8081));

        // Add fake allocated endpoints.
        mysql1.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5001));
        mysql2.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5002, "host3"));

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        builder.Eventing.PublishAsync<AfterEndpointsAllocatedEvent>(new(app.Services, app.Services.GetRequiredService<DistributedApplicationModel>()));

        var myAdmin = builder.Resources.Single(r => r.Name.EndsWith("-phpmyadmin"));
        var volume = myAdmin.Annotations.OfType<ContainerMountAnnotation>().Single();

        using var stream = File.OpenRead(volume.Source!);
        var fileContents = new StreamReader(stream).ReadToEnd();

        // check to see that the two hosts are in the file
        string pattern1 = $@"\$cfg\['Servers'\]\[\$i\]\['host'\] = '{mysql1.Resource.Name}:{mysql1.Resource.PrimaryEndpoint.TargetPort}';";
        string pattern2 = $@"\$cfg\['Servers'\]\[\$i\]\['host'\] = '{mysql2.Resource.Name}:{mysql2.Resource.PrimaryEndpoint.TargetPort}';";
        Match match1 = Regex.Match(fileContents, pattern1);
        Assert.IsTrue(match1.Success);
        Match match2 = Regex.Match(fileContents, pattern2);
        Assert.IsTrue(match2.Success);

        try
        {
            Directory.Delete(tempStorePath, true);
        }
        catch
        {
            // Ignore.
        }
    }

    [TestMethod]
    public void ThrowsWithIdenticalChildResourceNames()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var db = builder.AddMySql("mysql1");
        db.AddDatabase("db");

        Assert.Throws<DistributedApplicationException>(() => db.AddDatabase("db"));
    }

    [TestMethod]
    public void ThrowsWithIdenticalChildResourceNamesDifferentParents()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.AddMySql("mysql1")
            .AddDatabase("db");

        var db = builder.AddMySql("mysql2");
        Assert.Throws<DistributedApplicationException>(() => db.AddDatabase("db"));
    }

    [TestMethod]
    public void CanAddDatabasesWithDifferentNamesOnSingleServer()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var mysql1 = builder.AddMySql("mysql1");

        var db1 = mysql1.AddDatabase("db1", "customers1");
        var db2 = mysql1.AddDatabase("db2", "customers2");

        Assert.AreEqual(["db1", "db2"], mysql1.Resource.Databases.Keys);
        Assert.AreEqual(["customers1", "customers2"], mysql1.Resource.Databases.Values);

        Assert.AreEqual("customers1", db1.Resource.DatabaseName);
        Assert.AreEqual("customers2", db2.Resource.DatabaseName);

        Assert.AreEqual("{mysql1.connectionString};Database=customers1", db1.Resource.ConnectionStringExpression.ValueExpression);
        Assert.AreEqual("{mysql1.connectionString};Database=customers2", db2.Resource.ConnectionStringExpression.ValueExpression);
    }

    [TestMethod]
    public void CanAddDatabasesWithTheSameNameOnMultipleServers()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var db1 = builder.AddMySql("mysql1")
            .AddDatabase("db1", "imports");

        var db2 = builder.AddMySql("mysql2")
            .AddDatabase("db2", "imports");

        Assert.AreEqual("imports", db1.Resource.DatabaseName);
        Assert.AreEqual("imports", db2.Resource.DatabaseName);

        Assert.AreEqual("{mysql1.connectionString};Database=imports", db1.Resource.ConnectionStringExpression.ValueExpression);
        Assert.AreEqual("{mysql2.connectionString};Database=imports", db2.Resource.ConnectionStringExpression.ValueExpression);
    }
}
