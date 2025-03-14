// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Keycloak.Tests;

[TestClass]
public class KeycloakResourceBuilderTests
{
    [TestMethod]
    public void AddKeycloakWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var resourceName = "keycloak";
        appBuilder.AddKeycloak(resourceName);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.ContainsSingle(appModel.Resources.OfType<KeycloakResource>());
        Assert.AreEqual(resourceName, containerResource.Name);

        const string defaultEndpointName = "http";

        var endpoint = Assert.ContainsSingle(containerResource.Annotations.OfType<EndpointAnnotation>().Where(e => e.Name == defaultEndpointName));
        Assert.AreEqual(8080, endpoint.TargetPort);
        Assert.IsFalse(endpoint.IsExternal);
        Assert.AreEqual(defaultEndpointName, endpoint.Name);
        Assert.IsNull(endpoint.Port);
        Assert.AreEqual(ProtocolType.Tcp, endpoint.Protocol);
        Assert.AreEqual("http", endpoint.Transport);
        Assert.AreEqual("http", endpoint.UriScheme);

        const string managementEndpointName = "management";

        var healthEndpoint = Assert.ContainsSingle(containerResource.Annotations.OfType<EndpointAnnotation>().Where(e => e.Name == managementEndpointName));
        Assert.AreEqual(9000, healthEndpoint.TargetPort);
        Assert.IsFalse(healthEndpoint.IsExternal);
        Assert.AreEqual(managementEndpointName, healthEndpoint.Name);
        Assert.IsNull(healthEndpoint.Port);
        Assert.AreEqual(ProtocolType.Tcp, healthEndpoint.Protocol);
        Assert.AreEqual("http", healthEndpoint.Transport);
        Assert.AreEqual("http", healthEndpoint.UriScheme);

        var containerAnnotation = Assert.ContainsSingle(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.AreEqual(KeycloakContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.AreEqual(KeycloakContainerImageTags.Image, containerAnnotation.Image);
        Assert.AreEqual(KeycloakContainerImageTags.Registry, containerAnnotation.Registry);
    }

    [TestMethod]
    public void WithDataVolumeAddsVolumeAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var resourceName = "keycloak";
        var keycloak = builder.AddKeycloak(resourceName)
                              .WithDataVolume();

        var volumeAnnotation = keycloak.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.AreEqual($"{builder.GetVolumePrefix()}-{resourceName}-data", volumeAnnotation.Source);
        Assert.AreEqual("/opt/keycloak/data", volumeAnnotation.Target);
        Assert.AreEqual(ContainerMountType.Volume, volumeAnnotation.Type);
        Assert.IsFalse(volumeAnnotation.IsReadOnly);
    }

    [TestMethod]
    public void WithDataBindMountAddsMountAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var keycloak = builder.AddKeycloak("keycloak")
                              .WithDataBindMount("mydata");

        var volumeAnnotation = keycloak.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.AreEqual(Path.Combine(builder.AppHostDirectory, "mydata"), volumeAnnotation.Source);
        Assert.AreEqual("/opt/keycloak/data", volumeAnnotation.Target);
        Assert.AreEqual(ContainerMountType.BindMount, volumeAnnotation.Type);
        Assert.IsFalse(volumeAnnotation.IsReadOnly);
    }

    [TestMethod]
    public void AddAddKeycloakAddsGeneratedPasswordParameterWithUserSecretsParameterDefaultInRunMode()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var rmq = appBuilder.AddKeycloak("keycloak");

        Assert.AreEqual("Aspire.Hosting.ApplicationModel.UserSecretsParameterDefault", rmq.Resource.AdminPasswordParameter.Default?.GetType().FullName);
    }

    [TestMethod]
    public void AddAddKeycloakDoesNotAddGeneratedPasswordParameterWithUserSecretsParameterDefaultInPublishMode()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var rmq = appBuilder.AddKeycloak("keycloak");

        Assert.AreNotEqual("Aspire.Hosting.ApplicationModel.UserSecretsParameterDefault", rmq.Resource.AdminPasswordParameter.Default?.GetType().FullName);
    }

    [TestMethod]
    public async Task VerifyManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var keycloak = builder.AddKeycloak("keycloak");

        var manifest = await ManifestUtils.GetManifest(keycloak.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "image": "{{KeycloakContainerImageTags.Registry}}/{{KeycloakContainerImageTags.Image}}:{{KeycloakContainerImageTags.Tag}}",
              "args": [
                "start-dev",
                "--import-realm"
              ],
              "env": {
                "KC_BOOTSTRAP_ADMIN_USERNAME": "admin",
                "KC_BOOTSTRAP_ADMIN_PASSWORD": "{keycloak-password.value}",
                "KC_HEALTH_ENABLED": "true"
              },
              "bindings": {
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 8080
                },
                "management": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 9000
                }
              }
            }
            """;
        Assert.AreEqual(expectedManifest, manifest.ToString());
    }
}
