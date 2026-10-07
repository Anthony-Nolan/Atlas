using System;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Models.AzureManagement;
using Atlas.MatchingAlgorithm.Services.AzureManagement;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh;
using Atlas.MatchingAlgorithm.Services.DataRefresh.DonorImport;
using Atlas.MatchingAlgorithm.Services.DataRefresh.HlaProcessing;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Notifications;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Atlas.MatchingAlgorithm.Services.DonorManagement;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using AwesomeAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh.Runner
{
    [TestFixture]
    public partial class DataRefreshRunnerTests
    {
        private const string LatestHlaVersion = "latestHlaVersion";
        private static readonly DateTime SnapshotUtc = new(2026, 10, 5, 15, 4, 7, 123, DateTimeKind.Utc);

        private IActiveDatabaseProvider activeDatabaseProvider;
        private IAzureDatabaseManager azureDatabaseManager;
        private IDonorImportRepository donorImportRepository;
        private IHlaMetadataDictionary hlaMetadataDictionary;
        private IDonorImporter donorImporter;
        private IHlaProcessor hlaProcessor;
        private IDonorGenotypePrecomputationStage donorGenotypePrecomputationStage;
        private IDonorUpdateProcessor donorUpdateProcessor;
        private IDataRefreshSupportNotificationSender dataRefreshNotificationSender;
        private IDataRefreshHistoryRepository dataRefreshHistoryRepository;

        private IDataRefreshRunner dataRefreshRunner;
        private IMatchingAlgorithmImportLogger logger;
        private IDormantRepositoryFactory transientRepositoryFactory;

        [SetUp]
        public void SetUp()
        {
            activeDatabaseProvider = Substitute.For<IActiveDatabaseProvider>();
            azureDatabaseManager = Substitute.For<IAzureDatabaseManager>();
            donorImportRepository = Substitute.For<IDonorImportRepository>();
            transientRepositoryFactory = Substitute.For<IDormantRepositoryFactory>();
            hlaMetadataDictionary = Substitute.For<IHlaMetadataDictionary>();
            donorImporter = Substitute.For<IDonorImporter>();
            hlaProcessor = Substitute.For<IHlaProcessor>();
            donorGenotypePrecomputationStage = Substitute.For<IDonorGenotypePrecomputationStage>();
            donorUpdateProcessor = Substitute.For<IDonorUpdateProcessor>();
            logger = Substitute.For<IMatchingAlgorithmImportLogger>();
            dataRefreshNotificationSender = Substitute.For<IDataRefreshSupportNotificationSender>();
            dataRefreshHistoryRepository = Substitute.For<IDataRefreshHistoryRepository>();

            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(DataRefreshRecordBuilder.New.Build());
            transientRepositoryFactory.GetDonorImportRepository().Returns(donorImportRepository);

            hlaMetadataDictionary.GetLatestStableHlaNomenclatureVersion().Returns(LatestHlaVersion);
            hlaMetadataDictionary.RecreateHlaMetadataDictionary(default).ReturnsForAnyArgs(call =>
                new HlaMetadataDictionaryRecreationResult(call.Arg<CreationBehaviour>()?.SpecificVersion, SnapshotUtc));

            dataRefreshRunner = BuildDataRefreshRunner();
        }

        [Test]
        public async Task RefreshData_RemovesAllDonorInformation()
        {
            await dataRefreshRunner.RefreshData(default);

            await donorImportRepository.Received().RemoveAllDonorInformation();
        }

        [Test]
        public async Task RefreshData_ReportsHlaMetadataWasRecreated_WithRefreshHlaNomenclatureVersion()
        {
            const string hlaNomenclatureVersion = "3390";
            hlaMetadataDictionary.GetLatestStableHlaNomenclatureVersion().Returns(hlaNomenclatureVersion);

            var returnedHlaVersion = await dataRefreshRunner.RefreshData(default);

            returnedHlaVersion.Should().Be(hlaNomenclatureVersion);
        }

        [Test]
        public async Task RefreshData_ImportsDonors()
        {
            await dataRefreshRunner.RefreshData(default);

            await donorImporter.Received().ImportDonors();
        }

        [Test]
        public async Task RefreshData_WhenRunningMetadataDictionaryStep_RecordsLatestVersion()
        {
            await dataRefreshRunner.RefreshData(default);

            await dataRefreshHistoryRepository.Received().UpdateExecutionDetails(Arg.Any<int>(), LatestHlaVersion);
        }

        [Test]
        public async Task RefreshData_WhenActiveVersionMatchesLatest_RecreatesHlaMetadataDictionaryWithForce()
        {
            // A new record with no HLA version is what a request with ForceDataRefresh = true creates when the version has not changed.
            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(DataRefreshRecordBuilder.New.Build());
            hlaMetadataDictionary.IsActiveVersionDifferentFromLatestVersion().Returns(false);

            await dataRefreshRunner.RefreshData(default);

            await hlaMetadataDictionary.Received().RecreateHlaMetadataDictionary(
                Arg.Is<CreationBehaviour>(b => b.ShouldForce && b.SpecificVersion == LatestHlaVersion));
        }

        [Test]
        public async Task RefreshData_WhenRunningFromScratch_PassesRefreshHlaVersionToLaterSteps()
        {
            const string hlaNomenclatureVersion = "3390";
            hlaMetadataDictionary.GetLatestStableHlaNomenclatureVersion().Returns(hlaNomenclatureVersion);

            await dataRefreshRunner.RefreshData(default);

            await hlaProcessor.Received().UpdateDonorHla(hlaNomenclatureVersion, Arg.Any<Func<int, Task>>());
        }

        [Test]
        public async Task RefreshData_WhenRunningFromScratch_RecordsVersionBeforeRecreatingDictionary()
        {
            var calls = new List<string>();
            dataRefreshHistoryRepository
                .When(r => r.UpdateExecutionDetails(Arg.Any<int>(), LatestHlaVersion, Arg.Any<DateTime?>()))
                .Do(_ => calls.Add("record version"));
            hlaMetadataDictionary
                .When(d => d.RecreateHlaMetadataDictionary(Arg.Any<CreationBehaviour>()))
                .Do(_ => calls.Add("recreate dictionary"));

            await dataRefreshRunner.RefreshData(default);

            calls.Should().Equal("record version", "recreate dictionary");
        }

        [Test]
        public async Task RefreshData_WhenRunningFromScratch_RecreatesDictionaryAtLatestVersion()
        {
            await dataRefreshRunner.RefreshData(default);

            await hlaMetadataDictionary.Received(1).RecreateHlaMetadataDictionary(
                Arg.Is<CreationBehaviour>(b => b.CreationMode == CreationBehaviour.Mode.Specific && b.SpecificVersion == LatestHlaVersion));
        }

        [Test]
        public async Task RefreshData_WhenRunningFromScratch_RecordsDictionarySnapshot()
        {
            await dataRefreshRunner.RefreshData(default);

            await dataRefreshHistoryRepository.Received(1).MarkHlaMetadataDictionaryRefreshAsComplete(Arg.Any<DataRefreshRecord>(), SnapshotUtc);
        }

        [Test]
        public async Task RefreshData_WhenVersionRecordedButDictionaryStageIncomplete_RecreatesAtRecordedVersionWithoutReadingLatest()
        {
            const string recordedVersion = "recordedHlaVersion";
            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(
                DataRefreshRecordBuilder.New.With(r => r.HlaNomenclatureVersion, recordedVersion).Build());

            await dataRefreshRunner.RefreshData(default);

            hlaMetadataDictionary.DidNotReceive().GetLatestStableHlaNomenclatureVersion();
            await dataRefreshHistoryRepository.DidNotReceiveWithAnyArgs().UpdateExecutionDetails(default, default, default);
            await hlaMetadataDictionary.Received(1).RecreateHlaMetadataDictionary(
                Arg.Is<CreationBehaviour>(b => b.CreationMode == CreationBehaviour.Mode.Specific && b.SpecificVersion == recordedVersion));
            await dataRefreshHistoryRepository.Received(1).MarkHlaMetadataDictionaryRefreshAsComplete(Arg.Any<DataRefreshRecord>(), SnapshotUtc);
            await hlaProcessor.Received().UpdateDonorHla(recordedVersion, Arg.Any<Func<int, Task>>());
        }

        [Test]
        public async Task RefreshData_WhenDictionaryStageComplete_DoesNotRecreateDictionaryOrRecordSnapshot()
        {
            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(
                DataRefreshRecordBuilder.New.WithStageCompleted(DataRefreshStage.MetadataDictionaryRefresh).Build());

            await dataRefreshRunner.RefreshData(default);

            hlaMetadataDictionary.DidNotReceive().GetLatestStableHlaNomenclatureVersion();
            await hlaMetadataDictionary.DidNotReceiveWithAnyArgs().RecreateHlaMetadataDictionary(default);
            await dataRefreshHistoryRepository.DidNotReceiveWithAnyArgs().MarkHlaMetadataDictionaryRefreshAsComplete(default, default);
        }

        [Test]
        public async Task RefreshData_WhenDictionaryWasNotRecreated_ThrowsWithoutCompletingStage()
        {
            hlaMetadataDictionary.RecreateHlaMetadataDictionary(default)
                .ReturnsForAnyArgs(new HlaMetadataDictionaryRecreationResult(LatestHlaVersion, null));

            await dataRefreshRunner.Invoking(r => r.RefreshData(default)).Should().ThrowAsync<InvalidOperationException>();

            await dataRefreshHistoryRepository.DidNotReceiveWithAnyArgs().MarkHlaMetadataDictionaryRefreshAsComplete(default, default);
        }

        [Test]
        public async Task RefreshData_RunsTheDonorGenotypePrecomputationAfterIndexRecreationAndBeforeScalingTearDown()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.ActiveDatabaseSize, AzureDatabaseSize.S4.ToString())
                .Build();
            dataRefreshRunner = BuildDataRefreshRunner(settings);

            await dataRefreshRunner.RefreshData(default);

            Received.InOrder(() =>
            {
                donorImportRepository.CreateHlaTableIndexes();
                donorGenotypePrecomputationStage.Run(Arg.Any<DataRefreshRecord>(), Arg.Any<DataRefreshStageExecutionMode>(), Arg.Any<CancellationToken>());
                azureDatabaseManager.UpdateDatabaseSize(Arg.Any<string>(), AzureDatabaseSize.S4, Arg.Any<int?>());
            });
        }

        [Test]
        public async Task RefreshData_RunsTheDonorGenotypePrecomputationForTheRecord_WithTheLeaseToken_AndMarksItComplete()
        {
            var record = DataRefreshRecordBuilder.New.Build();
            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(record);
            using var cancellationTokenSource = new CancellationTokenSource();

            await dataRefreshRunner.RefreshData(record.Id, cancellationTokenSource.Token);

            await donorGenotypePrecomputationStage.Received(1)
                .Run(record, DataRefreshStageExecutionMode.FromScratch, cancellationTokenSource.Token);
            await dataRefreshHistoryRepository.Received(1).MarkStageAsComplete(record, DataRefreshStage.DonorGenotypePrecomputation);
        }

        [Test]
        public async Task RefreshData_WhenTheDonorGenotypePrecomputationFails_DoesNotMarkItCompleteAndScalesTheDatabaseToDormantSize()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DormantDatabaseSize, AzureDatabaseSize.S0.ToString())
                .Build();
            dataRefreshRunner = BuildDataRefreshRunner(settings);
            donorGenotypePrecomputationStage.Run(default, default, default).ThrowsAsyncForAnyArgs(new InvalidOperationException());

            var act = () => dataRefreshRunner.RefreshData(default);

            await act.Should().ThrowAsync<InvalidOperationException>();
            await dataRefreshHistoryRepository.DidNotReceive()
                .MarkStageAsComplete(Arg.Any<DataRefreshRecord>(), DataRefreshStage.DonorGenotypePrecomputation);
            await azureDatabaseManager.Received().UpdateDatabaseSize(Arg.Any<string>(), AzureDatabaseSize.S0, Arg.Any<int?>());
        }

        [Test]
        public async Task RefreshData_WhenTeardownFails_SendsAlert()
        {
            const AzureDatabaseSize databaseSize = AzureDatabaseSize.S0;
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DatabaseAName, "db-a")
                .With(s => s.DormantDatabaseSize, databaseSize.ToString())
                .Build();

            dataRefreshRunner = BuildDataRefreshRunner(settings);

            activeDatabaseProvider.GetDormantDatabase().Returns(TransientDatabase.DatabaseA);
            hlaProcessor.UpdateDonorHla(default, default).ThrowsForAnyArgs(new Exception());
            azureDatabaseManager.UpdateDatabaseSize(Arg.Any<string>(), databaseSize, Arg.Any<int?>()).Throws(new Exception());

            try
            {
                await dataRefreshRunner.RefreshData(default);
            }
            catch (Exception)
            {
                await dataRefreshNotificationSender.ReceivedWithAnyArgs().SendTeardownFailureAlert(default);
            }
        }

        private IDataRefreshRunner BuildDataRefreshRunner(DataRefreshSettings dataRefreshSettings = null)
        {
            var settings = dataRefreshSettings ?? DataRefreshSettingsBuilder.New.Build();
            return new DataRefreshRunner(
                settings,
                activeDatabaseProvider,
                new AzureDatabaseNameProvider(settings),
                azureDatabaseManager,
                transientRepositoryFactory,
                new HlaMetadataDictionaryBuilder().Returning(hlaMetadataDictionary),
                Substitute.For<IActiveHlaNomenclatureVersionAccessor>(),
                donorImporter,
                hlaProcessor,
                donorGenotypePrecomputationStage,
                donorUpdateProcessor,
                logger,
                dataRefreshNotificationSender,
                dataRefreshHistoryRepository,
                new MatchingAlgorithmImportLoggingContext()
            );
        }
    }
}