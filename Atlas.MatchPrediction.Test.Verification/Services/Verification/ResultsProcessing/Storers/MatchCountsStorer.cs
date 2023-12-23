using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.ManualTesting.Common.Models.Entities;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;

namespace Atlas.MatchPrediction.Test.Verification.Services.Verification.ResultsProcessing.Storers
{
    internal class MatchingResultCountsStorer : MatchCountsStorer<MatchingAlgorithmResult>
    {
        public MatchingResultCountsStorer(
            IProcessedResultsRepository<LocusMatchDetails> resultsRepository,
            IMatchedDonorsRepository matchedDonorsRepository)
            : base(resultsRepository, matchedDonorsRepository)
        {
        }
    }

    internal class SearchResultCountsStorer : MatchCountsStorer<SearchResult>
    {
        public SearchResultCountsStorer(
            IProcessedResultsRepository<LocusMatchDetails> resultsRepository,
            IMatchedDonorsRepository matchedDonorsRepository)
            : base(resultsRepository, matchedDonorsRepository)
        {
        }
    }

    internal abstract class MatchCountsStorer<TResult> : ResultsStorer<TResult, LocusMatchDetails> where TResult : Result
    {
        private readonly IMatchedDonorsRepository matchedDonorsRepository;

        protected MatchCountsStorer(
            IProcessedResultsRepository<LocusMatchDetails> resultsRepository,
            IMatchedDonorsRepository matchedDonorsRepository)
            : base(resultsRepository)
        {
            this.matchedDonorsRepository = matchedDonorsRepository;
        }

        /// <returns>Locus match counts greater than zero.</returns>
        protected override async Task<IEnumerable<LocusMatchDetails>> ProcessSingleSearchResult(int searchRequestRecordId, TResult result)
        {
            var matchedDonorId = await matchedDonorsRepository.GetMatchedDonorId(
                searchRequestRecordId, int.Parse(result.DonorCode));
            
            if (matchedDonorId == null)
            {
                throw new Exception($"Could not find matched donor record for donor code {result.DonorCode}.");
            }

            var lociResults = result.ScoringResult.ScoringResultsByLocus.ToLociInfo();

            return MatchPredictionStaticData.MatchPredictionLoci
                .Select(l => new LocusMatchDetails
                {
                    Locus = l,
                    MatchedDonor_Id = matchedDonorId.Value,
                    MatchCount = lociResults.GetLocus(l).MatchCount
                })
                .Where(m => m.MatchCount > 0);
        }
    }
}