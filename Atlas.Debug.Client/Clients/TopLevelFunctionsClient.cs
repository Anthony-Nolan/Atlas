using System.Net.Http;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Client.Models.SupportMessages;
using Atlas.Debug.Client.Models.SearchResults;
using Atlas.Debug.Client.Models.ServiceBus;

namespace Atlas.Debug.Client.Clients
{
    /// <summary>
    /// Client for calling debug functions hosted on the top-level `Atlas.Functions` app.
    /// </summary>
    public interface ITopLevelFunctionsClient : ICommonAtlasFunctions
    {
        /// <summary>
        /// Peek messages from the `debug` subscription of the alerts service bus topic.
        /// </summary>
        Task<PeekServiceBusMessagesResponse<Alert>> PeekAlerts(PeekServiceBusMessagesRequest request);

        /// <summary>
        /// Peek messages from the `debug` subscription of the notifications service bus topic.
        /// </summary>
        Task<PeekServiceBusMessagesResponse<Notification>> PeekNotifications(PeekServiceBusMessagesRequest request);

        /// <summary>
        /// Peek messages from the `debug` subscription of the `search-results-ready` service bus topic.
        /// </summary>
        Task<PeekServiceBusMessagesResponse<SearchResultsNotification>> PeekSearchResultNotifications(PeekServiceBusMessagesRequest request);

        /// <summary>
        /// Fetch search result set from blob storage.
        /// </summary>
        Task<OriginalSearchResultSet> FetchSearchResultSet(DebugSearchResultsRequest request);

        /// <summary>
        /// Peek messages from the `debug` subscription of the `repeat-search-results-ready` service bus topic.
        /// </summary>
        Task<PeekServiceBusMessagesResponse<SearchResultsNotification>> PeekRepeatSearchResultNotifications(PeekServiceBusMessagesRequest request);

        /// <summary>
        /// Fetch repeat search result set from blob storage.
        /// </summary>
        Task<RepeatSearchResultSet> FetchRepeatSearchResultSet(DebugSearchResultsRequest request);
    }

    /// <inheritdoc cref="ITopLevelFunctionsClient" />
    public class TopLevelFunctionsClient : HttpFunctionClient, ITopLevelFunctionsClient
    {
        /// <inheritdoc />
        public TopLevelFunctionsClient(HttpClient client) : base(client)
        {
        }

        /// <inheritdoc />
        public async Task<PeekServiceBusMessagesResponse<Alert>> PeekAlerts(PeekServiceBusMessagesRequest request)
        {
            return await PostRequest<PeekServiceBusMessagesRequest, PeekServiceBusMessagesResponse<Alert>>("debug/alerts", request);
        }

        /// <inheritdoc />
        public async Task<PeekServiceBusMessagesResponse<Notification>> PeekNotifications(PeekServiceBusMessagesRequest request)
        {
            return await PostRequest<PeekServiceBusMessagesRequest, PeekServiceBusMessagesResponse<Notification>>("debug/notifications", request);
        }

        /// <inheritdoc />
        public async Task<PeekServiceBusMessagesResponse<SearchResultsNotification>> PeekSearchResultNotifications(PeekServiceBusMessagesRequest request)
        {
            return await PostRequest<PeekServiceBusMessagesRequest, PeekServiceBusMessagesResponse<SearchResultsNotification>>("debug/search/notifications", request);
        }

        /// <inheritdoc />
        public async Task<OriginalSearchResultSet> FetchSearchResultSet(DebugSearchResultsRequest request)
        {
            return await PostRequest<DebugSearchResultsRequest, OriginalSearchResultSet>("debug/search/resultSet", request);
        }

        /// <inheritdoc />
        public async Task<PeekServiceBusMessagesResponse<SearchResultsNotification>> PeekRepeatSearchResultNotifications(PeekServiceBusMessagesRequest request)
        {
            return await PostRequest<PeekServiceBusMessagesRequest, PeekServiceBusMessagesResponse<SearchResultsNotification>>("debug/repeatSearch/notifications", request);
        }

        /// <inheritdoc />
        public async Task<RepeatSearchResultSet> FetchRepeatSearchResultSet(DebugSearchResultsRequest request)
        {
            return await PostRequest<DebugSearchResultsRequest, RepeatSearchResultSet>("debug/repeatSearch/resultSet", request);
        }
    }
}