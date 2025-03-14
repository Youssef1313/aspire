// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Provisioning;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.Azure.Tests;

[TestClass]
public class AzureBicepProvisionerTests
{
    [TestMethod]
    public async Task SetParametersTranslatesParametersToARMCompatibleJsonParameters()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var bicep0 = builder.AddBicepTemplateString("bicep0", "param name string")
               .WithParameter("name", "david");

        var parameters = new JsonObject();
        await BicepProvisioner.SetParametersAsync(parameters, bicep0.Resource);

        Assert.ContainsSingle(parameters);
        Assert.AreEqual("david", parameters["name"]?["value"]?.ToString());
    }

    [TestMethod]
    public async Task SetParametersTranslatesCompatibleParameterTypes()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var container = builder.AddContainer("foo", "image")
            .WithHttpEndpoint()
            .WithEndpoint("http", e =>
            {
                e.AllocatedEndpoint = new(e, "localhost", 1023);
            });

        builder.Configuration["Parameters:param"] = "paramValue";

        var connectionStringResource = builder.CreateResourceBuilder(
            new ResourceWithConnectionString("A", "connection string"));

        var param = builder.AddParameter("param");

        var bicep0 = builder.AddBicepTemplateString("bicep0", "param name string")
               .WithParameter("name", "john")
               .WithParameter("age", () => 20)
               .WithParameter("values", ["a", "b", "c"])
               .WithParameter("conn", connectionStringResource)
               .WithParameter("jsonObj", new JsonObject { ["key"] = "value" })
               .WithParameter("param", param)
               .WithParameter("expr", ReferenceExpression.Create($"{param.Resource}/1"))
               .WithParameter("endpoint", container.GetEndpoint("http"));

        var parameters = new JsonObject();
        await BicepProvisioner.SetParametersAsync(parameters, bicep0.Resource);

        Assert.AreEqual(8, parameters.Count);
        Assert.AreEqual("john", parameters["name"]?["value"]?.ToString());
        Assert.AreEqual(20, parameters["age"]?["value"]?.GetValue<int>());
        Assert.AreEqual(["a", "b", "c"], parameters["values"]?["value"]?.AsArray()?.Select(v => v?.ToString()) ?? []);
        Assert.AreEqual("connection string", parameters["conn"]?["value"]?.ToString());
        Assert.AreEqual("value", parameters["jsonObj"]?["value"]?["key"]?.ToString());
        Assert.AreEqual("paramValue", parameters["param"]?["value"]?.ToString());
        Assert.AreEqual("paramValue/1", parameters["expr"]?["value"]?.ToString());
        Assert.AreEqual("http://localhost:1023", parameters["endpoint"]?["value"]?.ToString());
    }

    [TestMethod]
    public async Task ResourceWithTheSameBicepTemplateAndParametersHaveTheSameCheckSum()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var bicep0 = builder.AddBicepTemplateString("bicep0", "param name string")
                       .WithParameter("name", "david")
                       .WithParameter("age", () => 20)
                       .WithParameter("values", ["a", "b", "c"])
                       .WithParameter("jsonObj", new JsonObject { ["key"] = "value" });

        var bicep1 = builder.AddBicepTemplateString("bicep1", "param name string")
                       .WithParameter("name", "david")
                       .WithParameter("age", () => 20)
                       .WithParameter("values", ["a", "b", "c"])
                       .WithParameter("jsonObj", new JsonObject { ["key"] = "value" });

        var parameters0 = new JsonObject();
        await BicepProvisioner.SetParametersAsync(parameters0, bicep0.Resource);
        var checkSum0 = BicepProvisioner.GetChecksum(bicep0.Resource, parameters0, null);

        var parameters1 = new JsonObject();
        await BicepProvisioner.SetParametersAsync(parameters1, bicep1.Resource);
        var checkSum1 = BicepProvisioner.GetChecksum(bicep1.Resource, parameters1, null);

        Assert.AreEqual(checkSum0, checkSum1);
    }

    [TestMethod]
    [DataRow("1alpha")]
    [DataRow("-alpha")]
    [DataRow("")]
    [DataRow(" alpha")]
    [DataRow("alpha 123")]
    public void WithParameterDoesNotAllowParameterNamesWhichAreInvalidBicepIdentifiers(string bicepParameterName)
    {
        Assert.Throws<ArgumentException>(() =>
        {
            using var builder = TestDistributedApplicationBuilder.Create();
            builder.AddAzureInfrastructure("infrastructure", _ => { })
                   .WithParameter(bicepParameterName);
        });
    }

    [TestMethod]
    [DataRow("alpha")]
    [DataRow("a1pha")]
    [DataRow("_alpha")]
    [DataRow("__alpha")]
    [DataRow("alpha1_")]
    [DataRow("Alpha1_A")]
    public void WithParameterAllowsParameterNamesWhichAreValidBicepIdentifiers(string bicepParameterName)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddAzureInfrastructure("infrastructure", _ => { })
                .WithParameter(bicepParameterName);
    }

    [TestMethod]
    public async Task ResourceWithSameTemplateButDifferentParametersHaveDifferentChecksums()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var bicep0 = builder.AddBicepTemplateString("bicep0", "param name string")
                       .WithParameter("name", "david")
                       .WithParameter("age", () => 20)
                       .WithParameter("values", ["a", "b", "c"]);

        var bicep1 = builder.AddBicepTemplateString("bicep1", "param name string")
                       .WithParameter("name", "david")
                       .WithParameter("age", () => 20)
                       .WithParameter("values", ["a", "b", "c"])
                       .WithParameter("jsonObj", new JsonObject { ["key"] = "value" });

        var parameters0 = new JsonObject();
        await BicepProvisioner.SetParametersAsync(parameters0, bicep0.Resource);
        var checkSum0 = BicepProvisioner.GetChecksum(bicep0.Resource, parameters0, null);

        var parameters1 = new JsonObject();
        await BicepProvisioner.SetParametersAsync(parameters1, bicep1.Resource);
        var checkSum1 = BicepProvisioner.GetChecksum(bicep1.Resource, parameters1, null);

        Assert.AreNotEqual(checkSum0, checkSum1);
    }

    [TestMethod]
    public async Task GetCurrentChecksumSkipsKnownValuesForCheckSumCreation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var bicep0 = builder.AddBicepTemplateString("bicep0", "param name string")
                       .WithParameter("name", "david")
                       .WithParameter(AzureBicepResource.KnownParameters.KeyVaultName);

        // Simulate the case where a known parameter has a value
        var bicep1 = builder.AddBicepTemplateString("bicep1", "param name string")
                       .WithParameter("name", "david")
                       .WithParameter(AzureBicepResource.KnownParameters.KeyVaultName, "blah")
                       .WithParameter(AzureBicepResource.KnownParameters.PrincipalId, "id")
                       .WithParameter(AzureBicepResource.KnownParameters.Location, "tomorrow")
                       .WithParameter(AzureBicepResource.KnownParameters.PrincipalType, "type");

        var parameters0 = new JsonObject();
        await BicepProvisioner.SetParametersAsync(parameters0, bicep0.Resource);
        var checkSum0 = BicepProvisioner.GetChecksum(bicep0.Resource, parameters0, null);

        // Save the old version of this resource's parameters to config
        var config = new ConfigurationManager();
        config["Parameters"] = parameters0.ToJsonString();

        var checkSum1 = await BicepProvisioner.GetCurrentChecksumAsync(bicep1.Resource, config);

        Assert.AreEqual(checkSum0, checkSum1);
    }

    [TestMethod]
    public async Task ResourceWithDifferentScopeHaveDifferentChecksums()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var bicep0 = builder.AddBicepTemplateString("bicep0", "param name string")
                       .WithParameter("key", "value");
        bicep0.Resource.Scope = new("rg0");

        var bicep1 = builder.AddBicepTemplateString("bicep1", "param name string")
                       .WithParameter("key", "value");
        bicep1.Resource.Scope = new("rg1");

        var parameters0 = new JsonObject();
        var scope0 = new JsonObject();
        await BicepProvisioner.SetParametersAsync(parameters0, bicep0.Resource);
        await BicepProvisioner.SetScopeAsync(scope0, bicep0.Resource);
        var checkSum0 = BicepProvisioner.GetChecksum(bicep0.Resource, parameters0, scope0);

        var parameters1 = new JsonObject();
        var scope1 = new JsonObject();
        await BicepProvisioner.SetParametersAsync(parameters1, bicep1.Resource);
        await BicepProvisioner.SetScopeAsync(scope1, bicep1.Resource);
        var checkSum1 = BicepProvisioner.GetChecksum(bicep1.Resource, parameters1, scope1);

        Assert.AreNotEqual(checkSum0, checkSum1);
    }

    [TestMethod]
    public async Task ResourceWithSameScopeHaveSameChecksums()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var bicep0 = builder.AddBicepTemplateString("bicep0", "param name string")
                       .WithParameter("key", "value");
        bicep0.Resource.Scope = new("rg0");

        var bicep1 = builder.AddBicepTemplateString("bicep1", "param name string")
                       .WithParameter("key", "value");
        bicep1.Resource.Scope = new("rg0");

        var parameters0 = new JsonObject();
        var scope0 = new JsonObject();
        await BicepProvisioner.SetParametersAsync(parameters0, bicep0.Resource);
        await BicepProvisioner.SetScopeAsync(scope0, bicep0.Resource);
        var checkSum0 = BicepProvisioner.GetChecksum(bicep0.Resource, parameters0, scope0);

        var parameters1 = new JsonObject();
        var scope1 = new JsonObject();
        await BicepProvisioner.SetParametersAsync(parameters1, bicep1.Resource);
        await BicepProvisioner.SetScopeAsync(scope1, bicep1.Resource);
        var checkSum1 = BicepProvisioner.GetChecksum(bicep1.Resource, parameters1, scope1);

        Assert.AreEqual(checkSum0, checkSum1);
    }

    private sealed class ResourceWithConnectionString(string name, string connectionString) :
        Resource(name),
        IResourceWithConnectionString
    {
        public ReferenceExpression ConnectionStringExpression =>
           ReferenceExpression.Create($"{connectionString}");
    }
}
