using Atlas.MatchingAlgorithm.Clients.Http;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.Utils.Notifications;
using ILogger = Atlas.Utils.Core.ApplicationInsights.ILogger;

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
            donorServiceClient.GetDonorsInfoForSearchAlgorithm(Arg.Any<int>(), Arg.Any<int>())                .Returns(EmptyPage);

            donorInfoConverter = Substitute.For<IDonorInfoConverter>();
            donorInfoConverter.ConvertDonorInfoAsync(Arg.Any<IEnumerable<SearchableDonorInformation>>(), Arg.Any<string>())
                .Returns(new DonorBatchProcessingResult<DonorInfo>());

            failedDonorsNotificationSender = Substitute.For<IFailedDonorsNotificationSender>();
            logger = Substitute.For<ILogger>();

            donorImporter = new DonorImporter(repositoryFactory, donorServiceClient, donorInfoConverter, failedDonorsNotificationSender, logger);
        }

        [Test]
        public async Task ImportDonors_GetsHighestDonorIdFromDataRefreshRepo()
        {
            await donorImporter.ImportDonors();

            await dataRefreshRepository.Received().HighestDonorId();
        }

        [Test]
        public async Task ImportDonors_FetchesDonorPageWithHighestDonorId()
        {
            const int donorId = 123;
            dataRefreshRepository.HighestDonorId().Returns(donorId);

            await donorImporter.ImportDonors();

            await donorServiceClient.Received()
                .GetDonorsInfoForSearchAlgorithm(Arg.Any<int>(), donorId);
        }

        [Test]
        public async Task ImportDonors_EmptyPage_DoesNotConvertDonorInfo()
        {
            await donorImporter.ImportDonors();

            await donorInfoConverter.DidNotReceive().ConvertDonorInfoAsync(
                Arg.Any<IEnumerable<SearchableDonorInformation>>(), Arg.Any<string>());
        }

        [Test]
        public async Task ImportDonors_EmptyPage_DoesNotInsertDonors()
        {
            await donorImporter.ImportDonors();

            await donorImportRepository.DidNotReceive().InsertBatchOfDonors(
                Arg.Any<IEnumerable<DonorInfo>>());
        }

        [Test]
        public async Task ImportDonors_PageWithDonors_ConvertsDonorInfo()
        {
            const int donorId = 123;
            var pageWithDonor = new SearchableDonorInformationPage
            {
                DonorsInfo = new List<SearchableDonorInformation>
                {
                    new SearchableDonorInformation
                    {
                        DonorId = donorId
                    }
                }
            };

            // return empty page last to stop pagination loop
            donorServiceClient
                .GetDonorsInfoForSearchAlgorithm(Arg.Any<int>(), Arg.Any<int>())
                .Returns(pageWithDonor, EmptyPage);

            await donorImporter.ImportDonors();

            await donorInfoConverter.Received(1).ConvertDonorInfoAsync(
                    Arg.Is<IEnumerable<SearchableDonorInformation>>(x => x.Single().DonorId == donorId), 
                    Arg.Any<string>());
        }

        [Test]
        public async Task ImportDonors_PageWithDonors_InsertsDonors()
        {
            const int donorId = 123;

            var pageWithDonor = new SearchableDonorInformationPage
            {
                DonorsInfo = new List<SearchableDonorInformation>
                {
                    new SearchableDonorInformation
                    {
                        DonorId = donorId
                    }
                }
            };

            // return empty page last to stop pagination loop
            donorServiceClient
                .GetDonorsInfoForSearchAlgorithm(Arg.Any<int>(), Arg.Any<int>())
                .Returns(pageWithDonor, EmptyPage);

            donorInfoConverter
                .ConvertDonorInfoAsync(pageWithDonor.DonorsInfo, Arg.Any<string>())
                .Returns(new DonorBatchProcessingResult<DonorInfo>
                {
                    ProcessingResults = new List<DonorInfo>
                    {
                        new DonorInfo
                        {
                            DonorId = donorId
                        }
                    }
                });

            await donorImporter.ImportDonors();

            await donorImportRepository.Received(1).InsertBatchOfDonors(
                Arg.Is<IEnumerable<DonorInfo>>(x => x.Single().DonorId == donorId));
        }

        [Test]
        public async Task ImportDonors_PageWithDonorsButNoLastId_GetHighestDonorIdFromRefreshRepo()
        {
            var pageWithDonor = new SearchableDonorInformationPage
            {
                DonorsInfo = new List<SearchableDonorInformation>
                {
                    new SearchableDonorInformation
                    {
                        DonorId = 1
                    }
                }
            };

            // return empty page last to stop pagination loop
            donorServiceClient
                .GetDonorsInfoForSearchAlgorithm(Arg.Any<int>(), Arg.Any<int>())
                .Returns(pageWithDonor, EmptyPage);

            await donorImporter.ImportDonors();

            // Expect highest donor ID to be fetched twice:
            // before pagination loop and for the first page
            await dataRefreshRepository.Received(2).HighestDonorId();
        }

        [Test]
        public async Task ImportDonors_PageWithDonorsButNoLastId_FetchesDonorPageWithHighestDonorId()
        {
            const int firstId = 1;
            const int secondId = 2;

            var pageWithDonor = new SearchableDonorInformationPage
            {
                DonorsInfo = new List<SearchableDonorInformation>
                {
                    new SearchableDonorInformation
                    {
                        DonorId = 1
                    }
                }
            };

            // return empty page last to stop pagination loop
            donorServiceClient
                .GetDonorsInfoForSearchAlgorithm(Arg.Any<int>(), Arg.Any<int>())
                .Returns(pageWithDonor, EmptyPage);

            // first id returned before pagination loop; second when the page is processed
            dataRefreshRepository.HighestDonorId().Returns(firstId, secondId);

            await donorImporter.ImportDonors();

            await donorServiceClient.Received(1)
                .GetDonorsInfoForSearchAlgorithm(Arg.Any<int>(), secondId);
        }

        [Test]
        public async Task ImportDonors_PageWithDonorsAndLastId_DoesNotGetHighestDonorIdFromRefreshRepo()
        {
            const int lastId = 1;

            var firstPageWithDonor = new SearchableDonorInformationPage
            {
                DonorsInfo = new List<SearchableDonorInformation>
                {
                    new SearchableDonorInformation
                    {
                        DonorId = 1
                    }
                }
            };

            var secondPageWithDonorAndLastId = new SearchableDonorInformationPage
            {
                DonorsInfo = new List<SearchableDonorInformation>
                {
                    new SearchableDonorInformation
                    {
                        DonorId = 2
                    }
                },
                LastId = lastId
            };

            // return empty page last to stop pagination loop
            donorServiceClient
                .GetDonorsInfoForSearchAlgorithm(Arg.Any<int>(), Arg.Any<int>())
                .Returns(
                    firstPageWithDonor,
                    secondPageWithDonorAndLastId,
                    EmptyPage);

            await donorImporter.ImportDonors();

            // Expect highest donor ID to only be fetched twice:
            // before pagination loop, for the first page, but not for the second page.
            await dataRefreshRepository.Received(2).HighestDonorId();
        }

        [Test]
        public async Task ImportDonors_PageWithDonorsAndLastId_FetchesDonorPageWithLastId()
        {
            const int donorId = 123;
            const int lastId = 1;

            var pageWithDonor = new SearchableDonorInformationPage
            {
                DonorsInfo = new List<SearchableDonorInformation>
                {
                    new SearchableDonorInformation
                    {
                        DonorId = donorId
                    }
                },
                LastId = lastId
            };

            // return empty page last to stop pagination loop
            donorServiceClient
                .GetDonorsInfoForSearchAlgorithm(Arg.Any<int>(), Arg.Any<int>())
                .Returns(pageWithDonor, EmptyPage);

            await donorImporter.ImportDonors();

            await donorServiceClient.Received(1)
                .GetDonorsInfoForSearchAlgorithm(Arg.Any<int>(), lastId);
        }

        [Test]
        public async Task ImportDonors_PageWithFailedDonor_SendsFailedDonorsAlert()
        {
            const string failedDonorId = "donor-id";

            var pageWithDonor = new SearchableDonorInformationPage
            {
                DonorsInfo = new List<SearchableDonorInformation>
                {
                    new SearchableDonorInformation()
                }
            };

            donorInfoConverter
                .ConvertDonorInfoAsync(pageWithDonor.DonorsInfo, Arg.Any<string>())
                .Returns(new DonorBatchProcessingResult<DonorInfo>
                {
                    FailedDonors = new List<FailedDonorInfo>
                    {
                        new FailedDonorInfo
                        {
                            DonorId = failedDonorId
                        }
                    }
                });

            // return empty page last to stop pagination loop
            donorServiceClient
                .GetDonorsInfoForSearchAlgorithm(Arg.Any<int>(), Arg.Any<int>())
                .Returns(pageWithDonor, EmptyPage);

            await donorImporter.ImportDonors();

            await failedDonorsNotificationSender.Received(1)
                .SendFailedDonorsAlert(
                    Arg.Is<IEnumerable<FailedDonorInfo>>(x => x.Single().DonorId == failedDonorId),
                    Arg.Any<string>(),
                    Arg.Any<Priority>());
        }
    }
}