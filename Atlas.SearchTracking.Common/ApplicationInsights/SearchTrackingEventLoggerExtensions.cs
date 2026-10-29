using Atlas.Common.ApplicationInsights;
using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Common.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Atlas.SearchTracking.Common.ApplicationInsights;

public static class SearchTrackingEventLoggerExtensions
{
    /// <summary>
    /// Sends a search tracking event to Application Insights as a custom event. All search tracking events have this format:
    /// <list type="bullet">
    /// <item>The event name is <paramref name="eventType"/>, the value of the <c>EventType</c> property of the Service Bus message.</item>
    /// <item>
    /// Each top-level field of <paramref name="searchTrackingEvent"/> is a property with the name of the field. Its value is the value
    /// in the Service Bus message body: a string without quotes (so identifiers and dates are as the message has them), <c>true</c> or
    /// <c>false</c>, a number, or an array or object as JSON.
    /// </item>
    /// <item>A field without a value has no property: Application Insights does not keep empty values.</item>
    /// </list>
    /// <see cref="SearchTrackingEventTelemetryInitializer"/> keeps these events out of sampling.
    /// </summary>
    public static void SendSearchTrackingEvent<TEvent>(this IAtlasLogger logger, TEvent searchTrackingEvent, SearchTrackingEventType eventType)
        where TEvent : ISearchTrackingEvent
    {
        logger.SendEvent(eventType.ToString(), LogLevel.Info, ToProperties(searchTrackingEvent));
    }

    private static Dictionary<string, string> ToProperties<TEvent>(TEvent searchTrackingEvent) where TEvent : ISearchTrackingEvent
    {
        using var messageBody = new JsonTextReader(new StringReader(JsonConvert.SerializeObject(searchTrackingEvent)))
        {
            // Keeps the dates as the text that the message has.
            DateParseHandling = DateParseHandling.None
        };

        return JObject.Load(messageBody).Properties()
            .Where(field => field.Value.Type != JTokenType.Null)
            .ToDictionary(
                field => field.Name,
                field => field.Value.Type == JTokenType.String ? field.Value.Value<string>()! : field.Value.ToString(Formatting.None));
    }
}
