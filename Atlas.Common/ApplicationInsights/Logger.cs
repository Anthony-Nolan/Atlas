using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace Atlas.Common.ApplicationInsights
{
    public interface ILogger
    {
        void SendEvent(EventModel eventModel);
        void SendTrace(string message, LogLevel messageLogLevel = LogLevel.Info, Dictionary<string, string> props = null);
        void SendException(Exception exception, LogLevel messageLogLevel = LogLevel.Error, Dictionary<string, string> props = null);
    }

    public class Logger : ILogger
    {
        private readonly TelemetryClient client;
        private readonly LogLevel configuredLogLevel;

        public Logger(TelemetryClient client, ApplicationInsightsSettings applicationInsightsSettings)
        {
            this.client = client;
            configuredLogLevel = applicationInsightsSettings.LogLevel.ToLogLevel();
        }

        public virtual void SendEvent(EventModel eventModel)
        {
            if (eventModel.Level >= configuredLogLevel)
            {
                eventModel.Properties.Add("LogLevel", $"{eventModel.Level}");
                client.TrackEvent(eventModel.Name, eventModel.Properties, eventModel.Metrics);
            }
        }

        public virtual void SendTrace(string message, LogLevel messageLogLevel, Dictionary<string, string> props)
        {
            if (messageLogLevel >= configuredLogLevel)
            {
                client.TrackTrace(message, GetSeverityLevel(messageLogLevel), props);
            }
        }

        public virtual void SendException(Exception exception, LogLevel messageLogLevel, Dictionary<string, string> props)
        {
            if (messageLogLevel >= configuredLogLevel)
            {
                var telemetry = new ExceptionTelemetry(exception)
                {
                    SeverityLevel = GetSeverityLevel(messageLogLevel)
                };

                if (props != null)
                {
                    foreach (var (key, value) in props)
                    {
                        telemetry.Properties[key] = value;
                    }
                }

                client.TrackException(telemetry);
            }
        }

        private static SeverityLevel GetSeverityLevel(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Verbose:
                    return SeverityLevel.Verbose;
                case LogLevel.Info:
                    return SeverityLevel.Information;
                case LogLevel.Warn:
                    return SeverityLevel.Warning;
                case LogLevel.Error:
                    return SeverityLevel.Error;
                case LogLevel.Critical:
                    return SeverityLevel.Critical;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
            }
        }
    }
}
