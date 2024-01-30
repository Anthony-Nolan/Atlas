using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Atlas.Debug.Client.Models.DonorImport;

namespace Atlas.Debug.Client.Clients
{
    /// <summary>
    /// Client for calling the donor import debug functions.
    /// </summary>
    public interface IDonorImportClient
    {
        /// <summary>
        /// Import a donor import file.
        /// </summary>
        Task ImportFile(DonorImportRequest request);

        /// <summary>
        /// Check for presence or absence of donors in donor import database (a.k.a. "Atlas donor store").
        /// </summary>
        Task<DebugDonorsResult> CheckDonors(IEnumerable<string> externalDonorCodes);
    }

    /// <inheritdoc cref="IDonorImportClient" />
    public class DonorImportClient : HttpFunctionClient, IDonorImportClient
    {
        /// <inheritdoc />
        public DonorImportClient(HttpClient client, string apiKey) : base(client, apiKey)
        {
        }

        /// <inheritdoc />
        public async Task ImportFile(DonorImportRequest request)
        {
            await PostRequest("debug/donorImport/file", request);
        }

        /// <inheritdoc />
        public async Task<DebugDonorsResult> CheckDonors(IEnumerable<string> externalDonorCodes)
        {
            return await PostRequest<IEnumerable<string>, DebugDonorsResult>("debug/donors", externalDonorCodes);
        }
    }
}