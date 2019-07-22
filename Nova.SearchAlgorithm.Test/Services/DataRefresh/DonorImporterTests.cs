using Nova.SearchAlgorithm.Clients.Http;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Common.Repositories.DonorUpdates;
using Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Nova.SearchAlgorithm.Services.DataRefresh;
using Nova.Utils.ApplicationInsights;
using NSubstitute;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Services.DataRefresh
{
    [TestFixture]
    public class DonorImporterTests
    {
        private IDataRefreshRepository dataRefreshRepository;
        private IDonorImportRepository donorImportRepository;
        private IDonorServiceClient donorServiceClient;
        private ITransientRepositoryFactory repositoryFactory;
        private ILogger logger;

        private IDonorImporter donorImporter;

        [SetUp]
        public void SetUp()
        {
            dataRefreshRepository = Substitute.For<IDataRefreshRepository>();
            donorImportRepository = Substitute.For<IDonorImportRepository>();
            donorServiceClient = Substitute.For<IDonorServiceClient>();
            repositoryFactory = Substitute.For<ITransientRepositoryFactory>();
            logger = Substitute.For<ILogger>();

            repositoryFactory.GetDataRefreshRepository().Returns(dataRefreshRepository);
            repositoryFactory.GetDonorImportRepository().Returns(donorImportRepository);

            donorImporter = new DonorImporter(repositoryFactory, donorServiceClient, logger);
        }

        [Test]
        public void DonorImporter_UsesDormantDonorImportRepository()
        {
            repositoryFactory.Received().GetDonorImportRepository(false);
            repositoryFactory.DidNotReceive().GetDonorImportRepository(true);
        }

        [Test]
        public void DonorImporter_UsesDormantDataRefreshRepository()
        {
            repositoryFactory.Received().GetDataRefreshRepository(false);
            repositoryFactory.DidNotReceive().GetDataRefreshRepository(true);
        }
    }
}