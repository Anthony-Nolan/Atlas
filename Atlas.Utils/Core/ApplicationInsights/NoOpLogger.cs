using System.Collections.Generic;
using Nova.Utils.ApplicationInsights.EventModels;

namespace Nova.Utils.ApplicationInsights
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
