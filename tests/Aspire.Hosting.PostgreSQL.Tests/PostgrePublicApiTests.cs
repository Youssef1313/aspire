// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Postgres;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.PostgreSQL.Tests;

[TestClass]
public class PostgresPublicApiTests
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void CtorPgAdminContainerResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var name = isNull ? null! : string.Empty;

        var action = () => new PgAdminContainerResource(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void CtorPgWebContainerResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var name = isNull ? null! : string.Empty;

        var action = () => new PgWebContainerResource(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void AddPostgresShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "PostgreSql";

        var action = () => builder.AddPostgres(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddPostgresShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddPostgres(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void AddDatabaseShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<PostgresServerResource> builder = null!;
        const string name = "PostgreDb";

        var action = () => builder.AddDatabase(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddDatabaseShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create()
            .AddPostgres("Postgres");
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddDatabase(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void WithPgAdminShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<PostgresServerResource> builder = null!;

        var action = () => builder.WithPgAdmin();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void WithHostPortForPgAdminContainerResourceShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<PgAdminContainerResource> builder = null!;
        int? port = null;

        var action = () => builder.WithHostPort(port);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void WithHostPortForPgWebContainerResourceShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<PgAdminContainerResource> builder = null!;
        int? port = null;

        var action = () => builder.WithHostPort(port);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void WithPgWebShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<PostgresServerResource> builder = null!;

        var action = () => builder.WithPgWeb();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void WithDataVolumeShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<PostgresServerResource> builder = null!;

        var action = () => builder.WithDataVolume();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void WithDataBindMountShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<PostgresServerResource> builder = null!;
        const string source = "/var/lib/postgresql/data";

        var action = () => builder.WithDataBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void WithDataBindMountShouldThrowWhenSourceIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create()
            .AddPostgres("Postgres");
        var source = isNull ? null! : string.Empty;

        var action = () => builder.WithDataBindMount(source);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(source), exception.ParamName);
    }

    [TestMethod]
    public void WithInitBindMountShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<PostgresServerResource> builder = null!;
        const string source = "/docker-entrypoint-initdb.d";

        var action = () => builder.WithInitBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void WithInitBindMountShouldThrowWhenSourceIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create()
            .AddPostgres("Postgres");
        var source = isNull ? null! : string.Empty;

        var action = () => builder.WithInitBindMount(source);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(source), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void CtorPostgresDatabaseResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var name = isNull ? null! : string.Empty;
        const string databaseName = "postgreDb";
        var builder = TestDistributedApplicationBuilder.Create()
            .AddParameter("password");
        ParameterResource? userName = null;
        var postgresParentResource = new PostgresServerResource("postgresServer", userName, builder.Resource);

        var action = () => new PostgresDatabaseResource(name, databaseName, postgresParentResource);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void CtorPostgresDatabaseResourceShouldThrowWhenDatabaseNameIsNullOrEmpty(bool isNull)
    {
        const string name = "postgreSql";
        var databaseName = isNull ? null! : string.Empty;
        var builder = TestDistributedApplicationBuilder.Create()
            .AddParameter("password");
        ParameterResource? userName = null;
        var postgresParentResource = new PostgresServerResource("postgresServer", userName, builder.Resource);

        var action = () => new PostgresDatabaseResource(name, databaseName, postgresParentResource);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(databaseName), exception.ParamName);
    }

    [TestMethod]
    public void CtorPostgresDatabaseResourceShouldThrowWhenPostgresParentResourceIsNull()
    {
        const string name = "postgreSql";
        const string databaseName = "postgreDb";
        PostgresServerResource postgresParentResource = null!;

        var action = () => new PostgresDatabaseResource(name, databaseName, postgresParentResource);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(postgresParentResource), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void CtorPostgresServerResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var name = isNull ? null! : string.Empty;
        var builder = TestDistributedApplicationBuilder.Create()
            .AddParameter("password");
        ParameterResource? userName = null;

        var action = () => new PostgresServerResource(name, userName, builder.Resource);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void CtorPostgresServerResourceShouldThrowWhenPasswordIsNull()
    {
        const string name = "postgreSql";
        ParameterResource? userName = null;
        ParameterResource password = null!;

        var action = () => new PostgresServerResource(name, userName, password);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(password), exception.ParamName);
    }
}
