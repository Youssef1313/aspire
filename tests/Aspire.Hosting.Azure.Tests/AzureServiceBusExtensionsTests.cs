// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Components.Common.Tests;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.ServiceBus;
using Aspire.Hosting.Utils;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting.Azure.Tests;

[TestClass]
public class AzureServiceBusExtensionsTests
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public async Task ResourceNamesCanBeDifferentThanAzureNames()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var serviceBus = builder.AddAzureServiceBus("sb");

        serviceBus.AddServiceBusQueue("queue1", "queueName")
            .WithProperties(queue => queue.DefaultMessageTimeToLive = TimeSpan.FromSeconds(1));
        var topic1 = serviceBus.AddServiceBusTopic("topic1", "topicName")
            .WithProperties(topic =>
            {
                topic.DefaultMessageTimeToLive = TimeSpan.FromSeconds(1);
            });
        topic1.AddServiceBusSubscription("subscription1", "subscriptionName")
            .WithProperties(sub =>
            {
                sub.Rules.Add(new AzureServiceBusRule("rule1"));
            });

        var manifest = await AzureManifestUtils.GetManifestWithBicep(serviceBus.Resource);

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param sku string = 'Standard'

            param principalType string

            param principalId string

            resource sb 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
              name: take('sb-${uniqueString(resourceGroup().id)}', 50)
              location: location
              properties: {
                disableLocalAuth: true
              }
              sku: {
                name: sku
              }
              tags: {
                'aspire-resource-name': 'sb'
              }
            }

            resource sb_AzureServiceBusDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(sb.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419')
                principalType: principalType
              }
              scope: sb
            }

            resource queue1 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
              name: 'queueName'
              properties: {
                defaultMessageTimeToLive: 'PT1S'
              }
              parent: sb
            }

            resource topic1 'Microsoft.ServiceBus/namespaces/topics@2024-01-01' = {
              name: 'topicName'
              properties: {
                defaultMessageTimeToLive: 'PT1S'
              }
              parent: sb
            }

            resource subscription1 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2024-01-01' = {
              name: 'subscriptionName'
              parent: topic1
            }

            resource rule1 'Microsoft.ServiceBus/namespaces/topics/subscriptions/rules@2024-01-01' = {
              name: 'rule1'
              properties: {
                filterType: 'CorrelationFilter'
              }
              parent: subscription1
            }

            output serviceBusEndpoint string = sb.properties.serviceBusEndpoint

            output name string = sb.name
            """;
        TestContext.WriteLine(manifest.BicepText);
        Assert.AreEqual(expectedBicep, manifest.BicepText);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task TopicNamesCanBeLongerThan24(bool useObsoleteMethods)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var serviceBus = builder.AddAzureServiceBus("sb");

        if (useObsoleteMethods)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            serviceBus.AddTopic("device-connection-state-events1234567890-even-longer");
#pragma warning restore CS0618 // Type or member is obsolete
        }
        else
        {
            serviceBus.AddServiceBusTopic("device-connection-state-events1234567890-even-longer");
        }

        var manifest = await AzureManifestUtils.GetManifestWithBicep(serviceBus.Resource);

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param sku string = 'Standard'

            param principalType string

            param principalId string

            resource sb 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
              name: take('sb-${uniqueString(resourceGroup().id)}', 50)
              location: location
              properties: {
                disableLocalAuth: true
              }
              sku: {
                name: sku
              }
              tags: {
                'aspire-resource-name': 'sb'
              }
            }

            resource sb_AzureServiceBusDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
              name: guid(sb.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419'))
              properties: {
                principalId: principalId
                roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '090c5cfd-751d-490a-894a-3ce6f1109419')
                principalType: principalType
              }
              scope: sb
            }

            resource device_connection_state_events1234567890_even_longer 'Microsoft.ServiceBus/namespaces/topics@2024-01-01' = {
              name: 'device-connection-state-events1234567890-even-longer'
              parent: sb
            }

            output serviceBusEndpoint string = sb.properties.serviceBusEndpoint

            output name string = sb.name
            """;
        TestContext.WriteLine(manifest.BicepText);
        Assert.AreEqual(expectedBicep, manifest.BicepText);
    }

    [TestMethod]
    [Ignore("Azure ServiceBus emulator is not reliable in CI - https://github.com/dotnet/aspire/issues/7066")]
    [RequiresDocker]
    public async Task VerifyWaitForOnServiceBusEmulatorBlocksDependentResources()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        using var builder = TestDistributedApplicationBuilder.Create(TestContext);

        var healthCheckTcs = new TaskCompletionSource<HealthCheckResult>();
        builder.Services.AddHealthChecks().AddAsyncCheck("blocking_check", () =>
        {
            return healthCheckTcs.Task;
        });

        var resource = builder.AddAzureServiceBus("resource")
                              .RunAsEmulator()
                              .WithHealthCheck("blocking_check");

        resource.AddServiceBusQueue("queue1");

        var dependentResource = builder.AddContainer("nginx", "mcr.microsoft.com/cbl-mariner/base/nginx", "1.22")
                                       .WaitFor(resource);

        using var app = builder.Build();

        var pendingStart = app.StartAsync(cts.Token);

        await app.ResourceNotifications.WaitForResourceAsync(resource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await app.ResourceNotifications.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Waiting, cts.Token);

        healthCheckTcs.SetResult(HealthCheckResult.Healthy());

        await app.ResourceNotifications.WaitForResourceHealthyAsync(resource.Resource.Name, cts.Token);

        await app.ResourceNotifications.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await pendingStart;

        await app.StopAsync();
    }

    [TestMethod]
    [Ignore("Azure ServiceBus emulator is not reliable in CI - https://github.com/dotnet/aspire/issues/7066")]
    [DataRow(null)]
    [DataRow("other")]
    [RequiresDocker]
    public async Task VerifyAzureServiceBusEmulatorResource(string? queueName)
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));

        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(TestContext);

        var serviceBus = builder.AddAzureServiceBus("servicebusns")
            .RunAsEmulator();

        var queueResource = serviceBus.AddServiceBusQueue("queue123", queueName);

        using var app = builder.Build();
        await app.StartAsync();

        var hb = Host.CreateApplicationBuilder();
        hb.Configuration["ConnectionStrings:servicebusns"] = await serviceBus.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
        hb.AddAzureServiceBusClient("servicebusns");

        using var host = hb.Build();
        await host.StartAsync();

        var rns = app.Services.GetRequiredService<ResourceNotificationService>();
        await rns.WaitForResourceAsync(serviceBus.Resource.Name, KnownResourceStates.Running, cts.Token);
        await rns.WaitForResourceHealthyAsync(serviceBus.Resource.Name, cts.Token);

        var serviceBusClient = host.Services.GetRequiredService<ServiceBusClient>();

        await using var sender = serviceBusClient.CreateSender(queueResource.Resource.QueueName);
        await sender.SendMessageAsync(new ServiceBusMessage("Hello, World!"), cts.Token);

        await using var receiver = serviceBusClient.CreateReceiver(queueResource.Resource.QueueName);
        var message = await receiver.ReceiveMessageAsync(cancellationToken: cts.Token);

        Assert.AreEqual("Hello, World!", message.Body.ToString());
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(8081)]
    [DataRow(9007)]
    public void AddAzureServiceBusWithEmulatorGetsExpectedPort(int? port = null)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var serviceBus = builder.AddAzureServiceBus("sb").RunAsEmulator(configureContainer: builder =>
        {
            builder.WithHostPort(port);
        });

        Assert.That.Collection(
            serviceBus.Resource.Annotations.OfType<EndpointAnnotation>(),
            e => Assert.AreEqual(port, e.Port)
            );
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("2.3.97-preview")]
    [DataRow("1.0.7")]
    public void AddAzureServiceBusWithEmulatorGetsExpectedImageTag(string? imageTag)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var serviceBus = builder.AddAzureServiceBus("sb");

        serviceBus.RunAsEmulator(container =>
        {
            if (!string.IsNullOrEmpty(imageTag))
            {
                container.WithImageTag(imageTag);
            }
        });

        var containerImageAnnotation = serviceBus.Resource.Annotations.OfType<ContainerImageAnnotation>().FirstOrDefault();
        Assert.IsNotNull(containerImageAnnotation);

        Assert.AreEqual(imageTag ?? ServiceBusEmulatorContainerImageTags.Tag, containerImageAnnotation.Tag);
        Assert.AreEqual(ServiceBusEmulatorContainerImageTags.Registry, containerImageAnnotation.Registry);
        Assert.AreEqual(ServiceBusEmulatorContainerImageTags.Image, containerImageAnnotation.Image);
    }

    [TestMethod]
    public async Task AzureServiceBusEmulatorResourceInitializesProvisioningModel()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        global::Azure.Provisioning.ServiceBus.ServiceBusQueue? queue = null;
        global::Azure.Provisioning.ServiceBus.ServiceBusTopic? topic = null;
        global::Azure.Provisioning.ServiceBus.ServiceBusSubscription? subscription = null;
        global::Azure.Provisioning.ServiceBus.ServiceBusRule? rule = null;

        var serviceBus = builder.AddAzureServiceBus("servicebusns");
        serviceBus.AddServiceBusQueue("queue1")
            .WithProperties(queue =>
            {
                queue.DeadLetteringOnMessageExpiration = true;
                queue.DefaultMessageTimeToLive = TimeSpan.FromMinutes(1);
                queue.DuplicateDetectionHistoryTimeWindow = TimeSpan.FromSeconds(20);
                queue.ForwardDeadLetteredMessagesTo = "someQueue";
                queue.LockDuration = TimeSpan.FromMinutes(5);
                queue.MaxDeliveryCount = 10;
                queue.RequiresDuplicateDetection = true;
                queue.RequiresSession = true;
            });

        var topic1 = serviceBus.AddServiceBusTopic("topic1")
            .WithProperties(topic =>
            {
                topic.DefaultMessageTimeToLive = TimeSpan.FromMinutes(1);
                topic.DuplicateDetectionHistoryTimeWindow = TimeSpan.FromSeconds(20);
                topic.RequiresDuplicateDetection = true;
            });
        topic1.AddServiceBusSubscription("subscription1")
            .WithProperties(sub =>
            {
                sub.DeadLetteringOnMessageExpiration = true;
                sub.DefaultMessageTimeToLive = TimeSpan.FromMinutes(1);
                sub.LockDuration = TimeSpan.FromMinutes(5);
                sub.MaxDeliveryCount = 10;
                sub.ForwardDeadLetteredMessagesTo = "";
                sub.RequiresSession = true;

                var rule = new AzureServiceBusRule("rule1")
                {
                    FilterType = AzureServiceBusFilterType.SqlFilter,
                    CorrelationFilter = new()
                    {
                        ContentType = "application/text",
                        CorrelationId = "id1",
                        Subject = "subject1",
                        MessageId = "msgid1",
                        ReplyTo = "someQueue",
                        ReplyToSessionId = "sessionId",
                        SessionId = "session1",
                        SendTo = "xyz"
                    }
                };
                sub.Rules.Add(rule);
            });

        serviceBus
            .ConfigureInfrastructure(infrastructure =>
            {
                queue = infrastructure.GetProvisionableResources().OfType<global::Azure.Provisioning.ServiceBus.ServiceBusQueue>().Single();
                topic = infrastructure.GetProvisionableResources().OfType<global::Azure.Provisioning.ServiceBus.ServiceBusTopic>().Single();
                subscription = infrastructure.GetProvisionableResources().OfType<global::Azure.Provisioning.ServiceBus.ServiceBusSubscription>().Single();
                rule = infrastructure.GetProvisionableResources().OfType<global::Azure.Provisioning.ServiceBus.ServiceBusRule>().Single();
            });

        using var app = builder.Build();

        var manifest = await AzureManifestUtils.GetManifestWithBicep(serviceBus.Resource);

        Assert.IsNotNull(queue);
        Assert.AreEqual("queue1", queue.Name.Value);
        Assert.IsTrue(queue.DeadLetteringOnMessageExpiration.Value);
        Assert.AreEqual(TimeSpan.FromMinutes(1), queue.DefaultMessageTimeToLive.Value);
        Assert.AreEqual(TimeSpan.FromSeconds(20), queue.DuplicateDetectionHistoryTimeWindow.Value);
        Assert.AreEqual("someQueue", queue.ForwardDeadLetteredMessagesTo.Value);
        Assert.AreEqual(TimeSpan.FromMinutes(5), queue.LockDuration.Value);
        Assert.AreEqual(10, queue.MaxDeliveryCount.Value);
        Assert.IsTrue(queue.RequiresDuplicateDetection.Value);
        Assert.IsTrue(queue.RequiresSession.Value);

        Assert.IsNotNull(topic);
        Assert.AreEqual("topic1", topic.Name.Value);
        Assert.AreEqual(TimeSpan.FromMinutes(1), topic.DefaultMessageTimeToLive.Value);
        Assert.AreEqual(TimeSpan.FromSeconds(20), topic.DuplicateDetectionHistoryTimeWindow.Value);
        Assert.IsTrue(topic.RequiresDuplicateDetection.Value);

        Assert.IsNotNull(subscription);
        Assert.AreEqual("subscription1", subscription.Name.Value);
        Assert.IsTrue(subscription.DeadLetteringOnMessageExpiration.Value);
        Assert.AreEqual(TimeSpan.FromMinutes(1), subscription.DefaultMessageTimeToLive.Value);
        Assert.AreEqual(TimeSpan.FromMinutes(5), subscription.LockDuration.Value);
        Assert.AreEqual(10, subscription.MaxDeliveryCount.Value);
        Assert.AreEqual("", subscription.ForwardDeadLetteredMessagesTo.Value);
        Assert.IsTrue(subscription.RequiresSession.Value);

        Assert.IsNotNull(rule);
        Assert.AreEqual("rule1", rule.Name.Value);
        Assert.AreEqual(global::Azure.Provisioning.ServiceBus.ServiceBusFilterType.SqlFilter, rule.FilterType.Value);
        Assert.AreEqual("application/text", rule.CorrelationFilter.ContentType.Value);
        Assert.AreEqual("id1", rule.CorrelationFilter.CorrelationId.Value);
        Assert.AreEqual("subject1", rule.CorrelationFilter.Subject.Value);
        Assert.AreEqual("msgid1", rule.CorrelationFilter.MessageId.Value);
        Assert.AreEqual("someQueue", rule.CorrelationFilter.ReplyTo.Value);
        Assert.AreEqual("sessionId", rule.CorrelationFilter.ReplyToSessionId.Value);
        Assert.AreEqual("session1", rule.CorrelationFilter.SessionId.Value);
        Assert.AreEqual("xyz", rule.CorrelationFilter.SendTo.Value);
    }

    [TestMethod]
    [RequiresDocker]
    public async Task AzureServiceBusEmulatorResourceGeneratesConfigJson()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var serviceBus = builder.AddAzureServiceBus("servicebusns")
            .RunAsEmulator();
        serviceBus.AddServiceBusQueue("queue1")
            .WithProperties(queue =>
            {
                queue.DeadLetteringOnMessageExpiration = true;
                queue.DefaultMessageTimeToLive = TimeSpan.FromMinutes(1);
                queue.DuplicateDetectionHistoryTimeWindow = TimeSpan.FromSeconds(20);
                queue.ForwardDeadLetteredMessagesTo = "someQueue";
                queue.LockDuration = TimeSpan.FromMinutes(5);
                queue.MaxDeliveryCount = 10;
                queue.RequiresDuplicateDetection = true;
                queue.RequiresSession = true;
            });

        var topic1 = serviceBus.AddServiceBusTopic("topic1")
            .WithProperties(topic =>
            {
                topic.DefaultMessageTimeToLive = TimeSpan.FromMinutes(1);
                topic.DuplicateDetectionHistoryTimeWindow = TimeSpan.FromSeconds(20);
                topic.RequiresDuplicateDetection = true;
            });
        topic1.AddServiceBusSubscription("subscription1")
            .WithProperties(sub =>
            {
                sub.DeadLetteringOnMessageExpiration = true;
                sub.DefaultMessageTimeToLive = TimeSpan.FromMinutes(1);
                sub.LockDuration = TimeSpan.FromMinutes(5);
                sub.MaxDeliveryCount = 10;
                sub.ForwardDeadLetteredMessagesTo = "";
                sub.RequiresSession = true;

                var rule = new AzureServiceBusRule("rule1")
                {
                    FilterType = AzureServiceBusFilterType.SqlFilter,
                    CorrelationFilter = new()
                    {
                        ContentType = "application/text",
                        CorrelationId = "id1",
                        Subject = "subject1",
                        MessageId = "msgid1",
                        ReplyTo = "someQueue",
                        ReplyToSessionId = "sessionId",
                        SessionId = "session1",
                        SendTo = "xyz"
                    }
                };
                sub.Rules.Add(rule);
            });

        using var app = builder.Build();
        await app.StartAsync();

        var serviceBusEmulatorResource = builder.Resources.OfType<AzureServiceBusResource>().Single(x => x is { } serviceBusResource && serviceBusResource.IsEmulator);
        var volumeAnnotation = serviceBusEmulatorResource.Annotations.OfType<ContainerMountAnnotation>().Single();

        if (!OperatingSystem.IsWindows())
        {
            // Ensure the configuration file has correct attributes
            var fileInfo = new FileInfo(volumeAnnotation.Source!);

            var expectedUnixFileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead;

            Assert.IsTrue(fileInfo.UnixFileMode.HasFlag(expectedUnixFileMode));
        }

        var configJsonContent = File.ReadAllText(volumeAnnotation.Source!);

        Assert.AreEqual(/*json*/"""
        {
          "UserConfig": {
            "Namespaces": [
              {
                "Name": "servicebusns",
                "Queues": [
                  {
                    "Name": "queue1",
                    "Properties": {
                      "DeadLetteringOnMessageExpiration": true,
                      "DefaultMessageTimeToLive": "PT1M",
                      "DuplicateDetectionHistoryTimeWindow": "PT20S",
                      "ForwardDeadLetteredMessagesTo": "someQueue",
                      "LockDuration": "PT5M",
                      "MaxDeliveryCount": 10,
                      "RequiresDuplicateDetection": true,
                      "RequiresSession": true
                    }
                  }
                ],
                "Topics": [
                  {
                    "Name": "topic1",
                    "Properties": {
                      "DefaultMessageTimeToLive": "PT1M",
                      "DuplicateDetectionHistoryTimeWindow": "PT20S",
                      "RequiresDuplicateDetection": true
                    },
                    "Subscriptions": [
                      {
                        "Name": "subscription1",
                        "Properties": {
                          "DeadLetteringOnMessageExpiration": true,
                          "DefaultMessageTimeToLive": "PT1M",
                          "ForwardDeadLetteredMessagesTo": "",
                          "LockDuration": "PT5M",
                          "MaxDeliveryCount": 10,
                          "RequiresSession": true
                        },
                        "Rules": [
                          {
                            "Name": "rule1",
                            "Properties": {
                              "FilterType": "Sql",
                              "CorrelationFilter": {
                                "CorrelationId": "id1",
                                "MessageId": "msgid1",
                                "To": "xyz",
                                "ReplyTo": "someQueue",
                                "Label": "subject1",
                                "SessionId": "session1",
                                "ReplyToSessionId": "sessionId",
                                "ContentType": "application/text"
                              }
                            }
                          }
                        ]
                      }
                    ]
                  }
                ]
              }
            ],
            "Logging": {
              "Type": "File"
            }
          }
        }
        """, configJsonContent);

        await app.StopAsync();
    }

    [TestMethod]
    [RequiresDocker]
    public async Task AzureServiceBusEmulatorResourceGeneratesConfigJsonOnlyChangedProperties()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var serviceBus = builder.AddAzureServiceBus("servicebusns")
            .RunAsEmulator();
        serviceBus.AddServiceBusQueue("queue1")
            .WithProperties(queue =>
            {
                queue.DefaultMessageTimeToLive = TimeSpan.FromMinutes(1);
            });

        using var app = builder.Build();
        await app.StartAsync();

        var serviceBusEmulatorResource = builder.Resources.OfType<AzureServiceBusResource>().Single(x => x is { } serviceBusResource && serviceBusResource.IsEmulator);
        var volumeAnnotation = serviceBusEmulatorResource.Annotations.OfType<ContainerMountAnnotation>().Single();

        var configJsonContent = File.ReadAllText(volumeAnnotation.Source!);

        Assert.AreEqual("""
            {
              "UserConfig": {
                "Namespaces": [
                  {
                    "Name": "servicebusns",
                    "Queues": [
                      {
                        "Name": "queue1",
                        "Properties": {
                          "DefaultMessageTimeToLive": "PT1M"
                        }
                      }
                    ],
                    "Topics": []
                  }
                ],
                "Logging": {
                  "Type": "File"
                }
              }
            }
            """, configJsonContent);

        await app.StopAsync();
    }

    [TestMethod]
    [RequiresDocker]
    public async Task AzureServiceBusEmulatorResourceGeneratesConfigJsonWithCustomizations()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var serviceBus = builder.AddAzureServiceBus("servicebusns")
            .RunAsEmulator(configure => configure
                .WithConfiguration(document =>
                {
                    document["UserConfig"]!["Logging"] = new JsonObject { ["Type"] = "Console" };
                })
                .WithConfiguration(document =>
                {
                    document["Custom"] = JsonValue.Create(42);
                })
            );

        using var app = builder.Build();
        await app.StartAsync();

        var serviceBusEmulatorResource = builder.Resources.OfType<AzureServiceBusResource>().Single(x => x is { } serviceBusResource && serviceBusResource.IsEmulator);
        var volumeAnnotation = serviceBusEmulatorResource.Annotations.OfType<ContainerMountAnnotation>().Single();

        var configJsonContent = File.ReadAllText(volumeAnnotation.Source!);

        Assert.AreEqual("""
            {
              "UserConfig": {
                "Namespaces": [
                  {
                    "Name": "servicebusns",
                    "Queues": [],
                    "Topics": []
                  }
                ],
                "Logging": {
                  "Type": "Console"
                }
              },
              "Custom": 42
            }
            """, configJsonContent);

        await app.StopAsync();
    }

    [TestMethod]
    [RequiresDocker]
    public async Task AzureServiceBusEmulator_WithConfigurationFile()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var configJsonPath = Path.GetTempFileName();

        File.WriteAllText(configJsonPath, """
            {
              "UserConfig": {
                "Namespaces": [
                  {
                    "Name": "servicebusns",
                    "Queues": [ "queue456" ],
                    "Topics": []
                  }
                ]
              }
            }
            """);

        var serviceBus = builder.AddAzureServiceBus("servicebusns")
            .RunAsEmulator(configure => configure.WithConfigurationFile(configJsonPath));

        using var app = builder.Build();

        var serviceBusEmulatorResource = builder.Resources.OfType<AzureServiceBusResource>().Single(x => x is { } serviceBusResource && serviceBusResource.IsEmulator);
        var volumeAnnotation = serviceBusEmulatorResource.Annotations.OfType<ContainerMountAnnotation>().Single();

        var configJsonContent = File.ReadAllText(volumeAnnotation.Source!);

        Assert.AreEqual("/ServiceBus_Emulator/ConfigFiles/Config.json", volumeAnnotation.Target);

        Assert.AreEqual("""
            {
              "UserConfig": {
                "Namespaces": [
                  {
                    "Name": "servicebusns",
                    "Queues": [ "queue456" ],
                    "Topics": []
                  }
                ]
              }
            }
            """, configJsonContent);

        await app.StopAsync();

        try
        {
            File.Delete(configJsonPath);
        }
        catch
        {
        }
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddAzureServiceBusWithEmulator_SetsSqlLifetime(bool isPersistent)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var lifetime = isPersistent ? ContainerLifetime.Persistent : ContainerLifetime.Session;

        var serviceBus = builder.AddAzureServiceBus("sb").RunAsEmulator(configureContainer: builder =>
        {
            builder.WithLifetime(lifetime);
        });

        var sql = builder.Resources.FirstOrDefault(x => x.Name == "sb-sqledge");

        Assert.IsNotNull(sql);

        serviceBus.Resource.TryGetLastAnnotation<ContainerLifetimeAnnotation>(out var sbLifetimeAnnotation);
        sql.TryGetLastAnnotation<ContainerLifetimeAnnotation>(out var sqlLifetimeAnnotation);

        Assert.AreEqual(lifetime, sbLifetimeAnnotation?.Lifetime);
        Assert.AreEqual(lifetime, sqlLifetimeAnnotation?.Lifetime);
    }

    [TestMethod]
    public void RunAsEmulator_CalledTwice_Throws()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var serviceBus = builder.AddAzureServiceBus("sb").RunAsEmulator();

        Assert.Throws<InvalidOperationException>(() => serviceBus.RunAsEmulator());
    }

    [TestMethod]
    public void AzureServiceBusHasCorrectConnectionStrings()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var serviceBus = builder.AddAzureServiceBus("sb");
        var queue = serviceBus.AddServiceBusQueue("queue");
        var topic = serviceBus.AddServiceBusTopic("topic");
        var subscription = topic.AddServiceBusSubscription("sub");

        // queue, topic, and subscription should have the same connection string as the service bus account, for now.
        // In the future, we can add the queue/topic/sub info to the connection string.
        Assert.AreEqual("{sb.outputs.serviceBusEndpoint}", serviceBus.Resource.ConnectionStringExpression.ValueExpression);
        Assert.AreEqual("{sb.outputs.serviceBusEndpoint}", queue.Resource.ConnectionStringExpression.ValueExpression);
        Assert.AreEqual("{sb.outputs.serviceBusEndpoint}", topic.Resource.ConnectionStringExpression.ValueExpression);
        Assert.AreEqual("{sb.outputs.serviceBusEndpoint}", subscription.Resource.ConnectionStringExpression.ValueExpression);
    }

    [TestMethod]
    public void AzureServiceBusAppliesAzureFunctionsConfiguration()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var serviceBus = builder.AddAzureServiceBus("sb");
        var queue = serviceBus.AddServiceBusQueue("queue");
        var topic = serviceBus.AddServiceBusTopic("topic");
        var subscription = topic.AddServiceBusSubscription("sub");

        var target = new Dictionary<string, object>();
        ((IResourceWithAzureFunctionsConfig)serviceBus.Resource).ApplyAzureFunctionsConfiguration(target, "sb");
        Assert.That.Collection(target.Keys.OrderBy(k => k),
            k => Assert.AreEqual("Aspire__Azure__Messaging__ServiceBus__sb__FullyQualifiedNamespace", k),
            k => Assert.AreEqual("sb__fullyQualifiedNamespace", k));

        target.Clear();
        ((IResourceWithAzureFunctionsConfig)queue.Resource).ApplyAzureFunctionsConfiguration(target, "queue");
        Assert.That.Collection(target.Keys.OrderBy(k => k),
            k => Assert.AreEqual("Aspire__Azure__Messaging__ServiceBus__queue__FullyQualifiedNamespace", k),
            k => Assert.AreEqual("queue__fullyQualifiedNamespace", k));

        target.Clear();
        ((IResourceWithAzureFunctionsConfig)topic.Resource).ApplyAzureFunctionsConfiguration(target, "topic");
        Assert.That.Collection(target.Keys.OrderBy(k => k),
            k => Assert.AreEqual("Aspire__Azure__Messaging__ServiceBus__topic__FullyQualifiedNamespace", k),
            k => Assert.AreEqual("topic__fullyQualifiedNamespace", k));

        target.Clear();
        ((IResourceWithAzureFunctionsConfig)subscription.Resource).ApplyAzureFunctionsConfiguration(target, "sub");
        Assert.That.Collection(target.Keys.OrderBy(k => k),
            k => Assert.AreEqual("Aspire__Azure__Messaging__ServiceBus__sub__FullyQualifiedNamespace", k),
            k => Assert.AreEqual("sub__fullyQualifiedNamespace", k));
    }
}
