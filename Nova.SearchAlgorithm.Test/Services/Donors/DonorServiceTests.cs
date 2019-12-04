using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories.DonorRetrieval;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Data.Repositories.DonorUpdates;
using Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Nova.SearchAlgorithm.Services.Donors;
using NSubstitute;
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
        private IActiveRepositoryFactory repositoryFactory;
        private IDonorValidator donorValidator;
        private IDonorHlaExpander donorHlaExpander;

        [SetUp]
        public void SetUp()
        {
            updateRepository = Substitute.For<IDonorUpdateRepository>();
            inspectionRepository = Substitute.For<IDonorInspectionRepository>();
            repositoryFactory = Substitute.For<IActiveRepositoryFactory>();
            donorValidator = Substitute.For<IDonorValidator>();
            donorHlaExpander = Substitute.For<IDonorHlaExpander>();

            repositoryFactory.GetDonorInspectionRepository().Returns(inspectionRepository);
            repositoryFactory.GetDonorUpdateRepository().Returns(updateRepository);

            inspectionRepository.GetDonors(Arg.Any<IEnumerable<int>>()).Returns(new Dictionary<int, DonorResult>(),
                new Dictionary<int, DonorResult> { { 0, new DonorResult() } });

            donorService = new SearchAlgorithm.Services.Donors.DonorService(
                repositoryFactory,
                donorValidator,
                donorHlaExpander
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
        public async Task CreateOrUpdateDonorBatch_ValidatesDonors()
        {
            const int donorId = 123;

            await donorService.CreateOrUpdateDonorBatch(new[]
            {
                new InputDonor
                {
                    DonorId = donorId
                }
            });

            await donorValidator.Received().ValidateDonorsAsync(Arg.Is<IEnumerable<InputDonor>>(x => x.Single().DonorId == donorId));
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_NoValidDonors_DoesNotExpandDonorHla()
        {
            await donorService.CreateOrUpdateDonorBatch(new[] { new InputDonor() });

            await donorHlaExpander.DidNotReceive().ExpandDonorHlaBatchAsync(Arg.Any<IEnumerable<InputDonor>>());
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_NoValidDonors_DoesNotCreateDonor()
        {
            await donorService.CreateOrUpdateDonorBatch(new[] { new InputDonor() });

            await updateRepository.DidNotReceive().InsertBatchOfDonorsWithExpandedHla(Arg.Any<IEnumerable<InputDonorWithExpandedHla>>());
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_NoValidDonors_DoesNotUpdateDonor()
        {
            await donorService.CreateOrUpdateDonorBatch(new[] { new InputDonor() });

            await updateRepository.DidNotReceive().UpdateDonorBatch(Arg.Any<IEnumerable<InputDonorWithExpandedHla>>());
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_DonorIsValid_ExpandsDonorHla()
        {
            const int donorId = 123;

            donorValidator
                .ValidateDonorsAsync(Arg.Any<IEnumerable<InputDonor>>())
                .Returns(new[] { new InputDonor { DonorId = donorId } });

            await donorService.CreateOrUpdateDonorBatch(new[] { new InputDonor() });

            await donorHlaExpander.Received().ExpandDonorHlaBatchAsync(Arg.Is<IEnumerable<InputDonor>>(x => x.Single().DonorId == donorId));
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_WhenValidDonorDoesNotExist_CreatesDonor()
        {
            const int donorId = 123;

            donorValidator
                .ValidateDonorsAsync(Arg.Any<IEnumerable<InputDonor>>())
                .Returns(new[] { new InputDonor() });

            donorHlaExpander
                .ExpandDonorHlaBatchAsync(Arg.Any<IEnumerable<InputDonor>>())
                .Returns(new[] { new InputDonorWithExpandedHla { DonorId = donorId } });

            await donorService.CreateOrUpdateDonorBatch(new[] { new InputDonor() });

            await updateRepository.Received().InsertBatchOfDonorsWithExpandedHla(Arg.Is<IEnumerable<InputDonorWithExpandedHla>>(x => x.Single().DonorId == donorId));
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_WhenValidDonorDoesNotExist_DoesNotUpdateDonor()
        {
            const int donorId = 123;

            donorValidator
                .ValidateDonorsAsync(Arg.Any<IEnumerable<InputDonor>>())
                .Returns(new[] { new InputDonor() });

            donorHlaExpander
                .ExpandDonorHlaBatchAsync(Arg.Any<IEnumerable<InputDonor>>())
                .Returns(new[] { new InputDonorWithExpandedHla { DonorId = donorId } });

            await donorService.CreateOrUpdateDonorBatch(new[] { new InputDonor() });

            await updateRepository.DidNotReceive().UpdateDonorBatch(Arg.Any<IEnumerable<InputDonorWithExpandedHla>>());
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_WhenValidDonorExists_UpdatesDonor()
        {
            const int donorId = 123;

            donorValidator
                .ValidateDonorsAsync(Arg.Any<IEnumerable<InputDonor>>())
                .Returns(new[] { new InputDonor() });

            donorHlaExpander
                .ExpandDonorHlaBatchAsync(Arg.Any<IEnumerable<InputDonor>>())
                .Returns(new[] { new InputDonorWithExpandedHla { DonorId = donorId } });

            inspectionRepository
                .GetDonors(Arg.Any<IEnumerable<int>>())
                .Returns(new Dictionary<int, DonorResult> { { donorId, new DonorResult() } });

            await donorService.CreateOrUpdateDonorBatch(new[] { new InputDonor() });

            await updateRepository.Received().UpdateDonorBatch(Arg.Is<IEnumerable<InputDonorWithExpandedHla>>(x => x.Single().DonorId == donorId));
        }

        [Test]
        public async Task CreateOrUpdateDonorBatch_WhenValidDonorExists_DoesNotCreateDonor()
        {
            const int donorId = 123;

            donorValidator
                .ValidateDonorsAsync(Arg.Any<IEnumerable<InputDonor>>())
                .Returns(new[] { new InputDonor() });

            donorHlaExpander
                .ExpandDonorHlaBatchAsync(Arg.Any<IEnumerable<InputDonor>>())
                .Returns(new[] { new InputDonorWithExpandedHla { DonorId = donorId } });

            inspectionRepository
                .GetDonors(Arg.Any<IEnumerable<int>>())
                .Returns(new Dictionary<int, DonorResult> { { donorId, new DonorResult() } });

            await donorService.CreateOrUpdateDonorBatch(new[] { new InputDonor() });

            await updateRepository.DidNotReceive().InsertBatchOfDonorsWithExpandedHla(Arg.Any<IEnumerable<InputDonorWithExpandedHla>>());
        }
    }
}