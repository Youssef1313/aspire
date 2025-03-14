// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Aspire.Elastic.Clients.Elasticsearch.Tests;

[TestClass]
public class ConfigurationTests
{
    [TestMethod]
    public void EndpointIsNullByDefault() =>
        Assert.IsNull(new ElasticClientsElasticsearchSettings().Endpoint);

    [TestMethod]
    public void HealthChecksEnabledByDefault() =>
        Assert.IsFalse(new ElasticClientsElasticsearchSettings().DisableHealthChecks);

    [TestMethod]
    public void TracingEnabledByDefault() =>
      Assert.IsFalse(new ElasticClientsElasticsearchSettings().DisableTracing);
}
