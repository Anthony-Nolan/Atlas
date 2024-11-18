using System.Net.Http;
using System.Threading.Tasks;
using Atlas.Client.Models.Common.Requests;
using Atlas.Client.Models.Search.Requests;
using Atlas.Debug.Client.Models.Validation;

namespace Atlas.Debug.Client.Clients
{
    /// <summary>
    /// Client for calling functions hosted on Public API app.
    /// Note, methods do not call a debug function, but the "production" endpoints.
    /// They have been added to the debug client to facilitate the automated testing of search functionality.
    /// </summary>
    public interface IPublicApiFunctionsClient : ICommonAtlasFunctions
    {
        /// <summary>
        /// Initiates a new search request.

        /// </summary>
        Task<ResponseFromValidatedRequest<SearchInitiationResponse>> PostSearchRequest(SearchRequest request);

        /// <summary>
        /// Initiates a new repeat search request.
        /// </summary>
        Task<ResponseFromValidatedRequest<SearchInitiationResponse>> PostRepeatSearchRequest(RepeatSearchRequest request);
    }

    /// <inheritdoc cref="IPublicApiFunctionsClient" />
    public class PublicApiFunctionsClient : HttpFunctionClient, IPublicApiFunctionsClient
    {
        /// <inheritdoc />
        public PublicApiFunctionsClient(HttpClient client) : base(client)
        {
        }

        /// <inheritdoc />
        public async Task<ResponseFromValidatedRequest<SearchInitiationResponse>> PostSearchRequest(SearchRequest request)
        {
            return await PostValidatedRequest<SearchRequest, SearchInitiationResponse>("Search", request);
        }

        /// <inheritdoc />
        public async Task<ResponseFromValidatedRequest<SearchInitiationResponse>> PostRepeatSearchRequest(RepeatSearchRequest request)
        {
            return await PostValidatedRequest<RepeatSearchRequest, SearchInitiationResponse>("RepeatSearch", request);
        }
    }
}