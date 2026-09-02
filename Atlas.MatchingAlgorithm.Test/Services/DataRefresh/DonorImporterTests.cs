using Atlas.Client.Models.SupportMessages;
using Atlas.Common.ApplicationInsights;
using Atlas.DonorImport.ExternalInterface;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh.DonorImport;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchingAlgorithm.Data.Models;
using Atlas.MatchingAlgorithm.Exceptions;
using AutoFixture;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh
{
    [TestFixture]
    public class DonorImporterTests
    {
        private IDonorImporter donorImporter;

        private IDataRefreshRepository dataRefreshRepository;
        private IDonorImportRepository donorImportRepository;
        private IDonorManagementLogRepository donorManagementLogRepository;
        private IDormantRepositoryFactory repositoryFactory;
        private IDonorInfoConverter donorInfoConverter;
        private IFailedDonorsNotificationSender failedDonorsNotificationSender;
        private IMatchingAlgorithmImportLogger logger;
        private IDonorReader donorReader;

        private Fixture fixture;

        /// <summary>Mirrors <c>DonorImporter.BatchSize</c>; tests that care about batch boundaries need to match it.</summary>
        private const int BatchSize = 10000;

        /// <summary>Generous, because it is only ever reached when the pipeline is broken and the test would hang.</summary>
        private static readonly TimeSpan PipeliningTimeout = TimeSpan.FromSeconds(10);

        private const string DonorStreamFailureMessage = "the donor stream could not be read";

        [SetUp]
        public void SetUp()
        {
            fixture = new Fixture();

            dataRefreshRepository = Substitute.For<IDataRefreshRepository>();
            donorImportRepository = Substitute.For<IDonorImportRepository>();
            donorManagementLogRepository = Substitute.For<IDonorManagementLogRepository>();
            repositoryFactory = Substitute.For<IDormantRepositoryFactory>();
            repositoryFactory.GetDataRefreshRepository().Returns(dataRefreshRepository);
            repositoryFactory.GetDonorImportRepository().Returns(donorImportRepository);
            repositoryFactory.GetDonorManagementLogRepository().Returns(donorManagementLogRepository);

            donorInfoConverter = Substitute.For<IDonorInfoConverter>();
            donorInfoConverter.ConvertDonorInfoAsync(Arg.Any<IEnumerable<SearchableDonorInformation>>(), Arg.Any<string>())
                .Returns(new DonorBatchProcessingResult<DonorInfo>());

            failedDonorsNotificationSender = Substitute.For<IFailedDonorsNotificationSender>();
            logger = Substitute.For<IMatchingAlgorithmImportLogger>();
            donorReader = Substitute.For<IDonorReader>();

            donorImporter = new DonorImporter(repositoryFactory, donorInfoConverter, failedDonorsNotificationSender, logger, donorReader);
        }

        [Test]
        public async Task ImportDonors_WhenNoDonorsExistInSource_DoesNotInsertDonors()
        {
            await donorImporter.ImportDonors();

            await donorImportRepository.DidNotReceive().InsertBatchOfDonors(Arg.Any<IEnumerable<DonorInfo>>());
        }

        [Test]
        public async Task ImportDonors_ConvertsDonorInfo()
        {
            var donor = DonorBuilder.New.With(d => d.AtlasDonorId, 123).Build();

            donorReader.StreamAllDonors().Returns(new List<Donor> {donor});

            await donorImporter.ImportDonors();

            await donorInfoConverter.Received(1).ConvertDonorInfoAsync(
                Arg.Is<IEnumerable<SearchableDonorInformation>>(x => x.Single().DonorId == donor.AtlasDonorId),
                Arg.Any<string>());
        }

        [Test]
        public async Task ImportDonors_InsertsDonors()
        {
            const int donorId = 123;
            var donor = DonorBuilder.New.With(d => d.AtlasDonorId, donorId).Build();

            donorReader.StreamAllDonors().Returns(new List<Donor> {donor});

            donorInfoConverter
                .ConvertDonorInfoAsync(null, null)
                .ReturnsForAnyArgs(new DonorBatchProcessingResult<DonorInfo>
                (
                    new List<DonorInfo>
                    {
                        new DonorInfo
                        {
                            DonorId = donorId
                        }
                    }
                ));

            await donorImporter.ImportDonors();

            await donorImportRepository.Received(1).InsertBatchOfDonors(
                Arg.Is<IEnumerable<DonorInfo>>(x => x.Single().DonorId == donorId));
        }

        [Test]
        public async Task ImportDonors_WithFailedDonor_SendsFailedDonorsAlert()
        {
            const int failedDonorId = 1;

            donorInfoConverter
                .ConvertDonorInfoAsync(null, null)
                .ReturnsForAnyArgs(new DonorBatchProcessingResult<DonorInfo>
                {
                    FailedDonors = new List<FailedDonorInfo>
                    {
                        new FailedDonorInfo
                        {
                            AtlasDonorId = failedDonorId
                        }
                    }.AsReadOnly()
                });

            var donor = DonorBuilder.New.With(d => d.AtlasDonorId, failedDonorId).Build();

            donorReader.StreamAllDonors().Returns(new List<Donor> {donor});

            await donorImporter.ImportDonors();

            await failedDonorsNotificationSender.Received(1)
                .SendFailedDonorsAlert(
                    Arg.Is<IEnumerable<FailedDonorInfo>>(x => x.Single().AtlasDonorId == failedDonorId),
                    Arg.Any<string>(),
                    Arg.Any<Priority>());
        }

        [Test]
        public async Task ImportDonors_WhenDonorsShouldBeMarkedAsUpdated_CreatesDonorManagementLogs()
        {
            var donorId = fixture.Create<int>();
            var donor = DonorBuilder.New.With(d => d.AtlasDonorId, donorId).Build();

            donorReader.StreamAllDonors().Returns(new List<Donor> {donor});

            await donorImporter.ImportDonors(true);

            await donorManagementLogRepository.Received(1).CreateDonorManagementLogBatch(
                Arg.Is<IEnumerable<DonorManagementInfo>>(x => x.Single().DonorId == donorId));
        }

        /// <summary>
        /// The refresh always runs against a freshly truncated donor management log table, so the log writes must not pay for the
        /// (expensive) read of existing logs that the upsert path performs. See <see cref="IDonorManagementLogRepository"/>.
        /// </summary>
        [Test]
        public async Task ImportDonors_WhenDonorsShouldBeMarkedAsUpdated_DoesNotReadExistingDonorManagementLogs()
        {
            var donor = DonorBuilder.New.With(d => d.AtlasDonorId, fixture.Create<int>()).Build();

            donorReader.StreamAllDonors().Returns(new List<Donor> {donor});

            await donorImporter.ImportDonors(true);

            await donorManagementLogRepository.DidNotReceive().GetDonorManagementLogBatch(Arg.Any<IEnumerable<int>>());
            await donorManagementLogRepository.DidNotReceive()
                .CreateOrUpdateDonorManagementLogBatch(Arg.Any<IEnumerable<DonorManagementInfo>>());
        }

        [Test]
        public async Task ImportDonors_WhenDonorsShouldNotBeMarkedAsUpdated_DoesNotWriteDonorManagementLogs()
        {
            var donor = DonorBuilder.New.With(d => d.AtlasDonorId, fixture.Create<int>()).Build();

            donorReader.StreamAllDonors().Returns(new List<Donor> {donor});

            await donorImporter.ImportDonors(false);

            await donorManagementLogRepository.DidNotReceive().CreateDonorManagementLogBatch(Arg.Any<IEnumerable<DonorManagementInfo>>());
            await donorManagementLogRepository.DidNotReceive()
                .CreateOrUpdateDonorManagementLogBatch(Arg.Any<IEnumerable<DonorManagementInfo>>());
        }

        #region Cancellation

        [Test]
        public async Task ImportDonors_WhenAlreadyCancelled_DoesNotImportAnyDonors()
        {
            var donor = DonorBuilder.New.With(d => d.AtlasDonorId, fixture.Create<int>()).Build();
            donorReader.StreamAllDonors().Returns(new List<Donor> {donor});
            var cancelled = new CancellationTokenSource();
            await cancelled.CancelAsync();

            await donorImporter.Invoking(i => i.ImportDonors(false, cancelled.Token)).Should().ThrowAsync<OperationCanceledException>();

            await donorImportRepository.DidNotReceive().InsertBatchOfDonors(Arg.Any<IEnumerable<DonorInfo>>());
        }

        [Test]
        public async Task ImportDonors_WhenCancelled_DoesNotReportCancellationAsAnImportFailure()
        {
            // The refresh recognises a lost lease by the exception type. Wrapping it as a DonorImportHttpException, as
            // every other failure here is wrapped, would disguise it as an import failure and defeat the abort.
            var donor = DonorBuilder.New.With(d => d.AtlasDonorId, fixture.Create<int>()).Build();
            donorReader.StreamAllDonors().Returns(new List<Donor> {donor});
            var cancelled = new CancellationTokenSource();
            await cancelled.CancelAsync();

            await donorImporter.Invoking(i => i.ImportDonors(false, cancelled.Token)).Should()
                .ThrowAsync<OperationCanceledException>();
            await donorImporter.Invoking(i => i.ImportDonors(false, cancelled.Token)).Should()
                .NotThrowAsync<DonorImportHttpException>();
        }

        [Test]
        public async Task ImportDonors_WhenCancelledMidImport_StopsOnABatchBoundary()
        {
            // Two batches, cancelled while the first is being written. The write side checks the token before beginning
            // each batch, so the second is never started - even though the read side has likely already buffered it.
            var donor = DonorBuilder.New.With(d => d.AtlasDonorId, fixture.Create<int>()).Build();
            donorReader.StreamAllDonors().Returns(Enumerable.Repeat(donor, BatchSize * 2));

            var cancellationTokenSource = new CancellationTokenSource();
            donorImportRepository.WhenForAnyArgs(r => r.InsertBatchOfDonors(default)).Do(_ => cancellationTokenSource.Cancel());

            await donorImporter.Invoking(i => i.ImportDonors(false, cancellationTokenSource.Token)).Should()
                .ThrowAsync<OperationCanceledException>();

            await donorImportRepository.Received(1).InsertBatchOfDonors(Arg.Any<IEnumerable<DonorInfo>>());
        }

        #endregion

        #region Pipelining

        /// <summary>The write below blocks until the read side reaches the next batch, which run serially it never could.</summary>
        [Test]
        public async Task ImportDonors_ReadsTheNextBatchWhileThePreviousOneIsStillBeingWritten()
        {
            var secondBatchStarted = new TaskCompletionSource();
            var donor = DonorBuilder.New.With(d => d.AtlasDonorId, fixture.Create<int>()).Build();

            donorReader.StreamAllDonors().Returns(TwoBatchesSignallingWhenTheSecondIsReached(donor, secondBatchStarted));

            // Captured on the first write only. By the second, the signal is set regardless and proves nothing.
            bool? readSideRanAheadDuringTheFirstWrite = null;
            donorImportRepository.WhenForAnyArgs(r => r.InsertBatchOfDonors(default))
                .Do(_ => readSideRanAheadDuringTheFirstWrite ??= secondBatchStarted.Task.Wait(PipeliningTimeout));

            await donorImporter.ImportDonors();

            readSideRanAheadDuringTheFirstWrite.Should()
                .BeTrue("the read side must continue fetching donors while a batch is being written, or the stage is still serial");
        }

        /// <summary>
        /// The read side now fails on its own thread, and a failure lost there would close the channel cleanly - leaving
        /// the stage to report success over a partial donor set, silently, for every subsequent search.
        /// </summary>
        [Test]
        public async Task ImportDonors_WhenTheDonorStreamFails_FailsTheImportWithTheUnderlyingFailure()
        {
            var donor = DonorBuilder.New.With(d => d.AtlasDonorId, fixture.Create<int>()).Build();
            donorReader.StreamAllDonors().Returns(DonorStreamThatFailsPartWayThrough(donor));

            var thrown = await donorImporter.Invoking(i => i.ImportDonors()).Should().ThrowAsync<DonorImportHttpException>();

            thrown.WithMessage($"*{DonorStreamFailureMessage}*").WithInnerException<InvalidOperationException>();
        }

        /// <summary>
        /// Left running, the read side stays blocked on a full channel holding open the cross-database connection
        /// <see cref="IDonorReader.StreamAllDonors"/> enumerates, outliving the stage that opened it.
        /// </summary>
        [Test]
        public async Task ImportDonors_WhenAWriteFails_DoesNotReturnUntilTheDonorStreamHasBeenTornDown()
        {
            var donorStreamTornDown = new TaskCompletionSource();
            var donor = DonorBuilder.New.With(d => d.AtlasDonorId, fixture.Create<int>()).Build();

            donorReader.StreamAllDonors().Returns(LongDonorStreamSignallingTeardown(donor, donorStreamTornDown));
            donorImportRepository.WhenForAnyArgs(r => r.InsertBatchOfDonors(default))
                .Do(_ => throw new InvalidOperationException("the write failed"));

            await donorImporter.Invoking(i => i.ImportDonors()).Should().ThrowAsync<DonorImportHttpException>();

            donorStreamTornDown.Task.IsCompleted.Should()
                .BeTrue("the import must not return while a thread is still enumerating the master donor store");
        }

        /// <summary>
        /// A write failing on its own never observes the channel, so the read side's own failure reaches nobody. Without
        /// the read side logging it where it is caught, a simultaneous failure of both would leave no trace of the read.
        /// </summary>
        [Test]
        public async Task ImportDonors_WhenBothSidesFailAtOnce_StillRecordsTheReadSideFailure()
        {
            var readAboutToFail = new TaskCompletionSource();
            var donor = DonorBuilder.New.With(d => d.AtlasDonorId, fixture.Create<int>()).Build();

            donorReader.StreamAllDonors().Returns(OneBatchThenFailure(donor, readAboutToFail));
            donorImportRepository.WhenForAnyArgs(r => r.InsertBatchOfDonors(default)).Do(_ =>
            {
                readAboutToFail.Task.Wait(PipeliningTimeout);
                throw new InvalidOperationException("the write failed");
            });

            (await donorImporter.Invoking(i => i.ImportDonors()).Should().ThrowAsync<DonorImportHttpException>())
                .WithMessage("*the write failed*");

            logger.Received().SendTrace(
                Arg.Is<string>(m => m.Contains(DonorStreamFailureMessage)),
                LogLevel.Error,
                Arg.Any<Dictionary<string, string>>());
        }

        private static IEnumerable<Donor> TwoBatchesSignallingWhenTheSecondIsReached(Donor donor, TaskCompletionSource secondBatchStarted)
        {
            for (var i = 0; i < BatchSize; i++)
            {
                yield return donor;
            }

            secondBatchStarted.TrySetResult();

            for (var i = 0; i < BatchSize; i++)
            {
                yield return donor;
            }
        }

        private static IEnumerable<Donor> DonorStreamThatFailsPartWayThrough(Donor donor)
        {
            yield return donor;
            throw new InvalidOperationException(DonorStreamFailureMessage);
        }

        /// <remarks>
        /// Yields one whole batch, so the write side gets something to fail on, then signals immediately before failing
        /// itself - which lets the write side hold off until both failures are genuinely in flight at once.
        /// </remarks>
        private static IEnumerable<Donor> OneBatchThenFailure(Donor donor, TaskCompletionSource readAboutToFail)
        {
            for (var i = 0; i < BatchSize; i++)
            {
                yield return donor;
            }

            readAboutToFail.TrySetResult();
            throw new InvalidOperationException(DonorStreamFailureMessage);
        }

        /// <remarks>Comfortably more donors than the channel can buffer, so the read side is still going when the write fails.</remarks>
        private static IEnumerable<Donor> LongDonorStreamSignallingTeardown(Donor donor, TaskCompletionSource tornDown)
        {
            try
            {
                for (var i = 0; i < BatchSize * 20; i++)
                {
                    yield return donor;
                }
            }
            finally
            {
                tornDown.TrySetResult();
            }
        }

        #endregion
    }
}