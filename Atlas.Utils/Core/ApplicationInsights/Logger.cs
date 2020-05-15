using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace Atlas.Utils.Core.ApplicationInsights
{
    public interface ILogger
    {
        void SendEvent(EventModel eventModel);
        void SendTrace(string message, LogLevel messageLogLevel, Dictionary<string, string> props = null);
    }

    public class Logger : ILogger
    {
        private readonly TelemetryClient client;
        private readonly LogLevel logLevel;

        public Logger(TelemetryClient client, LogLevel logLevel)
        {
            this.client = client;
            this.logLevel = logLevel;
        }

        public virtual void SendEvent(EventModel eventModel)
        {
            if (eventModel.Level >= logLevel)
            {
                eventModel.Properties.Add("LogLevel", $"{eventModel.Level}");
                client.TrackEvent(eventModel.Name, eventModel.Properties, eventModel.Metrics);
            }
        }

        public virtual void SendTrace(string message, LogLevel messageLogLevel, Dictionary<string, string> props)
        {
            if (messageLogLevel >= logLevel)
            {
                client.TrackTrace(message, GetSeverityLevel(messageLogLevel), props);
            }
        }

        private static SeverityLevel GetSeverityLevel(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
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
