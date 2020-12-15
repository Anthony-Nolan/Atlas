using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh.HlaProcessing;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Repositories;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Import
{
    public class HlaProcessorTests
    {
        private IDonorImportRepository importRepo;
        private IDonorInspectionRepository inspectionRepo;
        private ITestDataRefreshHistoryRepository dataRefreshHistoryRepository;
        private IHlaProcessor processor;

        private int refreshRecordId;
        private const string DefaultHlaNomenclatureVersion = FileBackedHlaMetadataRepositoryBaseReader.OlderTestHlaVersion;

        [SetUp]
        public void SetUp()
        {
            DependencyInjection.DependencyInjection.NewScope();
            dataRefreshHistoryRepository = DependencyInjection.DependencyInjection.Provider.GetService<ITestDataRefreshHistoryRepository>();
            
            // The data refresh is triggered as a two step process in this suite - (a) insert unprocessed donors (b) run pre-processing on all donors
            // Without clearing the data between test runs, processing is re-run on all existing donors, without making use of the data refresh continue mechanism 
            // Leading to duplicate donor relations, causing some tests to erroneously fail.
            DatabaseManager.ClearDatabases();
            refreshRecordId = dataRefreshHistoryRepository.InsertDummySuccessfulRefreshRecord();

            var repositoryFactory = DependencyInjection.DependencyInjection.Provider.GetService<IDormantRepositoryFactory>();
            importRepo = repositoryFactory.GetDonorImportRepository();
            // We want to inspect the dormant database, as this is what the import will have run on
            inspectionRepo = repositoryFactory.GetDonorInspectionRepository();
            processor = DependencyInjection.DependencyInjection.Provider.GetService<IHlaProcessor>();
        }

        [Test]
        public async Task UpdateDonorHla_DoesNotChangeStoredDonorInformation()
        {
            var donorInfo = new DonorInfoBuilder().Build();
            await importRepo.InsertBatchOfDonors(new List<DonorInfo> {donorInfo});

            await processor.UpdateDonorHla(
                DefaultHlaNomenclatureVersion,
                donorId => dataRefreshHistoryRepository.UpdateLastSafelyProcessedDonor(refreshRecordId, donorId));

            var storedDonor = await inspectionRepo.GetDonor(donorInfo.DonorId);
            AssertStoredDonorInfoMatchesOriginalDonorInfo(storedDonor, donorInfo);
        }

        [Test]
        public async Task UpdateDonorHla_DonorHlaHasMultiplePGroups_InsertsRowForEachPGroup()
        {
            // We know the number of p-groups for a given hla string from the in-memory metadata dictionary.
            // If the underlying data changes, this may become incorrect.
            const string hlaWithKnownPGroups = "01:XX";
            const int expectedPGroupCount = 213;

            var donorInfo = new DonorInfoBuilder()
                .WithHlaAtLocus(Locus.A, LocusPosition.One, hlaWithKnownPGroups)
                .Build();

            await importRepo.InsertBatchOfDonors(new List<DonorInfo> {donorInfo});

            await processor.UpdateDonorHla(
                DefaultHlaNomenclatureVersion,
                donorId => dataRefreshHistoryRepository.UpdateLastSafelyProcessedDonor(refreshRecordId, donorId));

            var actualPGroupCount = await GetPGroupCountAtLocusAPositionOne(donorInfo.DonorId);
            actualPGroupCount.Should().Be(expectedPGroupCount);
        }

        [Test]
        public async Task UpdateDonorHla_DonorHasNullExpressingAllele_InsertsPGroupsOfExpressingAllele()
        {
            // We know the number of p-groups for a given hla string from the in-memory metadata dictionary.
            // If the underlying data changes, this may become incorrect.
            const string hlaWithKnownPGroups = "02:01/02:02";
            const string nullExpressingAllele = "01:01N";

            var donorInfo = new DonorInfoBuilder()
                .WithHlaAtLocus(Locus.A, LocusPosition.Two, hlaWithKnownPGroups)
                .WithHlaAtLocus(Locus.A, LocusPosition.One, nullExpressingAllele)
                .Build();

            await importRepo.InsertBatchOfDonors(new List<DonorInfo> {donorInfo});

            await processor.UpdateDonorHla(
                DefaultHlaNomenclatureVersion,
                donorId => dataRefreshHistoryRepository.UpdateLastSafelyProcessedDonor(refreshRecordId, donorId));

            var actualPGroupCount = await GetPGroupCountAtLocusAPositionOne(donorInfo.DonorId);
            actualPGroupCount.Should().Be(2);
        }
        
        /// <summary>
        /// During initial development of normalisation of pre-processed hla data, an issue was introduced where p-groups were processed for the
        /// same HLA name multiple times, causing huge data inflation and corresponding performance degradation.
        ///
        /// This test exists to ensure we do not repeat such an error, as it does not invalidate any searches - so will not be caught by purely functional tests 
        /// </summary>
        [Test]
        public async Task UpdateDonorHla_MultipleDonorsWithSameHla_DoesNotInsertDuplicateHlaPGroupRelations_OrDuplicateDonorIdHlaRelations()
        {
            // We know the number of p-groups for a given hla string from the in-memory metadata dictionary.
            // If the underlying data changes, this may become incorrect.
            const string hlaWithKnownPGroups = "01:XX";
            const int expectedPGroupCount = 213;

            var donorInfo1 = new DonorInfoBuilder().WithHlaAtLocus(Locus.A, LocusPosition.One, hlaWithKnownPGroups).Build();
            var donorInfo2 = new DonorInfoBuilder().WithHlaAtLocus(Locus.A, LocusPosition.One, hlaWithKnownPGroups).Build();
            var donorInfo3 = new DonorInfoBuilder().WithHlaAtLocus(Locus.A, LocusPosition.One, hlaWithKnownPGroups).Build();
            
            await importRepo.InsertBatchOfDonors(new List<DonorInfo> {donorInfo1, donorInfo2});
            await importRepo.InsertBatchOfDonors(new List<DonorInfo> {donorInfo3});

            await processor.UpdateDonorHla(
                DefaultHlaNomenclatureVersion,
                donorId => dataRefreshHistoryRepository.UpdateLastSafelyProcessedDonor(refreshRecordId, donorId));

            var actualPGroupCount = await GetPGroupCountAtLocusAPositionOne(donorInfo1.DonorId);
            actualPGroupCount.Should().Be(expectedPGroupCount);
        }

        /// <summary>
        /// <see cref="UpdateDonorHla_WhenHlaUpdateIsContinued_DonorWithDuplicatePGroupsIsReturnedFromSearchWithNoErrors"/> proves that in this case,
        /// search will not be detrimentally affected.
        /// This test shows that in the case of a continued restart - some donors' hla will be processed twice, leading to some duplicate data.
        /// This duplicate data does not cause functional issues - and as long as the duplication is minimal (i.e. it only happens for the overlap
        /// batches of a continued refresh), it does not significantly alter performance.
        ///
        /// <see cref="UpdateDonorHla_MultipleDonorsWithSameHla_DoesNotInsertDuplicateHlaPGroupRelations_OrDuplicateDonorIdHlaRelations"/> ensures
        /// that such duplication does not occur in other circumstances.
        /// </summary>
        [Test]
        public async Task UpdateDonorHla_WhenHlaUpdateIsContinued_AddsDuplicatePGroups()
        {
            var donorInfo = new DonorInfoBuilder().Build();
            await importRepo.InsertBatchOfDonors(new List<DonorInfo> {donorInfo});

            await processor.UpdateDonorHla(
                DefaultHlaNomenclatureVersion,
                donorId => dataRefreshHistoryRepository.UpdateLastSafelyProcessedDonor(refreshRecordId, donorId),
                null,
                false);

            var initialPGroupCount = await GetPGroupCountAtLocusAPositionOne(donorInfo.DonorId) ?? int.MaxValue;

            var lastProcessedDonor = await dataRefreshHistoryRepository.GetLastSuccessfullyInsertedDonor(refreshRecordId);

            await processor.UpdateDonorHla(
                DefaultHlaNomenclatureVersion,
                donorId => dataRefreshHistoryRepository.UpdateLastSafelyProcessedDonor(refreshRecordId, donorId),
                lastProcessedDonor,
                true);

            var finalPGroupCount = await GetPGroupCountAtLocusAPositionOne(donorInfo.DonorId);
            finalPGroupCount.Should().BeGreaterThan(initialPGroupCount);
        }

        /// <summary>
        /// Previously when continuing a data refresh, we would delete all processed p-groups within an "overlap" of a few batches, then re-insert.
        /// This was shown to be prohibitively slow with large dataset sizes, as indexes have been removed at this stage, so we are trying to remove
        /// rows from a table with billions of rows, by an un-indexed ID.
        ///
        /// This test exists to prove that having duplicate P Group IDs does not cause any issues with searching for that donor - no exceptions
        /// are thrown, and the donor is still returned.
        /// </summary>
        [Test]
        public async Task UpdateDonorHla_WhenHlaUpdateIsContinued_DonorWithDuplicatePGroupsIsReturnedFromSearchWithNoErrors()
        {
            var donorInfo = new DonorInfoBuilder().Build();
            await importRepo.InsertBatchOfDonors(new List<DonorInfo> {donorInfo});
        
            await processor.UpdateDonorHla(
                DefaultHlaNomenclatureVersion,
                donorId => dataRefreshHistoryRepository.UpdateLastSafelyProcessedDonor(refreshRecordId, donorId)
            );
        
            var lastProcessedDonor = await dataRefreshHistoryRepository.GetLastSuccessfullyInsertedDonor(refreshRecordId);
        
            await processor.UpdateDonorHla(
                DefaultHlaNomenclatureVersion,
                donorId => dataRefreshHistoryRepository.UpdateLastSafelyProcessedDonor(refreshRecordId, donorId),
                lastProcessedDonor,
                true);
        
            var finalPGroupCount = await GetPGroupCountAtLocusAPositionOne(donorInfo.DonorId);
            
            // Assert we correctly set up duplicate P-Groups
            finalPGroupCount.Should().BeGreaterOrEqualTo(2);
        
            // Hla processor acts on the dormant database, so once complete, we need to activate it before running a search
            await dataRefreshHistoryRepository.SwitchDormantDatabase();
            // Database selection cached per-scope, so we need a new scope after switching the active database
            DependencyInjection.DependencyInjection.NewScope();
            
            var searchService = DependencyInjection.DependencyInjection.Provider.GetService<ISearchService>();
            var searchRequest = new SearchRequestBuilder().WithSearchHla(donorInfo.HlaNames).Build();
            var searchResults = await searchService.Search(searchRequest);
        
            searchResults.Should().ContainSingle(r => r.AtlasDonorId == donorInfo.DonorId);
        }

        [Test]
        public async Task UpdateDonorHla_WhenHlaUpdateIsContinued_ButIsToldItsANewRun_ReAddsPGroups()
        {
            var donorInfo = new DonorInfoBuilder().Build();
            await importRepo.InsertBatchOfDonors(new List<DonorInfo> {donorInfo});

            await processor.UpdateDonorHla(
                DefaultHlaNomenclatureVersion,
                donorId => dataRefreshHistoryRepository.UpdateLastSafelyProcessedDonor(refreshRecordId, donorId));

            var initialPGroupCount = await GetPGroupCountAtLocusAPositionOne(donorInfo.DonorId);

            await processor.UpdateDonorHla(
                DefaultHlaNomenclatureVersion,
                donorId => dataRefreshHistoryRepository.UpdateLastSafelyProcessedDonor(refreshRecordId, donorId));

            var finalPGroupCount = await GetPGroupCountAtLocusAPositionOne(donorInfo.DonorId);
            finalPGroupCount.Should().Be(initialPGroupCount * 2);
        }

        [Test]
        public async Task UpdateDonorHla_UpdatesHlaForDonorsInsertedSinceLastRun()
        {
            var donorInfo = new DonorInfoBuilder().Build();
            await importRepo.InsertBatchOfDonors(new List<DonorInfo> {donorInfo});
            await processor.UpdateDonorHla(
                DefaultHlaNomenclatureVersion,
                donorId => dataRefreshHistoryRepository.UpdateLastSafelyProcessedDonor(refreshRecordId, donorId));

            var newDonor = new DonorInfoBuilder().Build();
            await importRepo.InsertBatchOfDonors(new List<DonorInfo> {newDonor});
            await processor.UpdateDonorHla(
                DefaultHlaNomenclatureVersion,
                donorId => dataRefreshHistoryRepository.UpdateLastSafelyProcessedDonor(refreshRecordId, donorId));

            var pGroupCount = await GetPGroupCountAtLocusAPositionOne(newDonor.DonorId);
            pGroupCount.Should().NotBeNull();
        }

        private static void AssertStoredDonorInfoMatchesOriginalDonorInfo(DonorInfo donorInfoActual, DonorInfo donorInfoExpected)
        {
            donorInfoActual.DonorId.Should().Be(donorInfoExpected.DonorId);
            donorInfoActual.DonorType.Should().Be(donorInfoExpected.DonorType);
            donorInfoActual.IsAvailableForSearch.Should().Be(donorInfoExpected.IsAvailableForSearch);
            donorInfoActual.HlaNames.Should().BeEquivalentTo(donorInfoExpected.HlaNames);
        }

        private async Task<int?> GetPGroupCountAtLocusAPositionOne(int donorId)
        {
            var pGroups = await inspectionRepo.GetPGroupsForDonors(new[] {donorId});

            return pGroups.First().PGroupNames.A.Position1?.Count();
        }
    }
}