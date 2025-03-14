// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Text;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Model.MetricValues;
using Aspire.Dashboard.Otlp.Storage;
using Google.Protobuf;
using Google.Protobuf.Collections;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Metrics.V1;
using static Aspire.Tests.Shared.Telemetry.TelemetryTestHelpers;

namespace Aspire.Dashboard.Tests.TelemetryRepositoryTests;

[TestClass]
public class MetricsTests
{
    private static readonly DateTime s_testTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void AddMetrics()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var addContext = new AddContext();
        repository.AddMetrics(addContext, new RepeatedField<ResourceMetrics>()
        {
            new ResourceMetrics
            {
                Resource = CreateResource(),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(1)),
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(2)),
                            CreateSumMetric(metricName: "test2", startTime: s_testTime.AddMinutes(1)),
                        }
                    },
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter2"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(1)),
                            CreateHistogramMetric(metricName: "test2", startTime: s_testTime.AddMinutes(1))
                        }
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

        var instruments = repository.GetInstrumentsSummaries(applications[0].ApplicationKey);
        Assert.That.Collection(instruments,
            instrument =>
            {
                Assert.AreEqual("test", instrument.Name);
                Assert.AreEqual("Test metric description", instrument.Description);
                Assert.AreEqual("widget", instrument.Unit);
                Assert.AreEqual("test-meter", instrument.Parent.MeterName);
            },
            instrument =>
            {
                Assert.AreEqual("test2", instrument.Name);
                Assert.AreEqual("Test metric description", instrument.Description);
                Assert.AreEqual("widget", instrument.Unit);
                Assert.AreEqual("test-meter", instrument.Parent.MeterName);
            },
            instrument =>
            {
                Assert.AreEqual("test", instrument.Name);
                Assert.AreEqual("Test metric description", instrument.Description);
                Assert.AreEqual("widget", instrument.Unit);
                Assert.AreEqual("test-meter2", instrument.Parent.MeterName);
            },
            instrument =>
            {
                Assert.AreEqual("test2", instrument.Name);
                Assert.AreEqual("Test metric description", instrument.Description);
                Assert.AreEqual("widget", instrument.Unit);
                Assert.AreEqual("test-meter2", instrument.Parent.MeterName);
            });
    }

    [TestMethod]
    public void AddMetrics_MeterAttributeLimits_LimitsApplied()
    {
        // Arrange
        var repository = CreateRepository(maxAttributeCount: 5, maxAttributeLength: 16);

        var metricAttributes = new List<KeyValuePair<string, string>>();
        var meterAttributes = new List<KeyValuePair<string, string>>();

        for (var i = 0; i < 10; i++)
        {
            var value = GetValue((i + 1) * 5);
            metricAttributes.Add(new KeyValuePair<string, string>($"Metric_Key{i}", value));
            meterAttributes.Add(new KeyValuePair<string, string>($"Meter_Key{i}", value));
        }

        // Act
        var addContext = new AddContext();
        repository.AddMetrics(addContext, new RepeatedField<ResourceMetrics>()
        {
            new ResourceMetrics
            {
                Resource = CreateResource(),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter", attributes: meterAttributes),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(1), attributes: metricAttributes)
                        }
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

        var instrument = repository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationKey = applications[0].ApplicationKey,
            InstrumentName = "test",
            MeterName = "test-meter",
            StartTime = DateTime.MinValue,
            EndTime = DateTime.MaxValue
        })!;

        Assert.That.Collection(instrument.Summary.Parent.Attributes,
            p =>
            {
                Assert.AreEqual("Meter_Key0", p.Key);
                Assert.AreEqual("01234", p.Value);
            },
            p =>
            {
                Assert.AreEqual("Meter_Key1", p.Key);
                Assert.AreEqual("0123456789", p.Value);
            },
            p =>
            {
                Assert.AreEqual("Meter_Key2", p.Key);
                Assert.AreEqual("012345678901234", p.Value);
            },
            p =>
            {
                Assert.AreEqual("Meter_Key3", p.Key);
                Assert.AreEqual("0123456789012345", p.Value);
            },
            p =>
            {
                Assert.AreEqual("Meter_Key4", p.Key);
                Assert.AreEqual("0123456789012345", p.Value);
            });

        var dimensionAttributes = instrument.Dimensions.Single().Attributes;

        Assert.That.Collection(dimensionAttributes,
            p =>
            {
                Assert.AreEqual("Meter_Key0", p.Key);
                Assert.AreEqual("01234", p.Value);
            },
            p =>
            {
                Assert.AreEqual("Meter_Key1", p.Key);
                Assert.AreEqual("0123456789", p.Value);
            },
            p =>
            {
                Assert.AreEqual("Meter_Key2", p.Key);
                Assert.AreEqual("012345678901234", p.Value);
            },
            p =>
            {
                Assert.AreEqual("Meter_Key3", p.Key);
                Assert.AreEqual("0123456789012345", p.Value);
            },
            p =>
            {
                Assert.AreEqual("Meter_Key4", p.Key);
                Assert.AreEqual("0123456789012345", p.Value);
            });
    }

    [TestMethod]
    public void AddMetrics_MetricAttributeLimits_LimitsApplied()
    {
        // Arrange
        var repository = CreateRepository(maxAttributeCount: 5, maxAttributeLength: 16);

        var metricAttributes = new List<KeyValuePair<string, string>>();
        var meterAttributes = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("Meter_Key0", GetValue(5))
        };

        for (var i = 0; i < 10; i++)
        {
            var value = GetValue((i + 1) * 5);
            metricAttributes.Add(new KeyValuePair<string, string>($"Metric_Key{i}", value));
        }

        // Act
        var addContext = new AddContext();
        repository.AddMetrics(addContext, new RepeatedField<ResourceMetrics>()
        {
            new ResourceMetrics
            {
                Resource = CreateResource(),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter", attributes: meterAttributes),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(1), attributes: metricAttributes)
                        }
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

        var instrument = repository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationKey = applications[0].ApplicationKey,
            InstrumentName = "test",
            MeterName = "test-meter",
            StartTime = DateTime.MinValue,
            EndTime = DateTime.MaxValue
        })!;

        Assert.That.Collection(instrument.Summary.Parent.Attributes,
            p =>
            {
                Assert.AreEqual("Meter_Key0", p.Key);
                Assert.AreEqual("01234", p.Value);
            });

        var dimensionAttributes = instrument.Dimensions.Single().Attributes;

        Assert.That.Collection(dimensionAttributes,
            p =>
            {
                Assert.AreEqual("Meter_Key0", p.Key);
                Assert.AreEqual("01234", p.Value);
            },
            p =>
            {
                Assert.AreEqual("Metric_Key0", p.Key);
                Assert.AreEqual("01234", p.Value);
            },
            p =>
            {
                Assert.AreEqual("Metric_Key1", p.Key);
                Assert.AreEqual("0123456789", p.Value);
            },
            p =>
            {
                Assert.AreEqual("Metric_Key2", p.Key);
                Assert.AreEqual("012345678901234", p.Value);
            },
            p =>
            {
                Assert.AreEqual("Metric_Key3", p.Key);
                Assert.AreEqual("0123456789012345", p.Value);
            });
    }

    [TestMethod]
    public void RoundtripSeconds()
    {
        var start = s_testTime.AddMinutes(1);
        var nanoSeconds = DateTimeToUnixNanoseconds(start);
        var end = OtlpHelpers.UnixNanoSecondsToDateTime(nanoSeconds);
        Assert.AreEqual(start, end);
    }

    [TestMethod]
    public void GetInstrument()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var addContext = new AddContext();
        repository.AddMetrics(addContext, new RepeatedField<ResourceMetrics>()
        {
            new ResourceMetrics
            {
                Resource = CreateResource(),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(1), exemplars: new List<Exemplar> { CreateExemplar(startTime: s_testTime.AddMinutes(1), value: 2, attributes: [KeyValuePair.Create("key1", "value1")]) }),
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(2)),
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("key1", "value1")]),
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("key1", "value2")]),
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("key1", "value1"), KeyValuePair.Create("key2", "value1")]),
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("key1", "value1"), KeyValuePair.Create("key2", "")])
                        }
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

        var instrumentData = repository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationKey = applications[0].ApplicationKey,
            InstrumentName = "test",
            MeterName = "test-meter",
            StartTime = s_testTime.AddMinutes(1),
            EndTime = s_testTime.AddMinutes(1.5),
        });

        Assert.IsNotNull(instrumentData);
        Assert.AreEqual("test", instrumentData.Summary.Name);
        Assert.AreEqual("Test metric description", instrumentData.Summary.Description);
        Assert.AreEqual("widget", instrumentData.Summary.Unit);
        Assert.AreEqual("test-meter", instrumentData.Summary.Parent.MeterName);

        Assert.That.Collection(instrumentData.KnownAttributeValues.OrderBy(kvp => kvp.Key),
            e =>
            {
                Assert.AreEqual("key1", e.Key);
                Assert.AreEqual(new[] { null, "value1", "value2" }, e.Value);
            },
            e =>
            {
                Assert.AreEqual("key2", e.Key);
                Assert.AreEqual(new[] { null, "value1", "" }, e.Value);
            });

        Assert.AreEqual(5, instrumentData.Dimensions.Count);

        var dimension = instrumentData.Dimensions.Single(d => d.Attributes.Length == 0);
        var exemplar = Assert.ContainsSingle(dimension.Values[0].Exemplars);

        Assert.AreEqual("key1", exemplar.Attributes[0].Key);
        Assert.AreEqual("value1", exemplar.Attributes[0].Value);

        var instrument = applications.Single().GetInstrument("test-meter", "test", s_testTime.AddMinutes(1), s_testTime.AddMinutes(1.5));
        Assert.IsNotNull(instrument);

        AssertDimensionValues(instrument.Dimensions, Array.Empty<KeyValuePair<string, string>>(), valueCount: 1);
        AssertDimensionValues(instrument.Dimensions, new KeyValuePair<string, string>[] { KeyValuePair.Create("key1", "value1") }, valueCount: 1);
        AssertDimensionValues(instrument.Dimensions, new KeyValuePair<string, string>[] { KeyValuePair.Create("key1", "value2") }, valueCount: 1);
        AssertDimensionValues(instrument.Dimensions, new KeyValuePair<string, string>[] { KeyValuePair.Create("key1", "value1"), KeyValuePair.Create("key2", "value1") }, valueCount: 1);
    }

    private static Exemplar CreateExemplar(DateTime startTime, double value, IEnumerable<KeyValuePair<string, string>>? attributes = null)
    {
        var exemplar = new Exemplar
        {
            TimeUnixNano = DateTimeToUnixNanoseconds(startTime),
            AsDouble = value,
            SpanId = ByteString.CopyFrom(Encoding.UTF8.GetBytes("span-id")),
            TraceId = ByteString.CopyFrom(Encoding.UTF8.GetBytes("trace-id"))
        };

        if (attributes != null)
        {
            foreach (var attribute in attributes)
            {
                exemplar.FilteredAttributes.Add(new KeyValue { Key = attribute.Key, Value = new AnyValue { StringValue = attribute.Value } });
            }
        }

        return exemplar;
    }

    [TestMethod]
    public void AddMetrics_Capacity_ValuesRemoved()
    {
        // Arrange
        var repository = CreateRepository(maxMetricsCount: 3);

        // Act
        var addContext = new AddContext();
        repository.AddMetrics(addContext, new RepeatedField<ResourceMetrics>()
        {
            new ResourceMetrics
            {
                Resource = CreateResource(),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(1), value: 1),
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(2), value: 2),
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(3), value: 3),
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(4), value: 4),
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(5), value: 5),
                        }
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

        var instrument = repository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationKey = applications[0].ApplicationKey,
            InstrumentName = "test",
            MeterName = "test-meter",
            StartTime = DateTime.MinValue,
            EndTime = DateTime.MaxValue
        })!;

        Assert.AreEqual("test", instrument.Summary.Name);
        Assert.AreEqual("Test metric description", instrument.Summary.Description);
        Assert.AreEqual("widget", instrument.Summary.Unit);
        Assert.AreEqual("test-meter", instrument.Summary.Parent.MeterName);

        // Only the last 3 values should be kept.
        var dimension = Assert.ContainsSingle(instrument.Dimensions);
        Assert.That.Collection(dimension.Values,
            m =>
            {
                Assert.AreEqual(s_testTime.AddMinutes(2), m.Start);
                Assert.AreEqual(s_testTime.AddMinutes(3), m.End);
                Assert.AreEqual(3, ((MetricValue<long>)m).Value);
            },
            m =>
            {
                Assert.AreEqual(s_testTime.AddMinutes(3), m.Start);
                Assert.AreEqual(s_testTime.AddMinutes(4), m.End);
                Assert.AreEqual(4, ((MetricValue<long>)m).Value);
            },
            m =>
            {
                Assert.AreEqual(s_testTime.AddMinutes(4), m.Start);
                Assert.AreEqual(s_testTime.AddMinutes(5), m.End);
                Assert.AreEqual(5, ((MetricValue<long>)m).Value);
            });
    }

    [TestMethod]
    public void GetMetrics_MultipleInstances()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var addContext = new AddContext();
        repository.AddMetrics(addContext, new RepeatedField<ResourceMetrics>()
        {
            new ResourceMetrics
            {
                Resource = CreateResource(name: "app1", instanceId: "123"),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test1", value: 1, startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("key-1", "value-1")]),
                            CreateSumMetric(metricName: "test1", value: 2, startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("key-1", "value-2")])
                        }
                    }
                }
            },
            new ResourceMetrics
            {
                Resource = CreateResource(name: "app1", instanceId: "456"),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test1", value: 3, startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("key-1", "value-3")]),
                            CreateSumMetric(metricName: "test2", value: 4, startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("key-1", "value-4")])
                        }
                    }
                }
            },
            new ResourceMetrics
            {
                Resource = CreateResource(name: "app2"),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test1", value: 5, startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("key-1", "value-5")]),
                            CreateSumMetric(metricName: "test3", value: 6, startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("key-1", "value-6")])
                        }
                    }
                }
            }
        });

        // Assert
        Assert.AreEqual(0, addContext.FailureCount);

        var appKey = new ApplicationKey("app1", InstanceId: null);
        var instruments = repository.GetInstrumentsSummaries(appKey);
        Assert.That.Collection(instruments,
            instrument =>
            {
                Assert.AreEqual("test1", instrument.Name);
                Assert.AreEqual("Test metric description", instrument.Description);
                Assert.AreEqual("widget", instrument.Unit);
                Assert.AreEqual("test-meter", instrument.Parent.MeterName);
            },
            instrument =>
            {
                Assert.AreEqual("test2", instrument.Name);
                Assert.AreEqual("Test metric description", instrument.Description);
                Assert.AreEqual("widget", instrument.Unit);
                Assert.AreEqual("test-meter", instrument.Parent.MeterName);
            });

        var instrument = repository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationKey = appKey,
            InstrumentName = "test1",
            MeterName = "test-meter",
            StartTime = s_testTime,
            EndTime = s_testTime.AddMinutes(20)
        });

        Assert.IsNotNull(instrument);
        Assert.AreEqual("test1", instrument.Summary.Name);

        Assert.That.Collection(instrument.Dimensions.OrderBy(d => d.Name),
            d =>
            {
                Assert.AreEqual(KeyValuePair.Create("key-1", "value-1"), d.Attributes.Single());
                Assert.AreEqual(1, ((MetricValue<long>)d.Values.Single()).Value);
            },
            d =>
            {
                Assert.AreEqual(KeyValuePair.Create("key-1", "value-2"), d.Attributes.Single());
                Assert.AreEqual(2, ((MetricValue<long>)d.Values.Single()).Value);
            },
            d =>
            {
                Assert.AreEqual(KeyValuePair.Create("key-1", "value-3"), d.Attributes.Single());
                Assert.AreEqual(3, ((MetricValue<long>)d.Values.Single()).Value);
            });

        var knownValues = Assert.ContainsSingle(instrument.KnownAttributeValues);
        Assert.AreEqual("key-1", knownValues.Key);

        Assert.That.Collection(knownValues.Value.Order(),
            v => Assert.AreEqual("value-1", v),
            v => Assert.AreEqual("value-2", v),
            v => Assert.AreEqual("value-3", v));
    }

    [TestMethod]
    public void RemoveMetrics_All()
    {
        // Arrange
        var repository = CreateRepository();

        var addContext = new AddContext();
        repository.AddMetrics(addContext, new RepeatedField<ResourceMetrics>()
        {
            new ResourceMetrics
            {
                Resource = CreateResource(name: "app1", instanceId: "123"),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test1", value: 1, startTime: s_testTime.AddMinutes(1)),
                            CreateSumMetric(metricName: "test1", value: 2, startTime: s_testTime.AddMinutes(1))
                        }
                    }
                }
            },
            new ResourceMetrics
            {
                Resource = CreateResource(name: "app1", instanceId: "456"),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test1", value: 3, startTime: s_testTime.AddMinutes(1)),
                            CreateSumMetric(metricName: "test2", value: 4, startTime: s_testTime.AddMinutes(1))
                        }
                    }
                }
            },
            new ResourceMetrics
            {
                Resource = CreateResource(name: "app2"),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test1", value: 5, startTime: s_testTime.AddMinutes(1)),
                            CreateSumMetric(metricName: "test3", value: 6, startTime: s_testTime.AddMinutes(1))
                        }
                    }
                }
            }
        });

        // Act
        repository.ClearMetrics();

        // Assert
        Assert.AreEqual(0, addContext.FailureCount);

        var app1Key = new ApplicationKey("app1", InstanceId: null);
        var app1Instruments = repository.GetInstrumentsSummaries(app1Key);
        Assert.IsEmpty(app1Instruments);

        var app2Key = new ApplicationKey("app2", InstanceId: null);
        var app2Instruments = repository.GetInstrumentsSummaries(app2Key);

        Assert.IsEmpty(app2Instruments);
    }

    [TestMethod]
    public void RemoveMetrics_SelectedResource()
    {
        // Arrange
        var repository = CreateRepository();

        var addContext = new AddContext();
        repository.AddMetrics(addContext, new RepeatedField<ResourceMetrics>()
        {
            new ResourceMetrics
            {
                Resource = CreateResource(name: "app1", instanceId: "123"),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test1", value: 1, startTime: s_testTime.AddMinutes(1)),
                            CreateSumMetric(metricName: "test1", value: 2, startTime: s_testTime.AddMinutes(1))
                        }
                    }
                }
            },
            new ResourceMetrics
            {
                Resource = CreateResource(name: "app1", instanceId: "456"),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test1", value: 3, startTime: s_testTime.AddMinutes(1)),
                            CreateSumMetric(metricName: "test2", value: 4, startTime: s_testTime.AddMinutes(1))
                        }
                    }
                }
            },
            new ResourceMetrics
            {
                Resource = CreateResource(name: "app2"),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test1", value: 5, startTime: s_testTime.AddMinutes(1)),
                            CreateSumMetric(metricName: "test3", value: 6, startTime: s_testTime.AddMinutes(1))
                        }
                    }
                }
            }
        });

        // Act
        repository.ClearMetrics(new ApplicationKey("app1", "456"));

        // Assert
        Assert.AreEqual(0, addContext.FailureCount);

        var app1Key = new ApplicationKey("app1", InstanceId: null);
        var app1Instruments = repository.GetInstrumentsSummaries(app1Key);

        var app1Instrument = Assert.ContainsSingle(app1Instruments);
        Assert.AreEqual("test1", app1Instrument.Name);
        Assert.AreEqual("Test metric description", app1Instrument.Description);
        Assert.AreEqual("widget", app1Instrument.Unit);
        Assert.AreEqual("test-meter", app1Instrument.Parent.MeterName);

        var app1Test1Instrument = repository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationKey = app1Key,
            InstrumentName = "test1",
            MeterName = "test-meter",
            StartTime = s_testTime,
            EndTime = s_testTime.AddMinutes(20)
        });

        Assert.IsNotNull(app1Test1Instrument);
        Assert.AreEqual("test1", app1Test1Instrument.Summary.Name);

        var app1Test1Dimensions = Assert.ContainsSingle(app1Test1Instrument.Dimensions);
        Assert.That.Collection(app1Test1Dimensions.Values,
            v =>
            {
                Assert.AreEqual(1, ((MetricValue<long>)v).Value);
            },
            v =>
            {
                Assert.AreEqual(2, ((MetricValue<long>)v).Value);
            });

        var app1Test2Instrument = repository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationKey = app1Key,
            InstrumentName = "test2",
            MeterName = "test-meter",
            StartTime = s_testTime,
            EndTime = s_testTime.AddMinutes(20)
        });

        Assert.IsNull(app1Test2Instrument);

        var app2Key = new ApplicationKey("app2", InstanceId: null);
        var app2Instruments = repository.GetInstrumentsSummaries(app2Key);

        Assert.That.Collection(app2Instruments,
            instrument =>
            {
                Assert.AreEqual("test1", instrument.Name);
                Assert.AreEqual("Test metric description", instrument.Description);
                Assert.AreEqual("widget", instrument.Unit);
                Assert.AreEqual("test-meter", instrument.Parent.MeterName);
            },
            instrument =>
            {
                Assert.AreEqual("test3", instrument.Name);
                Assert.AreEqual("Test metric description", instrument.Description);
                Assert.AreEqual("widget", instrument.Unit);
                Assert.AreEqual("test-meter", instrument.Parent.MeterName);
            });

        var app2Test1Instrument = repository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationKey = app2Key,
            InstrumentName = "test1",
            MeterName = "test-meter",
            StartTime = s_testTime,
            EndTime = s_testTime.AddMinutes(20)
        });

        Assert.IsNotNull(app2Test1Instrument);
        Assert.AreEqual("test1", app2Test1Instrument.Summary.Name);

        var app2Test1Dimensions = Assert.ContainsSingle(app2Test1Instrument.Dimensions);
        Assert.AreEqual(5, ((MetricValue<long>)app2Test1Dimensions.Values.Single()).Value);

        var app2Test3Instrument = repository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationKey = app2Key,
            InstrumentName = "test3",
            MeterName = "test-meter",
            StartTime = s_testTime,
            EndTime = s_testTime.AddMinutes(20)
        });

        Assert.IsNotNull(app2Test3Instrument);
        Assert.AreEqual("test3", app2Test3Instrument.Summary.Name);

        var app2Test3Dimensions = Assert.ContainsSingle(app2Test3Instrument.Dimensions);
        Assert.AreEqual(6, ((MetricValue<long>)app2Test3Dimensions.Values.Single()).Value);
    }

    [TestMethod]
    public void RemoveMetrics_MultipleSelectedResources()
    {
        // Arrange
        var repository = CreateRepository();

        var addContext = new AddContext();
        repository.AddMetrics(addContext, new RepeatedField<ResourceMetrics>()
        {
            new ResourceMetrics
            {
                Resource = CreateResource(name: "app1", instanceId: "123"),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test1", value: 1, startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("key-1", "value-1")]),
                            CreateSumMetric(metricName: "test1", value: 2, startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("key-1", "value-2")]),
                        }
                    }
                }
            },
            new ResourceMetrics
            {
                Resource = CreateResource(name: "app1", instanceId: "456"),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test1", value: 3, startTime: s_testTime.AddMinutes(1)),
                            CreateSumMetric(metricName: "test2", value: 4, startTime: s_testTime.AddMinutes(1))
                        }
                    }
                }
            },
            new ResourceMetrics
            {
                Resource = CreateResource(name: "app2"),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test1", value: 5, startTime: s_testTime.AddMinutes(1)),
                            CreateSumMetric(metricName: "test3", value: 6, startTime: s_testTime.AddMinutes(1))
                        }
                    }
                }
            }
        });

        // Act
        repository.ClearMetrics(new ApplicationKey("app1", null));

        // Assert
        Assert.AreEqual(0, addContext.FailureCount);

        var app1Key = new ApplicationKey("app1", InstanceId: null);
        var app1Instruments = repository.GetInstrumentsSummaries(app1Key);
        Assert.IsEmpty(app1Instruments);

        var app1Test1Instrument = repository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationKey = app1Key,
            InstrumentName = "test1",
            MeterName = "test-meter",
            StartTime = s_testTime,
            EndTime = s_testTime.AddMinutes(20)
        });

        Assert.IsNull(app1Test1Instrument);

        var app1Test2Instrument = repository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationKey = app1Key,
            InstrumentName = "test2",
            MeterName = "test-meter",
            StartTime = s_testTime,
            EndTime = s_testTime.AddMinutes(20)
        });

        Assert.IsNull(app1Test2Instrument);

        var app2Key = new ApplicationKey("app2", InstanceId: null);
        var app2Instruments = repository.GetInstrumentsSummaries(app2Key);
        Assert.That.Collection(app2Instruments,
            instrument =>
            {
                Assert.AreEqual("test1", instrument.Name);
                Assert.AreEqual("Test metric description", instrument.Description);
                Assert.AreEqual("widget", instrument.Unit);
                Assert.AreEqual("test-meter", instrument.Parent.MeterName);
            },
            instrument =>
            {
                Assert.AreEqual("test3", instrument.Name);
                Assert.AreEqual("Test metric description", instrument.Description);
                Assert.AreEqual("widget", instrument.Unit);
                Assert.AreEqual("test-meter", instrument.Parent.MeterName);
            });

        var app2Test1Instrument = repository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationKey = app2Key,
            InstrumentName = "test1",
            MeterName = "test-meter",
            StartTime = s_testTime,
            EndTime = s_testTime.AddMinutes(20)
        });

        Assert.IsNotNull(app2Test1Instrument);
        Assert.AreEqual("test1", app2Test1Instrument.Summary.Name);

        var app2Test1Dimensions = Assert.ContainsSingle(app2Test1Instrument.Dimensions);
        Assert.AreEqual(5, ((MetricValue<long>)app2Test1Dimensions.Values.Single()).Value);

        var app2Test3Instrument = repository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationKey = app2Key,
            InstrumentName = "test3",
            MeterName = "test-meter",
            StartTime = s_testTime,
            EndTime = s_testTime.AddMinutes(20)
        });

        Assert.IsNotNull(app2Test3Instrument);
        Assert.AreEqual("test3", app2Test3Instrument.Summary.Name);

        var app2Test3Dimensions = Assert.ContainsSingle(app2Test3Instrument.Dimensions);
        Assert.AreEqual(6, ((MetricValue<long>)app2Test3Dimensions.Values.Single()).Value);
    }

    [TestMethod]
    public void AddMetrics_InvalidInstrument()
    {
        // Arrange
        var repository = CreateRepository();

        var addContext = new AddContext();

        // Act
        repository.AddMetrics(addContext, new RepeatedField<ResourceMetrics>()
        {
            new ResourceMetrics
            {
                Resource = CreateResource(name: "app1", instanceId: "123"),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "", value: 1, startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("key-1", "value-1")]),
                            CreateSumMetric(metricName: "test1", value: 2, startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("key-1", "value-2")]),
                        }
                    }
                }
            }
        });

        // Assert
        Assert.AreEqual(1, addContext.FailureCount);

        var app1Key = new ApplicationKey("app1", InstanceId: null);
        var app1Instruments = repository.GetInstrumentsSummaries(app1Key);
        Assert.That.Collection(app1Instruments,
            instrument =>
            {
                Assert.AreEqual("test1", instrument.Name);
                Assert.AreEqual("Test metric description", instrument.Description);
                Assert.AreEqual("widget", instrument.Unit);
                Assert.AreEqual("test-meter", instrument.Parent.MeterName);
            });
    }

    [TestMethod]
    public void AddMetrics_InvalidHistogramDataPoints()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var addContext = new AddContext();

        var histogramMetric = new Metric
        {
            Name = "test",
            Description = "Test metric description",
            Unit = "widget",
            Histogram = new Histogram
            {
                AggregationTemporality = AggregationTemporality.Cumulative,
                DataPoints =
                {
                    new HistogramDataPoint
                    {
                        Count = 6,
                        Sum = 1,
                        ExplicitBounds = { },
                        BucketCounts = { 1 },
                        TimeUnixNano = DateTimeToUnixNanoseconds(s_testTime.AddMinutes(1))
                    },
                    new HistogramDataPoint
                    {
                        Count = 6,
                        Sum = 1,
                        ExplicitBounds = { },
                        BucketCounts = { 1 },
                        TimeUnixNano = DateTimeToUnixNanoseconds(s_testTime.AddMinutes(2))
                    },
                    new HistogramDataPoint
                    {
                        Count = 6,
                        Sum = 1,
                        ExplicitBounds = { 1, 2, 3 },
                        BucketCounts = { 1, 2, 3 },
                        TimeUnixNano = DateTimeToUnixNanoseconds(s_testTime.AddMinutes(3))
                    }
                }
            }
        };

        repository.AddMetrics(addContext, new RepeatedField<ResourceMetrics>()
        {
            new ResourceMetrics
            {
                Resource = CreateResource(),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics = { histogramMetric }
                    }
                }
            }
        });

        // Assert
        Assert.AreEqual(2, addContext.FailureCount);

        var applications = Assert.ContainsSingle(repository.GetApplications());

        var instrument = repository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationKey = applications.ApplicationKey,
            MeterName = "test-meter",
            InstrumentName = "test",
            StartTime = DateTime.MinValue,
            EndTime = DateTime.MaxValue
        });

        Assert.IsNotNull(instrument);
        Assert.AreEqual("test", instrument.Summary.Name);
        Assert.AreEqual("Test metric description", instrument.Summary.Description);
        Assert.AreEqual("widget", instrument.Summary.Unit);
        Assert.AreEqual("test-meter", instrument.Summary.Parent.MeterName);

        var dimension = Assert.ContainsSingle(instrument.Dimensions);
        Assert.ContainsSingle(dimension.Values);
    }

    [TestMethod]
    public void AddMetrics_OverflowDimension()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var addContext = new AddContext();
        repository.AddMetrics(addContext, new RepeatedField<ResourceMetrics>()
        {
            new ResourceMetrics
            {
                Resource = CreateResource(),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("otel.metric.overflow", "true")])
                        }
                    },
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter2"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(1))
                        }
                    }
                }
            }
        });

        // Assert
        Assert.AreEqual(0, addContext.FailureCount);

        var instrument1 = repository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationKey = new ApplicationKey("TestService", "TestId"),
            InstrumentName = "test",
            MeterName = "test-meter",
            StartTime = DateTime.MinValue,
            EndTime = DateTime.MaxValue
        });

        Assert.IsNotNull(instrument1);
        Assert.IsTrue(instrument1.HasOverflow);

        var instrument2 = repository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationKey = new ApplicationKey("TestService", "TestId"),
            InstrumentName = "test",
            MeterName = "test-meter2",
            StartTime = DateTime.MinValue,
            EndTime = DateTime.MaxValue
        });

        Assert.IsNotNull(instrument2);
        Assert.IsFalse(instrument2.HasOverflow);
    }

    private static void AssertDimensionValues(Dictionary<ReadOnlyMemory<KeyValuePair<string, string>>, DimensionScope> dimensions, ReadOnlyMemory<KeyValuePair<string, string>> key, int valueCount)
    {
        var scope = dimensions[key];
        Assert.IsTrue(Enumerable.SequenceEqual(MemoryMarshal.ToEnumerable(key), scope.Attributes), "Key and attributes don't match.");

        Assert.AreEqual(valueCount, scope.Values.Count);
    }
}
