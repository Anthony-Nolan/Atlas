using Atlas.Client.Models.SupportMessages;
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
            // Two batches' worth of donors, cancelled while the first is being written. The check sits at the top of the
            // loop, so the first batch completes and the second is never started.
            const int batchSize = 10000;
            var donor = DonorBuilder.New.With(d => d.AtlasDonorId, fixture.Create<int>()).Build();
            donorReader.StreamAllDonors().Returns(Enumerable.Repeat(donor, batchSize * 2));

            var cancellationTokenSource = new CancellationTokenSource();
            donorImportRepository.WhenForAnyArgs(r => r.InsertBatchOfDonors(default)).Do(_ => cancellationTokenSource.Cancel());

            await donorImporter.Invoking(i => i.ImportDonors(false, cancellationTokenSource.Token)).Should()
                .ThrowAsync<OperationCanceledException>();

            await donorImportRepository.Received(1).InsertBatchOfDonors(Arg.Any<IEnumerable<DonorInfo>>());
        }

        #endregion
    }
}