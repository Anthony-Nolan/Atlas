using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Atlas.Common.ApplicationInsights
{
    /// <summary>
    /// Stamps SearchRequestId on all telemetry items (traces, dependencies, requests, exceptions)
    /// enabling Application Insights end-to-end transaction correlation without manual property stamping.
    ///
    /// This initializer reads from <see cref="SearchRequestContext"/> (AsyncLocal-backed) so it works
    /// correctly in Azure Functions isolated worker where there is no HttpContext.
    /// </summary>
    public class SearchRequestTelemetryInitializer : ITelemetryInitializer
    {
        private const string PropertyName = "SearchRequestId";

        public void Initialize(ITelemetry telemetry)
        {
            var searchRequestId = SearchRequestContext.SearchRequestId;

            if (string.IsNullOrEmpty(searchRequestId))
            {
                return;
            }

            if (telemetry is ISupportProperties supportProperties)
            {
                if (!supportProperties.Properties.ContainsKey(PropertyName))
                {
                    supportProperties.Properties[PropertyName] = searchRequestId;
                }
            }
        }
    }
}

