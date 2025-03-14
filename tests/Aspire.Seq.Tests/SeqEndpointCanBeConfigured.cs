
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;
using OpenTelemetry.Exporter;

namespace Aspire.Seq.Tests;

[TestClass]
public class SeqTests
{
    [TestMethod]
    public void SeqEndpointCanBeConfigured()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.AddSeqEndpoint("seq", s =>
        {
            s.DisableHealthChecks = true;
            s.Logs.TimeoutMilliseconds = 1000;
            s.Traces.Protocol = OtlpExportProtocol.Grpc;
        });

        using var host = builder.Build();
    }

    [TestMethod]
    public void ServerUrlSettingOverridesExporterEndpoints()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var serverUrl = "http://localhost:9876";

        SeqSettings settings = new SeqSettings();

        builder.AddSeqEndpoint("seq", s =>
        {
            settings = s;
            s.ServerUrl = serverUrl;
            s.ApiKey = "TestKey123!";
            s.Logs.Endpoint = new Uri("http://localhost:1234/ingest/otlp/v1/logs");
            s.Traces.Endpoint = new Uri("http://localhost:1234/ingest/otlp/v1/traces");
        });

        Assert.AreEqual(settings.Logs.Endpoint, new Uri("http://localhost:9876/ingest/otlp/v1/logs"));
        Assert.AreEqual(settings.Traces.Endpoint, new Uri("http://localhost:9876/ingest/otlp/v1/traces"));
    }

    [TestMethod]
    public void ApiKeySettingIsMergedWithConfiguredHeaders()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        SeqSettings settings = new SeqSettings();

        builder.AddSeqEndpoint("seq", s =>
        {
            settings = s;
            s.DisableHealthChecks = true;
            s.ApiKey = "TestKey123!";
            s.Logs.Headers = "speed=fast,quality=good";
            s.Traces.Headers = "quality=good,speed=fast";
        });

        Assert.AreEqual("speed=fast,quality=good,X-Seq-ApiKey=TestKey123!", settings.Logs.Headers);
        Assert.AreEqual("quality=good,speed=fast,X-Seq-ApiKey=TestKey123!", settings.Traces.Headers);
    }
}
