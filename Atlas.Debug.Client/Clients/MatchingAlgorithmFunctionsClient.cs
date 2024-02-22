using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Debug.Client.Models.DonorImport;
using Atlas.Debug.Client.Models.ServiceBus;

namespace Atlas.Debug.Client.Clients
{
    /// <summary>
    /// Client for calling matching algorithm-related debug functions.
    /// </summary>
    public interface IMatchingAlgorithmFunctionsClient : ICommonAtlasFunctions
    {
        /// <summary>
        /// Check for presence or absence of donors in the active copy of the matching algorithm database.
        /// </summary>
        Task<DebugDonorsResult> CheckDonors(IEnumerable<string> externalDonorCodes);

        /// <summary>
        /// Peek messages from the `debug` subscription of the `matching-results` service bus topic.
        /// </summary>
        Task<PeekServiceBusMessagesResponse<MatchingResultsNotification>> PeekMatchingResultNotifications(PeekServiceBusMessagesRequest request);
    }

    /// <inheritdoc cref="IMatchingAlgorithmFunctionsClient" />
    public class MatchingAlgorithmFunctionsClient : HttpFunctionClient, IMatchingAlgorithmFunctionsClient
    {
        /// <inheritdoc />
        public MatchingAlgorithmFunctionsClient(HttpClient client) : base(client)
        {
        }

        /// <inheritdoc />
        public async Task<DebugDonorsResult> CheckDonors(IEnumerable<string> externalDonorCodes)
        {
            return await PostRequest<IEnumerable<string>, DebugDonorsResult>("debug/donors/active", externalDonorCodes);
        }

        /// <inheritdoc />
        public async Task<PeekServiceBusMessagesResponse<MatchingResultsNotification>> PeekMatchingResultNotifications(PeekServiceBusMessagesRequest request)
        {
            return await PostRequest<PeekServiceBusMessagesRequest, PeekServiceBusMessagesResponse<MatchingResultsNotification>>("debug/matching/notifications", request);
        }
    }
}