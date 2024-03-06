using System.Net.Http;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Debug.Client.Models.SearchResults;
using Atlas.Debug.Client.Models.ServiceBus;

namespace Atlas.Debug.Client.Clients
{
    /// <summary>
    /// Client for calling functions on the repeat search app.
    /// </summary>
    public interface IRepeatSearchFunctionsClient : ICommonAtlasFunctions
    {
        /// <summary>
        /// Peek messages from the `debug` subscription of the `repeat-search-matching-results-ready` service bus topic.
        /// </summary>
        Task<PeekServiceBusMessagesResponse<MatchingResultsNotification>> PeekMatchingResultNotifications(PeekServiceBusMessagesRequest request);

        /// <summary>
        /// Fetch repeat search matching result set from blob storage.
        /// </summary>
        Task<RepeatMatchingAlgorithmResultSet> FetchMatchingResultSet(DebugSearchResultsRequest request);
    }

    /// <inheritdoc cref="IRepeatSearchFunctionsClient" />
    public class RepeatSearchFunctionsClient : HttpFunctionClient, IRepeatSearchFunctionsClient
    {
        /// <inheritdoc />
        public RepeatSearchFunctionsClient(HttpClient client) : base(client)
        {
        }

        /// <inheritdoc />
        public async Task<PeekServiceBusMessagesResponse<MatchingResultsNotification>> PeekMatchingResultNotifications(PeekServiceBusMessagesRequest request)
        {
            return await PostRequest<PeekServiceBusMessagesRequest, PeekServiceBusMessagesResponse<MatchingResultsNotification>>("debug/repeatSearch/notifications", request);
        }

        /// <inheritdoc />
        public async Task<RepeatMatchingAlgorithmResultSet> FetchMatchingResultSet(DebugSearchResultsRequest request)
        {
            return await PostRequest<DebugSearchResultsRequest, RepeatMatchingAlgorithmResultSet>("debug/repeatSearch/resultSet", request);
        }
    }
}