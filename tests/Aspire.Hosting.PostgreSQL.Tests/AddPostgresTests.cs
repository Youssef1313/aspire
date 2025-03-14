// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Postgres;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.PostgreSQL.Tests;

[TestClass]
public class AddPostgresTests
{
    [TestMethod]
    public void AddPostgresAddsHealthCheckAnnotationToResource()
    {
        var builder = DistributedApplication.CreateBuilder();
        var redis = builder.AddPostgres("postgres");
        Assert.ContainsSingle(redis.Resource.Annotations, a => a is HealthCheckAnnotation hca && hca.Key == "postgres_check");
    }

    [TestMethod]
    public void AddPostgresAddsGeneratedPasswordParameterWithUserSecretsParameterDefaultInRunMode()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var pg = appBuilder.AddPostgres("pg");

        Assert.AreEqual("Aspire.Hosting.ApplicationModel.UserSecretsParameterDefault", pg.Resource.PasswordParameter.Default?.GetType().FullName);
    }

    [TestMethod]
    public void AddPostgresDoesNotAddGeneratedPasswordParameterWithUserSecretsParameterDefaultInPublishMode()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var pg = appBuilder.AddPostgres("pg");

        Assert.AreNotEqual("Aspire.Hosting.ApplicationModel.UserSecretsParameterDefault", pg.Resource.PasswordParameter.Default?.GetType().FullName);
    }

    [TestMethod]
    public async Task AddPostgresWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddPostgres("myPostgres");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.ContainsSingle(appModel.GetContainerResources());
        Assert.AreEqual("myPostgres", containerResource.Name);

        var containerAnnotation = Assert.ContainsSingle(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.AreEqual(PostgresContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.AreEqual(PostgresContainerImageTags.Image, containerAnnotation.Image);
        Assert.AreEqual(PostgresContainerImageTags.Registry, containerAnnotation.Registry);

        var endpoint = Assert.ContainsSingle(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.AreEqual(5432, endpoint.TargetPort);
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
                Assert.AreEqual("POSTGRES_HOST_AUTH_METHOD", env.Key);
                Assert.AreEqual("scram-sha-256", env.Value);
            },
            env =>
            {
                Assert.AreEqual("POSTGRES_INITDB_ARGS", env.Key);
                Assert.AreEqual("--auth-host=scram-sha-256 --auth-local=scram-sha-256", env.Value);
            },
            env =>
            {
                Assert.AreEqual("POSTGRES_USER", env.Key);
                Assert.AreEqual("postgres", env.Value);
            },
            env =>
            {
                Assert.AreEqual("POSTGRES_PASSWORD", env.Key);
                Assert.IsFalse(string.IsNullOrEmpty(env.Value));
            });
    }

    [TestMethod]
    public async Task AddPostgresAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var pass = appBuilder.AddParameter("pass", "pass");
        appBuilder.AddPostgres("myPostgres", password: pass, port: 1234);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.ContainsSingle(appModel.GetContainerResources());
        Assert.AreEqual("myPostgres", containerResource.Name);

        var containerAnnotation = Assert.ContainsSingle(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.AreEqual(PostgresContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.AreEqual(PostgresContainerImageTags.Image, containerAnnotation.Image);
        Assert.AreEqual(PostgresContainerImageTags.Registry, containerAnnotation.Registry);

        var endpoint = Assert.ContainsSingle(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.AreEqual(5432, endpoint.TargetPort);
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
                Assert.AreEqual("POSTGRES_HOST_AUTH_METHOD", env.Key);
                Assert.AreEqual("scram-sha-256", env.Value);
            },
            env =>
            {
                Assert.AreEqual("POSTGRES_INITDB_ARGS", env.Key);
                Assert.AreEqual("--auth-host=scram-sha-256 --auth-local=scram-sha-256", env.Value);
            },
            env =>
            {
                Assert.AreEqual("POSTGRES_USER", env.Key);
                Assert.AreEqual("postgres", env.Value);
            },
            env =>
            {
                Assert.AreEqual("POSTGRES_PASSWORD", env.Key);
                Assert.AreEqual("pass", env.Value);
            });
    }

    [TestMethod]
    public async Task PostgresCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var postgres = appBuilder.AddPostgres("postgres")
                                 .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000));

        var connectionStringResource = postgres.Resource as IResourceWithConnectionString;

        var connectionString = await connectionStringResource.GetConnectionStringAsync();
        Assert.AreEqual("Host={postgres.bindings.tcp.host};Port={postgres.bindings.tcp.port};Username=postgres;Password={postgres-password.value}", connectionStringResource.ConnectionStringExpression.ValueExpression);
        Assert.AreEqual($"Host=localhost;Port=2000;Username=postgres;Password={postgres.Resource.PasswordParameter.Value}", connectionString);
    }

    [TestMethod]
    public async Task PostgresCreatesConnectionStringWithDatabase()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddPostgres("postgres")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000))
            .AddDatabase("db");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var postgresResource = Assert.ContainsSingle(appModel.Resources.OfType<PostgresServerResource>());
        var postgresConnectionString = await postgresResource.GetConnectionStringAsync();
        var postgresDatabaseResource = Assert.ContainsSingle(appModel.Resources.OfType<PostgresDatabaseResource>());
        var postgresDatabaseConnectionStringResource = (IResourceWithConnectionString)postgresDatabaseResource;
        var dbConnectionString = await postgresDatabaseConnectionStringResource.GetConnectionStringAsync();

        Assert.AreEqual("{postgres.connectionString};Database=db", postgresDatabaseResource.ConnectionStringExpression.ValueExpression);
        Assert.AreEqual(postgresConnectionString + ";Database=db", dbConnectionString);
    }

    [TestMethod]
    public async Task AddDatabaseToPostgresAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var pass = appBuilder.AddParameter("pass", "pass");
        appBuilder.AddPostgres("postgres", password: pass, port: 1234).AddDatabase("db");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var containerResources = appModel.GetContainerResources();

        var containerResource = Assert.ContainsSingle(containerResources);
        Assert.AreEqual("postgres", containerResource.Name);

        var containerAnnotation = Assert.ContainsSingle(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.AreEqual(PostgresContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.AreEqual(PostgresContainerImageTags.Image, containerAnnotation.Image);
        Assert.AreEqual(PostgresContainerImageTags.Registry, containerAnnotation.Registry);

        var endpoint = Assert.ContainsSingle(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.AreEqual(5432, endpoint.TargetPort);
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
                Assert.AreEqual("POSTGRES_HOST_AUTH_METHOD", env.Key);
                Assert.AreEqual("scram-sha-256", env.Value);
            },
            env =>
            {
                Assert.AreEqual("POSTGRES_INITDB_ARGS", env.Key);
                Assert.AreEqual("--auth-host=scram-sha-256 --auth-local=scram-sha-256", env.Value);
            },
            env =>
            {
                Assert.AreEqual("POSTGRES_USER", env.Key);
                Assert.AreEqual("postgres", env.Value);
            },
            env =>
            {
                Assert.AreEqual("POSTGRES_PASSWORD", env.Key);
                Assert.AreEqual("pass", env.Value);
            });
    }

    [TestMethod]
    public async Task VerifyManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var pgServer = builder.AddPostgres("pg");
        var db = pgServer.AddDatabase("db");

        var serverManifest = await ManifestUtils.GetManifest(pgServer.Resource);
        var dbManifest = await ManifestUtils.GetManifest(db.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "Host={pg.bindings.tcp.host};Port={pg.bindings.tcp.port};Username=postgres;Password={pg-password.value}",
              "image": "{{PostgresContainerImageTags.Registry}}/{{PostgresContainerImageTags.Image}}:{{PostgresContainerImageTags.Tag}}",
              "env": {
                "POSTGRES_HOST_AUTH_METHOD": "scram-sha-256",
                "POSTGRES_INITDB_ARGS": "--auth-host=scram-sha-256 --auth-local=scram-sha-256",
                "POSTGRES_USER": "postgres",
                "POSTGRES_PASSWORD": "{pg-password.value}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 5432
                }
              }
            }
            """;
        Assert.AreEqual(expectedManifest, serverManifest.ToString());

        expectedManifest = """
            {
              "type": "value.v0",
              "connectionString": "{pg.connectionString};Database=db"
            }
            """;
        Assert.AreEqual(expectedManifest, dbManifest.ToString());
    }

    [TestMethod]
    public async Task VerifyManifestWithParameters()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var userNameParameter = builder.AddParameter("user");
        var passwordParameter = builder.AddParameter("pass");

        var pgServer = builder.AddPostgres("pg", userNameParameter, passwordParameter);
        var serverManifest = await ManifestUtils.GetManifest(pgServer.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "Host={pg.bindings.tcp.host};Port={pg.bindings.tcp.port};Username={user.value};Password={pass.value}",
              "image": "{{PostgresContainerImageTags.Registry}}/{{PostgresContainerImageTags.Image}}:{{PostgresContainerImageTags.Tag}}",
              "env": {
                "POSTGRES_HOST_AUTH_METHOD": "scram-sha-256",
                "POSTGRES_INITDB_ARGS": "--auth-host=scram-sha-256 --auth-local=scram-sha-256",
                "POSTGRES_USER": "{user.value}",
                "POSTGRES_PASSWORD": "{pass.value}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 5432
                }
              }
            }
            """;
        Assert.AreEqual(expectedManifest, serverManifest.ToString());

        pgServer = builder.AddPostgres("pg2", userNameParameter);
        serverManifest = await ManifestUtils.GetManifest(pgServer.Resource);

        expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "Host={pg2.bindings.tcp.host};Port={pg2.bindings.tcp.port};Username={user.value};Password={pg2-password.value}",
              "image": "{{PostgresContainerImageTags.Registry}}/{{PostgresContainerImageTags.Image}}:{{PostgresContainerImageTags.Tag}}",
              "env": {
                "POSTGRES_HOST_AUTH_METHOD": "scram-sha-256",
                "POSTGRES_INITDB_ARGS": "--auth-host=scram-sha-256 --auth-local=scram-sha-256",
                "POSTGRES_USER": "{user.value}",
                "POSTGRES_PASSWORD": "{pg2-password.value}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 5432
                }
              }
            }
            """;
        Assert.AreEqual(expectedManifest, serverManifest.ToString());

        pgServer = builder.AddPostgres("pg3", password: passwordParameter);
        serverManifest = await ManifestUtils.GetManifest(pgServer.Resource);

        expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "Host={pg3.bindings.tcp.host};Port={pg3.bindings.tcp.port};Username=postgres;Password={pass.value}",
              "image": "{{PostgresContainerImageTags.Registry}}/{{PostgresContainerImageTags.Image}}:{{PostgresContainerImageTags.Tag}}",
              "env": {
                "POSTGRES_HOST_AUTH_METHOD": "scram-sha-256",
                "POSTGRES_INITDB_ARGS": "--auth-host=scram-sha-256 --auth-local=scram-sha-256",
                "POSTGRES_USER": "postgres",
                "POSTGRES_PASSWORD": "{pass.value}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 5432
                }
              }
            }
            """;
        Assert.AreEqual(expectedManifest, serverManifest.ToString());
    }

    [TestMethod]
    public async Task WithPgAdminAddsContainer()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddPostgres("mypostgres").WithPgAdmin(pga => pga.WithHostPort(8081));

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // The mount annotation is added in the AfterEndpointsAllocatedEvent.
        await builder.Eventing.PublishAsync<AfterEndpointsAllocatedEvent>(new(app.Services, app.Services.GetRequiredService<DistributedApplicationModel>()));

        var container = builder.Resources.Single(r => r.Name == "mypostgres-pgadmin");
        var volume = container.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.IsTrue(File.Exists(volume.Source)); // File should exist, but will be empty.
        Assert.AreEqual("/pgadmin4/servers.json", volume.Target);
    }

    [TestMethod]
    public void WithPgWebAddsWithPgWebResource()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddPostgres("mypostgres1").WithPgWeb();
        builder.AddPostgres("mypostgres2").WithPgWeb();

        Assert.ContainsSingle(builder.Resources.OfType<PgWebContainerResource>());
    }

    [TestMethod]
    public void WithPgWebSupportsChangingContainerImageValues()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddPostgres("mypostgres").WithPgWeb(c =>
        {
            c.WithImageRegistry("example.mycompany.com");
            c.WithImage("customrediscommander");
            c.WithImageTag("someothertag");
        });

        var resource = Assert.ContainsSingle(builder.Resources.OfType<PgWebContainerResource>());
        var containerAnnotation = Assert.ContainsSingle(resource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.AreEqual("example.mycompany.com", containerAnnotation.Registry);
        Assert.AreEqual("customrediscommander", containerAnnotation.Image);
        Assert.AreEqual("someothertag", containerAnnotation.Tag);
    }

    [TestMethod]
    public void WithRedisInsightSupportsChangingHostPort()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddPostgres("mypostgres").WithPgWeb(c =>
        {
            c.WithHostPort(1000);
        });

        var resource = Assert.ContainsSingle(builder.Resources.OfType<PgWebContainerResource>());
        var endpoint = Assert.ContainsSingle(resource.Annotations.OfType<EndpointAnnotation>());
        Assert.AreEqual(1000, endpoint.Port);
    }

    [TestMethod]
    public void WithPgAdminWithCallbackMutatesImage()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddPostgres("mypostgres").WithPgAdmin(pga => pga.WithImageTag("8.3"));

        var container = builder.Resources.Single(r => r.Name == "mypostgres-pgadmin");
        var imageAnnotation = container.Annotations.OfType<ContainerImageAnnotation>().Single();

        Assert.AreEqual("8.3", imageAnnotation.Tag);
    }

    [TestMethod]
    public void WithPostgresTwiceEndsUpWithOneContainer()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddPostgres("mypostgres1").WithPgAdmin(pga => pga.WithHostPort(8081));
        builder.AddPostgres("mypostgres2").WithPgAdmin(pga => pga.WithHostPort(8081));

        Assert.ContainsSingle(builder.Resources.Where(r => r.Name.EndsWith("-pgadmin")));
    }

    [TestMethod]
    public async Task WithPostgresProducesValidServersJsonFile()
    {
        var builder = DistributedApplication.CreateBuilder();

        var tempStorePath = Directory.CreateTempSubdirectory().FullName;
        builder.Configuration["Aspire:Store:Path"] = tempStorePath;

        var username = builder.AddParameter("pg-user", "myuser");
        var pg1 = builder.AddPostgres("mypostgres1").WithPgAdmin(pga => pga.WithHostPort(8081));
        var pg2 = builder.AddPostgres("mypostgres2", username).WithPgAdmin(pga => pga.WithHostPort(8081));

        // Add fake allocated endpoints.
        pg1.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5001));
        pg2.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5002, "host2"));

        using var app = builder.Build();

        await builder.Eventing.PublishAsync<AfterEndpointsAllocatedEvent>(new(app.Services, app.Services.GetRequiredService<DistributedApplicationModel>()));

        var pgadmin = builder.Resources.Single(r => r.Name.EndsWith("-pgadmin"));
        var volume = pgadmin.Annotations.OfType<ContainerMountAnnotation>().Single();

        using var stream = File.OpenRead(volume.Source!);
        var document = JsonDocument.Parse(stream);

        var servers = document.RootElement.GetProperty("Servers");

        // Make sure the first server is correct.
        Assert.AreEqual(pg1.Resource.Name, servers.GetProperty("1").GetProperty("Name").GetString());
        Assert.AreEqual("Servers", servers.GetProperty("1").GetProperty("Group").GetString());
        Assert.AreEqual("mypostgres1", servers.GetProperty("1").GetProperty("Host").GetString());
        Assert.AreEqual(5432, servers.GetProperty("1").GetProperty("Port").GetInt32());
        Assert.AreEqual("postgres", servers.GetProperty("1").GetProperty("Username").GetString());
        Assert.AreEqual("prefer", servers.GetProperty("1").GetProperty("SSLMode").GetString());
        Assert.AreEqual("postgres", servers.GetProperty("1").GetProperty("MaintenanceDB").GetString());
        Assert.AreEqual($"echo '{pg1.Resource.PasswordParameter.Value}'", servers.GetProperty("1").GetProperty("PasswordExecCommand").GetString());

        // Make sure the second server is correct.
        Assert.AreEqual(pg2.Resource.Name, servers.GetProperty("2").GetProperty("Name").GetString());
        Assert.AreEqual("Servers", servers.GetProperty("2").GetProperty("Group").GetString());
        Assert.AreEqual("mypostgres2", servers.GetProperty("2").GetProperty("Host").GetString());
        Assert.AreEqual(5432, servers.GetProperty("2").GetProperty("Port").GetInt32());
        Assert.AreEqual("myuser", servers.GetProperty("2").GetProperty("Username").GetString());
        Assert.AreEqual("prefer", servers.GetProperty("2").GetProperty("SSLMode").GetString());
        Assert.AreEqual("postgres", servers.GetProperty("2").GetProperty("MaintenanceDB").GetString());
        Assert.AreEqual($"echo '{pg2.Resource.PasswordParameter.Value}'", servers.GetProperty("2").GetProperty("PasswordExecCommand").GetString());

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
    public async Task WithPgwebProducesValidBookmarkFiles()
    {
        var builder = DistributedApplication.CreateBuilder();

        var tempStorePath = Directory.CreateTempSubdirectory().FullName;
        builder.Configuration["Aspire:Store:Path"] = tempStorePath;

        var pg1 = builder.AddPostgres("mypostgres1").WithPgWeb(pga => pga.WithHostPort(8081));
        var pg2 = builder.AddPostgres("mypostgres2").WithPgWeb(pga => pga.WithHostPort(8081));

        // Add fake allocated endpoints.
        pg1.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5001));
        pg2.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5002, "host2"));

        var db1 = pg1.AddDatabase("db1");
        var db2 = pg2.AddDatabase("db2");

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        await builder.Eventing.PublishAsync<AfterEndpointsAllocatedEvent>(new(app.Services, app.Services.GetRequiredService<DistributedApplicationModel>()));

        var pgadmin = builder.Resources.Single(r => r.Name.EndsWith("-pgweb"));
        var volume = pgadmin.Annotations.OfType<ContainerMountAnnotation>().Single();

        var bookMarkFiles = Directory.GetFiles(volume.Source!).OrderBy(f => f).ToArray();

        Assert.That.Collection(bookMarkFiles,
            filePath =>
            {
                Assert.AreEqual(".toml", Path.GetExtension(filePath));
            },
            filePath =>
            {
                Assert.AreEqual(".toml", Path.GetExtension(filePath));
            });

        var bookmarkFilesContent = new List<string>();

        foreach (var filePath in bookMarkFiles)
        {
            bookmarkFilesContent.Add(File.ReadAllText(filePath));
        }

        Assert.NotEmpty(bookmarkFilesContent);
        Assert.That.Collection(bookmarkFilesContent,
            content =>
            {
                Assert.AreEqual(CreatePgWebBookmarkfileContent(db1.Resource), content);
            },
            content =>
            {
                Assert.AreEqual(CreatePgWebBookmarkfileContent(db2.Resource), content);
            });

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

        var db = builder.AddPostgres("postgres1");
        db.AddDatabase("db");

        Assert.Throws<DistributedApplicationException>(() => db.AddDatabase("db"));
    }

    [TestMethod]
    public void ThrowsWithIdenticalChildResourceNamesDifferentParents()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.AddPostgres("postgres1")
            .AddDatabase("db");

        var db = builder.AddPostgres("postgres2");
        Assert.Throws<DistributedApplicationException>(() => db.AddDatabase("db"));
    }

    [TestMethod]
    public void CanAddDatabasesWithDifferentNamesOnSingleServer()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var postgres1 = builder.AddPostgres("postgres1");

        var db1 = postgres1.AddDatabase("db1", "customers1");
        var db2 = postgres1.AddDatabase("db2", "customers2");

        Assert.AreEqual("customers1", db1.Resource.DatabaseName);
        Assert.AreEqual("customers2", db2.Resource.DatabaseName);

        Assert.AreEqual("{postgres1.connectionString};Database=customers1", db1.Resource.ConnectionStringExpression.ValueExpression);
        Assert.AreEqual("{postgres1.connectionString};Database=customers2", db2.Resource.ConnectionStringExpression.ValueExpression);
    }

    [TestMethod]
    public void CanAddDatabasesWithTheSameNameOnMultipleServers()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var db1 = builder.AddPostgres("postgres1")
            .AddDatabase("db1", "imports");

        var db2 = builder.AddPostgres("postgres2")
            .AddDatabase("db2", "imports");

        Assert.AreEqual("imports", db1.Resource.DatabaseName);
        Assert.AreEqual("imports", db2.Resource.DatabaseName);

        Assert.AreEqual("{postgres1.connectionString};Database=imports", db1.Resource.ConnectionStringExpression.ValueExpression);
        Assert.AreEqual("{postgres2.connectionString};Database=imports", db2.Resource.ConnectionStringExpression.ValueExpression);
    }

    private static string CreatePgWebBookmarkfileContent(PostgresDatabaseResource postgresDatabase)
    {
        var user = postgresDatabase.Parent.UserNameParameter?.Value ?? "postgres";

        // We're hardcoding references to container resources based on a default Aspire network
        // This will need to be refactored once updated service discovery APIs are available
        var fileContent = $"""
                host = "{postgresDatabase.Parent.Name}"
                port = {postgresDatabase.Parent.PrimaryEndpoint.TargetPort}
                user = "{user}"
                password = "{postgresDatabase.Parent.PasswordParameter.Value}"
                database = "{postgresDatabase.DatabaseName}"
                sslmode = "disable"
                """;

        return fileContent;
    }
}
