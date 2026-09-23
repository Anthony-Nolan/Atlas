using System.Threading.Tasks;
using AwesomeAssertions;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.HlaMetadataDictionary.Services.DataGeneration;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using Atlas.HlaMetadataDictionary.Services.HlaConversion;
using Atlas.HlaMetadataDictionary.Services.HlaValidation;
using Atlas.HlaMetadataDictionary.Services.Notifications;
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
        private IHlaValidator hlaValidator;
        private IHlaMatchingMetadataService hlaMatchingMetadataService;
        private ILocusHlaMatchingMetadataService locusHlaMatchingMetadataService;
        private IHlaScoringMetadataService hlaScoringMetadataService;
        private IDpb1TceGroupMetadataService dpb1TceGroupMetadataService;
        private IGGroupToPGroupMetadataService gGroupToPGroupMetadataService;
        private ISmallGGroupToPGroupMetadataService smallGGroupToPGroupMetadataService;
        private ISerologyToAllelesMetadataService serologyToAllelesMetadataService;
        private IHlaMetadataGenerationOrchestrator hlaMetadataGenerationOrchestrator;
        private IWmdaHlaNomenclatureVersionAccessor wmdaHlaNomenclatureVersionAccessor;
        private IHlaMetadataDictionaryUpdateNotifier updateNotifier;
        private IAtlasLogger logger;

        private IHlaMetadataDictionary hlaMetadataDictionary;

        [SetUp]
        public void SetUp()
        {
            recreateMetadataService = Substitute.For<IRecreateHlaMetadataService>();
            hlaConverter = Substitute.For<IHlaConverter>();
            hlaValidator = Substitute.For<IHlaValidator>();
            hlaMatchingMetadataService = Substitute.For<IHlaMatchingMetadataService>();
            locusHlaMatchingMetadataService = Substitute.For<ILocusHlaMatchingMetadataService>();
            hlaScoringMetadataService = Substitute.For<IHlaScoringMetadataService>();
            dpb1TceGroupMetadataService = Substitute.For<IDpb1TceGroupMetadataService>();
            gGroupToPGroupMetadataService = Substitute.For<IGGroupToPGroupMetadataService>();
            smallGGroupToPGroupMetadataService = Substitute.For<ISmallGGroupToPGroupMetadataService>();
            serologyToAllelesMetadataService = Substitute.For<ISerologyToAllelesMetadataService>();
            hlaMetadataGenerationOrchestrator = Substitute.For<IHlaMetadataGenerationOrchestrator>();
            wmdaHlaNomenclatureVersionAccessor = Substitute.For<IWmdaHlaNomenclatureVersionAccessor>();
            updateNotifier = Substitute.For<IHlaMetadataDictionaryUpdateNotifier>();
            logger = Substitute.For<IAtlasLogger>();

            hlaMetadataDictionary = new HlaMetadataDictionary.ExternalInterface.HlaMetadataDictionary(
                DefaultVersion,
                recreateMetadataService,
                hlaConverter,
                hlaValidator,
                hlaMatchingMetadataService,
                locusHlaMatchingMetadataService,
                hlaScoringMetadataService,
                dpb1TceGroupMetadataService,
                gGroupToPGroupMetadataService,
                smallGGroupToPGroupMetadataService,
                serologyToAllelesMetadataService,
                hlaMetadataGenerationOrchestrator,
                wmdaHlaNomenclatureVersionAccessor,
                updateNotifier,
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

        [Test]
        public async Task RecreateHlaMetadataDictionary_WhenDictionaryIsRecreated_NotifiesOfUpdate()
        {
            wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().Returns("newer-version");

            await hlaMetadataDictionary.RecreateHlaMetadataDictionary(CreationBehaviour.Latest);

            await updateNotifier.Received().NotifyOfUpdate("newer-version");
        }

        [Test]
        public async Task RecreateHlaMetadataDictionary_WhenDictionaryIsNotRecreated_DoesNotNotifyOfUpdate()
        {
            wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().Returns(DefaultVersion);

            await hlaMetadataDictionary.RecreateHlaMetadataDictionary(CreationBehaviour.Latest);

            await updateNotifier.DidNotReceiveWithAnyArgs().NotifyOfUpdate(null);
        }

        /// <summary>
        /// The case ATL-395 was raised for: a forced recreation at the version that is already active changes the
        /// stored data without changing anything a consumer keys its cache on, so the notification is the only signal
        /// there is.
        /// </summary>
        [Test]
        public async Task RecreateHlaMetadataDictionary_ForSpecificVersionMatchingActiveVersion_NotifiesOfUpdate()
        {
            await hlaMetadataDictionary.RecreateHlaMetadataDictionary(CreationBehaviour.Specific(DefaultVersion));

            await updateNotifier.Received().NotifyOfUpdate(DefaultVersion);
        }

        [Test]
        public async Task RecreateHlaMetadataDictionary_NotifiesOnlyAfterDataHasBeenRewritten()
        {
            var notifiedBeforeDataWasRewritten = false;
            var dataWasRewritten = false;

            recreateMetadataService
                .When(s => s.RefreshAllHlaMetadata(Arg.Any<string>()))
                .Do(_ => dataWasRewritten = true);
            updateNotifier
                .When(n => n.NotifyOfUpdate(Arg.Any<string>()))
                .Do(_ => notifiedBeforeDataWasRewritten = !dataWasRewritten);

            await hlaMetadataDictionary.RecreateHlaMetadataDictionary(CreationBehaviour.Specific(DefaultVersion));

            // A consumer that clears its cache before the new data is in place would just re-cache the old data.
            notifiedBeforeDataWasRewritten.Should().BeFalse();
        }
    }
}