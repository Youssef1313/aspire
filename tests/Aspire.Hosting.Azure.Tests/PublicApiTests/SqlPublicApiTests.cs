// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Tests.PublicApiTests;

[TestClass]
public class SqlPublicApiTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CtorAzureSqlDatabaseResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var name = isNull ? null! : string.Empty;
        const string databaseName = "db";
        var parent = new AzureSqlServerResource("sql-server", (_) => { });

        var action = () => new AzureSqlDatabaseResource(name, databaseName, parent);

        var exception = isNull
           ? Assert.Throws<ArgumentNullException>(action)
           : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CtorAzureSqlDatabaseResourceShouldThrowWhenDatabaseNameIsNullOrEmpty(bool isNull)
    {
        const string name = "sql";
        var databaseName = isNull ? null! : string.Empty;
        var parent = new AzureSqlServerResource("sql-server", (_) => { });

        var action = () => new AzureSqlDatabaseResource(name, databaseName, parent);

        var exception = isNull
           ? Assert.Throws<ArgumentNullException>(action)
           : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(databaseName), exception.ParamName);
    }

    [TestMethod]
    public void CtorAzureSqlDatabaseResourceShouldThrowWhenParentIsNull()
    {
        var name = "sql";
        const string databaseName = "db";
        AzureSqlServerResource parent = null!;

        var action = () => new AzureSqlDatabaseResource(name, databaseName, parent);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(parent), exception.ParamName);
    }

    [TestMethod]
    [Obsolete($"This method is obsolete and will be removed in a future version. Use AddAzureSqlServer instead to add an Azure SQL server resource.")]
    public void PublishAsAzureSqlDatabaseShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<SqlServerServerResource> builder = null!;

        var action = () =>
        {
            builder.PublishAsAzureSqlDatabase();
        };

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [Obsolete($"This method is obsolete and will be removed in a future version. Use AddAzureSqlServer instead to add an Azure SQL server resource.")]
    public void AsAzureSqlDatabaseShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<SqlServerServerResource> builder = null!;

        var action = () =>
        {
            builder.AsAzureSqlDatabase();
        };

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void AddAzureSqlServerShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "sql-server";

        var action = () => builder.AddAzureSqlServer(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AddAzureSqlServerShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddAzureSqlServer(name);

        var exception = isNull
           ? Assert.Throws<ArgumentNullException>(action)
           : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void AddDatabaseShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<AzureSqlServerResource> builder = null!;
        const string name = "sql-server";

        var action = () => builder.AddDatabase(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AddDatabaseShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        using var testBuilder = TestDistributedApplicationBuilder.Create();
        var builder = testBuilder.AddAzureSqlServer("sql-server");
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddDatabase(name);

        var exception = isNull
           ? Assert.Throws<ArgumentNullException>(action)
           : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void RunAsContainerShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<AzureSqlServerResource> builder = null!;

        var action = () => builder.RunAsContainer();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }
}
