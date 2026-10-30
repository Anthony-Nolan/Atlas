namespace Atlas.SearchTracking.Common.Models;

/// <summary>
/// Marks a field of a search tracking event that can hold patient information, for example the patient's HLA typing.
/// <see cref="ApplicationInsights.SearchTrackingEventLoggerExtensions.SendSearchTrackingEvent{TEvent}"/> does not send this field to
/// Application Insights. The Service Bus message and the SQL row keep it.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
internal sealed class PatientInformationAttribute : Attribute;
