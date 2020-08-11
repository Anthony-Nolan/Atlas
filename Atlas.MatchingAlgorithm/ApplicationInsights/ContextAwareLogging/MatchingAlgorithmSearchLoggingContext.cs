using System;
using System.Collections.Generic;
using Atlas.Common.ApplicationInsights;

namespace Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging
{
    public class MatchingAlgorithmSearchLoggingContext : LoggingContext
    {
        private string searchRequestId;
        private string hlaNomenclatureVersion;

        public string SearchRequestId
        {
            get => searchRequestId;
            set
            {
                if (!string.IsNullOrEmpty(searchRequestId))
                {
                    throw new InvalidOperationException(
                        $"Cannot set {nameof(SearchRequestId)} to '{value}' as it is already set to '{searchRequestId}'.");
                }

                searchRequestId = value;
            }
        }

        public string HlaNomenclatureVersion
        {
            get => hlaNomenclatureVersion;
            set
            {
                if (!string.IsNullOrEmpty(hlaNomenclatureVersion))
                {
                    throw new InvalidOperationException(
                        $"Cannot set {nameof(hlaNomenclatureVersion)} to '{value}' as it is already set to '{hlaNomenclatureVersion}'.");
                }

                hlaNomenclatureVersion = value;
            }
        }

        /// <inheritdoc />
        public override Dictionary<string, string> PropertiesToLog() =>
            new Dictionary<string, string>
            {
                {nameof(SearchRequestId), SearchRequestId},
                {nameof(HlaNomenclatureVersion), HlaNomenclatureVersion},
            };
    }
}