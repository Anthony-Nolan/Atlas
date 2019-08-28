using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using AutoMapper;
using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories.DonorRetrieval;
using Nova.SearchAlgorithm.Common.Repositories.DonorUpdates;
using Nova.SearchAlgorithm.Config;
using Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Nova.SearchAlgorithm.Services.Donors;
using Nova.SearchAlgorithm.Services.MatchingDictionary;
using Nova.Utils.Http.Exceptions;
using NSubstitute;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Services.Donors
{
    [TestFixture]
    public class DonorServiceTests
    {
        private IDonorService donorService;
        private IDonorUpdateRepository updateRepository;
        private IDonorInspectionRepository inspectionRepository;
        private IExpandHlaPhenotypeService expandHlaPhenotypeService;
        private IMapper mapper;
        private IActiveRepositoryFactory repositoryFactory;

        [SetUp]
        public void SetUp()
        {
            updateRepository = Substitute.For<IDonorUpdateRepository>();
            inspectionRepository = Substitute.For<IDonorInspectionRepository>();
            repositoryFactory = Substitute.For<IActiveRepositoryFactory>();
            expandHlaPhenotypeService = Substitute.For<IExpandHlaPhenotypeService>();
            mapper = AutomapperConfig.CreateMapper();

            repositoryFactory.GetDonorInspectionRepository().Returns(inspectionRepository);
            repositoryFactory.GetDonorUpdateRepository().Returns(updateRepository);
            
            donorService = new SearchAlgorithm.Services.Donors.DonorService(expandHlaPhenotypeService, repositoryFactory, mapper);
        }

        [Test]
        public void CreateDonor_WhenDonorExists_ThrowsException()
        {
            inspectionRepository.GetDonors(Arg.Any<IEnumerable<int>>()).Returns(new[] { new DonorResult() });

            Assert.ThrowsAsync<NovaHttpException>(() => donorService.CreateDonor(new InputDonor()));
        }

        [Test]
        public async Task CreateDonor_WhenDonorDoesNotExist_CreatesDonor()
        {
            inspectionRepository.GetDonors(Arg.Any<IEnumerable<int>>()).Returns(new List<DonorResult>(),
                new List<DonorResult> { new DonorResult() });

            var inputDonor = new InputDonor
            {
                HlaNames = new PhenotypeInfo<string>("hla")
            };
            await donorService.CreateDonor(inputDonor);

            await updateRepository.Received()
                .InsertBatchOfDonorsWithExpandedHla(Arg.Any<IEnumerable<InputDonorWithExpandedHla>>());
        }

        [Test]
        public void UpdateDonor_WhenDonorDoesNotExist_ThrowsException()
        {
            Assert.ThrowsAsync<NovaNotFoundException>(() => donorService.UpdateDonor(new InputDonor()));
        }

        [Test]
        public async Task UpdateDonor_WhenDonorExists_UpdatesDonor()
        {
            var inputDonor = new InputDonor
            {
                HlaNames = new PhenotypeInfo<string>("hla")
            };
            inspectionRepository.GetDonors(Arg.Any<IEnumerable<int>>()).Returns(new[] { new DonorResult() });

            await donorService.UpdateDonor(inputDonor);

            await updateRepository.Received()
                .UpdateBatchOfDonorsWithExpandedHla(Arg.Any<IEnumerable<InputDonorWithExpandedHla>>());
        }

        [Test]
        public async Task SetDonorAsUnavailableForSearchBatch_SetsDonorAsUnavailableForSearch()
        {
            const int donorId = 123;

            inspectionRepository.GetDonors(Arg.Any<IEnumerable<int>>()).Returns(new[] { new DonorResult() });

            await donorService.SetDonorAsUnavailableForSearchBatch(new []{donorId});

            await updateRepository.Received().SetDonorAsUnavailableForSearchBatch(Arg.Is<IEnumerable<int>>(x => x.Single() == donorId));
        }
    }
}