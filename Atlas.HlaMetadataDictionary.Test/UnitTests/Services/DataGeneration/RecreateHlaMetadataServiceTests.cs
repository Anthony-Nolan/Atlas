using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using Atlas.HlaMetadataDictionary.Services.DataGeneration;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.DataGeneration
{
    [TestFixture]
    internal class RecreateHlaMetadataServiceTests
    {
        private const string Version = "3560";

        private IHlaMetadataGenerationOrchestrator generationOrchestrator;
        private IHlaMetadataRepository[] repositories;
        private RecreateHlaMetadataService service;

        [SetUp]
        public void SetUp()
        {
            generationOrchestrator = Substitute.For<IHlaMetadataGenerationOrchestrator>();
            generationOrchestrator.GenerateAllHlaMetadata(default).ReturnsForAnyArgs(new HlaMetadataCollection());

            var alleleNames = Substitute.For<IAlleleNamesMetadataRepository>();
            var hlaMatching = Substitute.For<IHlaMatchingMetadataRepository>();
            var hlaScoring = Substitute.For<IHlaScoringMetadataRepository>();
            var dpb1TceGroups = Substitute.For<IDpb1TceGroupsMetadataRepository>();
            var alleleGroups = Substitute.For<IAlleleGroupsMetadataRepository>();
            var gGroupToPGroup = Substitute.For<IGGroupToPGroupMetadataRepository>();
            var smallGGroups = Substitute.For<IHlaNameToSmallGGroupLookupRepository>();
            var smallGGroupToPGroup = Substitute.For<ISmallGGroupToPGroupMetadataRepository>();
            var serologyToAlleles = Substitute.For<ISerologyToAllelesMetadataRepository>();

            repositories = new IHlaMetadataRepository[]
            {
                alleleNames, hlaMatching, hlaScoring, dpb1TceGroups, alleleGroups,
                gGroupToPGroup, smallGGroups, smallGGroupToPGroup, serologyToAlleles
            };

            service = new RecreateHlaMetadataService(
                generationOrchestrator,
                alleleNames,
                hlaMatching,
                hlaScoring,
                dpb1TceGroups,
                alleleGroups,
                gGroupToPGroup,
                smallGGroups,
                smallGGroupToPGroup,
                serologyToAlleles,
                Substitute.For<IAtlasLogger>());
        }

        [Test]
        public async Task RefreshAllHlaMetadata_RecreatesEveryTableWithTheSameSnapshot()
        {
            var snapshotUtc = new DateTime(2026, 10, 5, 15, 4, 7, 123, DateTimeKind.Utc);

            await service.RefreshAllHlaMetadata(Version, snapshotUtc);

            foreach (var repository in repositories)
            {
                await repository.Received(1).RecreateHlaMetadataTable(Arg.Any<IEnumerable<ISerialisableHlaMetadata>>(), Version, snapshotUtc);
            }
        }
    }
}
