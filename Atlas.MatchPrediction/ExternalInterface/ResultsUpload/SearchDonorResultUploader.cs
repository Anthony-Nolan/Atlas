using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.ApplicationInsights;
using Atlas.MatchPrediction.ExternalInterface.Settings;

namespace Atlas.MatchPrediction.ExternalInterface.ResultsUpload
{
    internal interface ISearchDonorResultUploader
    {
        /// <summary>
        /// Uploads match probability result calculated for a Atlas donor found via a search request.
        /// </summary>
        /// <returns>Filename of results file</returns>
        Task<string> UploadSearchDonorResult(string searchRequestId, int atlasDonorId, MatchProbabilityResponse matchProbabilityResponse);
    }

    internal class SearchDonorResultUploader : MatchProbabilityResultUploader, ISearchDonorResultUploader
    {
        public SearchDonorResultUploader(AzureStorageSettings azureStorageSettings, ILogger logger) : base(azureStorageSettings, logger)
        {
        }

        /// <inheritdoc />
        public async Task<string> UploadSearchDonorResult(string searchRequestId, int atlasDonorId, MatchProbabilityResponse matchProbabilityResponse)
        {
            var fileName = $"{searchRequestId}/{atlasDonorId}.json";
            await UploadResult(fileName, matchProbabilityResponse);
            return fileName;
        }
    }
}