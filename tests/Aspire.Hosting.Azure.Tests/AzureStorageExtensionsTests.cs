// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Tests;

[TestClass]
public class AzureStorageExtensionsTests
{
    [TestMethod]
    [DataRow(null)]
    [DataRow(true)]
    [DataRow(false)]
    public void AzureStorageUseEmulatorCallbackWithWithDataBindMountResultsInBindMountAnnotationWithDefaultPath(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage").RunAsEmulator(configureContainer: builder =>
        {
            if (isReadOnly.HasValue)
            {
                builder.WithDataBindMount(isReadOnly: isReadOnly.Value);
            }
            else
            {
                builder.WithDataBindMount();
            }
        });

        var volumeAnnotation = storage.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();
        Assert.AreEqual(Path.Combine(builder.AppHostDirectory, ".azurite", "storage"), volumeAnnotation.Source);
        Assert.AreEqual("/data", volumeAnnotation.Target);
        Assert.AreEqual(ContainerMountType.BindMount, volumeAnnotation.Type);
        Assert.AreEqual(isReadOnly ?? false, volumeAnnotation.IsReadOnly);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(true)]
    [DataRow(false)]
    public void AzureStorageUseEmulatorCallbackWithWithDataBindMountResultsInBindMountAnnotation(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage").RunAsEmulator(configureContainer: builder =>
        {
            if (isReadOnly.HasValue)
            {
                builder.WithDataBindMount("mydata", isReadOnly: isReadOnly.Value);
            }
            else
            {
                builder.WithDataBindMount("mydata");
            }
        });

        var volumeAnnotation = storage.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();
        Assert.AreEqual(Path.Combine(builder.AppHostDirectory, "mydata"), volumeAnnotation.Source);
        Assert.AreEqual("/data", volumeAnnotation.Target);
        Assert.AreEqual(ContainerMountType.BindMount, volumeAnnotation.Type);
        Assert.AreEqual(isReadOnly ?? false, volumeAnnotation.IsReadOnly);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(true)]
    [DataRow(false)]
    public void AzureStorageUseEmulatorCallbackWithWithDataVolumeResultsInVolumeAnnotationWithDefaultName(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage").RunAsEmulator(configureContainer: builder =>
        {
            if (isReadOnly.HasValue)
            {
                builder.WithDataVolume(isReadOnly: isReadOnly.Value);
            }
            else
            {
                builder.WithDataVolume();
            }
        });

        var volumeAnnotation = storage.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();
        Assert.AreEqual($"{builder.GetVolumePrefix()}-storage-data", volumeAnnotation.Source);
        Assert.AreEqual("/data", volumeAnnotation.Target);
        Assert.AreEqual(ContainerMountType.Volume, volumeAnnotation.Type);
        Assert.AreEqual(isReadOnly ?? false, volumeAnnotation.IsReadOnly);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(true)]
    [DataRow(false)]
    public void AzureStorageUseEmulatorCallbackWithWithDataVolumeResultsInVolumeAnnotation(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage").RunAsEmulator(configureContainer: builder =>
        {
            if (isReadOnly.HasValue)
            {
                builder.WithDataVolume("mydata", isReadOnly: isReadOnly.Value);
            }
            else
            {
                builder.WithDataVolume("mydata");
            }
        });

        var volumeAnnotation = storage.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();
        Assert.AreEqual("mydata", volumeAnnotation.Source);
        Assert.AreEqual("/data", volumeAnnotation.Target);
        Assert.AreEqual(ContainerMountType.Volume, volumeAnnotation.Type);
        Assert.AreEqual(isReadOnly ?? false, volumeAnnotation.IsReadOnly);
    }

    [TestMethod]
    public void AzureStorageUserEmulatorUseBlobQueueTablePortMethodsMutateEndpoints()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage").RunAsEmulator(configureContainer: builder =>
        {
            builder.WithBlobPort(9001);
            builder.WithQueuePort(9002);
            builder.WithTablePort(9003);
        });

        Assert.That.Collection(
            storage.Resource.Annotations.OfType<EndpointAnnotation>(),
            e => Assert.AreEqual(9001, e.Port),
            e => Assert.AreEqual(9002, e.Port),
            e => Assert.AreEqual(9003, e.Port));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task AddAzureStorage_WithApiVersionCheck_ShouldSetSkipApiVersionCheck(bool enableApiVersionCheck)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage").RunAsEmulator(x => x.WithApiVersionCheck(enableApiVersionCheck));

        var args = await ArgumentEvaluator.GetArgumentListAsync(storage.Resource);

        Assert.All(["azurite", "-l", "/data", "--blobHost", "0.0.0.0", "--queueHost", "0.0.0.0", "--tableHost", "0.0.0.0"], x => args.Contains(x));

        if (enableApiVersionCheck)
        {
            Assert.Contains("--skipApiVersionCheck", args);
        }
        else
        {
            Assert.DoesNotContain("--skipApiVersionCheck", args);
        }
    }

    [TestMethod]
    public async Task AddAzureStorage_RunAsEmulator_SetSkipApiVersionCheck()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var storage = builder.AddAzureStorage("storage").RunAsEmulator();

        var args = await ArgumentEvaluator.GetArgumentListAsync(storage.Resource);

        Assert.Contains("--skipApiVersionCheck", args);
    }
}
