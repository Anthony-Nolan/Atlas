using System.Collections.Generic;

namespace Atlas.Common.ApplicationInsights
{
    public abstract class LoggingContext
    {
        public abstract Dictionary<string, string> PropertiesToLog();
    }
}