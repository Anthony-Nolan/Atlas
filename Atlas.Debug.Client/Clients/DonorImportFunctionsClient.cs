using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Atlas.Debug.Client.Models.DonorImport;
using Atlas.Debug.Client.Models.ServiceBus;
using Atlas.DonorImport.FileSchema.Models;

namespace Atlas.Debug.Client.Clients
{
    /// <summary>
    /// Client for calling the donor import debug functions.
    /// </summary>
    public interface IDonorImportFunctionsClient : ICommonAtlasFunctions
    {
        /// <summary>
        /// Import a donor import file.
        /// </summary>
        Task ImportFile(DonorImportRequest request);

        /// <summary>
        /// Check for presence or absence of donors in donor import database (a.k.a. "Atlas donor store").
        /// </summary>
        Task<DebugDonorsResult> CheckDonors(IEnumerable<string> externalDonorCodes);

        /// <summary>
        /// Peek messages from the `debug` subscription of the donor-import-results service bus topic.
        /// </summary>
        Task<PeekServiceBusMessagesResponse<DonorImportMessage>> PeekDonorImportResultMessages(PeekServiceBusMessagesRequest request);

        /// <summary>
        /// Retrieves donor import failures by file name.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        Task<DonorImportFailureInfo> GetDonorImportFailuresByFileName(string fileName);

        /// <summary>
        /// Check if full mode import is allowed.
        /// </summary>
        /// <returns></returns>
        Task<bool> IsFullModeImportAllowed();

        /// <summary>
        /// Get external donor codes by registry.
        /// <paramref name="updatedBeforeDate"/> is optional, and if provided, must be encoded as "yyyyMMdd".
        /// </summary>
        Task<IEnumerable<string>> GetExternalDonorCodesByRegistry(string registryCode, string updatedBeforeDate = null);

        /// <summary>
        /// Delete donors from donor store by external donor codes.
        /// Note: this change will NOT be propagated to the matching algorithm db.
        /// Donors have to be made unavailable for search by calling <see cref="MatchingAlgorithmFunctionsClient.SetDonorsAsUnavailableForSearch"/>.
        /// </summary>
        Task DeleteDonors(IEnumerable<string> externalDonorCodes);
    }

    /// <inheritdoc cref="IDonorImportFunctionsClient" />
    public class DonorImportFunctionsClient : HttpFunctionClient, IDonorImportFunctionsClient
    {
        /// <inheritdoc />
        public DonorImportFunctionsClient(HttpClient client) : base(client)
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

        /// <inheritdoc />
        public async Task<PeekServiceBusMessagesResponse<DonorImportMessage>> PeekDonorImportResultMessages(PeekServiceBusMessagesRequest request)
        {
            return await PostRequest<PeekServiceBusMessagesRequest, PeekServiceBusMessagesResponse<DonorImportMessage>>("debug/donorImport/results", request);
        }

        /// <inheritdoc />
        public async Task<DonorImportFailureInfo> GetDonorImportFailuresByFileName(string fileName)
        {
            return await GetRequest<DonorImportFailureInfo>($"debug/donorUpdates/failures/{fileName}");
        }

        /// <inheritdoc />
        public async Task<bool> IsFullModeImportAllowed()
        {
            return await GetRequest<bool>("debug/donorImport/isFullModeImportAllowed");
        }

        /// <inheritdoc />
        public async Task<IEnumerable<string>> GetExternalDonorCodesByRegistry(string registryCode, string updatedBeforeDate = null)
        {
            return await GetRequest<IEnumerable<string>>($"debug/donors/{registryCode}/externalDonorCodes/{updatedBeforeDate ?? string.Empty}");
        }

        /// <inheritdoc />
        public async Task DeleteDonors(IEnumerable<string> externalDonorCodes)
        {
            await PostRequest("debug/donors/delete", externalDonorCodes);
        }
    }
}