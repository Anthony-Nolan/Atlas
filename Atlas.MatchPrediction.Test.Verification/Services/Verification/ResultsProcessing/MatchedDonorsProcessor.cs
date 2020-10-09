using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Newtonsoft.Json;
#pragma warning disable 1998

namespace Atlas.MatchPrediction.Test.Verification.Services.Verification.ResultsProcessing
{
    internal class MatchedDonorsProcessor : ResultsProcessor<MatchedDonor>
    {
        public MatchedDonorsProcessor(IProcessedSearchResultsRepository<MatchedDonor> resultsRepository)
            : base(resultsRepository)
        {
        }

        protected override async Task<IEnumerable<MatchedDonor>> ProcessSingleSearchResult(int searchRequestRecordId, SearchResult result)
        {
            return new[]{
                new MatchedDonor
                {
                    SearchRequestRecord_Id = searchRequestRecordId,
                    MatchedDonorSimulant_Id = int.Parse(result.DonorCode),
                    TotalMatchCount = result.MatchingResult.MatchingResult.TotalMatchCount,
                    TypedLociCount = result.MatchingResult.MatchingResult.TypedLociCount ?? 0,
                    WasPatientRepresented = !result.MatchPredictionResult.IsPatientPhenotypeUnrepresented,
                    WasDonorRepresented = !result.MatchPredictionResult.IsDonorPhenotypeUnrepresented,
                    SearchResult = JsonConvert.SerializeObject(result)
                }
            };
        }
    }
}