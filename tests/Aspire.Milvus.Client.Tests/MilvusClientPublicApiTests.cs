// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;

namespace Aspire.Milvus.Client.Tests;

[TestClass]
public class MilvusClientPublicApiTests
{
    [TestMethod]
    public void AddMilvusClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string connectionName = "milvus";

        var action = () => builder.AddMilvusClient(connectionName);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddMilvusClientShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var connectionName = isNull ? null! : string.Empty;

        var action = () => builder.AddMilvusClient(connectionName);

        var exception = isNull
           ? Assert.Throws<ArgumentNullException>(action)
           : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(connectionName), exception.ParamName);
    }

    [TestMethod]
    public void AddKeyedMilvusClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string connectionName = "milvus";

        var action = () => builder.AddKeyedMilvusClient(connectionName);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddKeyedMilvusClientShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddKeyedMilvusClient(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }
}
