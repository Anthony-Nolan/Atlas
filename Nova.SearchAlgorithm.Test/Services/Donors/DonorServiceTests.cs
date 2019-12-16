using Nova.SearchAlgorithm.Data.Models.DonorInfo;
using Nova.SearchAlgorithm.Data.Repositories.DonorRetrieval;
using Nova.SearchAlgorithm.Data.Repositories.DonorUpdates;
using Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Nova.SearchAlgorithm.Services.Donors;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Models;
using Nova.Utils.Notifications;

namespace Nova.SearchAlgorithm.Test.Services.Donors
{
    [TestFixture]
    public class DonorServiceTests
    {
        private IDonorService donorService;
        private IDonorUpdateRepository updateRepository;
        private IDonorInspectionRepository inspectionRepository;
        private IActiveRepositoryFactory repositoryFactory;
        private IDonorHlaExpander donorHlaExpander;
        private IFailedDonorsNotificationSender failedDonorsNotificationSender;

        [SetUp]
        public void SetUp()
        {
            updateRepository = Substitute.For<IDonorUpdateRepository>();
            inspectionRepository = Substitute.For<IDonorInspectionRepository>();
            repositoryFactory = Substitute.For<IActiveRepositoryFactory>();
            donorHlaExpander = Substitute.For<IDonorHlaExpander>();
            failedDonorsNotificationSender = Substitute.For<IFailedDonorsNotificationSender>();

            repositoryFactory.GetDonorInspectionRepository().Returns(inspectionRepository);
            repositoryFactory.GetDonorUpdateRepository().Returns(updateRepository);

            inspectionRepository.GetDonors(Arg.Any<IEnumerable<int>>()).Returns(new Dictionary<int, DonorInfo>(),
                new Dictionary<int, DonorInfo> { { 0, new DonorInfo() } });

            donorHlaExpander.ExpandDonorHlaBatchAsync(Arg.Any<IEnumerable<DonorInfo>>(), Arg.Any<string>())
                .Returns(new DonorBatchProcessingResult<DonorInfoWithExpandedHla>());

            donorService = new SearchAlgorithm.Services.Donors.DonorService(
                repositoryFactory,
                donorHlaExpander,
                failedDonorsNotificationSender
            );
        }

        [Test]
        public async Task SetDonorAsUnavailableForSearchBatch_SetsDonorAsUnavailableForSearch()
        {
            const int donorId = 123;

            await donorService.SetDonorBatchAsUnavailableForSearch(new[] { donorId });

            await updateRepository.Received().SetDonorBatchAsUnavailableForSearch(Arg.Is<IEnumerable<int>>(x => x.Single() == donorId));
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_NoDonors_DoesNotExpandDonorHla()
        {
            await donorService.CreateOrUpdateDonorBatch(new DonorInfo[] { });

            await donorHlaExpander.DidNotReceive().ExpandDonorHlaBatchAsync(Arg.Any<IEnumerable<DonorInfo>>(), Arg.Any<string>());
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_NoDonors_DoesNotCreateDonor()
        {
            await donorService.CreateOrUpdateDonorBatch(new DonorInfo[] { });

            await updateRepository.DidNotReceive().InsertBatchOfDonorsWithExpandedHla(Arg.Any<IEnumerable<DonorInfoWithExpandedHla>>());
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_NoDonors_DoesNotUpdateDonor()
        {
            await donorService.CreateOrUpdateDonorBatch(new DonorInfo[] { });

            await updateRepository.DidNotReceive().UpdateDonorBatch(Arg.Any<IEnumerable<DonorInfoWithExpandedHla>>());
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_ExpandsDonorHla()
        {
            const int donorId = 123;

            await donorService.CreateOrUpdateDonorBatch(new[] { new DonorInfo { DonorId = donorId } });

            await donorHlaExpander.Received().ExpandDonorHlaBatchAsync(
                Arg.Is<IEnumerable<DonorInfo>>(x => x.Single().DonorId == donorId),
                Arg.Any<string>());
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_WhenDonorDoesNotExist_CreatesDonor()
        {
            const int donorId = 123;

            donorHlaExpander
                .ExpandDonorHlaBatchAsync(Arg.Any<IEnumerable<DonorInfo>>(), Arg.Any<string>())
                .Returns(new DonorBatchProcessingResult<DonorInfoWithExpandedHla>
                {
                    ProcessingResults = new[] { new DonorInfoWithExpandedHla { DonorId = donorId } }
                });

            await donorService.CreateOrUpdateDonorBatch(new[] { new DonorInfo() });

            await updateRepository.Received().InsertBatchOfDonorsWithExpandedHla(Arg.Is<IEnumerable<DonorInfoWithExpandedHla>>(x => x.Single().DonorId == donorId));
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_WhenDonorDoesNotExist_DoesNotUpdateDonor()
        {
            const int donorId = 123;

            donorHlaExpander
                .ExpandDonorHlaBatchAsync(Arg.Any<IEnumerable<DonorInfo>>(), Arg.Any<string>())
                .Returns(new DonorBatchProcessingResult<DonorInfoWithExpandedHla>
                {
                    ProcessingResults = new[] { new DonorInfoWithExpandedHla { DonorId = donorId } }
                });

            await donorService.CreateOrUpdateDonorBatch(new[] { new DonorInfo() });

            await updateRepository.DidNotReceive().UpdateDonorBatch(Arg.Any<IEnumerable<DonorInfoWithExpandedHla>>());
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_WhenDonorExists_UpdatesDonor()
        {
            const int donorId = 123;

            donorHlaExpander
                .ExpandDonorHlaBatchAsync(Arg.Any<IEnumerable<DonorInfo>>(), Arg.Any<string>())
                .Returns(new DonorBatchProcessingResult<DonorInfoWithExpandedHla>
                {
                    ProcessingResults = new[] { new DonorInfoWithExpandedHla { DonorId = donorId } }
                });

            inspectionRepository
                .GetDonors(Arg.Any<IEnumerable<int>>())
                .Returns(new Dictionary<int, DonorInfo> { { donorId, new DonorInfo() } });

            await donorService.CreateOrUpdateDonorBatch(new[] { new DonorInfo() });

            await updateRepository.Received().UpdateDonorBatch(Arg.Is<IEnumerable<DonorInfoWithExpandedHla>>(x => x.Single().DonorId == donorId));
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_WhenDonorExists_DoesNotCreateDonor()
        {
            const int donorId = 123;

            donorHlaExpander
                .ExpandDonorHlaBatchAsync(Arg.Any<IEnumerable<DonorInfo>>(), Arg.Any<string>())
                .Returns(new DonorBatchProcessingResult<DonorInfoWithExpandedHla>
                {
                    ProcessingResults = new[] { new DonorInfoWithExpandedHla { DonorId = donorId } }
                });

            inspectionRepository
                .GetDonors(Arg.Any<IEnumerable<int>>())
                .Returns(new Dictionary<int, DonorInfo> { { donorId, new DonorInfo() } });

            await donorService.CreateOrUpdateDonorBatch(new[] { new DonorInfo() });

            await updateRepository.DidNotReceive().InsertBatchOfDonorsWithExpandedHla(Arg.Any<IEnumerable<DonorInfoWithExpandedHla>>());
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_WhenDonorFailsHlaExpansion_SendFailedDonorsAlert()
        {
            const string donorId = "donor-id";

            donorHlaExpander
                .ExpandDonorHlaBatchAsync(Arg.Any<IEnumerable<DonorInfo>>(), Arg.Any<string>())
                .Returns(new DonorBatchProcessingResult<DonorInfoWithExpandedHla>
                {
                    FailedDonors = new[] { new FailedDonorInfo { DonorId = donorId } }
                });

            await donorService.CreateOrUpdateDonorBatch(new[] { new DonorInfo() });

            await failedDonorsNotificationSender.Received().SendFailedDonorsAlert(
                Arg.Is<IEnumerable<FailedDonorInfo>>(x => x.Single().DonorId == donorId),
                Arg.Any<string>(),
                Arg.Any<Priority>());
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_WhenNoDonorsFailHlaExpansion_DoesNotSendFailedDonorsAlert()
        {
            donorHlaExpander
                .ExpandDonorHlaBatchAsync(Arg.Any<IEnumerable<DonorInfo>>(), Arg.Any<string>())
                .Returns(new DonorBatchProcessingResult<DonorInfoWithExpandedHla>
                {
                    FailedDonors = new List<FailedDonorInfo>()
                });

            await donorService.CreateOrUpdateDonorBatch(new[] { new DonorInfo() });

            await failedDonorsNotificationSender.DidNotReceive().SendFailedDonorsAlert(
                Arg.Any<IEnumerable<FailedDonorInfo>>(),
                Arg.Any<string>(),
                Arg.Any<Priority>());
        }
    }
}