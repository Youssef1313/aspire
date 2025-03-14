// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading.Channels;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Tests.Integration;
using Google.Protobuf.Collections;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Proto.Logs.V1;
using static Aspire.Tests.Shared.Telemetry.TelemetryTestHelpers;

namespace Aspire.Dashboard.Tests.TelemetryRepositoryTests;

[TestClass]
public class LogTests
{
    private static readonly DateTime s_testTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly TestContext _testContext;

    public LogTests(TestContext testContext)
    {
        _testContext = testContext;
    }

    [TestMethod]
    public void AddLogs()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var addContext = new AddContext();
        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords = { CreateLogRecord() }
                    }
                }
            }
        });

        // Assert
        Assert.AreEqual(0, addContext.FailureCount);

        var applications = repository.GetApplications();
        Assert.That.Collection(applications,
            app =>
            {
                Assert.AreEqual("TestService", app.ApplicationName);
                Assert.AreEqual("TestId", app.InstanceId);
            });

        var logs = repository.GetLogs(new GetLogsContext
        {
            ApplicationKey = applications[0].ApplicationKey,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });
        Assert.That.Collection(logs.Items,
            app =>
            {
                Assert.AreEqual("546573745370616e4964", app.SpanId);
                Assert.AreEqual("5465737454726163654964", app.TraceId);
                Assert.AreEqual("Test {Log}", app.OriginalFormat);
                Assert.AreEqual("Test Value!", app.Message);
                Assert.AreEqual("TestLogger", app.Scope.ScopeName);
                Assert.That.Collection(app.Attributes,
                    p =>
                    {
                        Assert.AreEqual("Log", p.Key);
                        Assert.AreEqual("Value!", p.Value);
                    });
            });

        var propertyKeys = repository.GetLogPropertyKeys(applications[0].ApplicationKey)!;
        Assert.That.Collection(propertyKeys,
            s => Assert.AreEqual("Log", s));
    }

    [TestMethod]
    public void AddLogs_NoBody_EmptyMessage()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var addContext = new AddContext();
        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords = { CreateLogRecord(skipBody: true) }
                    }
                }
            }
        });

        // Assert
        Assert.AreEqual(0, addContext.FailureCount);

        var logs = repository.GetLogs(new GetLogsContext
        {
            ApplicationKey = null,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });
        Assert.That.Collection(logs.Items,
            app =>
            {
                Assert.AreEqual("", app.Message);
            });
    }

    [TestMethod]
    public void AddLogs_MultipleOutOfOrder()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var addContext = new AddContext();
        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        LogRecords =
                        {
                            CreateLogRecord(time: s_testTime.AddMinutes(1), message: "1"),
                            CreateLogRecord(time: s_testTime.AddMinutes(2), message: "2"),
                            CreateLogRecord(time: s_testTime.AddMinutes(3), message: "3"),
                            CreateLogRecord(time: s_testTime.AddMinutes(10), message: "10"),
                            CreateLogRecord(time: s_testTime.AddMinutes(9), message: "9"),
                            CreateLogRecord(time: s_testTime.AddMinutes(4), message: "4"),
                            CreateLogRecord(time: s_testTime.AddMinutes(5), message: "5"),
                            CreateLogRecord(time: s_testTime.AddMinutes(7), message: "7"),
                            CreateLogRecord(time: s_testTime.AddMinutes(6), message: "6"),
                            CreateLogRecord(time: s_testTime.AddMinutes(8), message: "8"),
                        }
                    }
                }
            }
        });

        // Assert
        Assert.AreEqual(0, addContext.FailureCount);

        var logs = repository.GetLogs(new GetLogsContext
        {
            ApplicationKey = null,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });
        Assert.That.Collection(logs.Items,
            l =>
            {
                Assert.AreEqual("1", l.Message);
                Assert.AreEqual("", l.Scope.ScopeName);
            },
            l => Assert.AreEqual("2", l.Message),
            l => Assert.AreEqual("3", l.Message),
            l => Assert.AreEqual("4", l.Message),
            l => Assert.AreEqual("5", l.Message),
            l => Assert.AreEqual("6", l.Message),
            l => Assert.AreEqual("7", l.Message),
            l => Assert.AreEqual("8", l.Message),
            l => Assert.AreEqual("9", l.Message),
            l => Assert.AreEqual("10", l.Message));
    }

    [TestMethod]
    public void AddLogs_Error_UnviewedCount()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var addContext = new AddContext();
        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(instanceId: "1"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords =
                        {
                            CreateLogRecord(time: s_testTime.AddMinutes(1), message: "1", severity: SeverityNumber.Trace),
                            CreateLogRecord(time: s_testTime.AddMinutes(2), message: "2", severity: SeverityNumber.Debug),
                            CreateLogRecord(time: s_testTime.AddMinutes(3), message: "3", severity: SeverityNumber.Info),
                            CreateLogRecord(time: s_testTime.AddMinutes(4), message: "4", severity: SeverityNumber.Warn),
                            CreateLogRecord(time: s_testTime.AddMinutes(5), message: "5", severity: SeverityNumber.Error),
                            CreateLogRecord(time: s_testTime.AddMinutes(6), message: "6", severity: SeverityNumber.Fatal)
                        }
                    }
                }
            },
            new ResourceLogs
            {
                Resource = CreateResource(instanceId: "2"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords =
                        {
                            CreateLogRecord(time: s_testTime.AddMinutes(1), message: "1", severity: SeverityNumber.Fatal)
                        }
                    }
                }
            }
        });

        // Assert
        Assert.AreEqual(0, addContext.FailureCount);

        var unviewedCounts1 = repository.GetApplicationUnviewedErrorLogsCount();

        Assert.IsTrue(unviewedCounts1.TryGetValue(new ApplicationKey("TestService", "1"), out var unviewedCount1));
        Assert.AreEqual(2, unviewedCount1);

        Assert.IsTrue(unviewedCounts1.TryGetValue(new ApplicationKey("TestService", "2"), out var unviewedCount2));
        Assert.AreEqual(1, unviewedCount2);

        repository.MarkViewedErrorLogs(new ApplicationKey("TestService", "1"));

        var unviewedCounts2 = repository.GetApplicationUnviewedErrorLogsCount();

        Assert.IsFalse(unviewedCounts2.TryGetValue(new ApplicationKey("TestService", "1"), out _));

        Assert.IsTrue(unviewedCounts2.TryGetValue(new ApplicationKey("TestService", "2"), out unviewedCount2));
        Assert.AreEqual(1, unviewedCount2);

        repository.MarkViewedErrorLogs(null);

        var unviewedCounts3 = repository.GetApplicationUnviewedErrorLogsCount();

        Assert.IsFalse(unviewedCounts3.TryGetValue(new ApplicationKey("TestService", "1"), out _));
        Assert.IsFalse(unviewedCounts3.TryGetValue(new ApplicationKey("TestService", "2"), out _));
    }

    [TestMethod]
    public void AddLogs_Error_UnviewedCount_WithReadSubscriptionAll()
    {
        // Arrange
        var repository = CreateRepository();
        using var subscription = repository.OnNewLogs(applicationKey: null, SubscriptionType.Read, () => Task.CompletedTask);

        // Act
        var addContext = new AddContext();
        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(instanceId: "1"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords =
                        {
                            CreateLogRecord(time: s_testTime.AddMinutes(1), message: "1", severity: SeverityNumber.Error),
                        }
                    }
                }
            },
            new ResourceLogs
            {
                Resource = CreateResource(instanceId: "2"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords =
                        {
                            CreateLogRecord(time: s_testTime.AddMinutes(1), message: "1", severity: SeverityNumber.Fatal)
                        }
                    }
                }
            }
        });

        // Assert
        Assert.AreEqual(0, addContext.FailureCount);

        var unviewedCounts = repository.GetApplicationUnviewedErrorLogsCount();

        Assert.IsFalse(unviewedCounts.TryGetValue(new ApplicationKey("TestService", "1"), out _));
        Assert.IsFalse(unviewedCounts.TryGetValue(new ApplicationKey("TestService", "2"), out _));
    }

    [TestMethod]
    public void AddLogs_Error_UnviewedCount_WithReadSubscriptionOneApp()
    {
        // Arrange
        var repository = CreateRepository();
        using var subscription = repository.OnNewLogs(applicationKey: new ApplicationKey("TestService", "1"), SubscriptionType.Read, () => Task.CompletedTask);

        // Act
        var addContext = new AddContext();
        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(instanceId: "1"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords =
                        {
                            CreateLogRecord(time: s_testTime.AddMinutes(1), message: "1", severity: SeverityNumber.Error),
                        }
                    }
                }
            },
            new ResourceLogs
            {
                Resource = CreateResource(instanceId: "2"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords =
                        {
                            CreateLogRecord(time: s_testTime.AddMinutes(1), message: "1", severity: SeverityNumber.Fatal)
                        }
                    }
                }
            }
        });

        // Assert
        Assert.AreEqual(0, addContext.FailureCount);

        var unviewedCounts = repository.GetApplicationUnviewedErrorLogsCount();

        Assert.IsFalse(unviewedCounts.TryGetValue(new ApplicationKey("TestService", "1"), out _));
        Assert.IsTrue(unviewedCounts.TryGetValue(new ApplicationKey("TestService", "2"), out var unviewedCount));
        Assert.AreEqual(1, unviewedCount);
    }

    [TestMethod]
    public void AddLogs_Error_UnviewedCount_WithNonReadSubscription()
    {
        // Arrange
        var repository = CreateRepository();
        using var subscription = repository.OnNewLogs(applicationKey: null, SubscriptionType.Other, () => Task.CompletedTask);

        // Act
        var addContext = new AddContext();
        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(instanceId: "1"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords =
                        {
                            CreateLogRecord(time: s_testTime.AddMinutes(1), message: "1", severity: SeverityNumber.Error),
                        }
                    }
                }
            }
        });

        // Assert
        Assert.AreEqual(0, addContext.FailureCount);

        var unviewedCounts = repository.GetApplicationUnviewedErrorLogsCount();

        Assert.IsTrue(unviewedCounts.TryGetValue(new ApplicationKey("TestService", "1"), out var unviewedCount));
        Assert.AreEqual(1, unviewedCount);
    }

    [TestMethod]
    public void GetLogs_UnknownApplication()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var logs = repository.GetLogs(new GetLogsContext
        {
            ApplicationKey = new ApplicationKey("TestService", "UnknownApplication"),
            StartIndex = 0,
            Count = 10,
            Filters = []
        });

        // Assert
        Assert.IsEmpty(logs.Items);
    }

    [TestMethod]
    public void GetLogPropertyKeys_UnknownApplication()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var propertyKeys = repository.GetLogPropertyKeys(new ApplicationKey("TestService", "UnknownApplication"));

        // Assert
        Assert.IsEmpty(propertyKeys);
    }

    [TestMethod]
    public async Task Subscriptions_AddLog()
    {
        // Arrange
        var repository = CreateRepository();

        var newApplicationsTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        repository.OnNewApplications(() =>
        {
            newApplicationsTcs.TrySetResult();
            return Task.CompletedTask;
        });

        // Act 1
        var addContext1 = new AddContext();
        repository.AddLogs(addContext1, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords = { CreateLogRecord() }
                    }
                }
            }
        });

        // Assert 1
        Assert.AreEqual(0, addContext1.FailureCount);
        await newApplicationsTcs.Task.DefaultTimeout();

        var applications = repository.GetApplications();
        Assert.That.Collection(applications,
            app =>
            {
                Assert.AreEqual("TestService", app.ApplicationName);
                Assert.AreEqual("TestId", app.InstanceId);
            });

        // Act 2
        var newLogsTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        repository.OnNewLogs(applications[0].ApplicationKey, SubscriptionType.Read, () =>
        {
            newLogsTcs.TrySetResult();
            return Task.CompletedTask;
        });

        var addContext2 = new AddContext();
        repository.AddLogs(addContext2, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords = { CreateLogRecord() }
                    }
                }
            }
        });

        await newLogsTcs.Task.DefaultTimeout();

        // Assert 2
        Assert.AreEqual(0, addContext2.FailureCount);

        var logs = repository.GetLogs(new GetLogsContext
        {
            ApplicationKey = applications[0].ApplicationKey,
            StartIndex = 0,
            Count = 1,
            Filters = []
        })!;
        Assert.ContainsSingle(logs.Items);
        Assert.AreEqual(2, logs.TotalItemCount);
    }

    [TestMethod]
    public void Unsubscribe()
    {
        // Arrange
        var repository = CreateRepository();

        var onNewApplicationsCalled = false;
        var subscription = repository.OnNewApplications(() =>
        {
            onNewApplicationsCalled = true;
            return Task.CompletedTask;
        });
        subscription.Dispose();

        // Act
        var addContext = new AddContext();
        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords = { CreateLogRecord() }
                    }
                }
            }
        });

        // Assert
        Assert.AreEqual(0, addContext.FailureCount);
        Assert.IsFalse(onNewApplicationsCalled, "Callback shouldn't have been called because subscription was disposed.");
    }

    [TestMethod]
    public async Task Subscription_RaisedFromDifferentContext_InitialContextPreserved()
    {
        // Arrange
        var asyncLocal = new AsyncLocal<string>();
        asyncLocal.Value = "CustomValue";

        var repository = CreateRepository();

        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var subscription = repository.OnNewApplications(() =>
        {
            tcs.SetResult(asyncLocal.Value);
            return Task.CompletedTask;
        });

        // Act
        Task task;
        using (ExecutionContext.SuppressFlow())
        {
            task = Task.Run(() =>
            {
                var addContext = new AddContext();
                repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
                {
                    new ResourceLogs
                    {
                        Resource = CreateResource(),
                        ScopeLogs =
                        {
                            new ScopeLogs
                            {
                                Scope = CreateScope("TestLogger"),
                                LogRecords = { CreateLogRecord() }
                            }
                        }
                    }
                });
            });
        }

        await task.DefaultTimeout();

        // Assert
        var callbackValue = await tcs.Task.DefaultTimeout();
        Assert.AreEqual("CustomValue", callbackValue);
    }

    [TestMethod]
    public void AddLogs_AttributeLimits_LimitsApplied()
    {
        // Arrange
        var repository = CreateRepository(maxAttributeCount: 5, maxAttributeLength: 16);

        // Act
        var attributes = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("{OriginalFormat}", "Test {Log}")
        };

        for (var i = 0; i < 10; i++)
        {
            var value = GetValue((i + 1) * 5);
            attributes.Add(new KeyValuePair<string, string>($"Key{i}", value));
        }

        var addContext = new AddContext();
        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords = { CreateLogRecord(message: GetValue(50), attributes: attributes) }
                    }
                }
            }
        });

        // Assert
        Assert.AreEqual(0, addContext.FailureCount);

        var applications = repository.GetApplications();
        Assert.That.Collection(applications,
            app =>
            {
                Assert.AreEqual("TestService", app.ApplicationName);
                Assert.AreEqual("TestId", app.InstanceId);
            });

        var logs = repository.GetLogs(new GetLogsContext
        {
            ApplicationKey = applications[0].ApplicationKey,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });
        Assert.That.Collection(logs.Items,
            app =>
            {
                Assert.AreEqual("Test {Log}", app.OriginalFormat);
                Assert.AreEqual("0123456789012345", app.Message);
                Assert.That.Collection(app.Attributes,
                    p =>
                    {
                        Assert.AreEqual("Key0", p.Key);
                        Assert.AreEqual("01234", p.Value);
                    },
                    p =>
                    {
                        Assert.AreEqual("Key1", p.Key);
                        Assert.AreEqual("0123456789", p.Value);
                    },
                    p =>
                    {
                        Assert.AreEqual("Key2", p.Key);
                        Assert.AreEqual("012345678901234", p.Value);
                    },
                    p =>
                    {
                        Assert.AreEqual("Key3", p.Key);
                        Assert.AreEqual("0123456789012345", p.Value);
                    },
                    p =>
                    {
                        Assert.AreEqual("Key4", p.Key);
                        Assert.AreEqual("0123456789012345", p.Value);
                    });
            });
    }

    [TestMethod]
    public async Task Subscription_MultipleUpdates_MinExecuteIntervalApplied()
    {
        // Arrange
        var minExecuteInterval = TimeSpan.FromMilliseconds(500);
        var loggerFactory = IntegrationTestHelpers.CreateLoggerFactory(_testContext);
        var logger = loggerFactory.CreateLogger(nameof(LogTests));
        var repository = CreateRepository(subscriptionMinExecuteInterval: minExecuteInterval, loggerFactory: loggerFactory);
        var stopwatch = new Stopwatch();

        var callCount = 0;
        var resultChannel = Channel.CreateUnbounded<int>();
        var subscription = repository.OnNewLogs(applicationKey: null, SubscriptionType.Read, async () =>
        {
            if (!stopwatch.IsRunning)
            {
                stopwatch.Start();
            }
            else
            {
                stopwatch.Stop();
            }
            ++callCount;
            resultChannel.Writer.TryWrite(callCount);
            await Task.Delay(20);
        });

        // Act
        var addContext = new AddContext();
        logger.LogInformation("Writing log 1");
        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords = { CreateLogRecord() }
                    }
                }
            }
        });

        // Assert
        var read1 = await resultChannel.Reader.ReadAsync().DefaultTimeout();
        Assert.AreEqual(1, read1);
        logger.LogInformation("Received log 1 callback");

        logger.LogInformation("Writing log 2");
        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords = { CreateLogRecord() }
                    }
                }
            }
        });

        var read2 = await resultChannel.Reader.ReadAsync().DefaultTimeout();
        Assert.AreEqual(2, read2);
        logger.LogInformation("Received log 2 callback");

        var elapsed = stopwatch.Elapsed;
        logger.LogInformation("Elapsed time: {Elapsed}", elapsed);
        CustomAssert.AssertExceedsMinInterval(elapsed, minExecuteInterval);
    }

    [TestMethod]
    public void FilterLogs_With_Message_Returns_CorrectLog()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var addContext = new AddContext();
        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(instanceId: "1"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords =
                        {
                            CreateLogRecord(time: s_testTime.AddMinutes(1), message: "test_message", severity: SeverityNumber.Error),
                        }
                    }
                }
            }
        });

        var applicationKey = repository.GetApplications().First().ApplicationKey;

        // Assert
        Assert.IsEmpty(repository.GetLogs(new GetLogsContext
        {
            ApplicationKey = applicationKey,
            StartIndex = 0,
            Count = 1,
            Filters = [new TelemetryFilter { Condition = FilterCondition.Contains, Field = nameof(OtlpLogEntry.Message), Value = "does_not_contain" }]
        }).Items);

        Assert.ContainsSingle(repository.GetLogs(new GetLogsContext
        {
            ApplicationKey = applicationKey,
            StartIndex = 0,
            Count = 1,
            Filters = [new TelemetryFilter { Condition = FilterCondition.Contains, Field = nameof(OtlpLogEntry.Message), Value = "message" }]
        }).Items);
    }

    [TestMethod]
    public void AddLogs_MultipleResources_SameInstanceId_CreateMultipleResources()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var addContext = new AddContext();
        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(name: "App1", instanceId: "computer-name"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords = { CreateLogRecord() }
                    }
                }
            },
            new ResourceLogs
            {
                Resource = CreateResource(name: "App2", instanceId: "computer-name"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords = { CreateLogRecord() }
                    }
                }
            }
        });

        // Assert
        Assert.AreEqual(0, addContext.FailureCount);

        var applications = repository.GetApplications();
        Assert.That.Collection(applications,
            app =>
            {
                Assert.AreEqual("App1", app.ApplicationName);
                Assert.AreEqual("computer-name", app.InstanceId);
            },
            app =>
            {
                Assert.AreEqual("App2", app.ApplicationName);
                Assert.AreEqual("computer-name", app.InstanceId);
            });

        var logs1 = repository.GetLogs(new GetLogsContext
        {
            ApplicationKey = applications[0].ApplicationKey,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });
        Assert.That.Collection(logs1.Items,
            app =>
            {
                Assert.AreEqual("546573745370616e4964", app.SpanId);
                Assert.AreEqual("5465737454726163654964", app.TraceId);
                Assert.AreEqual("Test {Log}", app.OriginalFormat);
                Assert.AreEqual("Test Value!", app.Message);
                Assert.AreEqual("TestLogger", app.Scope.ScopeName);
                Assert.That.Collection(app.Attributes,
                    p =>
                    {
                        Assert.AreEqual("Log", p.Key);
                        Assert.AreEqual("Value!", p.Value);
                    });
            });

        var logs2 = repository.GetLogs(new GetLogsContext
        {
            ApplicationKey = applications[1].ApplicationKey,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });
        Assert.That.Collection(logs2.Items,
            app =>
            {
                Assert.AreEqual("546573745370616e4964", app.SpanId);
                Assert.AreEqual("5465737454726163654964", app.TraceId);
                Assert.AreEqual("Test {Log}", app.OriginalFormat);
                Assert.AreEqual("Test Value!", app.Message);
                Assert.AreEqual("TestLogger", app.Scope.ScopeName);
                Assert.That.Collection(app.Attributes,
                    p =>
                    {
                        Assert.AreEqual("Log", p.Key);
                        Assert.AreEqual("Value!", p.Value);
                    });
            });
    }

    [TestMethod]
    public void GetLogs_MultipleInstances()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var addContext = new AddContext();
        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(name: "app1", instanceId: "123"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords = { CreateLogRecord(time: s_testTime.AddMinutes(1), message: "message-1", attributes: [KeyValuePair.Create("key-1", "value-1")]) }
                    }
                }
            },
            new ResourceLogs
            {
                Resource = CreateResource(name: "app1", instanceId: "456"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords = { CreateLogRecord(time: s_testTime.AddMinutes(2), message: "message-2", attributes: [KeyValuePair.Create("key-2", "value-2")]) }
                    }
                }
            },
            new ResourceLogs
            {
                Resource = CreateResource(name: "app2"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords = { CreateLogRecord(time: s_testTime.AddMinutes(3)) }
                    }
                }
            }
        });

        // Assert
        Assert.AreEqual(0, addContext.FailureCount);

        var appKey = new ApplicationKey("app1", InstanceId: null);
        var logs = repository.GetLogs(new GetLogsContext
        {
            ApplicationKey = appKey,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });
        Assert.That.Collection(logs.Items,
            app =>
            {
                Assert.AreEqual("message-1", app.Message);
                Assert.AreEqual("TestLogger", app.Scope.ScopeName);
                Assert.That.Collection(app.Attributes,
                    p =>
                    {
                        Assert.AreEqual("key-1", p.Key);
                        Assert.AreEqual("value-1", p.Value);
                    });
            },
            app =>
            {
                Assert.AreEqual("message-2", app.Message);
                Assert.AreEqual("TestLogger", app.Scope.ScopeName);
                Assert.That.Collection(app.Attributes,
                    p =>
                    {
                        Assert.AreEqual("key-2", p.Key);
                        Assert.AreEqual("value-2", p.Value);
                    });
            });

        var propertyKeys = repository.GetLogPropertyKeys(appKey)!;
        Assert.That.Collection(propertyKeys,
            s => Assert.AreEqual("key-1", s),
            s => Assert.AreEqual("key-2", s));
    }

    [TestMethod]
    public void RemoveLogs_All()
    {
        // Arrange
        var repository = CreateRepository();

        var addContext = new AddContext();
        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(name: "app1", instanceId: "123"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords = { CreateLogRecord(time: s_testTime.AddMinutes(1), message: "message-1") }
                    }
                }
            },
            new ResourceLogs
            {
                Resource = CreateResource(name: "app1", instanceId: "456"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords = { CreateLogRecord(time: s_testTime.AddMinutes(2), message: "message-2") }
                    }
                }
            },
            new ResourceLogs
            {
                Resource = CreateResource(name: "app2"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords = { CreateLogRecord(time: s_testTime.AddMinutes(3)) }
                    }
                }
            }
        });

        // Act
        repository.ClearStructuredLogs();

        // Assert
        Assert.AreEqual(0, addContext.FailureCount);

        var logs = repository.GetLogs(new GetLogsContext
        {
            ApplicationKey = null,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });
        Assert.IsNotNull(logs);
        Assert.IsEmpty(logs.Items);
        Assert.AreEqual(0, logs.TotalItemCount);
    }

    [TestMethod]
    public void RemoveLogs_SelectedResource()
    {
        // Arrange
        var repository = CreateRepository();

        var addContext = new AddContext();
        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(name: "app1", instanceId: "123"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords = { CreateLogRecord(time: s_testTime.AddMinutes(1), message: "message-1") }
                    }
                }
            },
            new ResourceLogs
            {
                Resource = CreateResource(name: "app1", instanceId: "456"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords = { CreateLogRecord(time: s_testTime.AddMinutes(2), message: "message-2") }
                    }
                }
            },
            new ResourceLogs
            {
                Resource = CreateResource(name: "app2"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords = { CreateLogRecord(time: s_testTime.AddMinutes(3), message: "message-3") }
                    }
                }
            }
        });

        // Act
        repository.ClearStructuredLogs(new ApplicationKey("app1", "123"));

        // Assert
        Assert.AreEqual(0, addContext.FailureCount);

        var logs = repository.GetLogs(new GetLogsContext
        {
            ApplicationKey = null,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });
        Assert.AreEqual(2, logs.TotalItemCount);
        Assert.That.Collection(logs.Items,
                    app =>
                    {
                        Assert.AreEqual("message-2", app.Message);
                        Assert.AreEqual("TestLogger", app.Scope.ScopeName);
                    },
                    app =>
                    {
                        Assert.AreEqual("message-3", app.Message);
                        Assert.AreEqual("TestLogger", app.Scope.ScopeName);
                    });
    }

    [TestMethod]
    public void RemoveLogs_MultipleSelectedResources()
    {
        // Arrange
        var repository = CreateRepository();

        var addContext = new AddContext();
        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(name: "app1", instanceId: "123"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords = { CreateLogRecord(time: s_testTime.AddMinutes(1), message: "message-1") }
                    }
                }
            },
            new ResourceLogs
            {
                Resource = CreateResource(name: "app1", instanceId: "456"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords = { CreateLogRecord(time: s_testTime.AddMinutes(2), message: "message-2") }
                    }
                }
            },
            new ResourceLogs
            {
                Resource = CreateResource(name: "app2"),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords = { CreateLogRecord(time: s_testTime.AddMinutes(3), message: "message-3") }
                    }
                }
            }
        });

        // Act
        repository.ClearStructuredLogs(new ApplicationKey("app1", null));

        // Assert
        Assert.AreEqual(0, addContext.FailureCount);

        var logs = repository.GetLogs(new GetLogsContext
        {
            ApplicationKey = null,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });
        Assert.AreEqual(1, logs.TotalItemCount);
        var log = Assert.ContainsSingle(logs.Items);
        Assert.AreEqual("message-3", log.Message);
        Assert.AreEqual("TestLogger", log.Scope.ScopeName);
    }

    [TestMethod]
    public void AddLogs_ObservedUnixTimeNanos()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var addContext = new AddContext();
        repository.AddLogs(addContext, new RepeatedField<ResourceLogs>()
        {
            new ResourceLogs
            {
                Resource = CreateResource(),
                ScopeLogs =
                {
                    new ScopeLogs
                    {
                        Scope = CreateScope("TestLogger"),
                        LogRecords = { CreateLogRecord(time: DateTime.UnixEpoch, observedTime: s_testTime.AddMinutes(1)) }
                    }
                }
            }
        });

        // Assert
        Assert.AreEqual(0, addContext.FailureCount);

        var logs = repository.GetLogs(new GetLogsContext
        {
            ApplicationKey = null,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });
        Assert.That.Collection(logs.Items,
            app =>
            {
                Assert.AreEqual(s_testTime.AddMinutes(1), app.TimeStamp);
            });
    }
}
