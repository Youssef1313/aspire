// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure.Tests;

[TestClass]
public class ExistingAzureExtensionsResourceTests
{
    [TestMethod]
    public void RunAsExistingInPublishModeNoOps()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var nameParameter = builder.AddParameter("name", "existingName");
        var resourceGroupParameter = builder.AddParameter("resourceGroup", "existingResourceGroup");

        var serviceBus = builder.AddAzureServiceBus("sb")
            .RunAsExisting(nameParameter, resourceGroupParameter);

        Assert.IsFalse(serviceBus.Resource.IsExisting());
    }

    [TestMethod]
    public void RunAsExistingInRunModeWorks()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);

        var nameParameter = builder.AddParameter("name", "existingName");
        var resourceGroupParameter = builder.AddParameter("resourceGroup", "existingResourceGroup");

        var serviceBus = builder.AddAzureServiceBus("sb")
            .RunAsExisting(nameParameter, resourceGroupParameter);

        Assert.IsTrue(serviceBus.Resource.TryGetLastAnnotation<ExistingAzureResourceAnnotation>(out var existingAzureResourceAnnotation));
        var existingNameParameter = Assert.IsInstanceOfType<ParameterResource>(existingAzureResourceAnnotation.Name);
        Assert.AreEqual("name", existingNameParameter.Name);
        var existingResourceGroupParameter = Assert.IsInstanceOfType<ParameterResource>(existingAzureResourceAnnotation.ResourceGroup);
        Assert.AreEqual("resourceGroup", existingResourceGroupParameter.Name);
    }

    [TestMethod]
    public void MultipleRunAsExistingInRunModeUsesLast()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);

        var nameParameter = builder.AddParameter("name", "existingName");
        var resourceGroupParameter = builder.AddParameter("resourceGroup", "existingResourceGroup");
        var nameParameter1 = builder.AddParameter("name1", "existingName");
        var resourceGroupParameter1 = builder.AddParameter("resourceGroup1", "existingResourceGroup");

        var serviceBus = builder.AddAzureServiceBus("sb")
            .RunAsExisting(nameParameter, resourceGroupParameter)
            .RunAsExisting(nameParameter1, resourceGroupParameter1);

        Assert.IsTrue(serviceBus.Resource.TryGetLastAnnotation<ExistingAzureResourceAnnotation>(out var existingAzureResourceAnnotation));
        var existingNameParameter = Assert.IsType<ParameterResource>(existingAzureResourceAnnotation.Name);
        Assert.AreEqual("name1", existingNameParameter.Name);
        var existingResourceGroupParameter = Assert.IsType<ParameterResource>(existingAzureResourceAnnotation.ResourceGroup);
        Assert.AreEqual("resourceGroup1", existingResourceGroupParameter.Name);
    }

    [TestMethod]
    public void PublishAsExistingInPublishModeWorks()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var nameParameter = builder.AddParameter("name", "existingName");
        var resourceGroupParameter = builder.AddParameter("resourceGroup", "existingResourceGroup");

        var serviceBus = builder.AddAzureServiceBus("sb")
            .PublishAsExisting(nameParameter, resourceGroupParameter);

        Assert.IsTrue(serviceBus.Resource.TryGetLastAnnotation<ExistingAzureResourceAnnotation>(out var existingAzureResourceAnnotation));
        var existingNameParameter = Assert.IsType<ParameterResource>(existingAzureResourceAnnotation.Name);
        Assert.AreEqual("name", existingNameParameter.Name);
        var existingResourceGroupParameter = Assert.IsType<ParameterResource>(existingAzureResourceAnnotation.ResourceGroup);
        Assert.AreEqual("resourceGroup", existingResourceGroupParameter.Name);
    }

    [TestMethod]
    public void MultiplePublishAsExistingInRunModeUsesLast()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var nameParameter = builder.AddParameter("name", "existingName");
        var resourceGroupParameter = builder.AddParameter("resourceGroup", "existingResourceGroup");
        var nameParameter1 = builder.AddParameter("name1", "existingName");
        var resourceGroupParameter1 = builder.AddParameter("resourceGroup1", "existingResourceGroup");

        var serviceBus = builder.AddAzureServiceBus("sb")
            .PublishAsExisting(nameParameter, resourceGroupParameter)
            .PublishAsExisting(nameParameter1, resourceGroupParameter1);

        Assert.IsTrue(serviceBus.Resource.TryGetLastAnnotation<ExistingAzureResourceAnnotation>(out var existingAzureResourceAnnotation));
        var existingNameParameter = Assert.IsType<ParameterResource>(existingAzureResourceAnnotation.Name);
        Assert.AreEqual("name1", existingNameParameter.Name);
        var existingResourceGroupParameter = Assert.IsType<ParameterResource>(existingAzureResourceAnnotation.ResourceGroup);
        Assert.AreEqual("resourceGroup1", existingResourceGroupParameter.Name);
    }

    public static TheoryData<Func<string, string, string, IResourceBuilder<IAzureResource>>> AsExistingMethodsWithString =>
        new()
        {
            { (name, resourceGroup, type) => TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run).AddAzureServiceBus(type).RunAsExisting(name, resourceGroup) },
            { (name, resourceGroup, type) => TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish).AddAzureServiceBus(type).PublishAsExisting(name, resourceGroup) }
        };

    [TestMethod]
    [MemberData(nameof(AsExistingMethodsWithString))]
    public void CanCallAsExistingWithStringArguments(Func<string, string, string, IResourceBuilder<IAzureResource>> runAsExisting)
    {
        var serviceBus = runAsExisting("existingName", "existingResourceGroup", "sb");

        Assert.IsTrue(serviceBus.Resource.TryGetLastAnnotation<ExistingAzureResourceAnnotation>(out var existingAzureResourceAnnotation));
        Assert.AreEqual("existingName", existingAzureResourceAnnotation.Name);
        Assert.AreEqual("existingResourceGroup", existingAzureResourceAnnotation.ResourceGroup);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AsExistingInBothModesWorks(bool isPublishMode)
    {
        using var builder = TestDistributedApplicationBuilder.Create(isPublishMode ? DistributedApplicationOperation.Publish : DistributedApplicationOperation.Run);

        var nameParameter = builder.AddParameter("name", "existingName");
        var resourceGroupParameter = builder.AddParameter("resourceGroup", "existingResourceGroup");

        var serviceBus = builder.AddAzureServiceBus("sb")
            .AsExisting(nameParameter, resourceGroupParameter);

        Assert.IsTrue(serviceBus.Resource.TryGetLastAnnotation<ExistingAzureResourceAnnotation>(out var existingAzureResourceAnnotation));
        var existingNameParameter = Assert.IsType<ParameterResource>(existingAzureResourceAnnotation.Name);
        Assert.AreEqual("name", existingNameParameter.Name);
        var existingResourceGroupParameter = Assert.IsType<ParameterResource>(existingAzureResourceAnnotation.ResourceGroup);
        Assert.AreEqual("resourceGroup", existingResourceGroupParameter.Name);
    }
}
