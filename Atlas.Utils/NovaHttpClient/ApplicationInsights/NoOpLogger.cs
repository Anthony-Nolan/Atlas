using System.Collections.Generic;
using Atlas.Utils.Core.ApplicationInsights;

namespace Atlas.Utils.NovaHttpClient.ApplicationInsights
{
    public class NoOpLogger : ILogger
    {
        public void SendEvent(EventModel eventModel)
        {
        }

        public void SendTrace(string message, LogLevel messageLogLevel, Dictionary<string, string> props)
        {
        }
    }
}
