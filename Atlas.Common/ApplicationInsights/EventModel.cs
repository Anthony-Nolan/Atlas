using System.Collections.Generic;

namespace Atlas.Common.ApplicationInsights
{
    public class EventModel
    {
        public EventModel(string name)
        {
            Name = name;
            Properties = new Dictionary<string, string>();
            Metrics = new Dictionary<string, double>();
        }

        public string Name { get; }
        public Dictionary<string, string> Properties { get; }
        public Dictionary<string, double> Metrics { get; }
        public LogLevel Level { get; set; }
    }
}
