// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Aspire.Milvus.Client.Tests;

[TestClass]
public class ConfigurationTests
{
    [TestMethod]
    public void EndpointIsNullByDefault()
    => Assert.IsNull(new MilvusClientSettings().Endpoint);

    [TestMethod]
    public void DatabaseIsNullByDefault()
    => Assert.IsNull(new MilvusClientSettings().Database);
}
