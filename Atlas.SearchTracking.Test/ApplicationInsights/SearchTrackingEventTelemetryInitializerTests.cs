using Atlas.SearchTracking.Common.ApplicationInsights;
using Atlas.SearchTracking.Common.Enums;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using NUnit.Framework;

namespace Atlas.SearchTracking.Test.ApplicationInsights;

[TestFixture]
public class SearchTrackingEventTelemetryInitializerTests
{
    private Fixture fixture = null!;
    private SearchTrackingEventTelemetryInitializer initializer = null!;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        initializer = new SearchTrackingEventTelemetryInitializer();
    }

    private static IEnumerable<SearchTrackingEventType> EventTypes() => Enum.GetValues<SearchTrackingEventType>();

    [TestCaseSource(nameof(EventTypes))]
    public void Initialize_SearchTrackingEvent_SetsSamplingPercentageTo100(SearchTrackingEventType eventType)
    {
        var telemetry = new EventTelemetry(eventType.ToString());

        initializer.Initialize(telemetry);

        ((ISupportSampling)telemetry).SamplingPercentage.Should().Be(100);
    }

    [Test]
    public void Initialize_OtherEvent_LeavesSamplingPercentageUnset()
    {
        var telemetry = new EventTelemetry(fixture.Create<string>());

        initializer.Initialize(telemetry);

        ((ISupportSampling)telemetry).SamplingPercentage.Should().BeNull();
    }

    [Test]
    public void Initialize_TraceWithSearchTrackingEventName_LeavesSamplingPercentageUnset()
    {
        var telemetry = new TraceTelemetry(fixture.Create<SearchTrackingEventType>().ToString());

        initializer.Initialize(telemetry);

        ((ISupportSampling)telemetry).SamplingPercentage.Should().BeNull();
    }

    /// <summary>
    /// The initializer works only because the samplers of the SDK skip an item whose sampling percentage is set already. This checks
    /// that with the version of the SDK that Atlas uses.
    /// </summary>
    [Test]
    public void TrackEvent_WithOnePercentSampling_SendsEverySearchTrackingEvent()
    {
        const int eventCount = 100;
        var searchTrackingEventName = fixture.Create<SearchTrackingEventType>().ToString();
        var otherEventName = fixture.Create<string>();
        var sentEvents = new List<EventTelemetry>();

        using var configuration = new TelemetryConfiguration();
        configuration.ConnectionString = $"InstrumentationKey={fixture.Create<Guid>()}";
        configuration.TelemetryChannel = new CollectingChannel(sentEvents);
        configuration.TelemetryInitializers.Add(initializer);
        configuration.DefaultTelemetrySink.TelemetryProcessorChainBuilder.UseSampling(1).Build();
        var client = new TelemetryClient(configuration);

        for (var i = 0; i < eventCount; i++)
        {
            client.TrackEvent(searchTrackingEventName);
            client.TrackEvent(otherEventName);
        }

        sentEvents.Count(e => e.Name == searchTrackingEventName).Should().Be(eventCount);
        sentEvents.Count(e => e.Name == otherEventName).Should().BeLessThan(eventCount, "the other events are sampled");
    }

    private sealed class CollectingChannel(List<EventTelemetry> sentEvents) : ITelemetryChannel
    {
        public bool? DeveloperMode { get; set; }
        public string EndpointAddress { get; set; } = string.Empty;

        public void Send(ITelemetry item) => sentEvents.Add((EventTelemetry)item);

        public void Flush()
        {
        }

        public void Dispose()
        {
        }
    }
}
