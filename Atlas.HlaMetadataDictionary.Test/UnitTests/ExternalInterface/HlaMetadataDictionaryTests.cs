using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.HlaMetadataDictionary.Services.DataGeneration;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using Atlas.HlaMetadataDictionary.Services.HlaConversion;
using Atlas.HlaMetadataDictionary.Services.HlaValidation;
using Atlas.HlaMetadataDictionary.Repositories;
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
        private IHlaMetadataRecreationRepository recreationRepository;
        private IHlaMetadataCacheInvalidator cacheInvalidator;
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
            recreationRepository = Substitute.For<IHlaMetadataRecreationRepository>();
            cacheInvalidator = Substitute.For<IHlaMetadataCacheInvalidator>();
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
                recreationRepository,
                cacheInvalidator,
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

            await recreationRepository.Received().RecordRecreation("newer-version");
        }

        [Test]
        public async Task RecreateHlaMetadataDictionary_WhenDictionaryIsNotRecreated_DoesNotNotifyOfUpdate()
        {
            wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().Returns(DefaultVersion);

            await hlaMetadataDictionary.RecreateHlaMetadataDictionary(CreationBehaviour.Latest);

            await recreationRepository.DidNotReceiveWithAnyArgs().RecordRecreation(null);
            cacheInvalidator.DidNotReceiveWithAnyArgs().InvalidateCaches(null);
        }

        /// <summary>
        /// The case this was built for: a forced recreation at the version that is already active changes the
        /// stored data without changing anything a consumer keys its cache on, so the notification is the only signal
        /// there is.
        /// </summary>
        [Test]
        public async Task RecreateHlaMetadataDictionary_ForSpecificVersionMatchingActiveVersion_NotifiesOfUpdate()
        {
            await hlaMetadataDictionary.RecreateHlaMetadataDictionary(CreationBehaviour.Specific(DefaultVersion));

            await recreationRepository.Received().RecordRecreation(DefaultVersion);
        }

        [Test]
        public async Task RecreateHlaMetadataDictionary_RecordsTheRecreationOnlyAfterDataHasBeenRewritten()
        {
            var recordedBeforeDataWasRewritten = false;
            var dataWasRewritten = false;

            recreateMetadataService
                .When(s => s.RefreshAllHlaMetadata(Arg.Any<string>()))
                .Do(_ => dataWasRewritten = true);
            recreationRepository
                .When(r => r.RecordRecreation(Arg.Any<string>()))
                .Do(_ => recordedBeforeDataWasRewritten = !dataWasRewritten);

            await hlaMetadataDictionary.RecreateHlaMetadataDictionary(CreationBehaviour.Specific(DefaultVersion));

            // A consumer that drops its cache before the new data is in place would just re-cache the old data.
            recordedBeforeDataWasRewritten.Should().BeFalse();
        }

        /// <summary>
        /// The recreating process does not wait to notice its own stamp - it would be serving data it knows to be
        /// stale for a whole poll interval, for no reason.
        /// </summary>
        [Test]
        public async Task RecreateHlaMetadataDictionary_DropsThisProcesssOwnCachedCopy()
        {
            await hlaMetadataDictionary.RecreateHlaMetadataDictionary(CreationBehaviour.Specific(DefaultVersion));

            cacheInvalidator.Received().InvalidateCaches(DefaultVersion);
        }

        /// <summary>
        /// Storage has already been rewritten by the time the stamp is written, so failing here would fail a data
        /// refresh whose first stage had in fact succeeded. The recreation stands; only the telling of others is lost.
        /// </summary>
        [Test]
        public async Task RecreateHlaMetadataDictionary_WhenTheRecreationCannotBeRecorded_StillSucceeds()
        {
            recreationRepository
                .When(r => r.RecordRecreation(Arg.Any<string>()))
                .Do(_ => throw new Exception("storage is unavailable"));

            var version = await hlaMetadataDictionary.RecreateHlaMetadataDictionary(CreationBehaviour.Specific(DefaultVersion));

            version.Should().Be(DefaultVersion);
            cacheInvalidator.Received().InvalidateCaches(DefaultVersion);
        }
    }
}