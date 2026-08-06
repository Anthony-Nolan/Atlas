using Atlas.Common.ApplicationInsights;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh.HlaProcessing;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using AutoFixture;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh
{
    /// <summary>
    /// Covers the stage-50 batch loop, which is driven by an explicit async enumerator rather than an
    /// <c>await foreach</c> so that the paged donor read - which happens on <c>MoveNextAsync</c>, and was previously
    /// stage 50's largest unmeasured slice - can be timed. That conversion carries two things worth pinning: the read
    /// must be timed once per <c>MoveNextAsync</c> including the final exhausted one, and the paging enumerator's
    /// end-of-stream signal (a final EMPTY batch) must still be skipped rather than processed.
    /// </summary>
    [TestFixture]
    public class HlaProcessorTests
    {
        private const string HlaNomenclatureVersion = "3650";

        private IHlaProcessor hlaProcessor;

        private IDataRefreshRepository dataRefreshRepository;
        private IDonorImportRepository donorImportRepository;
        private IHlaImportRepository hlaImportRepository;
        private IPGroupRepository pGroupRepository;
        private IDonorHlaExpander donorHlaExpander;
        private IMatchingAlgorithmImportLogger logger;
        private Fixture fixture;

        [SetUp]
        public void SetUp()
        {
            fixture = new Fixture();

            dataRefreshRepository = Substitute.For<IDataRefreshRepository>();
            donorImportRepository = Substitute.For<IDonorImportRepository>();
            hlaImportRepository = Substitute.For<IHlaImportRepository>();
            pGroupRepository = Substitute.For<IPGroupRepository>();

            var repositoryFactory = Substitute.For<IDormantRepositoryFactory>();
            repositoryFactory.GetDataRefreshRepository().Returns(dataRefreshRepository);
            repositoryFactory.GetDonorImportRepository().Returns(donorImportRepository);
            repositoryFactory.GetHlaImportRepository().Returns(hlaImportRepository);
            repositoryFactory.GetPGroupRepository().Returns(pGroupRepository);

            donorHlaExpander = Substitute.For<IDonorHlaExpander>();
            donorHlaExpander.ExpandDonorHlaBatchAsync(Arg.Any<IEnumerable<DonorInfo>>(), Arg.Any<string>())
                .Returns(new DonorBatchProcessingResult<DonorInfoWithExpandedHla>());

            var donorHlaExpanderFactory = Substitute.For<IDonorHlaExpanderFactory>();
            donorHlaExpanderFactory.BuildForSpecifiedHlaNomenclatureVersion(Arg.Any<string>()).Returns(donorHlaExpander);

            hlaImportRepository.ImportHla(Arg.Any<IList<DonorInfoWithExpandedHla>>())
                .Returns(new Dictionary<string, int>());

            logger = Substitute.For<IMatchingAlgorithmImportLogger>();

            hlaProcessor = new HlaProcessor(
                logger,
                donorHlaExpanderFactory,
                Substitute.For<IHlaMetadataDictionaryFactory>(),
                Substitute.For<IFailedDonorsNotificationSender>(),
                repositoryFactory,
                DataRefreshSettingsBuilder.New.Build(),
                Substitute.For<IMacDictionary>());
        }

        [Test]
        public async Task UpdateDonorHla_TimesTheDonorBatchReadOncePerBatchPlusOnceForTheExhaustedRead()
        {
            StubDonorBatches(DonorBatch(), DonorBatch());

            await ProcessAllDonors();

            // The read is timed on the enumerator's MoveNextAsync, so two batches cost three calls: one per batch,
            // plus the one that reports the stream is exhausted.
            logger.Received(3).SendMetric(
                DataRefreshMetrics.DurationMsMetric,
                Arg.Any<double>(),
                Arg.Is<Dictionary<string, string>>(d => IsOperation(d, DataRefreshMetrics.Operation_HlaDonorBatchRead)));
        }

        [Test]
        public async Task UpdateDonorHla_WhenTheStreamEndsWithAnEmptyBatch_SkipsItAndProcessesEveryOtherBatch()
        {
            // How NewOrderedDonorBatchesToImport actually signals exhaustion: it yields one final empty batch before
            // its loop ends. Processing that batch would throw on `donorBatch.Last()`, so this is a real invariant and
            // not a defensive nicety.
            StubDonorBatches(DonorBatch(), DonorBatch(), new List<DonorInfo>());

            await ProcessAllDonors();

            await donorImportRepository.Received(2).AddMatchingRelationsForExistingDonorBatch(
                Arg.Any<IEnumerable<DonorInfoForHlaPreProcessing>>(),
                Arg.Any<bool>());
        }

        [Test]
        public async Task UpdateDonorHla_WhenThereAreNoDonors_ProcessesNothingAndStillTimesTheRead()
        {
            StubDonorBatches();

            await ProcessAllDonors();

            await donorImportRepository.DidNotReceiveWithAnyArgs().AddMatchingRelationsForExistingDonorBatch(default, default);
            logger.Received(1).SendMetric(
                DataRefreshMetrics.DurationMsMetric,
                Arg.Any<double>(),
                Arg.Is<Dictionary<string, string>>(d => IsOperation(d, DataRefreshMetrics.Operation_HlaDonorBatchRead)));
        }

        [Test]
        public async Task UpdateDonorHla_CountsTheDonorsInEachBatch()
        {
            const int donorCount = 4;
            StubDonorBatches(DonorBatch(donorCount));

            await ProcessAllDonors();

            logger.Received(1).SendMetric(
                DataRefreshMetrics.CountMetric,
                donorCount,
                Arg.Is<Dictionary<string, string>>(d => IsOperation(d, DataRefreshMetrics.Operation_DonorsPerHlaBatch)));
        }

        private Task ProcessAllDonors() =>
            hlaProcessor.UpdateDonorHla(HlaNomenclatureVersion, _ => Task.CompletedTask);

        private List<DonorInfo> DonorBatch(int donorCount = 2) => fixture.CreateMany<DonorInfo>(donorCount).ToList();

        private void StubDonorBatches(params List<DonorInfo>[] batches) =>
            dataRefreshRepository.NewOrderedDonorBatchesToImport(Arg.Any<int>(), Arg.Any<int?>())
                .Returns(AsAsyncEnumerable(batches));

        private static async IAsyncEnumerable<List<DonorInfo>> AsAsyncEnumerable(IEnumerable<List<DonorInfo>> batches)
        {
            foreach (var batch in batches)
            {
                yield return batch;
            }

            await Task.CompletedTask;
        }

        private static bool IsOperation(Dictionary<string, string> dimensions, string operation) =>
            dimensions[DataRefreshMetrics.OperationDimension] == operation;
    }
}
