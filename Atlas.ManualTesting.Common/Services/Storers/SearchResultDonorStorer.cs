using Atlas.Client.Models.Search.Results;
using Atlas.ManualTesting.Common.Models.Entities;
using Atlas.ManualTesting.Common.Repositories;
using Newtonsoft.Json;

#pragma warning disable 1998

namespace Atlas.ManualTesting.Common.Services.Storers
{
    public class SearchResultDonorStorer : ResultsStorer<SearchResult, MatchedDonor>
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
                    DonorCode = result.DonorCode,
                    TotalMatchCount = result.MatchingResult.MatchingResult.TotalMatchCount,
                    TypedLociCount = result.MatchingResult.MatchingResult.TypedLociCount ?? 0,
                    WasPatientRepresented = !result.MatchPredictionResult.IsPatientPhenotypeUnrepresented,
                    WasDonorRepresented = !result.MatchPredictionResult.IsDonorPhenotypeUnrepresented,
                    PatientHfSetPopulationId = result.MatchPredictionResult.PatientHaplotypeFrequencySet.PopulationId,
                    DonorHfSetPopulationId = result.MatchPredictionResult.DonorHaplotypeFrequencySet.PopulationId,
                    MatchingResult = JsonConvert.SerializeObject(result.MatchingResult),
                    MatchPredictionResult = JsonConvert.SerializeObject(result.MatchPredictionResult)
                }
            };
        }
    }
}