// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Google.Protobuf.Collections;
using OpenTelemetry.Proto.Trace.V1;
using static Aspire.Tests.Shared.Telemetry.TelemetryTestHelpers;

namespace Aspire.Dashboard.Tests.TelemetryRepositoryTests;

[TestClass]
public class ApplicationTests
{
    [TestMethod]
    public void GetApplicationByCompositeName()
    {
        // Arrange
        var repository = CreateRepository();

        AddResource(repository, "app2");
        AddResource(repository, "app1");

        // Act 1
        var applications = repository.GetApplications();

        // Assert 1
        Assert.That.Collection(applications,
            app =>
            {
                Assert.AreEqual("app1", app.ApplicationName);
                Assert.AreEqual("TestId", app.InstanceId);
            },
            app =>
            {
                Assert.AreEqual("app2", app.ApplicationName);
                Assert.AreEqual("TestId", app.InstanceId);
            });

        // Act 2
        var app1 = repository.GetApplicationByCompositeName("app1-TestId");
        var app2 = repository.GetApplicationByCompositeName("APP2-TESTID");
        var notFound = repository.GetApplicationByCompositeName("APP2_TESTID");

        // Assert 2
        Assert.IsNotNull(app1);
        Assert.AreEqual("app1", app1.ApplicationName);
        Assert.AreEqual(applications[0], app1);

        Assert.IsNotNull(app2);
        Assert.AreEqual("app2", app2.ApplicationName);
        Assert.AreEqual(applications[1], app2);

        Assert.IsNull(notFound);
    }

    [TestMethod]
    public void GetApplications_WithNameAndNoKey()
    {
        // Arrange
        var repository = CreateRepository();

        AddResource(repository, "app2");
        AddResource(repository, "app1", instanceId: "123");
        AddResource(repository, "app1", instanceId: "456");

        // Act 1
        var applications1 = repository.GetApplications(new ApplicationKey("app1", InstanceId: null));

        // Assert 1
        Assert.That.Collection(applications1,
            app =>
            {
                Assert.AreEqual("app1", app.ApplicationName);
                Assert.AreEqual("123", app.InstanceId);
            },
            app =>
            {
                Assert.AreEqual("app1", app.ApplicationName);
                Assert.AreEqual("456", app.InstanceId);
            });

        // Act 2
        var applications2 = repository.GetApplications(new ApplicationKey("app2", InstanceId: null));

        // Assert 2
        Assert.That.Collection(applications2,
            app =>
            {
                Assert.AreEqual("app2", app.ApplicationName);
                Assert.AreEqual("TestId", app.InstanceId);
            });
    }

    [TestMethod]
    public void GetApplications_Order()
    {
        // Arrange
        var repository = CreateRepository();

        AddResource(repository, "app2");
        AddResource(repository, "app1", instanceId: "def");
        AddResource(repository, "app1", instanceId: "abc");

        // Act
        var applications = repository.GetApplications();

        // Assert
        Assert.That.Collection(applications,
            app =>
            {
                Assert.AreEqual("app1", app.ApplicationName);
                Assert.AreEqual("abc", app.InstanceId);
            },
            app =>
            {
                Assert.AreEqual("app1", app.ApplicationName);
                Assert.AreEqual("def", app.InstanceId);
            },
            app =>
            {
                Assert.AreEqual("app2", app.ApplicationName);
                Assert.AreEqual("TestId", app.InstanceId);
            });
    }

    [TestMethod]
    public void GetResourceName_GuidInstanceId_Shorten()
    {
        // Arrange
        var repository = CreateRepository();
        var guid1 = "19572b19-d1c0-4a51-98b4-fcc2658f73d3";
        var guid2 = "f66e2b1e-f420-4a22-a067-8dd2f6fcda86";

        AddResource(repository, "app1", guid1);
        AddResource(repository, "app1", guid2);

        // Act
        var applications = repository.GetApplications();

        var instance1Name = OtlpApplication.GetResourceName(applications[0], applications);
        var instance2Name = OtlpApplication.GetResourceName(applications[1], applications);

        // Assert
        Assert.AreEqual("app1-19572b19", instance1Name);
        Assert.AreEqual("app1-f66e2b1e", instance2Name);
    }

    private static void AddResource(TelemetryRepository repository, string name, string? instanceId = null)
    {
        var addContext = new AddContext();
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(name: name, instanceId: instanceId)
            }
        });

        Assert.AreEqual(0, addContext.FailureCount);
    }
}
