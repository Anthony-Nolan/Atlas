using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;

namespace Atlas.MatchPrediction.Test.Verification.Services.Verification.ResultsProcessing
{
    internal class MatchCountsProcessor : ResultsProcessor<LocusMatchCount>
    {
        private readonly IMatchedDonorsRepository matchedDonorsRepository;

        public MatchCountsProcessor(
            IProcessedSearchResultsRepository<LocusMatchCount> resultsRepository,
            IMatchedDonorsRepository matchedDonorsRepository)
            : base(resultsRepository)
        {
            this.matchedDonorsRepository = matchedDonorsRepository;
        }

        /// <returns>Locus match counts greater than zero.</returns>
        protected override async Task<IEnumerable<LocusMatchCount>> ProcessSingleSearchResult(int searchRequestRecordId, SearchResult result)
        {
            var matchedDonorId = await matchedDonorsRepository.GetMatchedDonorId(
                searchRequestRecordId, int.Parse(result.DonorCode));
            
            if (matchedDonorId == null)
            {
                throw new Exception($"Could not find matched donor record for donor code {result.DonorCode}.");
            }

            var lociResults = result.MatchingResult.ScoringResult.ScoringResultsByLocus.ToLociInfo();

            return MatchPredictionStaticData.MatchPredictionLoci
                .Select(l => new LocusMatchCount
                {
                    Locus = l,
                    MatchedDonor_Id = matchedDonorId.Value,
                    MatchCount = lociResults.GetLocus(l).MatchCount
                })
                .Where(m => m.MatchCount > 0);
        }
    }
}