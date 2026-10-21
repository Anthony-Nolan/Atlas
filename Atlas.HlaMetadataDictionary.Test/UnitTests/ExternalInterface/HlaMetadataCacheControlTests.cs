using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.ExternalInterface
{
    [TestFixture]
    internal class HlaMetadataCacheControlTests
    {
        private const string Version = "3660";

        private IAlleleNamesMetadataRepository alleleNames;
        private IAlleleGroupsMetadataRepository alleleGroups;
        private IHlaMatchingMetadataRepository matching;
        private IHlaScoringMetadataRepository scoring;
        private IDpb1TceGroupsMetadataRepository dpb1TceGroups;
        private IGGroupToPGroupMetadataRepository gGroupToPGroup;
        private IHlaNameToSmallGGroupLookupRepository smallGGroups;
        private ISmallGGroupToPGroupMetadataRepository smallGGroupToPGroup;
        private ISerologyToAllelesMetadataRepository serologyToAlleles;
        private IHlaMetadataCacheControl cacheControl;

        [SetUp]
        public void SetUp()
        {
            alleleNames = Substitute.For<IAlleleNamesMetadataRepository>();
            alleleGroups = Substitute.For<IAlleleGroupsMetadataRepository>();
            matching = Substitute.For<IHlaMatchingMetadataRepository>();
            scoring = Substitute.For<IHlaScoringMetadataRepository>();
            dpb1TceGroups = Substitute.For<IDpb1TceGroupsMetadataRepository>();
            gGroupToPGroup = Substitute.For<IGGroupToPGroupMetadataRepository>();
            smallGGroups = Substitute.For<IHlaNameToSmallGGroupLookupRepository>();
            smallGGroupToPGroup = Substitute.For<ISmallGGroupToPGroupMetadataRepository>();
            serologyToAlleles = Substitute.For<ISerologyToAllelesMetadataRepository>();

            cacheControl = new HlaMetadataCacheControl(
                Version, alleleNames, alleleGroups, matching, scoring, dpb1TceGroups, gGroupToPGroup, smallGGroups, smallGGroupToPGroup, serologyToAlleles);
        }

        [Test]
        public async Task PreWarmMatchPredictionCaches_LoadsTheTablesMatchPredictionReads()
        {
            await cacheControl.PreWarmMatchPredictionCaches();

            await alleleNames.Received(1).LoadDataIntoMemory(Version);
            await alleleGroups.Received(1).LoadDataIntoMemory(Version);
            await smallGGroups.Received(1).LoadDataIntoMemory(Version);
            await smallGGroupToPGroup.Received(1).LoadDataIntoMemory(Version);
            await gGroupToPGroup.Received(1).LoadDataIntoMemory(Version);
        }

        [Test]
        public async Task PreWarmMatchPredictionCaches_DoesNotLoadTheTablesMatchPredictionDoesNotRead()
        {
            await cacheControl.PreWarmMatchPredictionCaches();

            await matching.DidNotReceiveWithAnyArgs().LoadDataIntoMemory(default);
            await scoring.DidNotReceiveWithAnyArgs().LoadDataIntoMemory(default);
            await dpb1TceGroups.DidNotReceiveWithAnyArgs().LoadDataIntoMemory(default);
            await serologyToAlleles.DidNotReceiveWithAnyArgs().LoadDataIntoMemory(default);
        }
    }
}
