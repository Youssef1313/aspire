// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Keycloak.Tests;

[TestClass]
public class KeycloakPublicApiTests
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void CtorKeycloakResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var name = isNull ? null! : string.Empty;
        ParameterResource? admin = null;
        var adminPassword = new ParameterResource("adminPassword", (p) => "password");

        Action action = () => new KeycloakResource(name, admin, adminPassword);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void CtorKeycloakResourceShouldThrowWhenAdminPasswordIsNull()
    {
        const string name = "keycloak";
        ParameterResource? admin = null;
        ParameterResource adminPassword = null!;

        Action action = () => new KeycloakResource(name, admin, adminPassword);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(adminPassword), exception.ParamName);
    }

    [TestMethod]
    public void AddKeycloakShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "keycloak";

        Action action = () => builder.AddKeycloak(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddKeycloakShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;

        Action action = () => builder.AddKeycloak(name);

        var exception = isNull
           ? Assert.Throws<ArgumentNullException>(action)
           : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    public void WithDataVolumeShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<KeycloakResource> builder = null!;

        Action action = () => builder.WithDataVolume();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    public void WithDataBindMountShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<KeycloakResource> builder = null!;
        const string source = "/opt/keycloak/data";

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
            .AddKeycloak("keycloak");
        var source = isNull ? null! : string.Empty;

        Action action = () => builder.WithDataBindMount(source);

        var exception = isNull
           ? Assert.Throws<ArgumentNullException>(action)
           : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(source), exception.ParamName);
    }

    [TestMethod]
    public void WithRealmImportShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<KeycloakResource> builder = null!;
        const string importDirectory = "/opt/keycloak/data/import";

        Action action = () => builder.WithRealmImport(importDirectory);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void WithRealmImportShouldThrowWhenImportIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create()
            .AddKeycloak("keycloak");
        var import = isNull ? null! : string.Empty;

        Action action = () => builder.WithRealmImport(import);

        var exception = isNull
           ? Assert.Throws<ArgumentNullException>(action)
           : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(import), exception.ParamName);
    }

    [TestMethod]
    public void WithRealmImportShouldThrowWhenImportDoesNotExist()
    {
        var builder = TestDistributedApplicationBuilder.Create()
            .AddKeycloak("Keycloak");

        Action action = () => builder.WithRealmImport("does-not-exist");

        Assert.Throws<InvalidOperationException>(action);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(true)]
    [DataRow(false)]
    public void WithRealmImportDirectoryAddsBindMountAnnotation(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);

        var resourceName = "keycloak";
        var keycloak = builder.AddKeycloak(resourceName);

        if (isReadOnly.HasValue)
        {
            keycloak.WithRealmImport(tempDirectory, isReadOnly: isReadOnly.Value);
        }
        else
        {
            keycloak.WithRealmImport(tempDirectory);
        }

        var containerAnnotation = keycloak.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.AreEqual(tempDirectory, containerAnnotation.Source);
        Assert.AreEqual("/opt/keycloak/data/import", containerAnnotation.Target);
        Assert.AreEqual(ContainerMountType.BindMount, containerAnnotation.Type);
        Assert.AreEqual(isReadOnly ?? false, containerAnnotation.IsReadOnly);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(true)]
    [DataRow(false)]
    public void WithRealmImportFileAddsBindMountAnnotation(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);

        var file = "realm.json";
        var filePath = Path.Combine(tempDirectory, file);
        File.WriteAllText(filePath, string.Empty);

        var resourceName = "keycloak";
        var keycloak = builder.AddKeycloak(resourceName);

        if (isReadOnly.HasValue)
        {
            keycloak.WithRealmImport(filePath, isReadOnly: isReadOnly.Value);
        }
        else
        {
            keycloak.WithRealmImport(filePath);
        }

        var containerAnnotation = keycloak.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.AreEqual(filePath, containerAnnotation.Source);
        Assert.AreEqual($"/opt/keycloak/data/import/{file}", containerAnnotation.Target);
        Assert.AreEqual(ContainerMountType.BindMount, containerAnnotation.Type);
        Assert.AreEqual(isReadOnly ?? false, containerAnnotation.IsReadOnly);
    }
}
