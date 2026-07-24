using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace Atlas.Common.ApplicationInsights
{
    public interface IAtlasLogger
    {
        void SendEvent(string name, LogLevel level = LogLevel.Info, Dictionary<string, string> props = null, Dictionary<string, double> metrics = null);
        void SendTrace(string message, LogLevel messageLogLevel = LogLevel.Info, Dictionary<string, string> props = null);
        void SendException(Exception exception, LogLevel messageLogLevel = LogLevel.Error, Dictionary<string, string> props = null);

        /// <summary>
        /// Records a single numeric measurement (e.g. an elapsed duration) as a pre-aggregated Application Insights
        /// metric, rather than a Trace. Unlike <see cref="SendTrace"/>, pre-aggregated metrics are NEVER subject to
        /// sampling, so they survive the worker's adaptive sampling even when emitted in bursts (which is exactly
        /// what dropped the old text-based stopwatch summaries at Data Refresh stage boundaries).
        /// Callers must use the SAME set of dimension keys every time for a given <paramref name="metricName"/>,
        /// and keep dimension cardinality low (metrics aggregate per distinct dimension-value combination).
        /// </summary>
        void SendMetric(string metricName, double value, Dictionary<string, string> dimensions = null);
    }

    public class AtlasLogger : IAtlasLogger
    {
        private readonly TelemetryClient client;
        private readonly LogLevel configuredLogLevel;

        public AtlasLogger(TelemetryClient client, ApplicationInsightsSettings applicationInsightsSettings)
        {
            this.client = client;
            configuredLogLevel = applicationInsightsSettings.LogLevel.ToLogLevel();
        }

        public virtual void SendEvent(string name, LogLevel level = LogLevel.Info, Dictionary<string, string> props = null, Dictionary<string, double> metrics = null)
        {
            if (level >= configuredLogLevel)
            {
                props ??= new Dictionary<string, string>();
                props["LogLevel"] = $"{level}";
                client.TrackEvent(name, props, metrics);
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

        public virtual void SendMetric(string metricName, double value, Dictionary<string, string> dimensions = null)
        {
            // GetMetric caches one aggregator per (metric name + ordered dimension names); a given metricName must
            // therefore always be called with the same dimension names, else GetMetric throws. We order the keys so
            // callers don't have to, and dispatch on count because GetMetric's dimension arity is a compile-time arg.
            if (dimensions == null || dimensions.Count == 0)
            {
                client.GetMetric(metricName).TrackValue(value);
                return;
            }

            var ordered = dimensions.OrderBy(d => d.Key, StringComparer.Ordinal).ToList();
            switch (ordered.Count)
            {
                case 1:
                    client.GetMetric(metricName, ordered[0].Key)
                        .TrackValue(value, ordered[0].Value);
                    break;
                case 2:
                    client.GetMetric(metricName, ordered[0].Key, ordered[1].Key)
                        .TrackValue(value, ordered[0].Value, ordered[1].Value);
                    break;
                case 3:
                    client.GetMetric(metricName, ordered[0].Key, ordered[1].Key, ordered[2].Key)
                        .TrackValue(value, ordered[0].Value, ordered[1].Value, ordered[2].Value);
                    break;
                default:
                    // GetMetric supports at most 4 dimensions via these overloads. Any extra dimensions are dropped;
                    // this is a deliberate cap because high dimensionality defeats the point of a pre-aggregated metric.
                    client.GetMetric(metricName, ordered[0].Key, ordered[1].Key, ordered[2].Key, ordered[3].Key)
                        .TrackValue(value, ordered[0].Value, ordered[1].Value, ordered[2].Value, ordered[3].Value);
                    break;
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
