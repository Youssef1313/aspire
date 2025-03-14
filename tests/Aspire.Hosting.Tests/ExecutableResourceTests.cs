// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.InternalTesting;

namespace Aspire.Hosting.Tests;

[TestClass]
public class ExecutableResourceTests
{
    [TestMethod]
    public async Task AddExecutableWithArgs()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var testResource = new TestResource("test", "connectionString");
        var testResource2 = new TestResource("test2", "anotherConnectionString");

        var exe1 = appBuilder.AddExecutable("e1", "ruby", ".", "app.rb")
            .WithEndpoint("ep", e =>
            {
                e.UriScheme = "http";
                e.AllocatedEndpoint = new(e, "localhost", 1234);
            });

        var exe2 = appBuilder.AddExecutable("e2", "python", ".", "app.py", exe1.GetEndpoint("ep"))
             .WithArgs("arg1", testResource)
             .WithArgs(context =>
             {
                 context.Args.Add("arg2");
                 context.Args.Add(exe1.GetEndpoint("ep"));
                 context.Args.Add(testResource2);
             });

        using var app = appBuilder.Build();

        var args = await ArgumentEvaluator.GetArgumentListAsync(exe2.Resource).DefaultTimeout();

        Assert.That.Collection(args,
            arg => Assert.AreEqual("app.py", arg),
            arg => Assert.AreEqual("http://localhost:1234", arg),
            arg => Assert.AreEqual("arg1", arg),
            arg => Assert.AreEqual("connectionString", arg),
            arg => Assert.AreEqual("arg2", arg),
            arg => Assert.AreEqual("http://localhost:1234", arg),
            arg => Assert.AreEqual("anotherConnectionString", arg)
            );

        var manifest = await ManifestUtils.GetManifest(exe2.Resource).DefaultTimeout();

        var expectedManifest =
        """
        {
          "type": "executable.v0",
          "workingDirectory": ".",
          "command": "python",
          "args": [
            "app.py",
            "{e1.bindings.ep.url}",
            "arg1",
            "{test.connectionString}",
            "arg2",
            "{e1.bindings.ep.url}",
            "{test2.connectionString}"
          ]
        }
        """;

        Assert.AreEqual(expectedManifest, manifest.ToString());
    }

    private sealed class TestResource(string name, string connectionString) : Resource(name), IResourceWithConnectionString
    {
        public ReferenceExpression ConnectionStringExpression =>
            ReferenceExpression.Create($"{connectionString}");
    }
}
