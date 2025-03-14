// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Trace.V1;
using static Aspire.Tests.Shared.Telemetry.TelemetryTestHelpers;

namespace Aspire.Dashboard.Tests.TelemetryRepositoryTests;

[TestClass]
public class TraceTests
{
    private static readonly DateTime s_testTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    [DataRow(OtlpSpanKind.Server, Span.Types.SpanKind.Server)]
    [DataRow(OtlpSpanKind.Client, Span.Types.SpanKind.Client)]
    [DataRow(OtlpSpanKind.Consumer, Span.Types.SpanKind.Consumer)]
    [DataRow(OtlpSpanKind.Producer, Span.Types.SpanKind.Producer)]
    [DataRow(OtlpSpanKind.Internal, Span.Types.SpanKind.Internal)]
    [DataRow(OtlpSpanKind.Internal, Span.Types.SpanKind.Unspecified)]
    [DataRow(OtlpSpanKind.Unspecified, (Span.Types.SpanKind)1000)]
    public void ConvertSpanKind(OtlpSpanKind expected, Span.Types.SpanKind value)
    {
        var result = TelemetryRepository.ConvertSpanKind(value);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void AddTraces()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var addContext = new AddContext();
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10)),
                            CreateSpan(traceId: "1", spanId: "1-2", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1")
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

        var traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = applications[0].ApplicationKey,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });
        Assert.That.Collection(traces.PagedResult.Items,
            trace =>
            {
                AssertId("1", trace.TraceId);
                AssertId("1-1", trace.FirstSpan.SpanId);
                AssertId("1-1", trace.RootSpan!.SpanId);
                Assert.AreEqual(2, trace.Spans.Count);
            });
    }

    [TestMethod]
    public void AddTraces_SelfParent_Reject()
    {
        // Arrange
        var testSink = new TestSink();
        var factory = LoggerFactory.Create(b => b.AddProvider(new TestLoggerProvider(testSink)));

        var repository = CreateRepository(loggerFactory: factory);

        // Act
        var addContext = new AddContext();
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1")
                        }
                    }
                }
            }
        });

        // Assert
        Assert.AreEqual(1, addContext.FailureCount);

        var applications = repository.GetApplications();
        Assert.That.Collection(applications,
            app =>
            {
                Assert.AreEqual("TestService", app.ApplicationName);
                Assert.AreEqual("TestId", app.InstanceId);
            });

        var traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = applications[0].ApplicationKey,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });
        Assert.IsEmpty(traces.PagedResult.Items);

        var write = Assert.ContainsSingle(testSink.Writes);
        Assert.AreEqual("Error adding span.", write.Message);
        Assert.AreEqual("Circular loop detected for span '312d31' with parent '312d31'.", write.Exception!.Message);
    }

    [TestMethod]
    public void AddTraces_MultipleSpansLoop_Reject()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var addContext = new AddContext();
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-3"),
                            CreateSpan(traceId: "1", spanId: "1-2", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1"),
                            CreateSpan(traceId: "1", spanId: "1-3", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-2")
                        }
                    }
                }
            }
        });

        // Assert
        Assert.AreEqual(1, addContext.FailureCount);

        var applications = repository.GetApplications();
        Assert.That.Collection(applications,
            app =>
            {
                Assert.AreEqual("TestService", app.ApplicationName);
                Assert.AreEqual("TestId", app.InstanceId);
            });

        var traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = applications[0].ApplicationKey,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });
        Assert.That.Collection(traces.PagedResult.Items,
            trace =>
            {
                Assert.AreEqual(2, trace.Spans.Count);
            });
    }

    [TestMethod]
    public void AddTraces_DuplicateTraceIds_Reject()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var addContext = new AddContext();
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10)),
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10)),
                            CreateSpan(traceId: "1", spanId: "1-2", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1"),
                        }
                    }
                }
            }
        });

        // Assert
        Assert.AreEqual(1, addContext.FailureCount);

        var applications = repository.GetApplications();
        Assert.That.Collection(applications,
            app =>
            {
                Assert.AreEqual("TestService", app.ApplicationName);
                Assert.AreEqual("TestId", app.InstanceId);
            });

        var traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = applications[0].ApplicationKey,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });
        Assert.That.Collection(traces.PagedResult.Items,
            trace =>
            {
                Assert.AreEqual(2, trace.Spans.Count);
            });
    }

    [TestMethod]
    public void AddTraces_Scope_Multiple()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var addContext = new AddContext();
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope("scope1"),
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10)),
                        }
                    }
                }
            }
        });
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope("scope2"),
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-2", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1")
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

        var traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = applications[0].ApplicationKey,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });
        Assert.That.Collection(traces.PagedResult.Items,
            trace =>
            {
                AssertId("1", trace.TraceId);
                AssertId("1-1", trace.FirstSpan.SpanId);
                AssertId("1-1", trace.RootSpan!.SpanId);
                Assert.AreEqual(2, trace.Spans.Count);

                Assert.That.Collection(trace.Spans,
                    span => Assert.AreEqual("scope1", span.Scope.ScopeName),
                    span => Assert.AreEqual("scope2", span.Scope.ScopeName));
            });
    }

    [TestMethod]
    public void AddTraces_Traces_MultipleOutOrOrder()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var addContext1 = new AddContext();
        repository.AddTraces(addContext1, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-2", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1")
                        }
                    }
                }
            }
        });
        Assert.AreEqual(0, addContext1.FailureCount);

        var addContext2 = new AddContext();
        repository.AddTraces(addContext2, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Spans =
                        {
                            CreateSpan(traceId: "2", spanId: "2-1", startTime: s_testTime.AddMinutes(3), endTime: s_testTime.AddMinutes(10))
                        }
                    }
                }
            }
        });
        Assert.AreEqual(0, addContext2.FailureCount);

        var applications = repository.GetApplications();
        Assert.That.Collection(applications,
            app =>
            {
                Assert.AreEqual("TestService", app.ApplicationName);
                Assert.AreEqual("TestId", app.InstanceId);
            });

        var traces1 = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = applications[0].ApplicationKey,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });
        Assert.That.Collection(traces1.PagedResult.Items,
            trace =>
            {
                AssertId("2", trace.TraceId);
                AssertId("2-1", trace.FirstSpan.SpanId);
                AssertId("2-1", trace.RootSpan!.SpanId);
            },
            trace =>
            {
                AssertId("1", trace.TraceId);
                AssertId("1-2", trace.FirstSpan.SpanId);
                Assert.IsNull(trace.RootSpan);
            });

        var addContext3 = new AddContext();
        repository.AddTraces(addContext3, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10))
                        }
                    }
                }
            }
        });
        Assert.AreEqual(0, addContext3.FailureCount);

        var traces2 = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = applications[0].ApplicationKey,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });
        Assert.That.Collection(traces2.PagedResult.Items,
            trace =>
            {
                AssertId("1", trace.TraceId);
                AssertId("1-1", trace.FirstSpan.SpanId);
                Assert.AreEqual("", trace.FirstSpan.Scope.ScopeName);
                AssertId("1-1", trace.RootSpan!.SpanId);
            },
            trace =>
            {
                AssertId("2", trace.TraceId);
                AssertId("2-1", trace.FirstSpan.SpanId);
                Assert.AreEqual("", trace.FirstSpan.Scope.ScopeName);
                AssertId("2-1", trace.RootSpan!.SpanId);
            });
    }

    [TestMethod]
    public void AddTraces_Spans_MultipleOutOrOrder()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        repository.AddTraces(new AddContext(), new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-2", startTime: s_testTime.AddMinutes(2), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1"),
                            CreateSpan(traceId: "1", spanId: "1-5", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1"),
                            CreateSpan(traceId: "1", spanId: "1-3", startTime: s_testTime.AddMinutes(3), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1"),
                            CreateSpan(traceId: "1", spanId: "1-4", startTime: s_testTime.AddMinutes(4), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1"),
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10))
                        }
                    }
                }
            }
        });

        var traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = null,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });
        Assert.That.Collection(traces.PagedResult.Items,
            trace =>
            {
                AssertId("1", trace.TraceId);
                AssertId("1-1", trace.FirstSpan.SpanId);
                AssertId("1-1", trace.RootSpan!.SpanId);
                Assert.That.Collection(trace.Spans,
                    s => AssertId("1-1", s.SpanId),
                    s => AssertId("1-2", s.SpanId),
                    s => AssertId("1-3", s.SpanId),
                    s => AssertId("1-4", s.SpanId),
                    s => AssertId("1-5", s.SpanId));
            });
    }

    [TestMethod]
    public void AddTraces_SpanEvents_ReturnData()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        repository.AddTraces(new AddContext(), new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10), events: new List<Span.Types.Event>
                            {
                                new Span.Types.Event
                                {
                                    Name = "Event 2",
                                    TimeUnixNano = 2,
                                    Attributes =
                                    {
                                        new KeyValue { Key = "key2", Value = new AnyValue { StringValue = "Value!" } }
                                    }
                                },
                                new Span.Types.Event
                                {
                                    Name = "Event 1",
                                    TimeUnixNano = 1,
                                    Attributes =
                                    {
                                        new KeyValue { Key = "key1", Value = new AnyValue { StringValue = "Value!" } }
                                    }
                                }
                            })
                        }
                    }
                }
            }
        });

        var traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = null,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });
        Assert.That.Collection(traces.PagedResult.Items,
            trace =>
            {
                AssertId("1", trace.TraceId);
                AssertId("1-1", trace.FirstSpan.SpanId);
                Assert.That.Collection(trace.FirstSpan.Events,
                    e =>
                    {
                        Assert.AreEqual("Event 1", e.Name);
                        Assert.That.Collection(e.Attributes,
                            a =>
                            {
                                Assert.AreEqual("key1", a.Key);
                                Assert.AreEqual("Value!", a.Value);
                            });
                    },
                    e =>
                    {
                        Assert.AreEqual("Event 2", e.Name);
                    });
            });
    }

    [TestMethod]
    public void AddTraces_SpanLinks_ReturnData()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        repository.AddTraces(new AddContext(), new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10), links: new List<Span.Types.Link>
                            {
                                new Span.Types.Link
                                {
                                    TraceId = ByteString.CopyFrom(Encoding.UTF8.GetBytes("1")),
                                    SpanId = ByteString.CopyFrom(Encoding.UTF8.GetBytes("1-1")),
                                    Attributes =
                                    {
                                        new KeyValue { Key = "key2", Value = new AnyValue { StringValue = "Value!" } }
                                    }
                                },
                                new Span.Types.Link
                                {
                                    TraceId = ByteString.CopyFrom(Encoding.UTF8.GetBytes("2")),
                                    SpanId = ByteString.CopyFrom(Encoding.UTF8.GetBytes("2-1")),
                                    Attributes =
                                    {
                                        new KeyValue { Key = "key1", Value = new AnyValue { StringValue = "Value!" } }
                                    }
                                }
                            })
                        }
                    }
                }
            }
        });

        var traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = null,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });
        Assert.That.Collection(traces.PagedResult.Items,
            trace =>
            {
                AssertId("1", trace.TraceId);
                AssertId("1-1", trace.FirstSpan.SpanId);
                Assert.That.Collection(trace.FirstSpan.Links,
                    l =>
                    {
                        AssertId("1", l.TraceId);
                        AssertId("1-1", l.SpanId);
                        Assert.That.Collection(l.Attributes,
                            a =>
                            {
                                Assert.AreEqual("key2", a.Key);
                                Assert.AreEqual("Value!", a.Value);
                            });
                    },
                    l =>
                    {
                        AssertId("2", l.TraceId);
                        AssertId("2-1", l.SpanId);
                        Assert.That.Collection(l.Attributes,
                            a =>
                            {
                                Assert.AreEqual("key1", a.Key);
                                Assert.AreEqual("Value!", a.Value);
                            });
                    });
            });

        Assert.That.Collection(repository.SpanLinks,
            l =>
            {
                AssertId("1", l.TraceId);
                AssertId("1-1", l.SpanId);
                Assert.That.Collection(l.Attributes,
                    a =>
                    {
                        Assert.AreEqual("key2", a.Key);
                        Assert.AreEqual("Value!", a.Value);
                    });
            },
            l =>
            {
                AssertId("2", l.TraceId);
                AssertId("2-1", l.SpanId);
                Assert.That.Collection(l.Attributes,
                    a =>
                    {
                        Assert.AreEqual("key1", a.Key);
                        Assert.AreEqual("Value!", a.Value);
                    });
            });
    }

    [TestMethod]
    public void GetTraces_ReturnCopies()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var addContext1 = new AddContext();
        repository.AddTraces(addContext1, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10))
                        }
                    }
                }
            }
        });

        var traces1 = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = null,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });
        Assert.That.Collection(traces1.PagedResult.Items,
            trace =>
            {
                AssertId("1", trace.TraceId);
                AssertId("1-1", trace.FirstSpan.SpanId);
                AssertId("1-1", trace.RootSpan!.SpanId);
            });

        var traces2 = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = null,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });
        Assert.AreNotSame(traces1.PagedResult.Items[0], traces2.PagedResult.Items[0]);
        Assert.AreNotSame(traces1.PagedResult.Items[0].Spans[0].Trace, traces2.PagedResult.Items[0].Spans[0].Trace);

        var trace1 = repository.GetTrace(GetHexId("1"))!;
        var trace2 = repository.GetTrace(GetHexId("1"))!;
        Assert.AreNotSame(trace1, trace2);
        Assert.AreNotSame(trace1.Spans[0].Trace, trace2.Spans[0].Trace);
    }

    [TestMethod]
    public void AddTraces_AttributeAndEventLimits_LimitsApplied()
    {
        // Arrange
        var repository = CreateRepository(maxAttributeCount: 5, maxAttributeLength: 16, maxSpanEventCount: 5);

        var attributes = new List<KeyValuePair<string, string>>();
        for (var i = 0; i < 10; i++)
        {
            var value = GetValue((i + 1) * 5);
            attributes.Add(new KeyValuePair<string, string>($"Key{i}", value));
        }

        var events = new List<Span.Types.Event>();
        for (var i = 0; i < 10; i++)
        {
            events.Add(CreateSpanEvent($"Event {i}", i, attributes));
        }

        // Act
        var addContext = new AddContext();
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10), attributes: attributes, events: events)
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

        var traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = applications[0].ApplicationKey,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });

        var trace = Assert.ContainsSingle(traces.PagedResult.Items);

        AssertId("1", trace.TraceId);
        AssertId("1-1", trace.FirstSpan.SpanId);
        Assert.That.Collection(trace.FirstSpan.Attributes,
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

        Assert.AreEqual(5, trace.FirstSpan.Events.Count);
        Assert.AreEqual(5, trace.FirstSpan.Events[0].Attributes.Length);
    }

    [TestMethod]
    public void AddTraces_Links_BacklinksPopulated()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        AddTrace(repository, "1", s_testTime);
        var traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = null,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });

        // Assert
        var trace = Assert.ContainsSingle(traces.PagedResult.Items);

        Assert.That.Collection(trace.Spans,
            s =>
            {
                var link = Assert.ContainsSingle(s.Links);
                AssertId("1-2", link.SpanId);
                AssertId("1-1", link.SourceSpanId);

                var backLink = Assert.ContainsSingle(s.BackLinks);
                AssertId("1-1", backLink.SpanId);
                AssertId("1-2", backLink.SourceSpanId);
            },
            s =>
            {
                var link = Assert.ContainsSingle(s.Links);
                AssertId("1-1", link.SpanId);
                AssertId("1-2", link.SourceSpanId);

                var backLink = Assert.ContainsSingle(s.BackLinks);
                AssertId("1-2", backLink.SpanId);
                AssertId("1-1", backLink.SourceSpanId);
            });
    }

    [TestMethod]
    public void AddTraces_ExceedLimit_FirstInFirstOut()
    {
        // Arrange
        const int MaxTraceCount = 10;
        var repository = CreateRepository(maxTraceCount: MaxTraceCount);

        var testTime = s_testTime.AddDays(1);

        // Act
        for (var i = 0; i < 2000; i++)
        {
            var traceNumber = i + 1;
            var traceId = traceNumber.ToString(CultureInfo.InvariantCulture);

            // Insert traces out of order to stress the circular buffer type.
            var startTime = testTime.AddMinutes(i + (i % 2 == 0 ? -5 : 0));

            try
            {
                AddTrace(repository, traceId, startTime);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error adding trace number {i}.", ex);
            }
        }

        // Assert
        var applications = repository.GetApplications();
        Assert.That.Collection(applications,
            app =>
            {
                Assert.AreEqual("TestService", app.ApplicationName);
                Assert.AreEqual("TestId", app.InstanceId);
            });

        var traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = applications[0].ApplicationKey,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });

        // Most recent traces are returned.
        var first = GetStringId(traces.PagedResult.Items.First().TraceId);
        var last = GetStringId(traces.PagedResult.Items.Last().TraceId);
        Assert.AreEqual("1988", first);
        Assert.AreEqual("2000", last);

        // Traces returned are ordered by start time.
        var actualOrder = traces.PagedResult.Items.Select(t => t.TraceId).ToList();
        var expectedOrder = traces.PagedResult.Items.OrderBy(t => t.FirstSpan.StartTime).Select(t => t.TraceId).ToList();
        Assert.AreEqual(expectedOrder, actualOrder);

        Assert.AreEqual(MaxTraceCount * 2, repository.SpanLinks.Count);
    }

    private static void AddTrace(TelemetryRepository repository, string traceId, DateTime startTime)
    {
        var addContext = new AddContext();

        var link1 = new Span.Types.Link
        {
            TraceId = ByteString.CopyFrom(Encoding.UTF8.GetBytes(traceId)),
            SpanId = ByteString.CopyFrom(Encoding.UTF8.GetBytes($"{traceId}-2")),
            Attributes =
            {
                new KeyValue { Key = "key2", Value = new AnyValue { StringValue = "Value!" } }
            }
        };
        var link2 = new Span.Types.Link
        {
            TraceId = ByteString.CopyFrom(Encoding.UTF8.GetBytes(traceId)),
            SpanId = ByteString.CopyFrom(Encoding.UTF8.GetBytes($"{traceId}-1")),
            Attributes =
            {
                new KeyValue { Key = "key2", Value = new AnyValue { StringValue = "Value!" } }
            }
        };

        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: traceId, spanId: $"{traceId}-2", startTime: startTime.AddMinutes(5), endTime: startTime.AddMinutes(1), parentSpanId: $"{traceId}-1", links: new List<Span.Types.Link>
                            {
                                link2
                            }),
                            CreateSpan(traceId: traceId, spanId: $"{traceId}-1", startTime: startTime.AddMinutes(1), endTime: startTime.AddMinutes(10), links: new List<Span.Types.Link>
                            {
                                link1
                            })
                        }
                    }
                }
            }
        });

        Assert.AreEqual(0, addContext.FailureCount);
    }

    [TestMethod]
    public void AddTraces_MultipleRootSpans_RootSpanIsEarliestWithoutParent()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var addContext = new AddContext();
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10)),
                            CreateSpan(traceId: "1", spanId: "1-2", startTime: s_testTime.AddMinutes(3), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1"),
                            CreateSpan(traceId: "1", spanId: "1-3", startTime: s_testTime.AddMinutes(4), endTime: s_testTime.AddMinutes(10))
                        }
                    }
                }
            }
        });

        // Assert
        Assert.AreEqual(0, addContext.FailureCount);

        var traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = null,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });
        Assert.That.Collection(traces.PagedResult.Items,
            trace =>
            {
                AssertId("1", trace.TraceId);
                AssertId("1-2", trace.FirstSpan.SpanId); // First by time
                AssertId("1-3", trace.RootSpan!.SpanId); // First by time and without a parent
                Assert.AreEqual(3, trace.Spans.Count);
            });
    }

    [TestMethod]
    public void GetTraces_MultipleInstances()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var addContext = new AddContext();
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(name: "app1", instanceId: "123"),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans = { CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10), attributes: [KeyValuePair.Create("key-1", "value-1")]) }
                    }
                }
            },
            new ResourceSpans
            {
                Resource = CreateResource(name: "app1", instanceId: "456"),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans = { CreateSpan(traceId: "2", spanId: "2-1", startTime: s_testTime.AddMinutes(2), endTime: s_testTime.AddMinutes(10), attributes: [KeyValuePair.Create("key-2", "value-2")]) }
                    }
                }
            },
            new ResourceSpans
            {
                Resource = CreateResource(name: "app2"),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans = { CreateSpan(traceId: "3", spanId: "3-1", startTime: s_testTime.AddMinutes(3), endTime: s_testTime.AddMinutes(10)) }
                    }
                }
            }
        });

        // Assert
        Assert.AreEqual(0, addContext.FailureCount);

        var appKey = new ApplicationKey("app1", InstanceId: null);
        var traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = appKey,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });
        Assert.That.Collection(traces.PagedResult.Items,
            trace =>
            {
                AssertId("1", trace.TraceId);
            },
            trace =>
            {
                AssertId("2", trace.TraceId);
            });

        var propertyKeys = repository.GetTracePropertyKeys(appKey)!;
        Assert.That.Collection(propertyKeys,
            s => Assert.AreEqual("key-1", s),
            s => Assert.AreEqual("key-2", s));
    }

    [TestMethod]
    public void GetTraces_AttributeFilters()
    {
        // Arrange
        var repository = CreateRepository();

        var addContext = new AddContext();
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(name: "app1", instanceId: "123"),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans = { CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10), attributes: [KeyValuePair.Create("key1", "value1")]) }
                    }
                }
            },
            new ResourceSpans
            {
                Resource = CreateResource(name: "app1", instanceId: "456"),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans = { CreateSpan(traceId: "1", spanId: "1-2", startTime: s_testTime.AddMinutes(2), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1", attributes: [KeyValuePair.Create("key2", "value2")]) }
                    }
                }
            }
        });

        Assert.AreEqual(0, addContext.FailureCount);

        var appKey = new ApplicationKey("app1", InstanceId: null);

        // Act 1
        var traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = appKey,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10,
            Filters = [
                new TelemetryFilter { Field = "key1", Condition = FilterCondition.Equals, Value = "value1" }
            ]
        });
        // Assert 1
        // Match first span.
        Assert.That.Collection(traces.PagedResult.Items,
            trace =>
            {
                AssertId("1", trace.TraceId);
            });

        // Act 2
        traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = appKey,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10,
            Filters = [
                new TelemetryFilter { Field = "key2", Condition = FilterCondition.Equals, Value = "value2" }
            ]
        });
        // Assert 2
        // Match second span.
        Assert.That.Collection(traces.PagedResult.Items,
            trace =>
            {
                AssertId("1", trace.TraceId);
            });

        // Act 3
        traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = appKey,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10,
            Filters = [
                new TelemetryFilter { Field = "key1", Condition = FilterCondition.Equals, Value = "value1" },
                new TelemetryFilter { Field = "key2", Condition = FilterCondition.Equals, Value = "value2" }
            ]
        });
        // Assert 3
        // Match neither span.
        Assert.IsEmpty(traces.PagedResult.Items);
    }

    [TestMethod]
    [DataRow(KnownTraceFields.TraceIdField, "31")]
    [DataRow(KnownTraceFields.SpanIdField, "312d31")]
    [DataRow(KnownTraceFields.StatusField, "Unset")]
    [DataRow(KnownTraceFields.KindField, "Internal")]
    [DataRow(KnownResourceFields.ServiceNameField, "app1")]
    [DataRow(KnownSourceFields.NameField, "TestScope")]
    public void GetTraces_KnownFilters(string name, string value)
    {
        // Arrange
        var repository = CreateRepository();

        var addContext = new AddContext();
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(name: "app1", instanceId: "123"),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans = { CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10), attributes: [KeyValuePair.Create("key1", "value1")]) }
                    }
                }
            }
        });

        Assert.AreEqual(0, addContext.FailureCount);

        var appKey = new ApplicationKey("app1", InstanceId: null);

        // Act 1
        var traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = appKey,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10,
            Filters = [
                new TelemetryFilter { Field = name, Condition = FilterCondition.NotEqual, Value = value }
            ]
        });

        // Assert 1
        // Doesn't match filter.
        Assert.IsEmpty(traces.PagedResult.Items);

        // Act 2
        traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = appKey,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10,
            Filters = [
                new TelemetryFilter { Field = name, Condition = FilterCondition.Equals, Value = value }
            ]
        });

        // Assert 2
        // Matches filter.
        Assert.That.Collection(traces.PagedResult.Items,
            trace =>
            {
                AssertId("1", trace.TraceId);
            });
    }

    [TestMethod]
    public void AddTraces_OutOfOrder_FullName()
    {
        // Arrange
        var repository = CreateRepository();
        var request = new GetTracesRequest
        {
            ApplicationKey = new ApplicationKey("TestService", "TestId"),
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10,
            Filters = []
        };

        // Act 1
        var addContext = new AddContext();
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-3", startTime: s_testTime.AddMinutes(10), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1")
                        }
                    }
                }
            }
        });
        Assert.AreEqual(0, addContext.FailureCount);

        // Assert 1
        var trace = Assert.ContainsSingle(repository.GetTraces(request).PagedResult.Items);
        Assert.AreEqual("TestService: Test span. Id: 1-3", trace.FullName);

        // Act 2
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-2", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1")
                        }
                    }
                }
            }
        });
        Assert.AreEqual(0, addContext.FailureCount);

        // Assert 2
        trace = Assert.ContainsSingle(repository.GetTraces(request).PagedResult.Items);
        Assert.AreEqual("TestService: Test span. Id: 1-2", trace.FullName);

        // Act 3
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(10), endTime: s_testTime.AddMinutes(10))
                        }
                    }
                }
            }
        });
        Assert.AreEqual(0, addContext.FailureCount);

        // Assert 3
        trace = Assert.ContainsSingle(repository.GetTraces(request).PagedResult.Items);
        Assert.AreEqual("TestService: Test span. Id: 1-1", trace.FullName);

        // Act 4
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-4", startTime: s_testTime, endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1")
                        }
                    }
                }
            }
        });
        Assert.AreEqual(0, addContext.FailureCount);

        // Assert 4
        trace = Assert.ContainsSingle(repository.GetTraces(request).PagedResult.Items);
        Assert.AreEqual("TestService: Test span. Id: 1-1", trace.FullName);
    }

    [TestMethod]
    public void AddTraces_SameResourceDifferentProperties_MultipleResourceViews()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var addContext = new AddContext();
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource(attributes: [KeyValuePair.Create("prop1", "value1")]),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10))
                        }
                    }
                }
            },
            new ResourceSpans
            {
                Resource = CreateResource(attributes: [KeyValuePair.Create("prop2", "value1"), KeyValuePair.Create("prop1", "value2")]),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-2", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1")
                        }
                    }
                }
            },
            new ResourceSpans
            {
                Resource = CreateResource(attributes: [KeyValuePair.Create("prop1", "value2"), KeyValuePair.Create("prop2", "value1")]),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-3", startTime: s_testTime.AddMinutes(10), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1")
                        }
                    }
                }
            }
        });

        // Assert
        Assert.AreEqual(0, addContext.FailureCount);

        // Spans belong to the same application
        var application = Assert.ContainsSingle(repository.GetApplications());
        Assert.AreEqual("TestService", application.ApplicationName);
        Assert.AreEqual("TestId", application.InstanceId);

        // Spans have different views
        var views = application.GetViews().OrderBy(v => v.Properties.Length).ToList();
        Assert.That.Collection(views,
            v =>
            {
                Assert.That.Collection(v.Properties,
                    p =>
                    {
                        Assert.AreEqual("prop1", p.Key);
                        Assert.AreEqual("value1", p.Value);
                    });
            },
            v =>
            {
                Assert.That.Collection(v.Properties,
                    p =>
                    {
                        Assert.AreEqual("prop1", p.Key);
                        Assert.AreEqual("value2", p.Value);
                    },
                    p =>
                    {
                        Assert.AreEqual("prop2", p.Key);
                        Assert.AreEqual("value1", p.Value);
                    });
            });

        var traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = application.ApplicationKey,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });
        var trace = Assert.ContainsSingle(traces.PagedResult.Items);

        Assert.That.Collection(trace.Spans,
            s =>
            {
                AssertId("1-1", s.SpanId);
                Assert.That.Collection(s.Source.Properties,
                    p =>
                    {
                        Assert.AreEqual("prop1", p.Key);
                        Assert.AreEqual("value1", p.Value);
                    });
            },
            s =>
            {
                AssertId("1-2", s.SpanId);
                Assert.That.Collection(s.Source.Properties,
                    p =>
                    {
                        Assert.AreEqual("prop1", p.Key);
                        Assert.AreEqual("value2", p.Value);
                    },
                    p =>
                    {
                        Assert.AreEqual("prop2", p.Key);
                        Assert.AreEqual("value1", p.Value);
                    });
            },
            s =>
            {
                AssertId("1-3", s.SpanId);
                Assert.That.Collection(s.Source.Properties,
                    p =>
                    {
                        Assert.AreEqual("prop1", p.Key);
                        Assert.AreEqual("value2", p.Value);
                    },
                    p =>
                    {
                        Assert.AreEqual("prop2", p.Key);
                        Assert.AreEqual("value1", p.Value);
                    });
            });
    }

    [TestMethod]
    public void RemoveTraces_All()
    {
        // Arrange
        var repository = CreateRepository();

        var addContext = new AddContext();
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource("app1", "123"),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10)),
                            CreateSpan(traceId: "1", spanId: "1-2", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1")
                        }
                    }
                }
            },
            new ResourceSpans
            {
                Resource = CreateResource("app1", "456"),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "2", spanId: "2-1", startTime: s_testTime.AddMinutes(2), endTime: s_testTime.AddMinutes(10)),
                            CreateSpan(traceId: "2", spanId: "2-2", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10), parentSpanId: "2-1")
                        }
                    }
                }
            },
            new ResourceSpans
            {
                Resource = CreateResource("app2", "789"),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "3", spanId: "3-1", startTime: s_testTime.AddMinutes(3), endTime: s_testTime.AddMinutes(10)),
                            CreateSpan(traceId: "3", spanId: "3-2", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10), parentSpanId: "3-1")
                        }
                    }
                }
            }
        });

        // Act
        repository.ClearTraces();

        // Assert
        Assert.AreEqual(0, addContext.FailureCount);

        var traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = null,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });

        Assert.IsNotNull(traces?.PagedResult?.Items);
        Assert.IsEmpty(traces.PagedResult.Items);
        Assert.AreEqual(0, traces.PagedResult.TotalItemCount);
    }

    [TestMethod]
    public void RemoveTraces_SelectedResource()
    {
        // Arrange
        var repository = CreateRepository();

        var addContext = new AddContext();
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource("app1", "123"),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10)),
                            CreateSpan(traceId: "1", spanId: "1-2", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1")
                        }
                    }
                }
            },
            new ResourceSpans
            {
                Resource = CreateResource("app1", "456"),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "2", spanId: "2-1", startTime: s_testTime.AddMinutes(2), endTime: s_testTime.AddMinutes(10)),
                            CreateSpan(traceId: "2", spanId: "2-2", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10), parentSpanId: "2-1")
                        }
                    }
                }
            },
            new ResourceSpans
            {
                Resource = CreateResource("app2", "789"),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "3", spanId: "3-1", startTime: s_testTime.AddMinutes(3), endTime: s_testTime.AddMinutes(10)),
                            CreateSpan(traceId: "3", spanId: "3-2", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10), parentSpanId: "3-1")
                        }
                    }
                }
            }
        });

        // Act
        repository.ClearTraces(new ApplicationKey("app1", "123"));

        // Assert
        Assert.AreEqual(0, addContext.FailureCount);

        var traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = null,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });

        Assert.IsNotNull(traces?.PagedResult?.Items);
        Assert.AreEqual(2, traces.PagedResult.TotalItemCount);

        Assert.That.Collection(traces.PagedResult.Items,
            trace =>
            {
                AssertId("2", trace.TraceId);
                Assert.That.Collection(trace.Spans,
                    s =>
                    {
                        AssertId("2-1", s.SpanId);
                    },
                    s =>
                    {
                        AssertId("2-2", s.SpanId);
                    });
            },
            trace =>
            {
                AssertId("3", trace.TraceId);
                Assert.That.Collection(trace.Spans,
                    s =>
                    {
                        AssertId("3-1", s.SpanId);
                    },
                    s =>
                    {
                        AssertId("3-2", s.SpanId);
                    });
            });
    }

    [TestMethod]
    public void RemoveTraces_MultipleSelectedResources()
    {
        // Arrange
        var repository = CreateRepository();

        var addContext = new AddContext();
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource("app1", "123"),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10)),
                            CreateSpan(traceId: "1", spanId: "1-2", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1")
                        }
                    }
                }
            },
            new ResourceSpans
            {
                Resource = CreateResource("app1", "456"),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "2", spanId: "2-1", startTime: s_testTime.AddMinutes(2), endTime: s_testTime.AddMinutes(10)),
                            CreateSpan(traceId: "2", spanId: "2-2", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10), parentSpanId: "2-1")
                        }
                    }
                }
            },
            new ResourceSpans
            {
                Resource = CreateResource("app2", "789"),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "3", spanId: "3-1", startTime: s_testTime.AddMinutes(3), endTime: s_testTime.AddMinutes(10)),
                            CreateSpan(traceId: "3", spanId: "3-2", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10), parentSpanId: "3-1"),
                        }
                    },
                }
            }
        });

        // Act
        repository.ClearTraces(new ApplicationKey("app1", null));

        // Assert
        Assert.AreEqual(0, addContext.FailureCount);

        var traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = null,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });

        Assert.IsNotNull(traces?.PagedResult?.Items);
        var trace = Assert.ContainsSingle(traces.PagedResult.Items);

        AssertId("3", trace.TraceId);
        Assert.That.Collection(trace.Spans,
            s =>
            {
                AssertId("3-1", s.SpanId);
            },
            s =>
            {
                AssertId("3-2", s.SpanId);
            });
    }

    [TestMethod]
    public void RemoveTraces_SelectedResource_SpansFromDifferentTrace()
    {
        // Arrange
        var repository = CreateRepository();

        var addContext = new AddContext();
        repository.AddTraces(addContext, new RepeatedField<ResourceSpans>()
        {
            new ResourceSpans
            {
                Resource = CreateResource("app1", "123"),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "1", spanId: "1-1", startTime: s_testTime.AddMinutes(1), endTime: s_testTime.AddMinutes(10)),
                            CreateSpan(traceId: "1", spanId: "1-2", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-1")
                        }
                    }
                }
            },
            new ResourceSpans
            {
                Resource = CreateResource("app1", "456"),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "2", spanId: "2-2", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10), parentSpanId: "2-1")
                        }
                    }
                }
            },
            new ResourceSpans
            {
                Resource = CreateResource("app2", "789"),
                ScopeSpans =
                {
                    new ScopeSpans
                    {
                        Scope = CreateScope(),
                        Spans =
                        {
                            CreateSpan(traceId: "2", spanId: "2-1", startTime: s_testTime.AddMinutes(2), endTime: s_testTime.AddMinutes(10)),
                            CreateSpan(traceId: "3", spanId: "3-1", startTime: s_testTime.AddMinutes(3), endTime: s_testTime.AddMinutes(10)),
                            CreateSpan(traceId: "3", spanId: "3-2", startTime: s_testTime.AddMinutes(5), endTime: s_testTime.AddMinutes(10), parentSpanId: "3-1"),
                            // Spans on traces originating from other resources
                            CreateSpan(traceId: "1", spanId: "1-3", startTime: s_testTime.AddMinutes(6), endTime: s_testTime.AddMinutes(10), parentSpanId: "1-2"),
                            CreateSpan(traceId: "2", spanId: "2-3", startTime: s_testTime.AddMinutes(6), endTime: s_testTime.AddMinutes(10), parentSpanId: "2-2")
                        }
                    },
                }
            }
        });

        // Act
        repository.ClearTraces(new ApplicationKey("app1", null));

        // Assert
        Assert.AreEqual(0, addContext.FailureCount);

        var traces = repository.GetTraces(new GetTracesRequest
        {
            ApplicationKey = null,
            FilterText = string.Empty,
            StartIndex = 0,
            Count = 10,
            Filters = []
        });

        Assert.IsNotNull(traces?.PagedResult?.Items);
        var trace = Assert.ContainsSingle(traces.PagedResult.Items);

        AssertId("3", trace.TraceId);
        Assert.That.Collection(trace.Spans,
            s =>
            {
                AssertId("3-1", s.SpanId);
            },
            s =>
            {
                AssertId("3-2", s.SpanId);
            });
    }
}
