using System.Net.Http;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Debug.Client.Models.Validation;

namespace Atlas.Debug.Client.Clients
{
    /// <summary>
    /// Client for calling functions hosted on Public API app.
    /// </summary>
    public interface IPublicApiFunctionsClient : ICommonAtlasFunctions
    {
        /// <summary>
        /// Initiates a new search request.
        /// Note, this does not call a debug function, but the "production" endpoint.
        /// It has been added to the debug client to facilitate the automated testing of search functionality.
        /// </summary>
        Task<ResponseFromValidatedRequest<SearchInitiationResponse>> PostSearchRequest(SearchRequest searchRequest);
    }

    /// <inheritdoc cref="IPublicApiFunctionsClient" />
    public class PublicApiFunctionsClient : HttpFunctionClient, IPublicApiFunctionsClient
    {
        /// <inheritdoc />
        public PublicApiFunctionsClient(HttpClient client) : base(client)
        {
        }

        /// <inheritdoc />
        public async Task<ResponseFromValidatedRequest<SearchInitiationResponse>> PostSearchRequest(SearchRequest searchRequest)
        {
            return await PostValidatedRequest<SearchRequest, SearchInitiationResponse>("Search", searchRequest);
        }
    }
}