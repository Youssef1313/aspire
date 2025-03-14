// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.MongoDB.Tests;

[TestClass]
public class AddMongoDBTests
{
    [TestMethod]
    public void AddMongoDBContainerWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddMongoDB("mongodb");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.ContainsSingle(appModel.Resources.OfType<MongoDBServerResource>());
        Assert.AreEqual("mongodb", containerResource.Name);

        var endpoint = Assert.ContainsSingle(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.AreEqual(27017, endpoint.TargetPort);
        Assert.IsFalse(endpoint.IsExternal);
        Assert.AreEqual("tcp", endpoint.Name);
        Assert.IsNull(endpoint.Port);
        Assert.AreEqual(ProtocolType.Tcp, endpoint.Protocol);
        Assert.AreEqual("tcp", endpoint.Transport);
        Assert.AreEqual("tcp", endpoint.UriScheme);

        var containerAnnotation = Assert.ContainsSingle(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.AreEqual(MongoDBContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.AreEqual(MongoDBContainerImageTags.Image, containerAnnotation.Image);
        Assert.AreEqual(MongoDBContainerImageTags.Registry, containerAnnotation.Registry);
    }

    [TestMethod]
    public void AddMongoDBContainerAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddMongoDB("mongodb", 9813);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.ContainsSingle(appModel.Resources.OfType<MongoDBServerResource>());
        Assert.AreEqual("mongodb", containerResource.Name);

        var endpoint = Assert.ContainsSingle(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.AreEqual(27017, endpoint.TargetPort);
        Assert.IsFalse(endpoint.IsExternal);
        Assert.AreEqual("tcp", endpoint.Name);
        Assert.AreEqual(9813, endpoint.Port);
        Assert.AreEqual(ProtocolType.Tcp, endpoint.Protocol);
        Assert.AreEqual("tcp", endpoint.Transport);
        Assert.AreEqual("tcp", endpoint.UriScheme);

        var containerAnnotation = Assert.ContainsSingle(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.AreEqual(MongoDBContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.AreEqual(MongoDBContainerImageTags.Image, containerAnnotation.Image);
        Assert.AreEqual(MongoDBContainerImageTags.Registry, containerAnnotation.Registry);
    }

    [TestMethod]
    public async Task MongoDBCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder
            .AddMongoDB("mongodb")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 27017))
            .AddDatabase("mydatabase");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var dbResource = Assert.ContainsSingle(appModel.Resources.OfType<MongoDBDatabaseResource>());
        var serverResource = dbResource.Parent as IResourceWithConnectionString;
        var connectionStringResource = dbResource as IResourceWithConnectionString;
        Assert.IsNotNull(connectionStringResource);
        var connectionString = await connectionStringResource.GetConnectionStringAsync();
        
        Assert.AreEqual($"mongodb://admin:{dbResource.Parent.PasswordParameter?.Value}@localhost:27017?authSource=admin&authMechanism=SCRAM-SHA-256", await serverResource.GetConnectionStringAsync());
        Assert.AreEqual("mongodb://admin:{mongodb-password.value}@{mongodb.bindings.tcp.host}:{mongodb.bindings.tcp.port}?authSource=admin&authMechanism=SCRAM-SHA-256", serverResource.ConnectionStringExpression.ValueExpression);
        Assert.AreEqual($"mongodb://admin:{dbResource.Parent.PasswordParameter?.Value}@localhost:27017/mydatabase?authSource=admin&authMechanism=SCRAM-SHA-256", connectionString);
        Assert.AreEqual("mongodb://admin:{mongodb-password.value}@{mongodb.bindings.tcp.host}:{mongodb.bindings.tcp.port}/mydatabase?authSource=admin&authMechanism=SCRAM-SHA-256", connectionStringResource.ConnectionStringExpression.ValueExpression);
    }

    [TestMethod]
    public void WithMongoExpressAddsContainer()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddMongoDB("mongo")
            .WithMongoExpress();

        Assert.ContainsSingle(builder.Resources.OfType<MongoExpressContainerResource>());
    }

    [TestMethod]
    public void WithMongoExpressSupportsChangingContainerImageValues()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddMongoDB("mongo").WithMongoExpress(c =>
        {
            c.WithImageRegistry("example.mycompany.com");
            c.WithImage("customongoexpresscontainer");
            c.WithImageTag("someothertag");
        });

        var resource = Assert.ContainsSingle(builder.Resources.OfType<MongoExpressContainerResource>());
        var containerAnnotation = Assert.ContainsSingle(resource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.AreEqual("example.mycompany.com", containerAnnotation.Registry);
        Assert.AreEqual("customongoexpresscontainer", containerAnnotation.Image);
        Assert.AreEqual("someothertag", containerAnnotation.Tag);
    }

    [TestMethod]
    public void WithMongoExpressSupportsChangingHostPort()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddMongoDB("mongo").WithMongoExpress(c =>
        {
            c.WithHostPort(1000);
        });

        var resource = Assert.ContainsSingle(builder.Resources.OfType<MongoExpressContainerResource>());
        var endpoint = Assert.ContainsSingle(resource.Annotations.OfType<EndpointAnnotation>());
        Assert.AreEqual(1000, endpoint.Port);
    }

    [TestMethod]
    public async Task WithMongoExpressUsesContainerHost()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddMongoDB("mongo")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 3000))
            .WithMongoExpress();

        var mongoExpress = Assert.ContainsSingle(builder.Resources.OfType<MongoExpressContainerResource>());

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(mongoExpress, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.That.Collection(env,
            e =>
            {
                Assert.AreEqual("ME_CONFIG_MONGODB_SERVER", e.Key);
                Assert.AreEqual("mongo", e.Value);
            },
            e =>
            {
                Assert.AreEqual("ME_CONFIG_MONGODB_PORT", e.Key);
                Assert.AreEqual("27017", e.Value);
            },
            e =>
            {
                Assert.AreEqual("ME_CONFIG_BASICAUTH", e.Key);
                Assert.AreEqual("false", e.Value);
            },
            e =>
            {
                Assert.AreEqual("ME_CONFIG_MONGODB_ADMINUSERNAME", e.Key);
                Assert.AreEqual("admin", e.Value);
            },
            e =>
            {
                Assert.AreEqual("ME_CONFIG_MONGODB_ADMINPASSWORD", e.Key);
                Assert.NotEmpty(e.Value);
            });
    }

    [TestMethod]
    public void WithMongoExpressOnMultipleResources()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddMongoDB("mongo").WithMongoExpress();
        builder.AddMongoDB("mongo2").WithMongoExpress();

        Assert.AreEqual(2, builder.Resources.OfType<MongoExpressContainerResource>().Count());
    }

    [TestMethod]
    public async Task VerifyManifest()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var mongo = appBuilder.AddMongoDB("mongo");
        var db = mongo.AddDatabase("mydb");

        var mongoManifest = await ManifestUtils.GetManifest(mongo.Resource);
        var dbManifest = await ManifestUtils.GetManifest(db.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "mongodb://admin:{mongo-password.value}@{mongo.bindings.tcp.host}:{mongo.bindings.tcp.port}?authSource=admin\u0026authMechanism=SCRAM-SHA-256",
              "image": "{{MongoDBContainerImageTags.Registry}}/{{MongoDBContainerImageTags.Image}}:{{MongoDBContainerImageTags.Tag}}",
              "env": {
                "MONGO_INITDB_ROOT_USERNAME": "admin",
                "MONGO_INITDB_ROOT_PASSWORD": "{mongo-password.value}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 27017
                }
              }
            }
            """;
        Assert.AreEqual(expectedManifest, mongoManifest.ToString());

        expectedManifest = """
            {
              "type": "value.v0",
              "connectionString": "mongodb://admin:{mongo-password.value}@{mongo.bindings.tcp.host}:{mongo.bindings.tcp.port}/mydb?authSource=admin\u0026authMechanism=SCRAM-SHA-256"
            }
            """;
        Assert.AreEqual(expectedManifest, dbManifest.ToString());
    }

    [TestMethod]
    public void ThrowsWithIdenticalChildResourceNames()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var db = builder.AddMongoDB("mongo1");
        db.AddDatabase("db");

        Assert.Throws<DistributedApplicationException>(() => db.AddDatabase("db"));
    }

    [TestMethod]
    public void ThrowsWithIdenticalChildResourceNamesDifferentParents()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.AddMongoDB("mongo1")
            .AddDatabase("db");

        var db = builder.AddMongoDB("mongo2");
        Assert.Throws<DistributedApplicationException>(() => db.AddDatabase("db"));
    }

    [TestMethod]
    public void CanAddDatabasesWithDifferentNamesOnSingleServer()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var mongo1 = builder.AddMongoDB("mongo1");

        var db1 = mongo1.AddDatabase("db1", "customers1");
        var db2 = mongo1.AddDatabase("db2", "customers2");

        Assert.AreEqual("customers1", db1.Resource.DatabaseName);
        Assert.AreEqual("customers2", db2.Resource.DatabaseName);

        Assert.AreEqual("mongodb://admin:{mongo1-password.value}@{mongo1.bindings.tcp.host}:{mongo1.bindings.tcp.port}/customers1?authSource=admin&authMechanism=SCRAM-SHA-256", db1.Resource.ConnectionStringExpression.ValueExpression);
        Assert.AreEqual("mongodb://admin:{mongo1-password.value}@{mongo1.bindings.tcp.host}:{mongo1.bindings.tcp.port}/customers2?authSource=admin&authMechanism=SCRAM-SHA-256", db2.Resource.ConnectionStringExpression.ValueExpression);
    }

    [TestMethod]
    public void CanAddDatabasesWithTheSameNameOnMultipleServers()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var db1 = builder.AddMongoDB("mongo1")
            .AddDatabase("db1", "imports");

        var db2 = builder.AddMongoDB("mongo2")
            .AddDatabase("db2", "imports");

        Assert.AreEqual("imports", db1.Resource.DatabaseName);
        Assert.AreEqual("imports", db2.Resource.DatabaseName);

        Assert.AreEqual("mongodb://admin:{mongo1-password.value}@{mongo1.bindings.tcp.host}:{mongo1.bindings.tcp.port}/imports?authSource=admin&authMechanism=SCRAM-SHA-256", db1.Resource.ConnectionStringExpression.ValueExpression);
        Assert.AreEqual("mongodb://admin:{mongo2-password.value}@{mongo2.bindings.tcp.host}:{mongo2.bindings.tcp.port}/imports?authSource=admin&authMechanism=SCRAM-SHA-256", db2.Resource.ConnectionStringExpression.ValueExpression);
    }
}
