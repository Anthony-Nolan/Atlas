using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Debug.Client.Models.ApplicationInsights;
using Atlas.Debug.Client.Models.SearchResults;
using Atlas.Debug.Client.Models.ServiceBus;
using Atlas.MatchingAlgorithm.Client.Models.Scoring;

namespace Atlas.Debug.Client.Clients
{
    /// <summary>
    /// Client for calling matching algorithm-related debug functions.
    /// </summary>
    public interface IMatchingAlgorithmFunctionsClient : ICommonAtlasFunctions
    {
        /// <summary>
        /// Peek messages from the `debug` subscription of the `matching-results` service bus topic.
        /// </summary>
        Task<PeekServiceBusMessagesResponse<MatchingResultsNotification>> PeekMatchingResultNotifications(PeekServiceBusMessagesRequest request);

        /// <summary>
        /// Fetch matching result set from the matching algorithm results blob storage.
        /// </summary>
        Task<OriginalMatchingAlgorithmResultSet> FetchMatchingResultSet(DebugSearchResultsRequest request);

        /// <summary>
        /// Returns Hla Expansion Failures from logs for last <paramref name="daysToQuery"/> days
        /// </summary>
        /// <param name="daysToQuery"></param>
        /// <returns></returns>
        Task<IEnumerable<HlaExpansionFailure>> GetHlaExpansionFailures(int daysToQuery = 14);

        /// <summary>
        /// Scores a batch of donors via production endpoint on the matching algorithm app.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<IEnumerable<DonorScoringResult>> ScoreBatch(BatchScoringRequest request);
    }

    /// <inheritdoc cref="IMatchingAlgorithmFunctionsClient" />
    public class MatchingAlgorithmFunctionsClient : HttpFunctionClient, IMatchingAlgorithmFunctionsClient
    {
        /// <inheritdoc />
        public MatchingAlgorithmFunctionsClient(HttpClient client) : base(client)
        {
        }

        /// <inheritdoc />
        public async Task<PeekServiceBusMessagesResponse<MatchingResultsNotification>> PeekMatchingResultNotifications(PeekServiceBusMessagesRequest request)
        {
            return await PostRequest<PeekServiceBusMessagesRequest, PeekServiceBusMessagesResponse<MatchingResultsNotification>>("debug/matching/notifications", request);
        }

        /// <inheritdoc />
        public async Task<OriginalMatchingAlgorithmResultSet> FetchMatchingResultSet(DebugSearchResultsRequest request)
        {
            return await PostRequest<DebugSearchResultsRequest, OriginalMatchingAlgorithmResultSet>("debug/matching/resultSet", request);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<HlaExpansionFailure>> GetHlaExpansionFailures(int daysToQuery = 14)
        {
            return await GetRequest<IEnumerable<HlaExpansionFailure>>($"debug/HlaExpansionFailures/{daysToQuery}");
        }

        /// <inheritdoc />
        public async Task<IEnumerable<DonorScoringResult>> ScoreBatch(BatchScoringRequest request)
        {
            return await PostRequest<BatchScoringRequest, IEnumerable<DonorScoringResult>>("ScoreBatch", request);
        }
    }
}