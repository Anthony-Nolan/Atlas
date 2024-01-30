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
    }
}