// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Resize;
using Aspire.Dashboard.Components.Tests.Shared;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Model.MetricValues;
using Bunit;
using Microsoft.Extensions.Logging.Abstractions;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Metrics.V1;

namespace Aspire.Dashboard.Components.Tests.Controls;

[UseCulture("en-US")]
[TestClass]
public class PlotlyChartTests : Bunit.TestContext
{
    private static string GetContainerHtml(string divId) => $"""<div id="{divId}" class="plotly-chart-container"></div>""";

    [TestMethod]
    public void Render_NoInstrument_NoPlotlyInvocations()
    {
        // Arrange
        MetricsSetupHelpers.SetupPlotlyChart(this);

        var model = new InstrumentViewModel();

        // Act
        var cut = RenderComponent<PlotlyChart>(builder =>
        {
            builder.Add(p => p.InstrumentViewModel, model);
            builder.Add(p => p.ViewportInformation, new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false));
        });

        // Assert
        cut.MarkupMatches(GetContainerHtml(cut.Instance.ChartDivId));

        Assert.That.Collection(JSInterop.Invocations,
            i =>
            {
                Assert.AreEqual("import", i.Identifier);
                Assert.AreEqual("/js/app-metrics.js", i.Arguments[0]);
            });
    }

    [TestMethod]
    public async Task Render_HasInstrument_InitializeChartInvocation()
    {
        // Arrange
        MetricsSetupHelpers.SetupPlotlyChart(this);

        var options = new TelemetryLimitOptions();
        var logger = NullLogger.Instance;
        var context = new OtlpContext { Options = options, Logger = logger };
        var instrument = new OtlpInstrument
        {
            Summary = new OtlpInstrumentSummary
            {
                Name = "Name-<b>Bold</b>",
                Unit = "Unit-<b>Bold</b>",
                Description = "Description-<b>Bold</b>",
                Parent = new OtlpMeter(new InstrumentationScope
                {
                    Name = "Parent-Name-<b>Bold</b>"
                }, context),
                Type = OtlpInstrumentType.Sum
            },
            Context = context
        };

        var model = new InstrumentViewModel();
        var dimension = new DimensionScope(capacity: 100, []);
        dimension.AddPointValue(new NumberDataPoint
        {
            AsInt = 1,
            StartTimeUnixNano = 0,
            TimeUnixNano = long.MaxValue
        }, context);

        await model.UpdateDataAsync(instrument.Summary, [dimension]);

        // Act
        var cut = RenderComponent<PlotlyChart>(builder =>
        {
            builder.Add(p => p.InstrumentViewModel, model);
            builder.Add(p => p.Duration, TimeSpan.FromSeconds(1));
            builder.Add(p => p.ViewportInformation, new ViewportInformation(IsDesktop: true, IsUltraLowHeight: false, IsUltraLowWidth: false));
        });

        // Assert
        cut.MarkupMatches(GetContainerHtml(cut.Instance.ChartDivId));

        Assert.That.Collection(JSInterop.Invocations,
            i =>
            {
                Assert.AreEqual("import", i.Identifier);
                Assert.AreEqual("/js/app-metrics.js", i.Arguments[0]);
            },
            i =>
            {
                Assert.AreEqual("initializeChart", i.Identifier);
                Assert.AreEqual(cut.Instance.ChartDivId, i.Arguments[0]);
                Assert.That.Collection((IEnumerable<PlotlyTrace>)i.Arguments[1]!, trace =>
                {
                    Assert.AreEqual("Unit-&lt;b&gt;Bold&lt;/b&gt;", trace.Name);
                    Assert.AreEqual("<b>Name-&lt;b&gt;Bold&lt;/b&gt;</b><br />Unit-&lt;b&gt;Bold&lt;/b&gt;: 1<br />Time: 12:59:57 AM", trace.Tooltips[0], ignoreWhiteSpaceDifferences: true);
                });
            });
    }
}
