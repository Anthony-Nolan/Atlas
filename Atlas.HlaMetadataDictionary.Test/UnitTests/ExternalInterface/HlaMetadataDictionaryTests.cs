using System;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.HlaMetadataDictionary.Services;
using Atlas.HlaMetadataDictionary.Services.DataGeneration;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.ExternalInterface
{
    [TestFixture]
    internal class HlaMetadataDictionaryTests
    {
        private const string DefaultVersion = "hla-version";

        private IRecreateHlaMetadataService recreateMetadataService;
        private IAlleleNamesLookupService alleleNamesLookupService;
        private IHlaMatchingLookupService hlaMatchingLookupService;
        private ILocusHlaMatchingLookupService locusHlaMatchingLookupService;
        private IHlaScoringLookupService hlaScoringLookupService;
        private IHlaLookupResultsService hlaLookupResultsService;
        private IDpb1TceGroupLookupService dpb1TceGroupLookupService;
        private IWmdaHlaNomenclatureVersionAccessor wmdaHlaNomenclatureVersionAccessor;
        private ILogger logger;

        private IHlaMetadataDictionary hlaMetadataDictionary;

        [SetUp]
        public void SetUp()
        {
            recreateMetadataService = Substitute.For<IRecreateHlaMetadataService>();
            alleleNamesLookupService = Substitute.For<IAlleleNamesLookupService>();
            hlaMatchingLookupService = Substitute.For<IHlaMatchingLookupService>();
            locusHlaMatchingLookupService = Substitute.For<ILocusHlaMatchingLookupService>();
            hlaScoringLookupService = Substitute.For<IHlaScoringLookupService>();
            hlaLookupResultsService = Substitute.For<IHlaLookupResultsService>();
            dpb1TceGroupLookupService = Substitute.For<IDpb1TceGroupLookupService>();
            wmdaHlaNomenclatureVersionAccessor = Substitute.For<IWmdaHlaNomenclatureVersionAccessor>();
            logger = Substitute.For<ILogger>();

            hlaMetadataDictionary =
                new HlaMetadataDictionary.ExternalInterface.HlaMetadataDictionary(
                    DefaultVersion,
                    recreateMetadataService,
                    alleleNamesLookupService,
                    hlaMatchingLookupService,
                    locusHlaMatchingLookupService,
                    hlaScoringLookupService,
                    hlaLookupResultsService,
                    dpb1TceGroupLookupService,
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