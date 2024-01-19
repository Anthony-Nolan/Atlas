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
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh
{
    [TestFixture]
    public class DonorImporterTests
    {
        private IDonorImporter donorImporter;

        private IDataRefreshRepository dataRefreshRepository;
        private IDonorImportRepository donorImportRepository;
        private IDormantRepositoryFactory repositoryFactory;
        private IDonorInfoConverter donorInfoConverter;
        private IFailedDonorsNotificationSender failedDonorsNotificationSender;
        private IMatchingAlgorithmImportLogger logger;
        private IDonorReader donorReader;

        [SetUp]
        public void SetUp()
        {
            dataRefreshRepository = Substitute.For<IDataRefreshRepository>();
            donorImportRepository = Substitute.For<IDonorImportRepository>();
            repositoryFactory = Substitute.For<IDormantRepositoryFactory>();
            repositoryFactory.GetDataRefreshRepository().Returns(dataRefreshRepository);
            repositoryFactory.GetDonorImportRepository().Returns(donorImportRepository);

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
    }
}