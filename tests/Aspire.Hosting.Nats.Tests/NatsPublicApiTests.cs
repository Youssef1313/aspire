// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Nats.Tests;

[TestClass]
public class NatsPublicApiTests
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddNatsShouldThrowWhenBuilderIsNull(bool includePort)
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "Nats";

        Action action = () => _ = includePort ? builder.AddNats(name, 4222) : builder.AddNats(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true, true)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(false, false)]
    public void AddNatsShouldThrowWhenNameIsNullOrEmpty(bool isNull, bool includePort)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;

        Action action = () => _ = includePort ? builder.AddNats(name, 4222) : builder.AddNats(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddNatsWithParametersShouldThrowWhenBuilderIsNull(bool includePort)
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "Nats";
        IResourceBuilder<ParameterResource>? userName = null;
        IResourceBuilder<ParameterResource>? password = null;

        Action action = () => _ = includePort
            ? builder.AddNats(name, 4222, userName: userName, password: password)
            : builder.AddNats(name, userName: userName, password: password);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true, true)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(false, false)]
    public void AddNatsWithParametersShouldThrowWhenNameIsNullOrEmpty(bool isNull, bool includePort)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;
        IResourceBuilder<ParameterResource>? userName = null;
        IResourceBuilder<ParameterResource>? password = null;

        Action action = () => _ = includePort
            ? builder.AddNats(name, 4222, userName: userName, password: password)
            : builder.AddNats(name, userName: userName, password: password);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    [Obsolete("This method is obsolete and will be removed in a future version. Use the overload without the srcMountPath parameter and WithDataBindMount extension instead if you want to keep data locally.")]
    public void ObsoleteWithJetStreamShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<NatsServerResource> builder = null!;
        string? srcMountPath = null;

        Action action = () => builder.WithJetStream(srcMountPath);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void WithJetStreamShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<NatsServerResource> builder = null!;

        Action action = () => builder.WithJetStream();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void WithDataVolumeShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<NatsServerResource> builder = null!;

        Action action = () => builder.WithDataVolume();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void WithDataBindMountShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<NatsServerResource> builder = null!;
        const string source = "/data";

        Action action = () => builder.WithDataBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void WithDataBindMountShouldThrowWhenSourceIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create()
            .AddNats("Nats");
        var source = isNull ? null! : string.Empty;

        Action action = () => builder.WithDataBindMount(source);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(source), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void CtorNatsServerResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var name = isNull ? null! : string.Empty;

        Action action = () => new NatsServerResource(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true, true, true)]
    [DataRow(true, true, false)]
    [DataRow(true, false, true)]
    [DataRow(true, false, false)]
    [DataRow(false, true, true)]
    [DataRow(false, true, false)]
    [DataRow(false, false, true)]
    [DataRow(false, false, false)]
    public void CtorNatsServerResourceWithParametersShouldThrowWhenNameIsNullOrEmpty(bool isNull, bool isNullUser, bool isNullPassword)
    {
        var name = isNull ? null! : string.Empty;
        var builder = TestDistributedApplicationBuilder.Create();
        var user = isNullUser ? null : builder.AddParameter("user");
        var password = isNullPassword ? null : builder.AddParameter("password");

        Action action = () => new NatsServerResource(name, user?.Resource, password?.Resource);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }
}
