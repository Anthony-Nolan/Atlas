using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Functions.Functions;
using Atlas.Functions.Services;
using Atlas.Functions.Settings;
using Atlas.MatchPrediction.Data.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.Functions.Test.Functions
{
    [TestFixture]
    internal class ParallelMatchPredictionAggregatorFunctionsTests
    {
        private IParallelMatchPredictionRepository repository;
        private IParallelMatchPredictionCompletionService completionService;
        private ParallelMatchPredictionAggregatorFunctions functions;

        [SetUp]
        public void SetUp()
        {
            repository = Substitute.For<IParallelMatchPredictionRepository>();
            completionService = Substitute.For<IParallelMatchPredictionCompletionService>();

            functions = new ParallelMatchPredictionAggregatorFunctions(
                repository,
                completionService,
                Options.Create(new OrchestrationSettings { AbandonBatchAfterMinutes = 60, ParallelBatchRetentionDays = 90 }),
                Substitute.For<ILogger<ParallelMatchPredictionAggregatorFunctions>>()
            );
        }

        [Test]
        public async Task MarkRunsAsAbandoned_AbandonsEachRunReturnedByTheRepository()
        {
            repository.GetRunIdsToAbandon(Arg.Any<DateTime>())
                .Returns(new List<int> { 1, 2, 3 });

            await functions.MarkRunsAsAbandoned(null);

            await completionService.Received(1).AbandonRun(1);
            await completionService.Received(1).AbandonRun(2);
            await completionService.Received(1).AbandonRun(3);
        }

        [Test]
        public async Task MarkRunsAsAbandoned_WhenAbandoningOneRunThrows_StillAbandonsTheOthersThenThrows()
        {
            repository.GetRunIdsToAbandon(Arg.Any<DateTime>())
                .Returns(new List<int> { 1, 2, 3 });
            completionService.AbandonRun(2).Returns(Task.FromException(new InvalidOperationException("boom")));

            // A single failure does not block the other runs, but the invocation is still reported as failed so the
            // partial failure surfaces in monitoring rather than being silently swallowed.
            var ex = Assert.ThrowsAsync<AggregateException>(() => functions.MarkRunsAsAbandoned(null));
            Assert.That(ex.InnerExceptions, Has.Count.EqualTo(1));

            await completionService.Received(1).AbandonRun(1);
            await completionService.Received(1).AbandonRun(3);
        }

        [Test]
        public async Task MarkRunsAsAbandoned_WhenNoRunsToAbandon_DoesNotCallCompletionService()
        {
            repository.GetRunIdsToAbandon(Arg.Any<DateTime>())
                .Returns(new List<int>());

            await functions.MarkRunsAsAbandoned(null);

            await completionService.DidNotReceiveWithAnyArgs().AbandonRun(default);
        }

        [Test]
        public async Task CleanupOldParallelMatchPredictionBatches_CleansRunsRegardlessOfStatus()
        {
            await functions.CleanupOldParallelMatchPredictionBatches(null);

            // A single status-agnostic clean-up (keyed off initiation time) purges finalised, failed and
            // abandoned runs alike, marking each IsCleanedUp=true.
            await repository.Received(1).CleanupBatchesForRunsInitiatedBefore(Arg.Any<DateTime>());
        }
    }
}
