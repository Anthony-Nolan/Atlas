using Nova.SearchAlgorithm.ApplicationInsights;
using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories.DonorRetrieval;
using Nova.SearchAlgorithm.Common.Repositories.DonorUpdates;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Nova.SearchAlgorithm.Services.Donors;
using Nova.SearchAlgorithm.Services.MatchingDictionary;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Notifications;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Test.Services.Donors
{
    [TestFixture]
    public class DonorServiceTests
    {
        private IDonorService donorService;
        private IDonorUpdateRepository updateRepository;
        private IDonorInspectionRepository inspectionRepository;
        private IExpandHlaPhenotypeService expandHlaPhenotypeService;
        private IActiveRepositoryFactory repositoryFactory;
        private ILogger logger;
        private INotificationsClient notificationsClient;

        [SetUp]
        public void SetUp()
        {
            updateRepository = Substitute.For<IDonorUpdateRepository>();
            inspectionRepository = Substitute.For<IDonorInspectionRepository>();
            repositoryFactory = Substitute.For<IActiveRepositoryFactory>();
            expandHlaPhenotypeService = Substitute.For<IExpandHlaPhenotypeService>();
            logger = Substitute.For<ILogger>();
            notificationsClient = Substitute.For<INotificationsClient>();

            repositoryFactory.GetDonorInspectionRepository().Returns(inspectionRepository);
            repositoryFactory.GetDonorUpdateRepository().Returns(updateRepository);

            donorService = new SearchAlgorithm.Services.Donors.DonorService(
                expandHlaPhenotypeService,
                repositoryFactory,
                logger,
                notificationsClient
            );
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_WhenDonorDoesNotExist_CreatesDonor()
        {
            inspectionRepository.GetDonors(Arg.Any<IEnumerable<int>>()).Returns(new Dictionary<int, DonorResult>(),
                new Dictionary<int, DonorResult> { { 0, new DonorResult() } });

            var inputDonor = new InputDonor
            {
                HlaNames = new PhenotypeInfo<string>("hla")
            };
            await donorService.CreateOrUpdateDonorBatch(new[] { inputDonor });

            await updateRepository.Received().InsertBatchOfDonorsWithExpandedHla(Arg.Any<IEnumerable<InputDonorWithExpandedHla>>());
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_WhenDonorExists_UpdatesDonor()
        {
            var inputDonor = new InputDonor
            {
                HlaNames = new PhenotypeInfo<string>("hla")
            };
            inspectionRepository.GetDonors(Arg.Any<IEnumerable<int>>()).Returns(new Dictionary<int, DonorResult> { { 0, new DonorResult() } });

            await donorService.CreateOrUpdateDonorBatch(new[] { inputDonor });

            await updateRepository.Received().UpdateDonorBatch(Arg.Any<IEnumerable<InputDonorWithExpandedHla>>());
        }

        [Test]
        public void CreateOrUpdateDonorBatch_WhenDonorHlaCannotBeProcessed_DoesNotThrowException()
        {
            var inputDonor = new InputDonor { HlaNames = new PhenotypeInfo<string>("hla") };
            inspectionRepository.GetDonors(Arg.Any<IEnumerable<int>>()).Returns(new Dictionary<int, DonorResult> { { 0, new DonorResult() } });
            expandHlaPhenotypeService.GetPhenotypeOfExpandedHla(null).ThrowsForAnyArgs(
                new MatchingDictionaryException(new HlaInfo(Locus.A, "hla"), "message"));

            Assert.DoesNotThrowAsync(async () =>
                await donorService.CreateOrUpdateDonorBatch(new[] { inputDonor })
            );
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_WhenDonorHlaCannotBeProcessed_LogsEvent()
        {
            var inputDonor = new InputDonor { HlaNames = new PhenotypeInfo<string>("hla") };
            inspectionRepository.GetDonors(Arg.Any<IEnumerable<int>>()).Returns(new Dictionary<int, DonorResult> { { 0, new DonorResult() } });
            expandHlaPhenotypeService.GetPhenotypeOfExpandedHla(null).ThrowsForAnyArgs(
                new MatchingDictionaryException(new HlaInfo(Locus.A, "hla"), "message"));

            await donorService.CreateOrUpdateDonorBatch(new[] { inputDonor });

            logger.Received().SendEvent(Arg.Any<MatchingDictionaryLookupFailureEventModel>());
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_WhenDonorHlaCannotBeProcessed_SendsAlert()
        {
            var inputDonor = new InputDonor { HlaNames = new PhenotypeInfo<string>("hla"), DonorId = 1 };
            inspectionRepository.GetDonors(Arg.Any<IEnumerable<int>>()).Returns(new Dictionary<int, DonorResult> { { 0, new DonorResult() } });
            expandHlaPhenotypeService.GetPhenotypeOfExpandedHla(null).ThrowsForAnyArgs(
                new MatchingDictionaryException(new HlaInfo(Locus.A, "hla"), "message"));

            await donorService.CreateOrUpdateDonorBatch(new[] { inputDonor });

            await notificationsClient.Received().SendAlert(Arg.Is<Alert>(n => n.Description.Contains(inputDonor.DonorId.ToString())));
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_WhenOneDonorsHlaCannotBeProcessed_UpdatesOtherDonors()
        {
            var donor1 = new InputDonor { HlaNames = new PhenotypeInfo<string>("hla"), DonorId = 1 };
            var donor2 = new InputDonor { HlaNames = new PhenotypeInfo<string>("hla"), DonorId = 2 };
            inspectionRepository.GetDonors(Arg.Any<IEnumerable<int>>()).Returns(new Dictionary<int, DonorResult>());
            expandHlaPhenotypeService.GetPhenotypeOfExpandedHla(null)
                .ReturnsForAnyArgs(
                    x => new PhenotypeInfo<ExpandedHla>(),
                    x => throw new MatchingDictionaryException(new HlaInfo(Locus.A, "hla"), "message")
                );

            await donorService.CreateOrUpdateDonorBatch(new[] { donor1, donor2 });

            await updateRepository.Received().InsertBatchOfDonorsWithExpandedHla(Arg.Is<IEnumerable<InputDonorWithExpandedHla>>(d => d.Count() == 1));
        }

        [Test]
        public async Task SetDonorAsUnavailableForSearchBatch_SetsDonorAsUnavailableForSearch()
        {
            const int donorId = 123;

            inspectionRepository.GetDonors(Arg.Any<IEnumerable<int>>()).Returns(new Dictionary<int, DonorResult> { { 0, new DonorResult() } });

            await donorService.SetDonorBatchAsUnavailableForSearch(new[] { donorId });

            await updateRepository.Received().SetDonorBatchAsUnavailableForSearch(Arg.Is<IEnumerable<int>>(x => x.Single() == donorId));
        }
    }
}