using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results;
using Atlas.ManualTesting.Common.Models.Entities;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Newtonsoft.Json;

#pragma warning disable 1998

namespace Atlas.MatchPrediction.Test.Verification.Services.Verification.ResultsProcessing.Storers
{
    internal class SearchResultDonorStorer : ResultsStorer<SearchResult, MatchedDonor>
    {
        public SearchResultDonorStorer(IProcessedResultsRepository<MatchedDonor> resultsRepository)
            : base(resultsRepository)
        {
        }

        protected override async Task<IEnumerable<MatchedDonor>> ProcessSingleSearchResult(int searchRequestRecordId, SearchResult result)
        {
            return new[]{
                new MatchedDonor
                {
                    SearchRequestRecord_Id = searchRequestRecordId,
                    DonorId = int.Parse(result.DonorCode),
                    TotalMatchCount = result.MatchingResult.MatchingResult.TotalMatchCount,
                    TypedLociCount = result.MatchingResult.MatchingResult.TypedLociCount ?? 0,
                    WasPatientRepresented = !result.MatchPredictionResult.IsPatientPhenotypeUnrepresented,
                    WasDonorRepresented = !result.MatchPredictionResult.IsDonorPhenotypeUnrepresented,
                    MatchingResult = JsonConvert.SerializeObject(result.MatchingResult),
                    MatchPredictionResult = JsonConvert.SerializeObject(result.MatchPredictionResult)
                }
            };
        }
    }
}