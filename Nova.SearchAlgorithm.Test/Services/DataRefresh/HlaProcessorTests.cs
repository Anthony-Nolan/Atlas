using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Common.Repositories.DonorUpdates;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Nova.SearchAlgorithm.Services.DataRefresh;
using Nova.SearchAlgorithm.Services.MatchingDictionary;
using Nova.Utils.ApplicationInsights;
using NSubstitute;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Services.DataRefresh
{
    [TestFixture]
    public class HlaProcessorTests
    {
        private ILogger logger;
        private IExpandHlaPhenotypeService expandHlaPhenotypeService;
        private IAntigenCachingService antigenCachingService;
        private IDonorImportRepository donorImportRepository;
        private IDataRefreshRepository dataRefreshRepository;
        private IHlaMatchingLookupRepository hlaMatchingLookupRepository;
        private IAlleleNamesLookupRepository alleleNamesLookupRepository;
        private IPGroupRepository pGroupRepository;
        private ITransientRepositoryFactory repositoryFactory;

        private IHlaProcessor hlaProcessor;

        [SetUp]
        public void SetUp()
        {
            logger = Substitute.For<ILogger>();
            expandHlaPhenotypeService = Substitute.For<IExpandHlaPhenotypeService>();
            antigenCachingService = Substitute.For<IAntigenCachingService>();
            donorImportRepository = Substitute.For<IDonorImportRepository>();
            dataRefreshRepository = Substitute.For<IDataRefreshRepository>();
            hlaMatchingLookupRepository = Substitute.For<IHlaMatchingLookupRepository>();
            alleleNamesLookupRepository = Substitute.For<IAlleleNamesLookupRepository>();
            pGroupRepository = Substitute.For<IPGroupRepository>();
            repositoryFactory = Substitute.For<ITransientRepositoryFactory>();

            repositoryFactory.GetDataRefreshRepository().Returns(dataRefreshRepository);
            repositoryFactory.GetDonorImportRepository().Returns(donorImportRepository);
            repositoryFactory.GetPGroupRepository().Returns(pGroupRepository);
            
            hlaProcessor = new HlaProcessor(
                logger,
                expandHlaPhenotypeService,
                antigenCachingService,
                repositoryFactory,
                hlaMatchingLookupRepository,
                alleleNamesLookupRepository
            );
        }

        [Test]
        public void HlaProcessor_UsesDormantPGroupRepository()
        {
            repositoryFactory.Received().GetPGroupRepository(false);
            repositoryFactory.DidNotReceive().GetPGroupRepository(true);
        }

        [Test]
        public void HlaProcessor_UsesDormantDonorImportRepository()
        {
            repositoryFactory.Received().GetDonorImportRepository(false);
            repositoryFactory.DidNotReceive().GetDonorImportRepository(true);
        }

        [Test]
        public void HlaProcessor_UsesDormantDataRefreshRepository()
        {
            repositoryFactory.Received().GetDataRefreshRepository(false);
            repositoryFactory.DidNotReceive().GetDataRefreshRepository(true);
        }
    }
}