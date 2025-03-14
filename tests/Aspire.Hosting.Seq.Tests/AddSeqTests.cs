// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Seq.Tests;

[TestClass]
public class AddSeqTests
{
    [TestMethod]
    public void AddSeqContainerWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddSeq("mySeq").PublishAsContainer();

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.ContainsSingle(appModel.Resources.OfType<SeqResource>());
        Assert.AreEqual("mySeq", containerResource.Name);

        var endpoint = Assert.ContainsSingle(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.AreEqual(80, endpoint.TargetPort);
        Assert.IsFalse(endpoint.IsExternal);
        Assert.AreEqual("http", endpoint.Name);
        Assert.IsNull(endpoint.Port);
        Assert.AreEqual(ProtocolType.Tcp, endpoint.Protocol);
        Assert.AreEqual("http", endpoint.Transport);
        Assert.AreEqual("http", endpoint.UriScheme);

        var containerAnnotation = Assert.ContainsSingle(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.AreEqual(SeqContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.AreEqual(SeqContainerImageTags.Image, containerAnnotation.Image);
        Assert.AreEqual(SeqContainerImageTags.Registry, containerAnnotation.Registry);
    }

    [TestMethod]
    public void AddSeqContainerAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddSeq("mySeq", port: 9813);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.ContainsSingle(appModel.Resources.OfType<SeqResource>());
        Assert.AreEqual("mySeq", containerResource.Name);

        var endpoint = Assert.ContainsSingle(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.AreEqual(80, endpoint.TargetPort);
        Assert.IsFalse(endpoint.IsExternal);
        Assert.AreEqual("http", endpoint.Name);
        Assert.AreEqual(9813, endpoint.Port);
        Assert.AreEqual(ProtocolType.Tcp, endpoint.Protocol);
        Assert.AreEqual("http", endpoint.Transport);
        Assert.AreEqual("http", endpoint.UriScheme);

        var containerAnnotation = Assert.ContainsSingle(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.AreEqual(SeqContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.AreEqual(SeqContainerImageTags.Image, containerAnnotation.Image);
        Assert.AreEqual(SeqContainerImageTags.Registry, containerAnnotation.Registry);
    }

    [TestMethod]
    public async Task SeqCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddSeq("mySeq")
            .WithEndpoint("http", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000));

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.ContainsSingle(appModel.Resources.OfType<IResourceWithConnectionString>());
        var connectionString = await connectionStringResource.GetConnectionStringAsync(default);
        Assert.AreEqual("{mySeq.bindings.http.url}", connectionStringResource.ConnectionStringExpression.ValueExpression);
        StringAssert.StartsWith(connectionString, "http://localhost:2000");
    }

    [TestMethod]
    public async Task VerifyManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var seq = builder.AddSeq("seq");

        var manifest = await ManifestUtils.GetManifest(seq.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "{seq.bindings.http.url}",
              "image": "{{SeqContainerImageTags.Registry}}/{{SeqContainerImageTags.Image}}:{{SeqContainerImageTags.Tag}}",
              "env": {
                "ACCEPT_EULA": "Y"
              },
              "bindings": {
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 80
                }
              }
            }
            """;
        Assert.AreEqual(expectedManifest, manifest.ToString());
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(true)]
    [DataRow(false)]
    public void WithDataVolumeAddsVolumeAnnotation(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var seq = builder.AddSeq("mySeq");
        if (isReadOnly.HasValue)
        {
            seq.WithDataVolume(isReadOnly: isReadOnly.Value);
        }
        else
        {
            seq.WithDataVolume();
        }

        var volumeAnnotation = seq.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.AreEqual($"{builder.GetVolumePrefix()}-mySeq-data", volumeAnnotation.Source);
        Assert.AreEqual("/data", volumeAnnotation.Target);
        Assert.AreEqual(ContainerMountType.Volume, volumeAnnotation.Type);
        Assert.AreEqual(isReadOnly ?? false, volumeAnnotation.IsReadOnly);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(true)]
    [DataRow(false)]
    public void WithDataBindMountAddsMountAnnotation(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var seq = builder.AddSeq("mySeq");
        if (isReadOnly.HasValue)
        {
            seq.WithDataBindMount("mydata", isReadOnly: isReadOnly.Value);
        }
        else
        {
            seq.WithDataBindMount("mydata");
        }

        var volumeAnnotation = seq.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.AreEqual(Path.Combine(builder.AppHostDirectory, "mydata"), volumeAnnotation.Source);
        Assert.AreEqual("/data", volumeAnnotation.Target);
        Assert.AreEqual(ContainerMountType.BindMount, volumeAnnotation.Type);
        Assert.AreEqual(isReadOnly ?? false, volumeAnnotation.IsReadOnly);
    }
}
