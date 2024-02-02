using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Atlas.Debug.Client.Models.DonorImport;

namespace Atlas.Debug.Client.Clients
{
    /// <summary>
    /// Client for calling matching algorithm-related debug functions.
    /// </summary>
    public interface IMatchingAlgorithmClient
    {
        /// <summary>
        /// Check for presence or absence of donors in the active copy of the matching algorithm database.
        /// </summary>
        Task<DebugDonorsResult> CheckDonors(IEnumerable<string> externalDonorCodes);
    }

    /// <inheritdoc cref="IMatchingAlgorithmClient" />
    public class MatchingAlgorithmClient : HttpFunctionClient, IMatchingAlgorithmClient
    {
        /// <inheritdoc />
        public MatchingAlgorithmClient(HttpClient client) : base(client)
        {
        }

        /// <inheritdoc />
        public async Task<DebugDonorsResult> CheckDonors(IEnumerable<string> externalDonorCodes)
        {
            return await PostRequest<IEnumerable<string>, DebugDonorsResult>("debug/donors/active", externalDonorCodes);
        }
    }
}