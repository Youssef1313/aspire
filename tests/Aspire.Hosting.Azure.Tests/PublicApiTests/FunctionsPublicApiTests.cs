// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Tests.PublicApiTests;

[TestClass]
public class FunctionsPublicApiTests
{
    [TestMethod]
    public void AddAzureFunctionsProjectShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "funcstorage";

        var action = () => builder.AddAzureFunctionsProject<TestProject>(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AddAzureFunctionsProjectShouldThrowWhenBuilderIsNullOrEmpty(bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddAzureFunctionsProject<TestProject>(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void WithHostStorageShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<AzureFunctionsProjectResource> builder = null!;
        using var hostBuilder = TestDistributedApplicationBuilder.Create();
        var storage = hostBuilder.AddAzureStorage("funcstorage");

        var action = () => builder.WithHostStorage(storage);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void WithHostStorageShouldThrowWhenStorageIsNull()
    {
        using var hostBuilder = TestDistributedApplicationBuilder.Create();
        var builder = hostBuilder.AddAzureFunctionsProject<TestProject>("funcstorage");
        IResourceBuilder<AzureStorageResource> storage = null!;

        var action = () => builder.WithHostStorage(storage);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(storage), exception.ParamName);
    }

    [TestMethod]
    public void WithReferenceShouldThrowWhenSourceIsNull()
    {
        using var hostBuilder = TestDistributedApplicationBuilder.Create();
        var destination = hostBuilder.AddAzureFunctionsProject<TestProject>("funcstorage");
        IResourceBuilder<IResourceWithConnectionString> source = null!;

        var action = () =>
        {
            destination.WithReference(source);
        };

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(source), exception.ParamName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CtorAzureFunctionsProjectResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var name = isNull ? null! : string.Empty;

        var action = () => new AzureFunctionsProjectResource(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    private sealed class TestProject : IProjectMetadata
    {
        public string ProjectPath => "some-path";

        public LaunchSettings LaunchSettings => new()
        {
            Profiles = new Dictionary<string, LaunchProfile>
            {
                ["funcapp"] = new()
                {
                    CommandLineArgs = "--port 7071",
                    LaunchBrowser = false,
                }
            }
        };
    }
}
