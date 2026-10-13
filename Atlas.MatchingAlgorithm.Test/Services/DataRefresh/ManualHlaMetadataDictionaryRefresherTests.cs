using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.DataRefresh;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh
{
    [TestFixture]
    public class ManualHlaMetadataDictionaryRefresherTests
    {
        private const string ActiveHlaVersion = "3650";

        private IHlaMetadataDictionary dictionary;
        private IDataRefreshHistoryRepository dataRefreshHistoryRepository;
        private IManualHlaMetadataDictionaryRefresher refresher;

        [SetUp]
        public void SetUp()
        {
            dictionary = Substitute.For<IHlaMetadataDictionary>();

            var dictionaryFactory = Substitute.For<IHlaMetadataDictionaryFactory>();
            dictionaryFactory.BuildDictionary(ActiveHlaVersion).Returns(dictionary);

            var activeVersionAccessor = Substitute.For<IActiveHlaNomenclatureVersionAccessor>();
            activeVersionAccessor.GetActiveHlaNomenclatureVersion().Returns(ActiveHlaVersion);

            dataRefreshHistoryRepository = Substitute.For<IDataRefreshHistoryRepository>();
            NoDataRefreshIsInProgress();

            refresher = new ManualHlaMetadataDictionaryRefresher(dictionaryFactory, activeVersionAccessor, dataRefreshHistoryRepository);
        }

        private void NoDataRefreshIsInProgress() =>
            dataRefreshHistoryRepository.GetIncompleteRefreshJobs().Returns(new List<DataRefreshRecord>());

        private void ADataRefreshIsInProgress() =>
            dataRefreshHistoryRepository.GetIncompleteRefreshJobs().Returns(new List<DataRefreshRecord> { new() });

        [Test]
        public async Task TryRecreate_WhenNoDataRefreshIsInProgress_RecreatesTheDictionary()
        {
            var recreated = await refresher.TryRecreate(CreationBehaviour.Latest);

            recreated.Should().BeTrue();
            await dictionary.Received(1).RecreateHlaMetadataDictionary(CreationBehaviour.Latest);
        }

        /// <summary>
        /// A data refresh recreates the dictionary as its own first stage. Recreating alongside it would have two
        /// processes rewriting the same tables, with the last to update the table reference orphaning the other.
        /// </summary>
        [Test]
        public async Task TryRecreate_WhenADataRefreshIsInProgress_RecreatesNothing()
        {
            ADataRefreshIsInProgress();

            var recreated = await refresher.TryRecreate(CreationBehaviour.Latest);

            recreated.Should().BeFalse();
            await dictionary.DidNotReceiveWithAnyArgs().RecreateHlaMetadataDictionary(default);
        }

        /// <summary>The route that prompted the guard: re-creating a version that is already active.</summary>
        [Test]
        public async Task TryRecreate_ForASpecificVersion_WhenADataRefreshIsInProgress_RecreatesNothing()
        {
            ADataRefreshIsInProgress();

            var recreated = await refresher.TryRecreate(CreationBehaviour.Specific(ActiveHlaVersion));

            recreated.Should().BeFalse();
            await dictionary.DidNotReceiveWithAnyArgs().RecreateHlaMetadataDictionary(default);
        }

        [Test]
        public async Task TryRecreate_PassesTheRequestedBehaviourThrough()
        {
            var specific = CreationBehaviour.Specific(ActiveHlaVersion);

            await refresher.TryRecreate(specific);

            await dictionary.Received(1).RecreateHlaMetadataDictionary(specific);
        }
    }
}
