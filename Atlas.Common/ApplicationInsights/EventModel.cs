using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Utils.Extensions;

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

        /// <inheritdoc />
        public override string ToString()
        {
            return $@"
EventType: {GetType().Name}
EventName: {Name},
LogLevel: {Level.ToString()}
{nameof(Properties)}:
{Properties?.Select(kvp => $"    {kvp.Key}: {kvp.Value}").StringJoinWithNewline()},
{nameof(Metrics)}:
{Metrics?.Select(kvp => $"    {kvp.Key}: {kvp.Value}").StringJoinWithNewline()}
";
        }
    }
}
