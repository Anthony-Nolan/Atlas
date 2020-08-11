using System.Collections.Generic;
using Atlas.Common.ApplicationInsights;

namespace Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging
{
    /// <summary>
    /// Context containing all information useful for logging on all import related tasks - i.e. data refresh, donor updates
    /// </summary>
    public class MatchingAlgorithmImportLoggingContext : LoggingContext
    {
        public string HlaNomenclatureVersion { get; set; }

        /// <inheritdoc />
        public override Dictionary<string, string> PropertiesToLog()
        {
            return new Dictionary<string, string>
            {
                {nameof(HlaNomenclatureVersion), HlaNomenclatureVersion}
            };
        }
    }
}