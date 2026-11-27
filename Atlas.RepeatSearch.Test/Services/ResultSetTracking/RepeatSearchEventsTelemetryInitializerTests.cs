using Atlas.RepeatSearch.Services.ResultSetTracking;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using NUnit.Framework;

namespace Atlas.RepeatSearch.Test.Services.ResultSetTracking;

[TestFixture]
public class RepeatSearchEventsTelemetryInitializerTests
{
    private RepeatSearchEventsTelemetryInitializer initializer;

    [SetUp]
    public void SetUp()
    {
        initializer = new RepeatSearchEventsTelemetryInitializer();
    }

    [TestCase(OriginalSearchResultSetTracker.StoredEventName)]
    [TestCase(ExpiredResultSetCleaner.DeletedEventName)]
    public void Initialize_RepeatSearchEvent_SetsSamplingPercentageTo100(string eventName)
    {
        var telemetry = new EventTelemetry(eventName);

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
