// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Aspire.Microsoft.EntityFrameworkCore.Cosmos.Tests;

[TestClass]
public class MicrosoftEntityFrameworkCoreCosmosPublicApiTests
{
    [TestMethod]
    public void AddCosmosDbContextShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string connectionName = "cosmos";
        const string databaseName = "cosmosdb";

        var action = () => builder.AddCosmosDbContext<DbContext>(connectionName, databaseName);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddCosmosDbContextShouldThrowWhenConnectionNameIsNullOrEmpty(bool isNull)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var connectionName = isNull ? null! : string.Empty;
        const string databaseName = "cosmosdb";

        var action = () => builder.AddCosmosDbContext<DbContext>(connectionName, databaseName);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(connectionName), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddCosmosDbContextShouldThrowWhenDatabaseNameIsNullOrEmpty(bool isNull)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        const string connectionName = "cosmos";
        var databaseName = isNull ? null! : string.Empty;

        var action = () => builder.AddCosmosDbContext<DbContext>(connectionName, databaseName);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(databaseName), exception.ParamName);
    }

    [TestMethod]
    public void EnrichCosmosDbContextShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;

        var action = () => builder.EnrichCosmosDbContext<DbContext>();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }
}
