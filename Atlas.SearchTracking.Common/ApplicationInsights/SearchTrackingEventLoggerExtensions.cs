using System.Reflection;
using Atlas.Common.ApplicationInsights;
using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Common.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Atlas.SearchTracking.Common.ApplicationInsights;

public static class SearchTrackingEventLoggerExtensions
{
    private static readonly JsonSerializerSettings WithoutPatientInformation = new()
    {
        ContractResolver = new PatientInformationIgnoringContractResolver()
    };

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
    /// <item>
    /// A field that can hold patient information has no property. Such a field is marked with <see cref="PatientInformationAttribute"/>.
    /// The Service Bus message keeps it.
    /// </item>
    /// <item><see cref="AtlasLogger"/> also adds a <c>LogLevel</c> property, with the value <c>Info</c>.</item>
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
        using var messageBody = new JsonTextReader(new StringReader(JsonConvert.SerializeObject(searchTrackingEvent, WithoutPatientInformation)));
        // Keeps the dates as the text that the message has.
        messageBody.DateParseHandling = DateParseHandling.None;

        return JObject.Load(messageBody).Properties()
            .Where(field => field.Value.Type != JTokenType.Null)
            .ToDictionary(
                field => field.Name,
                field => field.Value.Type == JTokenType.String ? field.Value.Value<string>()! : field.Value.ToString(Formatting.None));
    }

    /// <summary>
    /// Leaves out the fields that are marked with <see cref="PatientInformationAttribute"/>. <c>[JsonIgnore]</c> cannot do this, because it
    /// would also take these fields out of the Service Bus message.
    /// </summary>
    private sealed class PatientInformationIgnoringContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            if (member.IsDefined(typeof(PatientInformationAttribute)))
            {
                property.Ignored = true;
            }

            return property;
        }
    }
}
