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
    public class MatchingGenotypesRequest
    {
        public SubjectRequest Patient { get; set; }
        public SubjectRequest Donor { get; set; }

        public string MatchLoci { get; set; }
        public string HlaVersion { get; set; }

        public class SubjectRequest
        {
            public PhenotypeInfo<string> SubjectHla { get; set; }
            public int ExternalHfSetId { get; set; }
        }
    }

    public interface IMatchingGenotypesRequester
    {
        Task<AtlasHttpResult<GenotypeMatcherResponse>> Request(MatchingGenotypesRequest request);
    }

    internal class MatchingGenotypesRequester : AtlasHttpRequester, IMatchingGenotypesRequester
    {
        private static readonly HttpClient HttpRequestClient = new();

        /// <inheritdoc />
        public MatchingGenotypesRequester(IOptions<ValidationHomeworkSettings> settings) 
            : base(HttpRequestClient, settings.Value.MatchingGenotypesRequestUrl)
        {
        }

        /// <inheritdoc />
        public async Task<AtlasHttpResult<GenotypeMatcherResponse>> Request(MatchingGenotypesRequest request)
        {
            var imputationRequest = new GenotypeMatcherRequest
            {
                Patient = new SubjectInfo
                {
                    HlaTyping = request.Patient.SubjectHla.ToPhenotypeInfoTransfer(),
                    FrequencySetMetadata = new FrequencySetMetadata
                    {
                        EthnicityCode = request.Patient.ExternalHfSetId.ToString(),
                        RegistryCode = request.Patient.ExternalHfSetId.ToString()
                    }
                },
                Donor = new SubjectInfo
                {
                    HlaTyping = request.Donor.SubjectHla.ToPhenotypeInfoTransfer(),
                    FrequencySetMetadata = new FrequencySetMetadata
                    {
                        EthnicityCode = request.Donor.ExternalHfSetId.ToString(),
                        RegistryCode = request.Donor.ExternalHfSetId.ToString()
                    }
                },
                // doesn't matter if we use patient or donor info here, as they will be the same
                MatchPredictionParameters = new MatchPredictionParameters
                {
                    AllowedLoci = request.MatchLoci.ToSet(),
                    MatchingAlgorithmHlaNomenclatureVersion = request.HlaVersion
                }
            };

            return await PostRequest<GenotypeMatcherRequest, GenotypeMatcherResponse>(imputationRequest);
        }
    }
}