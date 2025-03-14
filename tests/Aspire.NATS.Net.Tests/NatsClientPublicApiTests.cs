// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NATS.Client.Core;

namespace Aspire.NATS.Net.Tests;

[TestClass]
public class NatsClientPublicApiTests
{
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    public void AddNatsClientShouldThrowWhenBuilderIsNull(int overrideIndex)
    {
        IHostApplicationBuilder builder = null!;
        const string connectionName = "nats";
        Action<NatsClientSettings>? configureSettings = null;
        Func<NatsOpts, NatsOpts>? configureOptions = null;
        Func<IServiceProvider, NatsOpts, NatsOpts>? configureOptionsWithService = null;

        Action action = overrideIndex switch
        {
            0 => () => builder.AddNatsClient(connectionName),
            1 => () => builder.AddNatsClient(connectionName, configureSettings),
            2 => () => builder.AddNatsClient(connectionName, configureOptions),
            3 => () => builder.AddNatsClient(connectionName, configureOptionsWithService),
            4 => () => builder.AddNatsClient(connectionName, configureSettings, configureOptions),
            5 => () => builder.AddNatsClient(connectionName, configureSettings, configureOptionsWithService),
            _ => throw new InvalidOperationException()
        };

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(0, false)]
    [DataRow(0, true)]
    [DataRow(1, false)]
    [DataRow(1, true)]
    [DataRow(2, false)]
    [DataRow(2, true)]
    [DataRow(3, false)]
    [DataRow(3, true)]
    [DataRow(4, false)]
    [DataRow(4, true)]
    [DataRow(5, false)]
    [DataRow(5, true)]
    public void AddNatsClientShouldThrowWhenConnectionNameIsNullOrEmpty(int overrideIndex, bool isNull)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var connectionName = isNull ? null! : string.Empty;
        Action<NatsClientSettings>? configureSettings = null;
        Func<NatsOpts, NatsOpts>? configureOptions = null;
        Func<IServiceProvider, NatsOpts, NatsOpts>? configureOptionsWithService = null;

        Action action = overrideIndex switch
        {
            0 => () => builder.AddNatsClient(connectionName),
            1 => () => builder.AddNatsClient(connectionName, configureSettings),
            2 => () => builder.AddNatsClient(connectionName, configureOptions),
            3 => () => builder.AddNatsClient(connectionName, configureOptionsWithService),
            4 => () => builder.AddNatsClient(connectionName, configureSettings, configureOptions),
            5 => () => builder.AddNatsClient(connectionName, configureSettings, configureOptionsWithService),
            _ => throw new InvalidOperationException()
        };

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(connectionName), exception.ParamName);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    public void AddKeyedNatsClientShouldThrowWhenBuilderIsNull(int overrideIndex)
    {
        IHostApplicationBuilder builder = null!;
        const string name = "nats";
        Action<NatsClientSettings>? configureSettings = null;
        Func<NatsOpts, NatsOpts>? configureOptions = null;
        Func<IServiceProvider, NatsOpts, NatsOpts>? configureOptionsWithService = null;

        Action action = overrideIndex switch
        {
            0 => () => builder.AddKeyedNatsClient(name),
            1 => () => builder.AddKeyedNatsClient(name, configureSettings),
            2 => () => builder.AddKeyedNatsClient(name, configureOptions),
            3 => () => builder.AddKeyedNatsClient(name, configureOptionsWithService),
            4 => () => builder.AddKeyedNatsClient(name, configureSettings, configureOptions),
            5 => () => builder.AddKeyedNatsClient(name, configureSettings, configureOptionsWithService),
            _ => throw new InvalidOperationException()
        };

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(0, false)]
    [DataRow(0, true)]
    [DataRow(1, false)]
    [DataRow(1, true)]
    [DataRow(2, false)]
    [DataRow(2, true)]
    [DataRow(3, false)]
    [DataRow(3, true)]
    [DataRow(4, false)]
    [DataRow(4, true)]
    [DataRow(5, false)]
    [DataRow(5, true)]
    public void AddKeyedNatsClientShouldThrowWhenNameIsNullOrEmpty(int overrideIndex, bool isNull)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var name = isNull ? null! : string.Empty;
        Action<NatsClientSettings>? configureSettings = null;
        Func<NatsOpts, NatsOpts>? configureOptions = null;
        Func<IServiceProvider, NatsOpts, NatsOpts>? configureOptionsWithService = null;

        Action action = overrideIndex switch
        {
            0 => () => builder.AddKeyedNatsClient(name),
            1 => () => builder.AddKeyedNatsClient(name, configureSettings),
            2 => () => builder.AddKeyedNatsClient(name, configureOptions),
            3 => () => builder.AddKeyedNatsClient(name, configureOptionsWithService),
            4 => () => builder.AddKeyedNatsClient(name, configureSettings, configureOptions),
            5 => () => builder.AddKeyedNatsClient(name, configureSettings, configureOptionsWithService),
            _ => throw new InvalidOperationException()
        };

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void AddNatsJetStreamShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;

        var action = builder.AddNatsJetStream;

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true, true, false, false)]
    [DataRow(true, true, true, false)]
    [DataRow(true, true, false, true)]
    [DataRow(true, false, false, false)]
    [DataRow(true, false, true, false)]
    [DataRow(true, false, false, true)]
    [DataRow(false, true, false, false)]
    [DataRow(false, true, true, false)]
    [DataRow(false, true, false, true)]
    [DataRow(false, false, false, false)]
    [DataRow(false, false, true, false)]
    [DataRow(false, false, false, true)]
    public void AddNatsClientConfigured(bool useKeyed, bool useConfigureSettings, bool useConfigureOptions, bool useConfigureOptionsWithServiceProvider)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:Nats", "nats")
        ]);
        var name = "Nats";
        bool configureSettingsIsCalled = false, configureOptionsIsCalled = false;

        Action action = (useKeyed, useConfigureSettings, useConfigureOptions, useConfigureOptionsWithServiceProvider) switch
        {
            // Single Client
            (false, false, false, false) => () => builder.AddNatsClient(name),
            (false, true, false, false) => () => builder.AddNatsClient(name, configureSettings: ConfigureSettings),
            (false, false, true, false) => () => builder.AddNatsClient(name, configureOptions: ConfigureOptions),
            (false, false, false, true) => () => builder.AddNatsClient(name, configureOptions: ConfigureOptionsWithServiceProvider),
            (false, true, true, false) => () => builder.AddNatsClient(name, configureSettings: ConfigureSettings, configureOptions: ConfigureOptions),
            (false, true, false, true) => () => builder.AddNatsClient(name, configureSettings: ConfigureSettings, configureOptions: ConfigureOptionsWithServiceProvider),

            // Keyed Client
            (true, false, false, false) => () => builder.AddKeyedNatsClient(name),
            (true, true, false, false) => () => builder.AddKeyedNatsClient(name, configureSettings: ConfigureSettings),
            (true, false, true, false) => () => builder.AddKeyedNatsClient(name, configureOptions: ConfigureOptions),
            (true, false, false, true) => () => builder.AddKeyedNatsClient(name, configureOptions: ConfigureOptionsWithServiceProvider),
            (true, true, true, false) => () => builder.AddKeyedNatsClient(name, configureSettings: ConfigureSettings, configureOptions: ConfigureOptions),
            (true, true, false, true) => () => builder.AddKeyedNatsClient(name, configureSettings: ConfigureSettings, configureOptions: ConfigureOptionsWithServiceProvider),

            _ => throw new InvalidOperationException()
        };

        action();

        using var host = builder.Build();

        _ = useKeyed
            ? host.Services.GetRequiredKeyedService<INatsConnection>(name)
            : host.Services.GetRequiredService<INatsConnection>();

        if (useConfigureSettings)
        {
            Assert.IsTrue(configureSettingsIsCalled);
        }

        if (useConfigureOptions || useConfigureOptionsWithServiceProvider)
        {
            Assert.IsTrue(configureOptionsIsCalled);
        }

        void ConfigureSettings(NatsClientSettings _)
        {
            configureSettingsIsCalled = true;
        }

        NatsOpts ConfigureOptions(NatsOpts _)
        {
            configureOptionsIsCalled = true;
            return NatsOpts.Default;
        }

        NatsOpts ConfigureOptionsWithServiceProvider(IServiceProvider provider, NatsOpts _)
        {
            var __ = provider.GetRequiredService<IConfiguration>();
            configureOptionsIsCalled = true;
            return NatsOpts.Default;
        }
    }
}
