using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.HlaMetadataDictionary.Services.DataGeneration;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using Atlas.HlaMetadataDictionary.Services.HlaConversion;
using Atlas.HlaMetadataDictionary.WmdaDataAccess;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.ExternalInterface
{
    [TestFixture]
    internal class HlaMetadataDictionaryTests
    {
        private const string DefaultVersion = "hla-version";

        private IRecreateHlaMetadataService recreateMetadataService;
        private IHlaConverter hlaConverter;
        private IHlaMatchingMetadataService hlaMatchingMetadataService;
        private ILocusHlaMatchingMetadataService locusHlaMatchingMetadataService;
        private IHlaScoringMetadataService hlaScoringMetadataService;
        private IDpb1TceGroupMetadataService dpb1TceGroupMetadataService;
        private IHlaMetadataGenerationOrchestrator hlaMetadataGenerationOrchestrator;
        private IWmdaHlaNomenclatureVersionAccessor wmdaHlaNomenclatureVersionAccessor;
        private ILogger logger;

        private IHlaMetadataDictionary hlaMetadataDictionary;

        [SetUp]
        public void SetUp()
        {
            recreateMetadataService = Substitute.For<IRecreateHlaMetadataService>();
            hlaConverter = Substitute.For<IHlaConverter>();
            hlaMatchingMetadataService = Substitute.For<IHlaMatchingMetadataService>();
            locusHlaMatchingMetadataService = Substitute.For<ILocusHlaMatchingMetadataService>();
            hlaScoringMetadataService = Substitute.For<IHlaScoringMetadataService>();
            dpb1TceGroupMetadataService = Substitute.For<IDpb1TceGroupMetadataService>();
            hlaMetadataGenerationOrchestrator = Substitute.For<IHlaMetadataGenerationOrchestrator>();
            wmdaHlaNomenclatureVersionAccessor = Substitute.For<IWmdaHlaNomenclatureVersionAccessor>();
            logger = Substitute.For<ILogger>();

            hlaMetadataDictionary = new HlaMetadataDictionary.ExternalInterface.HlaMetadataDictionary(
                DefaultVersion,
                recreateMetadataService,
                hlaConverter,
                hlaMatchingMetadataService,
                locusHlaMatchingMetadataService,
                hlaScoringMetadataService,
                dpb1TceGroupMetadataService,
                hlaMetadataGenerationOrchestrator,
                wmdaHlaNomenclatureVersionAccessor,
                logger);
        }

        [Test]
        public async Task RecreateHlaMetadataDictionaryIfNecessary_ForLatestVersion_WhenAlreadyUpToDate_DoesNotRecreateDictionary()
        {
            wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().Returns(DefaultVersion);

            await hlaMetadataDictionary.RecreateHlaMetadataDictionary(CreationBehaviour.Latest);

            await recreateMetadataService.DidNotReceiveWithAnyArgs().RefreshAllHlaMetadata(null);
        }

        [Test]
        public async Task RecreateHlaMetadataDictionaryIfNecessary_ForLatestVersion_WhenNotUpToDate_RecreatesDictionary()
        {
            wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().Returns("newer-version");

            await hlaMetadataDictionary.RecreateHlaMetadataDictionary(CreationBehaviour.Latest);

            await recreateMetadataService.ReceivedWithAnyArgs().RefreshAllHlaMetadata(null);
        }

        [Test]
        public async Task RecreateHlaMetadataDictionaryIfNecessary_ForActiveVersion_WhenAlreadyUpToDate_RecreatesDictionary()
        {
            wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().Returns(DefaultVersion);

            await hlaMetadataDictionary.RecreateHlaMetadataDictionary(CreationBehaviour.Active);

            await recreateMetadataService.ReceivedWithAnyArgs().RefreshAllHlaMetadata(null);
        }

        [Test]
        public async Task RecreateHlaMetadataDictionaryIfNecessary_ForSpecificVersion_WhenAlreadyUpToDate_RecreatesDictionary()
        {
            wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().Returns(DefaultVersion);

            await hlaMetadataDictionary.RecreateHlaMetadataDictionary(CreationBehaviour.Specific("different-version"));

            await recreateMetadataService.ReceivedWithAnyArgs().RefreshAllHlaMetadata(null);
        }
    }
}