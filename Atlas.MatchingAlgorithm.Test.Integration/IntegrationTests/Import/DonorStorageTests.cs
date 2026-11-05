using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Data.Services;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Repositories;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Utils;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Dapper;
using Microsoft.Data.SqlClient;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using SqlException = Microsoft.Data.SqlClient.SqlException;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Import
{
    [NonParallelizable]
    public class DonorStorageTests
    {
        private IDonorImportRepository donorImportRepository;
        private IDonorUpdateRepository donorUpdateRepository;
        private IDonorInspectionRepository inspectionRepo;
        private IHlaImportRepository hlaImportRepository;
        private TestDonorInspectionRepository testInspectionRepo;
        private IConnectionStringProvider dormantConnectionStringProvider;

        private readonly DonorInfoWithExpandedHla donorInfoWithAllelesAtThreeLoci = new DonorInfoWithExpandedHla
        {
            DonorType = DonorType.Cord,
            ExternalDonorCode = DonorIdGenerator.NewExternalCode,
            HlaNames = new PhenotypeInfo<string>
            (
                valueA: new LocusInfo<string>("01:02", "30:02"),
                valueB: new LocusInfo<string>("07:02", "08:01"),
                valueDrb1: new LocusInfo<string>("01:11", "03:41")
            ),
            MatchingHla = new PhenotypeInfo<INullHandledHlaMatchingMetadata>
            (
                valueA: new LocusInfo<INullHandledHlaMatchingMetadata>
                (
                    new TestHlaMetadata {LookupName = "01:02", MatchingPGroups = new List<string> {"01:01P", "01:02"}},
                    new TestHlaMetadata {LookupName = "30:02", MatchingPGroups = new List<string> {"01:01P", "30:02P"}}
                ),
                valueB: new LocusInfo<INullHandledHlaMatchingMetadata>
                (
                    new TestHlaMetadata {LookupName = "07:02", MatchingPGroups = new List<string> {"07:02P"}},
                    new TestHlaMetadata {LookupName = "08:01", MatchingPGroups = new List<string> {"08:01P"}}
                ),
                valueDrb1: new LocusInfo<INullHandledHlaMatchingMetadata>
                (
                    new TestHlaMetadata {LookupName = "01:11", MatchingPGroups = new List<string> {"01:11P"}},
                    new TestHlaMetadata {LookupName = "03:41", MatchingPGroups = new List<string> {"03:41P"}}
                )
            )
        };

        private readonly DonorInfoWithExpandedHla donorInfoWithXxCodesAtThreeLoci = new DonorInfoWithExpandedHla
        {
            DonorType = DonorType.Cord,
            ExternalDonorCode = DonorIdGenerator.NewExternalCode,
            HlaNames = new PhenotypeInfo<string>
            (
                valueA: new LocusInfo<string>("*01:XX", "30:XX"),
                valueB: new LocusInfo<string>("*07:XX", "08:XX"),
                valueDrb1: new LocusInfo<string>("*01:XX", "03:XX")
            ),
            MatchingHla = new PhenotypeInfo<INullHandledHlaMatchingMetadata>
            (
                valueA: new LocusInfo<INullHandledHlaMatchingMetadata>
                (
                    new TestHlaMetadata {LookupName = "*01:XX", MatchingPGroups = new List<string> {"01:01P", "01:02"}},
                    new TestHlaMetadata {LookupName = "30:XX", MatchingPGroups = new List<string> {"01:01P", "30:02P"}}
                ),
                valueB: new LocusInfo<INullHandledHlaMatchingMetadata>
                (
                    new TestHlaMetadata {LookupName = "*07:XX", MatchingPGroups = new List<string> {"07:02P"}},
                    new TestHlaMetadata {LookupName = "08:XX", MatchingPGroups = new List<string> {"08:01P"}}
                ),
                valueDrb1: new LocusInfo<INullHandledHlaMatchingMetadata>
                (
                    new TestHlaMetadata {LookupName = "*01:XX", MatchingPGroups = new List<string> {"01:11P"}},
                    new TestHlaMetadata {LookupName = "03:XX", MatchingPGroups = new List<string> {"03:41P"}}
                )
            )
        };

        [SetUp]
        public void ResolveSearchRepo()
        {
            var repositoryFactory = DependencyInjection.DependencyInjection.Provider.GetService<IDormantRepositoryFactory>();
            // By default donor update and import will happen on different databases - override this for these tests so the same database is used throughout
            donorImportRepository = repositoryFactory.GetDonorImportRepository();
            donorUpdateRepository = repositoryFactory.GetDonorUpdateRepository();
            inspectionRepo = repositoryFactory.GetDonorInspectionRepository();
            hlaImportRepository = repositoryFactory.GetHlaImportRepository();

            dormantConnectionStringProvider = DependencyInjection.DependencyInjection.Provider
                .GetService<DormantTransientSqlConnectionStringProvider>();
            testInspectionRepo = new TestDonorInspectionRepository(dormantConnectionStringProvider);
        }

        [Test]
        public async Task InsertBatchOfDonorsWithExpandedHla_ForDonorWithAlleles_InsertsDonorInfoCorrectly()
        {
            var donor = donorInfoWithAllelesAtThreeLoci;
            donor.DonorId = DonorIdGenerator.NextId();
            await donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(new[] {donor}, false);

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            AssertStoredDonorInfoMatchesOriginalDonorInfo(donor, result);
        }

        [Test]
        public void InsertBatchOfDonorsWithExpandedHla_ForNewDonor_ForDonorWithUntypedRequiredLocus_ThrowsException()
        {
            var donor = new DonorInfoWithTestHlaBuilder(DonorIdGenerator.NextId()).Build();

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(new[] {donor}, false));
        }

        [Test]
        public async Task InsertBatchOfDonorsWithExpandedHla_ForNewDonor_ForDonorWithUntypedOptionalLocus_InsertsUntypedLocusAsNull()
        {
            var donor = donorInfoWithAllelesAtThreeLoci;
            donor.DonorId = DonorIdGenerator.NextId();
            await donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(new[] {donor}, false);

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            result.HlaNames.C.Position1.Should().BeNull();
        }

        [Test]
        public async Task InsertBatchOfDonorsWithExpandedHla_ForDonorWithXXCodes_InsertsDonorInfoCorrectly()
        {
            var donor = donorInfoWithXxCodesAtThreeLoci;
            donor.DonorId = DonorIdGenerator.NextId();
            await donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(new[] {donor}, false);

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            AssertStoredDonorInfoMatchesOriginalDonorInfo(donor, result);
        }

        [Test]
        public async Task InsertBatchOfDonors_ForDonorWithAlleles_InsertsDonorInfoCorrectly()
        {
            var donor = donorInfoWithAllelesAtThreeLoci;
            donor.DonorId = DonorIdGenerator.NextId();
            await donorImportRepository.InsertBatchOfDonors(new List<DonorInfo> {donor});

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            AssertStoredDonorInfoMatchesOriginalDonorInfo(donorInfoWithAllelesAtThreeLoci, result);
        }

        [Test]
        public async Task InsertBatchOfDonors_ForDonorWithXXCodes_InsertsDonorInfoCorrectly()
        {
            var donor = donorInfoWithXxCodesAtThreeLoci;
            donor.DonorId = DonorIdGenerator.NextId();
            await donorImportRepository.InsertBatchOfDonors(new List<DonorInfo> {donor});

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            AssertStoredDonorInfoMatchesOriginalDonorInfo(donorInfoWithXxCodesAtThreeLoci, result);
        }

        [Test]
        public void InsertBatchOfDonors_ForDonorWithUntypedRequiredLocus_ThrowsException()
        {
            var donor = new DonorInfoBuilder()
                .WithHlaAtLocus(Locus.A, LocusPosition.One, null)
                .WithHlaAtLocus(Locus.A, LocusPosition.Two, null)
                .Build();

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await donorImportRepository.InsertBatchOfDonors(new List<DonorInfo> {donor}));
        }

        [Test]
        public async Task InsertBatchOfDonors_ForDonorWithUntypedOptionalLocus_InsertsUntypedLocusAsNull()
        {
            var donor = donorInfoWithXxCodesAtThreeLoci;
            donor.DonorId = DonorIdGenerator.NextId();
            donor.MatchingHla = donor.MatchingHla.SetPosition(Locus.C, LocusPosition.One, null);
            await donorImportRepository.InsertBatchOfDonors(new List<DonorInfo> {donor});

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            result.HlaNames.C.Position1.Should().BeNull();
        }

        [Test]
        public async Task UpdateDonorBatch_ForDonorWithAlleles_UpdatesDonorInfoCorrectly()
        {
            var donorInfo = new DonorInfoBuilder().Build();
            var donor = donorInfoWithAllelesAtThreeLoci;
            donor.DonorId = donorInfo.DonorId;
            await donorImportRepository.InsertBatchOfDonors(new List<DonorInfo> {donorInfo});
            await donorUpdateRepository.UpdateDonorBatch(new List<DonorInfoWithExpandedHla> {donor}, false);

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            AssertStoredDonorInfoMatchesOriginalDonorInfo(donorInfoWithAllelesAtThreeLoci, result);
        }

        [Test]
        public async Task UpdateDonorBatch_ForDonorWithXXCodes_UpdatesDonorInfoCorrectly()
        {
            var donorInfo = new DonorInfoBuilder().Build();
            var donor = donorInfoWithXxCodesAtThreeLoci;
            donor.DonorId = donorInfo.DonorId;
            await donorImportRepository.InsertBatchOfDonors(new List<DonorInfo> {donorInfo});
            await donorUpdateRepository.UpdateDonorBatch(new List<DonorInfoWithExpandedHla> {donor}, false);

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            AssertStoredDonorInfoMatchesOriginalDonorInfo(donorInfoWithXxCodesAtThreeLoci, result);
        }

        [Test]
        public async Task UpdateDonorBatch_WithUntypedRequiredLocus_ThrowsException()
        {
            // arbitrary hla at all loci, as the value of the hla does not matter for this test case
            var expandedHla = new TestHlaMetadata {LookupName = "01:02", MatchingPGroups = new List<string> {"01:01P", "01:02"}};
            var donor = new DonorInfoWithTestHlaBuilder(DonorIdGenerator.NextId())
                .WithHla(new PhenotypeInfo<INullHandledHlaMatchingMetadata>(expandedHla))
                .Build();
            await donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(new[] {donor}, false);

            donor.HlaNames = donor.HlaNames.SetPosition(Locus.A, LocusPosition.One, null);

            Assert.ThrowsAsync<SqlException>(async () =>
                await donorUpdateRepository.UpdateDonorBatch(new[] {donor}, false));
        }

        [Test]
        public async Task UpdateDonorBatch_WithUntypedOptionalLocus_UpdatesUntypedLocusAsNull()
        {
            // arbitrary hla at all loci, as the value of the hla does not matter for this test case
            var expandedHla = new TestHlaMetadata {LookupName = "01:02", MatchingPGroups = new List<string> {"01:01P", "01:02"}};
            var donor = new DonorInfoWithTestHlaBuilder(DonorIdGenerator.NextId())
                .WithHla(new PhenotypeInfo<INullHandledHlaMatchingMetadata>(expandedHla))
                .Build();
            await donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(new[] {donor}, false);

            donor.HlaNames = donor.HlaNames.SetPosition(Locus.Dqb1, LocusPosition.One, null);
            await donorUpdateRepository.UpdateDonorBatch(new[] {donor}, false);

            var result = await inspectionRepo.GetDonor(donor.DonorId);

            result.HlaNames.Dqb1.Position1.Should().BeNull();
        }

        private static void AssertStoredDonorInfoMatchesOriginalDonorInfo(DonorInfo expectedDonorInfo, DonorInfo actualDonorInfo)
        {
            actualDonorInfo.DonorId.Should().Be(expectedDonorInfo.DonorId);
            actualDonorInfo.DonorType.Should().Be(expectedDonorInfo.DonorType);
            actualDonorInfo.IsAvailableForSearch.Should().Be(expectedDonorInfo.IsAvailableForSearch);
            actualDonorInfo.HlaNames.Should().BeEquivalentTo(expectedDonorInfo.HlaNames);
        }

        #region Bulk write sessions

        // A session's bulk copies are reused across every write in it, and hold a connection open for its whole
        // length - so these cover that successive writes over one session all land, and that reuse is declined while a
        // transaction is ambient, which is the only part of this that would otherwise fail silently.

        [Test]
        public async Task InsertBatchOfDonors_WithinOneBulkWriteSession_InsertsEveryBatch()
        {
            var firstDonor = new DonorInfoBuilder().Build();
            var secondDonor = new DonorInfoBuilder().Build();

            using (donorImportRepository.OpenBulkWriteSession())
            {
                await donorImportRepository.InsertBatchOfDonors(new List<DonorInfo> {firstDonor});
                await donorImportRepository.InsertBatchOfDonors(new List<DonorInfo> {secondDonor});
            }

            (await inspectionRepo.GetDonor(firstDonor.DonorId)).Should().NotBeNull();
            (await inspectionRepo.GetDonor(secondDonor.DonorId)).Should().NotBeNull();
        }

        [Test]
        public async Task AddMatchingRelationsForExistingDonorBatch_WithinOneBulkWriteSession_InsertsRelationsForEveryBatch()
        {
            var firstDonor = BuildDonorWithRequiredHla();
            var secondDonor = BuildDonorWithRequiredHla();
            await donorImportRepository.InsertBatchOfDonors(new List<DonorInfo> {firstDonor, secondDonor});

            using (donorImportRepository.OpenBulkWriteSession())
            {
                await AddMatchingRelationsFor(firstDonor);
                await AddMatchingRelationsFor(secondDonor);
            }

            testInspectionRepo.GetMatchingHlaRowCount(Locus.A, firstDonor.DonorId).Should().Be(2);
            testInspectionRepo.GetMatchingHlaRowCount(Locus.A, secondDonor.DonorId).Should().Be(2);
        }

        [Test]
        public void OpenBulkWriteSession_WhenASessionIsAlreadyOpen_ThrowsException()
        {
            using (donorImportRepository.OpenBulkWriteSession())
            {
                Assert.Throws<InvalidOperationException>(() => donorImportRepository.OpenBulkWriteSession());
            }
        }

        [Test]
        public async Task InsertBatchOfDonors_AfterABulkWriteSessionIsClosed_StillInsertsDonors()
        {
            // The session's bulk copies, and the connections they held, are disposed with it; writes after it has to
            // fall back to building their own again.
            using (donorImportRepository.OpenBulkWriteSession())
            {
                await donorImportRepository.InsertBatchOfDonors(new List<DonorInfo> {new DonorInfoBuilder().Build()});
            }

            var donorAfterSession = new DonorInfoBuilder().Build();
            await donorImportRepository.InsertBatchOfDonors(new List<DonorInfo> {donorAfterSession});

            (await inspectionRepo.GetDonor(donorAfterSession.DonorId)).Should().NotBeNull();
        }

        [Test]
        public async Task InsertBatchOfDonors_WithinOneBulkWriteSession_UnderATransactionThatRollsBack_WritesNothing()
        {
            // A bulk copy enlists in the ambient transaction once, when its connection is opened, and takes part in no
            // scope after that - so a session's bulk copy, whose connection an earlier write already opened, would
            // write straight past this rollback. Declining to reuse one while a transaction is ambient is what makes
            // the second write below undoable, and this is the only test that fails if that decision is removed.
            //
            // Donors, rather than the matching HLA tables, because this write has to put exactly one connection into
            // the transaction: AddMatchingRelationsForExistingDonorBatch writes at all five matching loci whether or
            // not a donor has HLA at them, and five connections in one transaction would promote it to a distributed
            // transaction, which is unsupported off Windows.
            var warmUpDonor = new DonorInfoBuilder().Build();
            var rolledBackDonor = new DonorInfoBuilder().Build();

            using (donorImportRepository.OpenBulkWriteSession())
            {
                // Written with no transaction ambient, so the session's bulk copy for Donors opens its connection here.
                await donorImportRepository.InsertBatchOfDonors(new List<DonorInfo> {warmUpDonor});

                using (new AsyncTransactionScope())
                {
                    await donorImportRepository.InsertBatchOfDonors(new List<DonorInfo> {rolledBackDonor});
                    // Deliberately not completed, so disposing the scope rolls this write back.
                }
            }

            (await inspectionRepo.GetDonor(warmUpDonor.DonorId)).Should().NotBeNull();
            (await inspectionRepo.GetDonor(rolledBackDonor.DonorId)).Should().BeNull();
        }

        [Test]
        public async Task InsertBatchOfDonors_WithinOneBulkWriteSession_TracesHowManyWritesReusedABulkCopy()
        {
            // Guards the count itself: a trace that always reported zero would satisfy the unit test covering a session
            // that reused nothing, and would then be silently useless for the case it exists to reveal.
            var logger = Substitute.For<IAtlasLogger>();
            var repository = new DonorImportRepository(
                Substitute.For<IHlaNamesRepository>(), dormantConnectionStringProvider, logger);

            using (repository.OpenBulkWriteSession())
            {
                await repository.InsertBatchOfDonors(new List<DonorInfo> {new DonorInfoBuilder().Build()});
                await repository.InsertBatchOfDonors(new List<DonorInfo> {new DonorInfoBuilder().Build()});
            }

            logger.Received(1).SendTrace(
                Arg.Any<string>(),
                Arg.Any<LogLevel>(),
                Arg.Is<Dictionary<string, string>>(p => p["ReusedWrites"] == "2"));
        }

        #endregion

        #region Retry after a dropped connection (ATL-524)

        // These tests drop a session bulk copy's connection for real, with KILL, between two writes. The write must be
        // retried on a new connection, and the session must keep working after it.
        //
        // SqlClient reconnects by itself a connection that broke while idle (ConnectRetryCount, 1 by default). It cannot
        // do that for a connection that breaks during a write, which is what happened on AN LIVE. So these tests turn
        // that reconnection off: the next write then fails with a transport-level error of severity 20, as on AN LIVE.

        [TearDown]
        public void ClearConnectionPools()
        {
            // A KILL breaks pooled connections too. Clear the pools so that no later test gets a broken one.
            SqlConnection.ClearAllPools();
        }

        [Test]
        public async Task InsertBatchOfDonors_WithinOneBulkWriteSession_WhenTheConnectionIsDropped_RetriesOnANewConnection()
        {
            var logger = Substitute.For<IAtlasLogger>();
            var repository = BuildRepositoryThatDoesNotReconnect(logger);
            var donorBeforeDrop = new DonorInfoBuilder().Build();
            var donorAfterDrop = new DonorInfoBuilder().Build();
            var donorAfterRetry = new DonorInfoBuilder().Build();

            using (repository.OpenBulkWriteSession())
            {
                await repository.InsertBatchOfDonors(new List<DonorInfo> {donorBeforeDrop});

                (await KillOtherConnectionsToTheTestDatabase()).Should().BePositive();

                await repository.InsertBatchOfDonors(new List<DonorInfo> {donorAfterDrop});
                // The session's new bulk copy is used again, with no further error.
                await repository.InsertBatchOfDonors(new List<DonorInfo> {donorAfterRetry});
            }

            (await inspectionRepo.GetDonor(donorBeforeDrop.DonorId)).Should().NotBeNull();
            (await inspectionRepo.GetDonor(donorAfterDrop.DonorId)).Should().NotBeNull();
            (await inspectionRepo.GetDonor(donorAfterRetry.DonorId)).Should().NotBeNull();
            logger.Received(1).SendTrace(
                Arg.Is<string>(m => m.Contains("transient SQL error")),
                LogLevel.Warn,
                Arg.Is<Dictionary<string, string>>(p => p["Table"] == "Donors" && p["FailedAttempt"] == "1"));
        }

        [Test]
        public async Task AddMatchingRelationsForExistingDonorBatch_WithinOneBulkWriteSession_WhenTheConnectionsAreDropped_RetriesWithoutDuplicateRows()
        {
            var logger = Substitute.For<IAtlasLogger>();
            var repository = BuildRepositoryThatDoesNotReconnect(logger);
            var donorBeforeDrop = BuildDonorWithRequiredHla();
            var donorAfterDrop = BuildDonorWithRequiredHla();
            await repository.InsertBatchOfDonors(new List<DonorInfo> {donorBeforeDrop, donorAfterDrop});

            using (repository.OpenBulkWriteSession())
            {
                await AddMatchingRelationsFor(donorBeforeDrop, repository);

                (await KillOtherConnectionsToTheTestDatabase()).Should().BePositive();

                // Writes to all five matching loci at the same time, and every one of their connections was killed.
                await AddMatchingRelationsFor(donorAfterDrop, repository);
            }

            testInspectionRepo.GetMatchingHlaRowCount(Locus.A, donorBeforeDrop.DonorId).Should().Be(2);
            testInspectionRepo.GetMatchingHlaRowCount(Locus.A, donorAfterDrop.DonorId).Should().Be(2);
            logger.Received().SendTrace(
                Arg.Is<string>(m => m.Contains("transient SQL error")),
                LogLevel.Warn,
                Arg.Is<Dictionary<string, string>>(p => p["Table"] == MatchingHla.TableName(Locus.A)));
        }

        private DonorImportRepository BuildRepositoryThatDoesNotReconnect(IAtlasLogger logger)
        {
            var connectionString = new SqlConnectionStringBuilder(dormantConnectionStringProvider.GetConnectionString())
            {
                ConnectRetryCount = 0
            }.ConnectionString;

            var connectionStringProvider = Substitute.For<IConnectionStringProvider>();
            connectionStringProvider.GetConnectionString().Returns(connectionString);

            return new DonorImportRepository(Substitute.For<IHlaNamesRepository>(), connectionStringProvider, logger);
        }

        /// <summary>
        /// Kills every other user connection to the test database. The open session's bulk copies hold their
        /// connections open, so these are always among them. The SQL text of a connection cannot pick them out, because
        /// the test connection strings use MARS, which hides it. Integration tests do not run in parallel, so no other
        /// test is using these connections.
        /// </summary>
        /// <returns>The number of connections killed.</returns>
        private async Task<int> KillOtherConnectionsToTheTestDatabase()
        {
            int killed;
            await using (var connection = new SqlConnection(dormantConnectionStringProvider.GetConnectionString()))
            {
                await connection.OpenAsync();

                var sessionIds = (await connection.QueryAsync<int>(@"
                    SELECT session_id
                    FROM sys.dm_exec_sessions
                    WHERE database_id = DB_ID() AND session_id <> @@SPID AND is_user_process = 1")).ToList();

                foreach (var sessionId in sessionIds)
                {
                    await connection.ExecuteAsync($"KILL {sessionId}");
                }

                killed = sessionIds.Count;
            }

            // The idle pooled connections were killed too. Clear the pools so that the reads that follow this get new
            // ones. A connection that a bulk copy holds open is not in the pool, so this does not touch it.
            SqlConnection.ClearAllPools();

            return killed;
        }

        #endregion

        private static DonorInfoWithExpandedHla BuildDonorWithRequiredHla() =>
            new DonorInfoWithTestHlaBuilder(DonorIdGenerator.NextId())
                .WithDefaultRequiredHla(new TestHlaMetadata
                {
                    LookupName = "01:01",
                    MatchingPGroups = new List<string> {"01:01P"}
                })
                .Build();

        /// <summary>
        /// Imports a donor's HLA and then writes its matching relations, which is what the HLA processing stage of the
        /// data refresh does for every batch.
        /// </summary>
        private async Task AddMatchingRelationsFor(DonorInfoWithExpandedHla donor, IDonorImportRepository repository = null)
        {
            var hlaLookup = await hlaImportRepository.ImportHla(new List<DonorInfoWithExpandedHla> {donor});
            var donorEntry = donor.ToDonorInfoForPreProcessing(hla => hlaLookup[hla]);

            await (repository ?? donorImportRepository).AddMatchingRelationsForExistingDonorBatch(
                new List<DonorInfoForHlaPreProcessing> {donorEntry},
                false);
        }

    }
}