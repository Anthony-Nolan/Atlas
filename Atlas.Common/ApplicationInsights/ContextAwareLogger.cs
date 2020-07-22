using System.Collections.Generic;
using Atlas.Common.Utils.Extensions;
using Microsoft.ApplicationInsights;

namespace Atlas.Common.ApplicationInsights
{
    public class ContextAwareLogger<TLogContext> : Logger where TLogContext: LoggingContext
    {
        private readonly TLogContext loggingContext;

        public ContextAwareLogger(
            TLogContext loggingContext,
            TelemetryClient client, 
            ApplicationInsightsSettings applicationInsightsSettings) : base(client, applicationInsightsSettings)
        {
            this.loggingContext = loggingContext;
        }

        public override void SendEvent(EventModel eventModel)
        {
            AdornWithContextProps(eventModel.Properties);
            base.SendEvent(eventModel);
        }

        public override void SendTrace(string message, LogLevel messageLogLevel, Dictionary<string, string> props)
        {
            props ??= new Dictionary<string, string>();
            AdornWithContextProps(props);
            base.SendTrace(message, messageLogLevel, props);
        }

        private void AdornWithContextProps(IDictionary<string, string> properties)
        {
            foreach (var (key, value) in loggingContext.PropertiesToLog())
            {
                if (!properties.ContainsKey(key))
                {
                    properties.AddIfNotNullOrEmpty(key, value);
                }
            }
        }
    }
}