using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh
{
    /// <summary>
    /// Covers the cancellation behaviour of the HLA processing stage. The refresh aborts by cancelling a token when it
    /// loses its run-level lease, and that abort is only safe if it lands on a batch boundary - stopping mid-batch would
    /// leave the last-safely-processed donor marker ahead of what was actually written.
    /// </summary>
    [TestFixture]
    public class HlaProcessorTests
    {
        private const string HlaNomenclatureVersion = "version";
        private const int BatchSize = 2000;

        private IDataRefreshRepository dataRefreshRepository;
        private IDonorImportRepository donorImportRepository;
        private IHlaImportRepository hlaImportRepository;
        private IDonorHlaExpander donorHlaExpander;

        private IHlaProcessor hlaProcessor;

        [SetUp]
        public void SetUp()
        {
            dataRefreshRepository = Substitute.For<IDataRefreshRepository>();
            donorImportRepository = Substitute.For<IDonorImportRepository>();
            hlaImportRepository = Substitute.For<IHlaImportRepository>();
            hlaImportRepository.ImportHla(default).ReturnsForAnyArgs(new Dictionary<string, int>());

            var repositoryFactory = Substitute.For<IDormantRepositoryFactory>();
            repositoryFactory.GetDataRefreshRepository().Returns(dataRefreshRepository);
            repositoryFactory.GetDonorImportRepository().Returns(donorImportRepository);
            repositoryFactory.GetHlaImportRepository().Returns(hlaImportRepository);
            repositoryFactory.GetPGroupRepository().Returns(Substitute.For<IPGroupRepository>());
            repositoryFactory.GetHlaNamesRepository().Returns(Substitute.For<IHlaNamesRepository>());

            donorHlaExpander = Substitute.For<IDonorHlaExpander>();
            donorHlaExpander.ExpandDonorHlaBatchAsync(default, default)
                .ReturnsForAnyArgs(new DonorBatchProcessingResult<DonorInfoWithExpandedHla>());
            var donorHlaExpanderFactory = Substitute.For<IDonorHlaExpanderFactory>();
            donorHlaExpanderFactory.BuildForSpecifiedHlaNomenclatureVersion(default).ReturnsForAnyArgs(donorHlaExpander);

            hlaProcessor = new HlaProcessor(
                Substitute.For<IMatchingAlgorithmImportLogger>(),
                donorHlaExpanderFactory,
                Substitute.For<IHlaMetadataDictionaryFactory>(),
                Substitute.For<IFailedDonorsNotificationSender>(),
                repositoryFactory,
                DataRefreshSettingsBuilder.New.Build(),
                Substitute.For<IMacDictionary>());
        }

        [Test]
        public async Task UpdateDonorHla_WhenCancelledMidProcessing_StopsOnABatchBoundary()
        {
            // Cancelled while the first batch is being written, with four batches available. The check sits at the top
            // of the loop, so the first batch finishes and the second is never started.
            var cancellationTokenSource = new CancellationTokenSource();
            GivenDonorBatches(4);
            donorImportRepository.WhenForAnyArgs(r => r.AddMatchingRelationsForExistingDonorBatch(default, default, default))
                .Do(_ => cancellationTokenSource.Cancel());

            await hlaProcessor.Invoking(p => p.UpdateDonorHla(
                    HlaNomenclatureVersion, _ => Task.CompletedTask, null, false, cancellationTokenSource.Token))
                .Should().ThrowAsync<OperationCanceledException>();

            await donorImportRepository.ReceivedWithAnyArgs(1).AddMatchingRelationsForExistingDonorBatch(default, default, default);
        }

        [Test]
        public async Task UpdateDonorHla_WhenCancelledMidProcessing_DoesNotAdvanceLastSafelyProcessedDonor()
        {
            // The marker is what a continued run resumes from. Advancing it for a batch that was never written would
            // silently skip those donors on the next attempt.
            var cancellationTokenSource = new CancellationTokenSource();
            GivenDonorBatches(4);
            donorImportRepository.WhenForAnyArgs(r => r.AddMatchingRelationsForExistingDonorBatch(default, default, default))
                .Do(_ => cancellationTokenSource.Cancel());
            var recordedDonorIds = new List<int>();

            await hlaProcessor.Invoking(p => p.UpdateDonorHla(
                    HlaNomenclatureVersion,
                    donorId =>
                    {
                        recordedDonorIds.Add(donorId);
                        return Task.CompletedTask;
                    },
                    null,
                    false,
                    cancellationTokenSource.Token))
                .Should().ThrowAsync<OperationCanceledException>();

            recordedDonorIds.Should().BeEmpty();
        }

        [Test]
        public async Task UpdateDonorHla_WhenNotCancelled_ProcessesEveryBatch()
        {
            // Guards the tests above: without this, they would still pass if the batches were never produced at all.
            GivenDonorBatches(4);

            await hlaProcessor.UpdateDonorHla(HlaNomenclatureVersion, _ => Task.CompletedTask);

            await donorImportRepository.ReceivedWithAnyArgs(4).AddMatchingRelationsForExistingDonorBatch(default, default, default);
        }

        private void GivenDonorBatches(int batchCount)
        {
            dataRefreshRepository.NewOrderedDonorBatchesToImport(default, default).ReturnsForAnyArgs(
                Enumerable.Range(0, batchCount).Select(BuildBatch).ToAsyncEnumerable());
        }

        private static List<DonorInfo> BuildBatch(int batchIndex) =>
            Enumerable.Range(batchIndex * BatchSize, BatchSize).Select(id => new DonorInfo {DonorId = id}).ToList();
    }
}
