using System.Net.Http;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.Debug.Client.Models.MatchPrediction;
using Atlas.ManualTesting.Common.Services;
using Atlas.MatchPrediction.Test.Validation.Models;
using Atlas.MatchPrediction.Test.Validation.Settings;
using Microsoft.Extensions.Options;

namespace Atlas.MatchPrediction.Test.Validation.Services.Exercise4.Homework
{
    public class HomeworkImputationRequest
    {
        public PhenotypeInfo<string> SubjectHla { get; set; }
        public int? ExternalHfSetId { get; set; }
        public string MatchLoci { get; set; }
        public string HlaVersion { get; set; }
    }

    public interface IImputationRequester
    {
        Task<AtlasHttpResult<GenotypeImputationResponse>> Request(HomeworkImputationRequest request);
    }

    internal class ImputationRequester : AtlasHttpRequester, IImputationRequester
    {
        private static readonly HttpClient HttpRequestClient = new();

        /// <inheritdoc />
        public ImputationRequester(IOptions<ValidationHomeworkSettings> settings) 
            : base(HttpRequestClient, settings.Value.ImputationRequestUrl)
        {
        }

        /// <inheritdoc />
        public async Task<AtlasHttpResult<GenotypeImputationResponse>> Request(HomeworkImputationRequest request)
        {
            var imputationRequest = new GenotypeImputationRequest
            {
                SubjectInfo = new SubjectInfo
                {
                    HlaTyping = request.SubjectHla.ToPhenotypeInfoTransfer(),
                    FrequencySetMetadata = new FrequencySetMetadata
                    {
                        EthnicityCode = request.ExternalHfSetId.ToString(),
                        RegistryCode = request.ExternalHfSetId.ToString()
                    }
                },
                MatchPredictionParameters = new MatchPredictionParameters
                {
                    AllowedLoci = request.MatchLoci.ToSet(),
                    MatchingAlgorithmHlaNomenclatureVersion = request.HlaVersion
                }
            };

            return await PostRequest<GenotypeImputationRequest, GenotypeImputationResponse>(imputationRequest);
        }
    }
}