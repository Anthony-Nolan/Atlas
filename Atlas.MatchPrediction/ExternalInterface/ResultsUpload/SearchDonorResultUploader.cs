using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.ExternalInterface.Settings;

namespace Atlas.MatchPrediction.ExternalInterface.ResultsUpload
{
    internal interface ISearchDonorResultUploader
    {
        /// <summary>
        /// Uploads match probability result calculated for a Atlas donor found via a search request.
        /// </summary>
        /// <returns>List of filenames of results files</returns>
        Task<Dictionary<int, string>> UploadSearchDonorResults(string searchRequestId, IEnumerable<int> atlasDonorIds, MatchProbabilityResponse matchProbabilityResponse);
    }

    internal class SearchDonorResultUploader : MatchProbabilityResultUploader, ISearchDonorResultUploader
    {
        public SearchDonorResultUploader(AzureStorageSettings azureStorageSettings, ILogger logger) : base(azureStorageSettings, logger)
        {
        }

        public async Task<Dictionary<int, string>> UploadSearchDonorResults(string searchRequestId, IEnumerable<int> atlasDonorIds, MatchProbabilityResponse matchProbabilityResponse)
        {
            var fileNames = atlasDonorIds.Select(id => new KeyValuePair<int, string> (id, $"{searchRequestId}/{id}.json") ).ToDictionary();
            await UploadResults(fileNames.Select(f => f.Value), matchProbabilityResponse);
            return fileNames;
        }
    }
}