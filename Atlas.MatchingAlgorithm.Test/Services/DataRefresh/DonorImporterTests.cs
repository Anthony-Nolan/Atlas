using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.DonorImport.ExternalInterface;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Clients.Http.DonorService;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh;
using Atlas.MatchingAlgorithm.Services.Donors;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh
{
    [TestFixture]
    public class DonorImporterTests
    {
        private IDonorImporter donorImporter;

        private IDataRefreshRepository dataRefreshRepository;
        private IDonorImportRepository donorImportRepository;
        private IDormantRepositoryFactory repositoryFactory;
        private IDonorServiceClient donorServiceClient;
        private IDonorInfoConverter donorInfoConverter;
        private IFailedDonorsNotificationSender failedDonorsNotificationSender;
        private ILogger logger;
        private IDonorReader donorReader;

        private static readonly SearchableDonorInformationPage EmptyPage = new SearchableDonorInformationPage
        {
            DonorsInfo = new List<SearchableDonorInformation>()
        };

        [SetUp]
        public void SetUp()
        {
            dataRefreshRepository = Substitute.For<IDataRefreshRepository>();
            donorImportRepository = Substitute.For<IDonorImportRepository>();
            repositoryFactory = Substitute.For<IDormantRepositoryFactory>();
            repositoryFactory.GetDataRefreshRepository().Returns(dataRefreshRepository);
            repositoryFactory.GetDonorImportRepository().Returns(donorImportRepository);

            donorServiceClient = Substitute.For<IDonorServiceClient>();
            donorServiceClient.GetDonorsInfoForSearchAlgorithm(Arg.Any<int>(), Arg.Any<int>()).Returns(EmptyPage);

            donorInfoConverter = Substitute.For<IDonorInfoConverter>();
            donorInfoConverter.ConvertDonorInfoAsync(Arg.Any<IEnumerable<SearchableDonorInformation>>(), Arg.Any<string>())
                .Returns(new DonorBatchProcessingResult<DonorInfo>());

            failedDonorsNotificationSender = Substitute.For<IFailedDonorsNotificationSender>();
            logger = Substitute.For<ILogger>();
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
            var donor = DonorBuilder.New.With(d => d.DonorId, "123").Build();

            donorReader.GetAllDonors().Returns(new List<Donor> {donor});

            await donorImporter.ImportDonors();

            await donorInfoConverter.Received(1).ConvertDonorInfoAsync(
                Arg.Is<IEnumerable<SearchableDonorInformation>>(x => x.Single().DonorId.ToString() == donor.DonorId),
                Arg.Any<string>());
        }

        [Test]
        public async Task ImportDonors_InsertsDonors()
        {
            // TODO: ATLAS-294: Use string instead of int
            const int donorIdAsInt = 123;
            var donor = DonorBuilder.New.With(d => d.DonorId, donorIdAsInt.ToString()).Build();

            donorReader.GetAllDonors().Returns(new List<Donor> {donor});

            donorInfoConverter
                .ConvertDonorInfoAsync(null, null)
                .ReturnsForAnyArgs(new DonorBatchProcessingResult<DonorInfo>
                {
                    ProcessingResults = new List<DonorInfo>
                    {
                        new DonorInfo
                        {
                            DonorId = donorIdAsInt
                        }
                    }
                });

            await donorImporter.ImportDonors();

            await donorImportRepository.Received(1).InsertBatchOfDonors(
                Arg.Is<IEnumerable<DonorInfo>>(x => x.Single().DonorId == donorIdAsInt));
        }

        [Test]
        public async Task ImportDonors_WithFailedDonor_SendsFailedDonorsAlert()
        {
            const string failedDonorId = "1";

            donorInfoConverter
                .ConvertDonorInfoAsync(null, null)
                .ReturnsForAnyArgs(new DonorBatchProcessingResult<DonorInfo>
                {
                    FailedDonors = new List<FailedDonorInfo>
                    {
                        new FailedDonorInfo
                        {
                            DonorId = failedDonorId
                        }
                    }
                });
            
            var donor = DonorBuilder.New.With(d => d.DonorId, failedDonorId).Build();

            donorReader.GetAllDonors().Returns(new List<Donor> {donor});

            await donorImporter.ImportDonors();

            await failedDonorsNotificationSender.Received(1)
                .SendFailedDonorsAlert(
                    Arg.Is<IEnumerable<FailedDonorInfo>>(x => x.Single().DonorId == failedDonorId),
                    Arg.Any<string>(),
                    Arg.Any<Priority>());
        }
    }
}