﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Client.Models.Scoring;
using Atlas.MatchingAlgorithm.Services.Search.Scoring;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchResults;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Services.Verification;
using Atlas.MatchPrediction.Test.Verification.Services.Verification.ResultsProcessing;
using Atlas.MatchPrediction.Test.Verification.Test.TestHelpers;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Verification.Test.UnitTests
{
    [TestFixture]
    public class MismatchedDonorsProcessorTests
    {
        private IGenotypeSimulantsInfoCache cache;
        private IDonorScoringService scoringService;
        private IProcessedSearchResultsRepository<MatchedDonor> bulkInsertDonorRepository;
        private IMatchedDonorsRepository matchedDonorsRepository;
        private IProcessedSearchResultsRepository<LocusMatchCount> matchCountsRepository;

        private IMismatchedDonorsProcessor mismatchedDonorsProcessor;

        private static readonly Simulant Patient = SimulantBuilder.New.Build();
        private static readonly Simulant MissingDonor = SimulantBuilder.New.Build();
        private static readonly SearchRequestRecord SearchRequest = new SearchRequestRecord
        {
            PatientSimulant_Id = Patient.Id
        };

        [SetUp]
        public void SetUp()
        {
            cache = Substitute.For<IGenotypeSimulantsInfoCache>();
            scoringService = Substitute.For<IDonorScoringService>();
            bulkInsertDonorRepository = Substitute.For<IProcessedSearchResultsRepository<MatchedDonor>>();
            matchedDonorsRepository = Substitute.For<IMatchedDonorsRepository>();
            matchCountsRepository = Substitute.For<IProcessedSearchResultsRepository<LocusMatchCount>>();

            mismatchedDonorsProcessor = new MismatchedDonorsProcessor(
                cache, scoringService, bulkInsertDonorRepository, matchedDonorsRepository, matchCountsRepository);


            cache.GetOrAddGenotypeSimulantsInfo(default).ReturnsForAnyArgs(
                GenotypeSimulantsInfoBuilder.New
                    .WithPatient(Patient)
                    .WithDonor(MissingDonor));

            scoringService.ScoreDonorHlaAgainstPatientHla(default)
                .ReturnsForAnyArgs(new ScoreResultBuilder().Build());

            matchedDonorsRepository.GetMatchedDonorId(default, default)
                .ReturnsForAnyArgs(0);
        }

        [Test]
        public async Task CreateRecordsForGenotypeDonorsWithTooManyMismatches_GetsGenotypeSimulantsInfo()
        {
            const int runId = 12345;

            await mismatchedDonorsProcessor.CreateRecordsForGenotypeDonorsWithTooManyMismatches(
                new SearchRequestRecord { VerificationRun_Id = runId }, default);

            await cache.Received().GetOrAddGenotypeSimulantsInfo(runId);
        }

        [Test]
        public async Task CreateRecordsForGenotypeDonorsWithTooManyMismatches_PatientIsNotGenotype_DoesNotCreateRecords()
        {
            cache.GetOrAddGenotypeSimulantsInfo(default)
                .ReturnsForAnyArgs(GenotypeSimulantsInfoBuilder.WithEmptySimulantsInfo);

            await mismatchedDonorsProcessor.CreateRecordsForGenotypeDonorsWithTooManyMismatches(SearchRequest, default);

            await bulkInsertDonorRepository.DidNotReceiveWithAnyArgs().BulkInsertResults(default);
        }

        [Test]
        public async Task CreateRecordsForGenotypeDonorsWithTooManyMismatches_NoMissingDonors_DoesNotCreateRecords()
        {
            var resultSet = SearchResultSetBuilder.New.WithSearchResult(MissingDonor.Id);

            await mismatchedDonorsProcessor.CreateRecordsForGenotypeDonorsWithTooManyMismatches(SearchRequest, resultSet);

            await bulkInsertDonorRepository.DidNotReceiveWithAnyArgs().BulkInsertResults(default);
        }

        [Test]
        public async Task CreateRecordsForGenotypeDonorsWithTooManyMismatches_ScoresMissingDonorHlaAgainstPatientHla()
        {
            await mismatchedDonorsProcessor.CreateRecordsForGenotypeDonorsWithTooManyMismatches(SearchRequest, SearchResultSetBuilder.Empty);

            await scoringService.Received().ScoreDonorHlaAgainstPatientHla(Arg.Any<DonorHlaScoringRequest>());
        }

        [Test]
        public async Task CreateRecordsForGenotypeDonorsWithTooManyMismatches_MissingDonorHasMatchCountZero_DoesNotCreateRecord()
        {
            scoringService.ScoreDonorHlaAgainstPatientHla(default)
                .ReturnsForAnyArgs(new ScoreResultBuilder().WithTotalMatchCount(0).Build());

            await mismatchedDonorsProcessor.CreateRecordsForGenotypeDonorsWithTooManyMismatches(SearchRequest, SearchResultSetBuilder.Empty);

            await bulkInsertDonorRepository.DidNotReceiveWithAnyArgs().BulkInsertResults(default);
        }

        [Test]
        public async Task CreateRecordsForGenotypeDonorsWithTooManyMismatches_CreateRecordForMissingDonor()
        {
            const int totalMatchCount = 1;

            scoringService.ScoreDonorHlaAgainstPatientHla(default)
                .ReturnsForAnyArgs(new ScoreResultBuilder().WithTotalMatchCount(totalMatchCount).Build());

            await mismatchedDonorsProcessor.CreateRecordsForGenotypeDonorsWithTooManyMismatches(SearchRequest, SearchResultSetBuilder.Empty);

            await bulkInsertDonorRepository.Received().BulkInsertResults(Arg.Is<IReadOnlyCollection<MatchedDonor>>(x =>
                x.Single().MatchedDonorSimulant_Id == MissingDonor.Id &&
                x.Single().TotalMatchCount == totalMatchCount));
        }

        [Test]
        public async Task CreateRecordsForGenotypeDonorsWithTooManyMismatches_OnlyCreatesRecordsForLociWithNonZeroMatchCount()
        {
            const int matchCount = 1;
            const Locus nonZeroLocus = Locus.A;
            var scoreResult = new ScoreResultBuilder()
                .WithTotalMatchCount(matchCount)
                .WithMatchCountAtLocus(nonZeroLocus, matchCount)
                .Build();
            scoringService.ScoreDonorHlaAgainstPatientHla(default).ReturnsForAnyArgs(scoreResult);

            const int matchedDonorId = 12345;
            matchedDonorsRepository.GetMatchedDonorId(default, default).ReturnsForAnyArgs(matchedDonorId);

            await mismatchedDonorsProcessor.CreateRecordsForGenotypeDonorsWithTooManyMismatches(SearchRequest, SearchResultSetBuilder.Empty);

            await matchCountsRepository.Received().BulkInsertResults(Arg.Is<IReadOnlyCollection<LocusMatchCount>>(x =>
                x.Single().Locus == nonZeroLocus &&
                x.Single().MatchCount == matchCount &&
                x.Single().MatchedDonor_Id == matchedDonorId));
        }
    }
}