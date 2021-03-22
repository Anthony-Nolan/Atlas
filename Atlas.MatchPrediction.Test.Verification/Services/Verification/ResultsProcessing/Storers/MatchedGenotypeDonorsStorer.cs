using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Newtonsoft.Json;

#pragma warning disable 1998

namespace Atlas.MatchPrediction.Test.Verification.Services.Verification.ResultsProcessing.Storers
{
    internal class MatchedGenotypeDonorsStorer : ResultsStorer<MatchingAlgorithmResult, MatchedDonor>
    {
        public MatchedGenotypeDonorsStorer(IProcessedResultsRepository<MatchedDonor> resultsRepository)
            : base(resultsRepository)
        {
        }

        protected override async Task<IEnumerable<MatchedDonor>> ProcessSingleSearchResult(int searchRequestRecordId, MatchingAlgorithmResult result)
        {
            return new[]{
                new MatchedDonor
                {
                    SearchRequestRecord_Id = searchRequestRecordId,
                    MatchedDonorSimulant_Id = int.Parse(result.ExternalDonorCode),
                    TotalMatchCount = result.MatchingResult.TotalMatchCount,
                    TypedLociCount = result.MatchingResult.TypedLociCount ?? 0,
                    MatchingResult = JsonConvert.SerializeObject(result)
                }
            };
        }
    }
}