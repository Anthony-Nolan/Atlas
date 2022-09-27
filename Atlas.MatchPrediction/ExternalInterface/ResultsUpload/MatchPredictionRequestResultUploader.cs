using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.ApplicationInsights;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Settings;

namespace Atlas.MatchPrediction.ExternalInterface.ResultsUpload
{
    public interface IMatchPredictionRequestResultUploader
    {
        /// <summary>
        /// Uploads match probability result from a match prediction request (i.e., patient-donor pair submitted outside of a search)
        /// </summary>
        ///<returns>Filename of results file</returns>
        Task<MatchPredictionResultLocation> UploadMatchPredictionRequestResult(string matchPredictionRequestId, MatchProbabilityResponse matchProbabilityResponse);
    }

    public class MatchPredictionRequestResultUploader : MatchProbabilityResultUploader, IMatchPredictionRequestResultUploader
    {
        public MatchPredictionRequestResultUploader(AzureStorageSettings azureStorageSettings, ILogger logger) : base(azureStorageSettings, logger)
        {
        }

        /// <inheritdoc />
        public async Task<MatchPredictionResultLocation> UploadMatchPredictionRequestResult(string matchPredictionRequestId, MatchProbabilityResponse matchProbabilityResponse)
        {
            var location = ResultLocationBuilder.BuildMatchPredictionRequestResultLocation(matchPredictionRequestId, ResultsContainer);
            await UploadResult(location.FileName, matchProbabilityResponse);
            return location;
        }
    }
}