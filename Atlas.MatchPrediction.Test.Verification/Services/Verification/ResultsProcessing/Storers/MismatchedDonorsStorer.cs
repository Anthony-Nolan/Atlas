using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Common.Requests;
using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.ManualTesting.Common.Models.Entities;
using Atlas.ManualTesting.Common.Repositories;
using Atlas.MatchingAlgorithm.Client.Models.Scoring;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using Atlas.MatchingAlgorithm.Services.Search.Scoring;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.Test.Verification.Config;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification;
using Atlas.MatchPrediction.Test.Verification.Models;
using DonorScores = System.Collections.Generic.Dictionary<int, Atlas.MatchingAlgorithm.Data.Models.SearchResults.ScoreResult>;

namespace Atlas.MatchPrediction.Test.Verification.Services.Verification.ResultsProcessing.Storers
{
    internal interface IMismatchedDonorsStorer<TResult> where TResult : Result
    {
        /// <summary>
        /// If patient typing was of category <see cref="SimulatedHlaTypingCategory.Genotype"/>,
        /// then account for <see cref="SimulatedHlaTypingCategory.Genotype"/> donors that would not have been
        /// returned in the search results due to having too many mismatches, by creating records with appropriate values.
        /// Note: 0/10 donors and 0/2 loci match counts will not be stored to reduce number of rows added to the database.
        /// </summary>
        Task CreateRecordsForGenotypeDonorsWithTooManyMismatches(VerificationSearchRequestRecord searchRequest, ResultSet<TResult> resultSet);
    }

    internal class MismatchedDonorsStorer<TResult> : IMismatchedDonorsStorer<TResult> where TResult : Result
    {
        private readonly IGenotypeSimulantsInfoCache cache;
        private readonly IDonorScoringService scoringService;
        private readonly IProcessedResultsRepository<MatchedDonor> bulkInsertDonorRepository;
        private readonly IMatchedDonorsRepository matchedDonorsRepository;
        private readonly IProcessedResultsRepository<LocusMatchDetails> matchCountsRepository;

        public MismatchedDonorsStorer(
            IGenotypeSimulantsInfoCache cache,
            IDonorScoringService scoringService,
            IProcessedResultsRepository<MatchedDonor> bulkInsertDonorRepository,
            IMatchedDonorsRepository matchedDonorsRepository,
            IProcessedResultsRepository<LocusMatchDetails> matchCountsRepository)
        {
            this.scoringService = scoringService;
            this.bulkInsertDonorRepository = bulkInsertDonorRepository;
            this.matchedDonorsRepository = matchedDonorsRepository;
            this.matchCountsRepository = matchCountsRepository;
            this.cache = cache;
        }

        public async Task CreateRecordsForGenotypeDonorsWithTooManyMismatches(VerificationSearchRequestRecord searchRequest, ResultSet<TResult> resultSet)
        {
            var info = await cache.GetOrAddGenotypeSimulantsInfo(searchRequest.VerificationRun_Id);

            if (!info.Patients.Ids.Contains(searchRequest.PatientId))
            {
                return;
            }

            var searchResultDonorIds = resultSet.Results.Select(r => int.Parse(r.DonorCode));
            var missingDonorIds = info.Donors.Ids.Except(searchResultDonorIds).ToList();

            if (!missingDonorIds.Any())
            {
                return;
            }

            await CreateRecordsForDonorsWithAtLeastOneMatch(info, searchRequest, missingDonorIds);
        }

        private async Task CreateRecordsForDonorsWithAtLeastOneMatch(
            GenotypeSimulantsInfo info,
            VerificationSearchRequestRecord searchRequest,
            IEnumerable<int> donorSimulantIds)
        {
            var patient = info.Patients.Hla.Single(p => p.Id == searchRequest.PatientId);
            var scores = new DonorScores();

            foreach (var donorSimulantId in donorSimulantIds)
            {
                var donor = info.Donors.Hla.Single(d => d.Id == donorSimulantId);
                var score = await ScoreDonor(patient, donor);

                if (score.AggregateScoreDetails.MatchCount == 0)
                {
                    continue;
                }

                scores.Add(donorSimulantId, score);
            }

            if (!scores.Any())
            {
                return;
            }

            await StoreDonors(searchRequest.Id, scores);
            await StoreNonZeroLocusMatchCounts(searchRequest.Id, scores);
        }

        private async Task<ScoreResult> ScoreDonor(Simulant patient, Simulant donor)
        {
            var request = new DonorHlaScoringRequest
            {
                PatientHla = patient.ToPhenotypeInfo().ToPhenotypeInfoTransfer(),
                DonorHla = donor.ToPhenotypeInfo().ToPhenotypeInfoTransfer(),
                ScoringCriteria = new ScoringCriteria
                {
                    LociToScore = MatchPredictionStaticData.MatchPredictionLoci.ToList(),
                    LociToExcludeFromAggregateScore = new[] { Locus.Dpb1 }
                }
            };

            return await scoringService.ScoreDonorHlaAgainstPatientHla(request);
        }

        private async Task StoreDonors(int searchRequestId, DonorScores scores)
        {
            var matchedDonors = scores.Select(r => new MatchedDonor
                {
                    SearchRequestRecord_Id = searchRequestId,
                    DonorCode = r.Key.ToString(),
                    TotalMatchCount = r.Value.AggregateScoreDetails.MatchCount,
                    TypedLociCount = VerificationConstants.SearchLociCount
                }).ToList();

            await bulkInsertDonorRepository.BulkInsert(matchedDonors);
        }

        private async Task StoreNonZeroLocusMatchCounts(int searchRequestId, DonorScores scores)
        {
            var matchCounts = new List<LocusMatchDetails>();

            foreach (var (donorSimulantId, scoreResult) in scores)
            {
                matchCounts.AddRange(await BuildNonZeroMatchCounts(searchRequestId, donorSimulantId, scoreResult));
            }

            await matchCountsRepository.BulkInsert(matchCounts);
        }

        private async Task<IEnumerable<LocusMatchDetails>> BuildNonZeroMatchCounts(int searchRequestId, int simulantDonorId, ScoreResult scoreResult)
        {
            var matchedDonorId = await matchedDonorsRepository.GetMatchedDonorId(searchRequestId, simulantDonorId.ToString());

            if (matchedDonorId == null)
            {
                throw new Exception(
                    $"No matched donor found for search request {searchRequestId} with simulant Id {simulantDonorId}.");
            }

            var lociInfo = scoreResult.ToLociScoreDetailsInfo();
            return MatchPredictionStaticData.MatchPredictionLoci.Select(l =>
                    new LocusMatchDetails
                    {
                        Locus = l,
                        MatchCount = lociInfo.GetLocus(l).MatchCount(),
                        MatchedDonor_Id = matchedDonorId.Value
                    })
                .Where(m => m.MatchCount > 0);
        }
    }
}
