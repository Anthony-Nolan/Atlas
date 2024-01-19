using Atlas.Client.Models.SupportMessages;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.Donors;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Test.Services.Donors
{
    [TestFixture]
    public class DonorServiceTests
    {
        private IDonorService donorService;
        private IDonorUpdateRepository updateRepository;
        private IDonorInspectionRepository inspectionRepository;
        private IDonorHlaExpander donorHlaExpander;
        private IFailedDonorsNotificationSender failedDonorsNotificationSender;

        [SetUp]
        public void SetUp()
        {
            updateRepository = Substitute.For<IDonorUpdateRepository>();
            inspectionRepository = Substitute.For<IDonorInspectionRepository>();
            var repositoryFactory = Substitute.For<IStaticallyChosenDatabaseRepositoryFactory>();
            failedDonorsNotificationSender = Substitute.For<IFailedDonorsNotificationSender>();
            donorHlaExpander = Substitute.For<IDonorHlaExpander>();
            var donorHlaExpanderFactory = Substitute.For<IDonorHlaExpanderFactory>();
            donorHlaExpanderFactory.BuildForSpecifiedHlaNomenclatureVersion(Arg.Any<string>()).Returns(donorHlaExpander);
            donorHlaExpanderFactory.BuildForActiveHlaNomenclatureVersion().Returns(donorHlaExpander);

            repositoryFactory.GetDonorInspectionRepositoryForDatabase(default).ReturnsForAnyArgs(inspectionRepository);
            repositoryFactory.GetDonorUpdateRepositoryForDatabase(default).ReturnsForAnyArgs(updateRepository);

            inspectionRepository.GetDonors(Arg.Any<IEnumerable<int>>()).Returns(new Dictionary<int, DonorInfo>(),
                new Dictionary<int, DonorInfo> { { 0, new DonorInfo() } });

            donorHlaExpander.ExpandDonorHlaBatchAsync(Arg.Any<IEnumerable<DonorInfo>>(), Arg.Any<string>())
                .Returns(new DonorBatchProcessingResult<DonorInfoWithExpandedHla>());

            donorService = new DonorService(
                repositoryFactory,
                donorHlaExpanderFactory,
                failedDonorsNotificationSender
            );
        }

        [Test]
        public async Task SetDonorAsUnavailableForSearchBatch_SetsDonorAsUnavailableForSearch()
        {
            const int donorId = 123;

            await donorService.SetDonorBatchAsUnavailableForSearch(new List<int> { donorId }, default);

            await updateRepository.Received().SetDonorBatchAsUnavailableForSearch(Arg.Is<IEnumerable<int>>(x => x.Single() == donorId));
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_NoDonors_DoesNotExpandDonorHla()
        {
            await donorService.CreateOrUpdateDonorBatch(new DonorInfo[] { }, default, default, default);

            await donorHlaExpander.DidNotReceiveWithAnyArgs().ExpandDonorHlaBatchAsync(default, default);
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_NoDonors_DoesNotCreateDonor()
        {
            await donorService.CreateOrUpdateDonorBatch(new DonorInfo[] { }, default, default, default);

            await updateRepository.DidNotReceiveWithAnyArgs().InsertBatchOfDonorsWithExpandedHla(default, default);
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_NoDonors_DoesNotUpdateDonor()
        {
            await donorService.CreateOrUpdateDonorBatch(new DonorInfo[] { }, default, default, default);

            await updateRepository.DidNotReceiveWithAnyArgs().UpdateDonorBatch(default, default);
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_ExpandsDonorHla()
        {
            const int donorId = 123;

            await donorService.CreateOrUpdateDonorBatch(new[] { new DonorInfo { DonorId = donorId } }, default, default, default);

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
                (
                    new[] { new DonorInfoWithExpandedHla { DonorId = donorId } }
                ));

            await donorService.CreateOrUpdateDonorBatch(new[] { new DonorInfo() }, default, default, default);

            await updateRepository.Received().InsertBatchOfDonorsWithExpandedHla(Arg.Is<IEnumerable<DonorInfoWithExpandedHla>>(x => x.Single().DonorId == donorId), Arg.Any<bool>());
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_WhenDonorDoesNotExist_DoesNotUpdateDonor()
        {
            const int donorId = 123;

            donorHlaExpander
                .ExpandDonorHlaBatchAsync(Arg.Any<IEnumerable<DonorInfo>>(), Arg.Any<string>())
                .Returns(new DonorBatchProcessingResult<DonorInfoWithExpandedHla>
                (
                    new[] { new DonorInfoWithExpandedHla { DonorId = donorId } }
                ));

            await donorService.CreateOrUpdateDonorBatch(new[] { new DonorInfo() }, default, default, default);

            await updateRepository.DidNotReceiveWithAnyArgs().UpdateDonorBatch(default, default);
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_WhenDonorExists_UpdatesDonor()
        {
            const int donorId = 123;

            donorHlaExpander
                .ExpandDonorHlaBatchAsync(Arg.Any<IEnumerable<DonorInfo>>(), Arg.Any<string>())
                .Returns(new DonorBatchProcessingResult<DonorInfoWithExpandedHla>
                (
                    new[] { new DonorInfoWithExpandedHla { DonorId = donorId } }
                ));

            inspectionRepository
                .GetDonors(Arg.Any<IEnumerable<int>>())
                .Returns(new Dictionary<int, DonorInfo> { { donorId, new DonorInfo() } });

            await donorService.CreateOrUpdateDonorBatch(new[] { new DonorInfo() }, default, default, default);

            await updateRepository.Received().UpdateDonorBatch(Arg.Is<IEnumerable<DonorInfoWithExpandedHla>>(x => x.Single().DonorId == donorId), Arg.Any<bool>());
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_WhenDonorExists_DoesNotCreateDonor()
        {
            const int donorId = 123;

            donorHlaExpander
                .ExpandDonorHlaBatchAsync(Arg.Any<IEnumerable<DonorInfo>>(), Arg.Any<string>())
                .Returns(new DonorBatchProcessingResult<DonorInfoWithExpandedHla>
                (
                    new[] { new DonorInfoWithExpandedHla { DonorId = donorId } }
                ));

            inspectionRepository
                .GetDonors(Arg.Any<IEnumerable<int>>())
                .Returns(new Dictionary<int, DonorInfo> { { donorId, new DonorInfo() } });

            await donorService.CreateOrUpdateDonorBatch(new[] { new DonorInfo() }, default, default, default);

            await updateRepository.DidNotReceiveWithAnyArgs().InsertBatchOfDonorsWithExpandedHla(default, default);
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_WhenDonorFailsHlaExpansion_SendFailedDonorsAlert()
        {
            const int donorId = 21;

            donorHlaExpander
                .ExpandDonorHlaBatchAsync(Arg.Any<IEnumerable<DonorInfo>>(), Arg.Any<string>())
                .Returns(new DonorBatchProcessingResult<DonorInfoWithExpandedHla>
                {
                    FailedDonors = new[] { new FailedDonorInfo { AtlasDonorId = donorId } }.ToList().AsReadOnly()
                });

            await donorService.CreateOrUpdateDonorBatch(new[] { new DonorInfo() }, default, default, default);

            await failedDonorsNotificationSender.Received().SendFailedDonorsAlert(
                Arg.Is<IEnumerable<FailedDonorInfo>>(x => x.Single().AtlasDonorId == donorId),
                Arg.Any<string>(),
                Arg.Any<Priority>());
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_WhenNoDonorsFailHlaExpansion_DoesNotSendFailedDonorsAlert()
        {
            donorHlaExpander
                .ExpandDonorHlaBatchAsync(Arg.Any<IEnumerable<DonorInfo>>(), Arg.Any<string>())
                .Returns(new DonorBatchProcessingResult<DonorInfoWithExpandedHla>());

            await donorService.CreateOrUpdateDonorBatch(new[] { new DonorInfo() }, default, default, default);

            await failedDonorsNotificationSender.DidNotReceiveWithAnyArgs().SendFailedDonorsAlert(default, default, default);
        }
    }
}