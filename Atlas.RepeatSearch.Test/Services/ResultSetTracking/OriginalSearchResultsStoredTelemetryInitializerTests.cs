using Atlas.RepeatSearch.Services.ResultSetTracking;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using NUnit.Framework;

namespace Atlas.RepeatSearch.Test.Services.ResultSetTracking;

[TestFixture]
public class OriginalSearchResultsStoredTelemetryInitializerTests
{
    private OriginalSearchResultsStoredTelemetryInitializer initializer;

    [SetUp]
    public void SetUp()
    {
        initializer = new OriginalSearchResultsStoredTelemetryInitializer();
    }

    [Test]
    public void Initialize_StoredEvent_SetsSamplingPercentageTo100()
    {
        var telemetry = new EventTelemetry(OriginalSearchResultSetTracker.StoredEventName);

        initializer.Initialize(telemetry);

        ((ISupportSampling)telemetry).SamplingPercentage.Should().Be(100);
    }

    [Test]
    public void Initialize_OtherEvent_LeavesSamplingPercentageUnset()
    {
        var telemetry = new EventTelemetry(new Fixture().Create<string>());

        initializer.Initialize(telemetry);

        ((ISupportSampling)telemetry).SamplingPercentage.Should().BeNull();
    }

    [Test]
    public void Initialize_TraceWithStoredEventName_LeavesSamplingPercentageUnset()
    {
        var telemetry = new TraceTelemetry(OriginalSearchResultSetTracker.StoredEventName);

        initializer.Initialize(telemetry);

        ((ISupportSampling)telemetry).SamplingPercentage.Should().BeNull();
    }
}
