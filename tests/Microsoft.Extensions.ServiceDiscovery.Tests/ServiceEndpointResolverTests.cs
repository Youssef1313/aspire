// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.ServiceDiscovery.Http;
using Microsoft.Extensions.ServiceDiscovery.Internal;

namespace Microsoft.Extensions.ServiceDiscovery.Tests;

/// <summary>
/// Tests for <see cref="ServiceEndpointWatcherFactory"/> and <see cref="ServiceEndpointWatcher"/>.
/// </summary>
public class ServiceEndpointResolverTests
{
    [TestMethod]
    public void ResolveServiceEndpoint_NoProvidersConfigured_Throws()
    {
        var services = new ServiceCollection()
            .AddServiceDiscoveryCore()
            .BuildServiceProvider();
        var resolverFactory = services.GetRequiredService<ServiceEndpointWatcherFactory>();
        var exception = Assert.Throws<InvalidOperationException>(() => resolverFactory.CreateWatcher("https://basket"));
        Assert.AreEqual("No provider which supports the provided service name, 'https://basket', has been configured.", exception.Message);
    }

    [TestMethod]
    public async Task ServiceEndpointResolver_NoProvidersConfigured_Throws()
    {
        var services = new ServiceCollection()
            .AddServiceDiscoveryCore()
            .BuildServiceProvider();
        var watcher = new ServiceEndpointWatcher([], NullLogger.Instance, "foo", TimeProvider.System, Options.Options.Create(new ServiceDiscoveryOptions()));
        var exception = Assert.Throws<InvalidOperationException>(watcher.Start);
        Assert.AreEqual("No service endpoint providers are configured.", exception.Message);
        exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await watcher.GetEndpointsAsync());
        Assert.AreEqual("No service endpoint providers are configured.", exception.Message);
    }

    [TestMethod]
    public void ResolveServiceEndpoint_NullServiceName_Throws()
    {
        var services = new ServiceCollection()
            .AddServiceDiscoveryCore()
            .BuildServiceProvider();
        var resolverFactory = services.GetRequiredService<ServiceEndpointWatcherFactory>();
        Assert.Throws<ArgumentNullException>(() => resolverFactory.CreateWatcher(null!));
    }

    [TestMethod]
    public async Task AddServiceDiscovery_NoProviders_Throws()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHttpClient("foo", c => c.BaseAddress = new("http://foo")).AddServiceDiscovery();
        var services = serviceCollection.BuildServiceProvider();
        var client = services.GetRequiredService<IHttpClientFactory>().CreateClient("foo");
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await client.GetStringAsync("/"));
        Assert.AreEqual("No provider which supports the provided service name, 'http://foo', has been configured.", exception.Message);
    }

    private sealed class FakeEndpointResolverProvider(Func<ServiceEndpointQuery, (bool Result, IServiceEndpointProvider? Resolver)> createResolverDelegate) : IServiceEndpointProviderFactory
    {
        public bool TryCreateProvider(ServiceEndpointQuery query, [NotNullWhen(true)] out IServiceEndpointProvider? resolver)
        {
            bool result;
            (result, resolver) = createResolverDelegate(query);
            return result;
        }
    }

    private sealed class FakeEndpointResolver(Func<IServiceEndpointBuilder, CancellationToken, ValueTask> resolveAsync, Func<ValueTask> disposeAsync) : IServiceEndpointProvider
    {
        public ValueTask PopulateAsync(IServiceEndpointBuilder endpoints, CancellationToken cancellationToken) => resolveAsync(endpoints, cancellationToken);
        public ValueTask DisposeAsync() => disposeAsync();
    }

    [TestMethod]
    public async Task ResolveServiceEndpoint()
    {
        var cts = new[] { new CancellationTokenSource() };
        var innerResolver = new FakeEndpointResolver(
            resolveAsync: (collection, ct) =>
            {
                collection.AddChangeToken(new CancellationChangeToken(cts[0].Token));
                collection.Endpoints.Add(ServiceEndpoint.Create(new IPEndPoint(IPAddress.Parse("127.1.1.1"), 8080)));

                if (cts[0].Token.IsCancellationRequested)
                {
                    cts[0] = new();
                    collection.Endpoints.Add(ServiceEndpoint.Create(new IPEndPoint(IPAddress.Parse("127.1.1.2"), 8888)));
                }
                return default;
            },
            disposeAsync: () => default);
        var resolverProvider = new FakeEndpointResolverProvider(name => (true, innerResolver));
        var services = new ServiceCollection()
            .AddSingleton<IServiceEndpointProviderFactory>(resolverProvider)
            .AddServiceDiscoveryCore()
            .BuildServiceProvider();
        var watcherFactory = services.GetRequiredService<ServiceEndpointWatcherFactory>();

        ServiceEndpointWatcher watcher;
        await using ((watcher = watcherFactory.CreateWatcher("http://basket")).ConfigureAwait(false))
        {
            Assert.IsNotNull(watcher);
            var initialResult = await watcher.GetEndpointsAsync(CancellationToken.None);
            Assert.IsNotNull(initialResult);
            var sep = Assert.ContainsSingle(initialResult.Endpoints);
            var ip = Assert.IsType<IPEndPoint>(sep.EndPoint);
            Assert.AreEqual(IPAddress.Parse("127.1.1.1"), ip.Address);
            Assert.AreEqual(8080, ip.Port);

            var tcs = new TaskCompletionSource<ServiceEndpointResolverResult>();
            watcher.OnEndpointsUpdated = tcs.SetResult;
            Assert.IsFalse(tcs.Task.IsCompleted);

            cts[0].Cancel();
            var resolverResult = await tcs.Task;
            Assert.IsNotNull(resolverResult);
            Assert.IsTrue(resolverResult.ResolvedSuccessfully);
            Assert.AreEqual(2, resolverResult.EndpointSource.Endpoints.Count);
            var endpoints = resolverResult.EndpointSource.Endpoints.Select(ep => ep.EndPoint).OfType<IPEndPoint>().ToList();
            endpoints.Sort((l, r) => l.Port - r.Port);
            Assert.AreEqual(new IPEndPoint(IPAddress.Parse("127.1.1.1"), 8080), endpoints[0]);
            Assert.AreEqual(new IPEndPoint(IPAddress.Parse("127.1.1.2"), 8888), endpoints[1]);
        }
    }

    [TestMethod]
    public async Task ResolveServiceEndpointOneShot()
    {
        var cts = new[] { new CancellationTokenSource() };
        var innerResolver = new FakeEndpointResolver(
            resolveAsync: (collection, ct) =>
            {
                collection.AddChangeToken(new CancellationChangeToken(cts[0].Token));
                collection.Endpoints.Add(ServiceEndpoint.Create(new IPEndPoint(IPAddress.Parse("127.1.1.1"), 8080)));

                if (cts[0].Token.IsCancellationRequested)
                {
                    cts[0] = new();
                    collection.Endpoints.Add(ServiceEndpoint.Create(new IPEndPoint(IPAddress.Parse("127.1.1.2"), 8888)));
                }
                return default;
            },
            disposeAsync: () => default);
        var resolverProvider = new FakeEndpointResolverProvider(name => (true, innerResolver));
        var services = new ServiceCollection()
            .AddSingleton<IServiceEndpointProviderFactory>(resolverProvider)
            .AddServiceDiscoveryCore()
            .BuildServiceProvider();
        var resolver = services.GetRequiredService<ServiceEndpointResolver>();

        Assert.IsNotNull(resolver);
        var initialResult = await resolver.GetEndpointsAsync("http://basket", CancellationToken.None);
        Assert.IsNotNull(initialResult);
        var sep = Assert.ContainsSingle(initialResult.Endpoints);
        var ip = Assert.IsType<IPEndPoint>(sep.EndPoint);
        Assert.AreEqual(IPAddress.Parse("127.1.1.1"), ip.Address);
        Assert.AreEqual(8080, ip.Port);

        await services.DisposeAsync();
    }

    [TestMethod]
    public async Task ResolveHttpServiceEndpointOneShot()
    {
        var cts = new[] { new CancellationTokenSource() };
        var innerResolver = new FakeEndpointResolver(
            resolveAsync: (collection, ct) =>
            {
                collection.AddChangeToken(new CancellationChangeToken(cts[0].Token));
                collection.Endpoints.Add(ServiceEndpoint.Create(new IPEndPoint(IPAddress.Parse("127.1.1.1"), 8080)));

                if (cts[0].Token.IsCancellationRequested)
                {
                    cts[0] = new();
                    collection.Endpoints.Add(ServiceEndpoint.Create(new IPEndPoint(IPAddress.Parse("127.1.1.2"), 8888)));
                }
                return default;
            },
            disposeAsync: () => default);
        var fakeResolverProvider = new FakeEndpointResolverProvider(name => (true, innerResolver));
        var services = new ServiceCollection()
            .AddSingleton<IServiceEndpointProviderFactory>(fakeResolverProvider)
            .AddServiceDiscoveryCore()
            .BuildServiceProvider();
        var resolverProvider = services.GetRequiredService<ServiceEndpointWatcherFactory>();
        await using var resolver = new HttpServiceEndpointResolver(resolverProvider, services, TimeProvider.System);

        Assert.IsNotNull(resolver);
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, "http://basket");
        var endpoint = await resolver.GetEndpointAsync(httpRequest, CancellationToken.None);
        Assert.IsNotNull(endpoint);
        var ip = Assert.IsType<IPEndPoint>(endpoint.EndPoint);
        Assert.AreEqual(IPAddress.Parse("127.1.1.1"), ip.Address);
        Assert.AreEqual(8080, ip.Port);

        await services.DisposeAsync();
    }

    [TestMethod]
    public async Task ResolveServiceEndpoint_ThrowOnReload()
    {
        var sem = new SemaphoreSlim(0);
        var cts = new[] { new CancellationTokenSource() };
        var throwOnNextResolve = new[] { false };
        var innerResolver = new FakeEndpointResolver(
            resolveAsync: async (collection, ct) =>
            {
                await sem.WaitAsync(ct).ConfigureAwait(false);
                if (cts[0].IsCancellationRequested)
                {
                    // Always be sure to have a fresh token.
                    cts[0] = new();
                }

                if (throwOnNextResolve[0])
                {
                    throwOnNextResolve[0] = false;
                    throw new InvalidOperationException("throwing");
                }

                collection.AddChangeToken(new CancellationChangeToken(cts[0].Token));
                collection.Endpoints.Add(ServiceEndpoint.Create(new IPEndPoint(IPAddress.Parse("127.1.1.1"), 8080)));
            },
            disposeAsync: () => default);
        var resolverProvider = new FakeEndpointResolverProvider(name => (true, innerResolver));
        var services = new ServiceCollection()
            .AddSingleton<IServiceEndpointProviderFactory>(resolverProvider)
            .AddServiceDiscoveryCore()
            .BuildServiceProvider();
        var watcherFactory = services.GetRequiredService<ServiceEndpointWatcherFactory>();

        ServiceEndpointWatcher watcher;
        await using ((watcher = watcherFactory.CreateWatcher("http://basket")).ConfigureAwait(false))
        {
            Assert.IsNotNull(watcher);
            var initialEndpointsTask = watcher.GetEndpointsAsync(CancellationToken.None);
            sem.Release(1);
            var initialEndpoints = await initialEndpointsTask;
            Assert.IsNotNull(initialEndpoints);
            Assert.ContainsSingle(initialEndpoints.Endpoints);

            // Tell the resolver to throw on the next resolve call and then trigger a reload.
            throwOnNextResolve[0] = true;
            cts[0].Cancel();

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                var resolveTask = watcher.GetEndpointsAsync(CancellationToken.None);
                sem.Release(1);
                await resolveTask.ConfigureAwait(false);
            });

            Assert.AreEqual("throwing", exception.Message);

            var channel = Channel.CreateUnbounded<ServiceEndpointResolverResult>();
            watcher.OnEndpointsUpdated = result => channel.Writer.TryWrite(result);

            do
            {
                cts[0].Cancel();
                sem.Release(1);
                var resolveTask = watcher.GetEndpointsAsync(CancellationToken.None);
                await resolveTask;
                var next = await channel.Reader.ReadAsync(CancellationToken.None);
                if (next.ResolvedSuccessfully)
                {
                    break;
                }
            } while (true);

            var task = watcher.GetEndpointsAsync(CancellationToken.None);
            sem.Release(1);
            var result = await task;
            Assert.AreNotSame(initialEndpoints, result);
            var sep = Assert.ContainsSingle(result.Endpoints);
            var ip = Assert.IsType<IPEndPoint>(sep.EndPoint);
            Assert.AreEqual(IPAddress.Parse("127.1.1.1"), ip.Address);
            Assert.AreEqual(8080, ip.Port);
        }
    }
}
