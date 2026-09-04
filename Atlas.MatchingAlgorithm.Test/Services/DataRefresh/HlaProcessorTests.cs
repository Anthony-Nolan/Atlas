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
    /// Covers the cancellation and prefetch behaviour of the HLA processing stage. The stage reads pages of donors
    /// ahead of the processing side, so the two properties that matter are that the resume checkpoint tracks what has
    /// been *written* rather than what has been read, and that the refresh aborts on a batch boundary when it loses its
    /// run-level lease - stopping mid-batch, or checkpointing a merely-prefetched page, would leave the
    /// last-safely-processed donor marker ahead of what was actually written, silently skipping donors on resume.
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
        private IMatchingAlgorithmImportLogger logger;

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

        [Test]
        public async Task UpdateDonorHla_ReadsAheadOfProcessing()
        {
            // The point of the pipeline. The first batch's write does not complete until the read side has pulled
            // further pages; serial code could never satisfy that, because it does not read again until the write has
            // returned. A timeout waiting for that surfaces as the TimeoutException that fails this test.
            var readRanAhead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var batchesRead = 0;

            // Two pages read is already more than serial code can manage, and the read side gets there without the
            // processing side draining anything - it fills the channel and then blocks - so this cannot deadlock, and
            // it holds for any channel depth, leaving ChannelDepth free to be retuned.
            GivenDonorBatches(8, onBatchRead: () =>
            {
                if (Interlocked.Increment(ref batchesRead) >= 2)
                {
                    readRanAhead.TrySetResult();
                }
            });

            var isFirstWrite = true;
            donorImportRepository.AddMatchingRelationsForExistingDonorBatch(default, default, default)
                .ReturnsForAnyArgs(async _ =>
                {
                    if (!isFirstWrite)
                    {
                        return;
                    }

                    isFirstWrite = false;

                    // Awaited rather than blocked on. The write is the processing side's own continuation, so blocking
                    // here would hold its thread and leave the read side waiting on the pool to inject another - which
                    // it would, but only after a delay, and only if the pool is not already saturated by the rest of
                    // the suite. Returning a task the processing side awaits gives up the thread instead.
                    await readRanAhead.Task.WaitAsync(TimeSpan.FromSeconds(30));
                });

            await hlaProcessor.UpdateDonorHla(HlaNomenclatureVersion, _ => Task.CompletedTask);

            await donorImportRepository.ReceivedWithAnyArgs(8).AddMatchingRelationsForExistingDonorBatch(default, default, default);
        }

        [Test]
        public async Task UpdateDonorHla_TracesTheDurationOfEveryDonorBatchRead()
        {
            // With the read overlapped, its cost can no longer be inferred as the stage's wall clock minus the
            // batchProgress timings - that difference is now time the read side spent blocked on a full channel. So it
            // has to be traced per page, or the stage's occupancy cannot be measured at all.
            const int batchCount = 4;
            GivenDonorBatches(batchCount);

            var readTraces = new List<string>();
            logger.WhenForAnyArgs(l => l.SendTrace(default, default, default))
                .Do(call =>
                {
                    var message = call.Arg<string>();
                    if (message.StartsWith("Read donor batch"))
                    {
                        readTraces.Add(message);
                    }
                });

            await hlaProcessor.UpdateDonorHla(HlaNomenclatureVersion, _ => Task.CompletedTask);

            // At least one per page - the enumerator is also timed as it reports exhaustion, which is not worth pinning.
            readTraces.Should().HaveCountGreaterThanOrEqualTo(batchCount);

            // Paired with the depth the read side found the channel at, which is what distinguishes a read side running
            // ahead from a processing side being starved.
            readTraces.Should().OnlyContain(message => message.Contains("QueueDepth:"));
        }

        [Test]
        public async Task UpdateDonorHla_AdvancesCheckpointOnlyForProcessedBatches()
        {
            // The checkpoint is what a continued run resumes from, so it has to track batches that were actually
            // written - never ones the read side merely prefetched. Asserting the donor ids alone would not catch a
            // checkpoint driven from the read side, since it would eventually report the same ids; so each value is
            // paired with the number of batches written at the moment it was recorded.
            const int batchCount = 8;
            GivenDonorBatches(batchCount);

            var batchesWritten = 0;
            donorImportRepository.WhenForAnyArgs(r => r.AddMatchingRelationsForExistingDonorBatch(default, default, default))
                .Do(_ => batchesWritten++);

            var checkpoints = new List<(int DonorId, int BatchesWritten)>();

            await hlaProcessor.UpdateDonorHla(
                HlaNomenclatureVersion,
                donorId =>
                {
                    checkpoints.Add((donorId, batchesWritten));
                    return Task.CompletedTask;
                });

            // NumberOfBatchesOverlapOnRestart is 3 and Peek() returns the oldest of the three, so the checkpoint trails
            // the batch just written by two: the first update lands only once three batches have been written.
            var expected = Enumerable.Range(0, batchCount - 2)
                .Select(batchIndex => (DonorId: LastDonorIdOfBatch(batchIndex), BatchesWritten: batchIndex + 3))
                .ToList();

            checkpoints.Should().Equal(expected);
        }

        [Test]
        public async Task UpdateDonorHla_WhenProcessingFails_StopsReadingDonors()
        {
            // A failed processing side stops draining the channel. Without the linked cancellation tearing the read
            // side down, it would block on a full channel forever and this call would never return.
            const int batchCount = 1000;
            var batchesRead = 0;
            GivenDonorBatches(batchCount, onBatchRead: () => Interlocked.Increment(ref batchesRead));

            donorImportRepository.WhenForAnyArgs(r => r.AddMatchingRelationsForExistingDonorBatch(default, default, default))
                .Do(_ => throw new InvalidOperationException("Processing failed"));

            await hlaProcessor.Invoking(p => p.UpdateDonorHla(HlaNomenclatureVersion, _ => Task.CompletedTask))
                .Should().ThrowAsync<InvalidOperationException>();

            batchesRead.Should().BeLessThan(10, "reading is bounded by the channel's depth, not by the pages available");
        }

        [Test]
        public async Task UpdateDonorHla_WhenStreamEndsWithAnEmptyBatch_ProcessesEveryPopulatedBatch()
        {
            // The paging query signals exhaustion by yielding one final empty batch. It travels through the channel
            // like any other page, so the processing side still has to recognise it rather than reading its last donor.
            GivenDonorBatches(4, includeTerminalEmptyBatch: true);

            await hlaProcessor.UpdateDonorHla(HlaNomenclatureVersion, _ => Task.CompletedTask);

            await donorImportRepository.ReceivedWithAnyArgs(4).AddMatchingRelationsForExistingDonorBatch(default, default, default);
        }

        private void GivenDonorBatches(int batchCount, Action onBatchRead = null, bool includeTerminalEmptyBatch = false)
        {
            // Built afresh per call, rather than handed a single pre-built sequence, so the stream can be enumerated
            // again if a test ever drives the processor twice.
            dataRefreshRepository.NewOrderedDonorBatchesToImport(default, default)
                .ReturnsForAnyArgs(_ => BuildBatches(batchCount, onBatchRead, includeTerminalEmptyBatch));
        }

        private static async IAsyncEnumerable<List<DonorInfo>> BuildBatches(int batchCount, Action onBatchRead, bool includeTerminalEmptyBatch)
        {
            for (var batchIndex = 0; batchIndex < batchCount; batchIndex++)
            {
                // Genuinely async, like the paged query this stands in for, so the read side really does hand control
                // back rather than running to completion inside the first MoveNextAsync.
                await Task.Yield();

                onBatchRead?.Invoke();
                yield return BuildBatch(batchIndex);
            }

            if (includeTerminalEmptyBatch)
            {
                await Task.Yield();
                onBatchRead?.Invoke();
                yield return new List<DonorInfo>();
            }
        }

        private static List<DonorInfo> BuildBatch(int batchIndex) =>
            Enumerable.Range(batchIndex * BatchSize, BatchSize).Select(id => new DonorInfo {DonorId = id}).ToList();

        private static int LastDonorIdOfBatch(int batchIndex) => BuildBatch(batchIndex).Last().DonorId;
    }
}
