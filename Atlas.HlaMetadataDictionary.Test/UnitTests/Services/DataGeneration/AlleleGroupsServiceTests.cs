using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.InternalModels.HLATypings;
using Atlas.HlaMetadataDictionary.Services.DataGeneration.Generators;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.DataGeneration
{
    [TestFixture]
    public class AlleleGroupsServiceTests
    {
        private const string TypingLocus = "A*";
        private const Locus DefaultLocus = Locus.A;
        private const string PGroupName = "p-group";
        private const string GGroupName = "g-group";
        private const string SmallGGroupName = "small-g-group";
        private const string AlleleName = "allele";
        private static readonly IReadOnlyCollection<string> Alleles = new List<string> { AlleleName };

        private IWmdaDataRepository wmdaDataRepository;
        private ISmallGGroupsBuilder smallGGroupsBuilder;
        private IHlaCategorisationService hlaCategorisationService;
        private IAlleleGroupsService alleleGroupsService;

        [SetUp]
        public void SetUp()
        {
            wmdaDataRepository = Substitute.For<IWmdaDataRepository>();
            smallGGroupsBuilder = Substitute.For<ISmallGGroupsBuilder>();
            hlaCategorisationService = Substitute.For<IHlaCategorisationService>();

            alleleGroupsService = new AlleleGroupsService(wmdaDataRepository, smallGGroupsBuilder, hlaCategorisationService);

            wmdaDataRepository.GetWmdaDataset(default).ReturnsForAnyArgs(WmdaDatasetBuilder.New.Build());
            hlaCategorisationService.GetHlaTypingCategory(AlleleName).Returns(HlaTypingCategory.Allele);
            hlaCategorisationService.GetHlaTypingCategory(PGroupName).Returns(HlaTypingCategory.PGroup);
            hlaCategorisationService.GetHlaTypingCategory(GGroupName).Returns(HlaTypingCategory.GGroup);
            hlaCategorisationService.GetHlaTypingCategory(SmallGGroupName).Returns(HlaTypingCategory.SmallGGroup);
        }

        [Test]
        public void GetPGroupsMetadata_GetsWmdaDatasetOfRequiredVersion()
        {
            const string version = "version";

            alleleGroupsService.GetAlleleGroupsMetadata(version);

            wmdaDataRepository.Received().GetWmdaDataset(version);
        }

        [Test]
        public void GetPGroupsMetadata_BuildsSmallGGroupsOfRequiredVersion()
        {
            const string version = "version";

            alleleGroupsService.GetAlleleGroupsMetadata(version);

            smallGGroupsBuilder.Received().BuildSmallGGroups(version);
        }

        [TestCase("A*", Locus.A)]
        [TestCase("B*", Locus.B)]
        [TestCase("C*", Locus.C)]
        [TestCase("DPB1*", Locus.Dpb1)]
        [TestCase("DQB1*", Locus.Dqb1)]
        [TestCase("DRB1*", Locus.Drb1)]
        public void GetAlleleGroupsMetadata_SetsPGroupLocus(string typingLocus, Locus expectedLocus)
        {
            var pGroups = new List<HlaNomP> { new HlaNomP(typingLocus, PGroupName, Alleles) };

            wmdaDataRepository.GetWmdaDataset(default).ReturnsForAnyArgs(
                WmdaDatasetBuilder.New
                .With(w => w.PGroups, pGroups)
                .Build());

            var result = alleleGroupsService.GetAlleleGroupsMetadata("version").ToList();

            result.Select(grp => grp.Locus).Should().BeEquivalentTo(expectedLocus);
        }

        [TestCase("A*", Locus.A)]
        [TestCase("B*", Locus.B)]
        [TestCase("C*", Locus.C)]
        [TestCase("DPB1*", Locus.Dpb1)]
        [TestCase("DQB1*", Locus.Dqb1)]
        [TestCase("DRB1*", Locus.Drb1)]
        public void GetAlleleGroupsMetadata_SetsGGroupLocus(string typingLocus, Locus expectedLocus)
        {
            var gGroups = new List<HlaNomG> { new HlaNomG(typingLocus, GGroupName, Alleles) };

            wmdaDataRepository.GetWmdaDataset(default).ReturnsForAnyArgs(
                WmdaDatasetBuilder.New
                .With(w => w.GGroups, gGroups)
                .Build());

            var result = alleleGroupsService.GetAlleleGroupsMetadata("version").ToList();

            result.Select(grp => grp.Locus).Should().BeEquivalentTo(expectedLocus);
        }

        [TestCase(Locus.A)]
        [TestCase(Locus.B)]
        [TestCase(Locus.C)]
        [TestCase(Locus.Dpb1)]
        [TestCase(Locus.Dqb1)]
        [TestCase(Locus.Drb1)]
        public void GetAlleleGroupsMetadata_SetsSmallGGroupLocus(Locus locus)
        {
            var gGroups = new[] { new SmallGGroup
            {
                Locus = locus,
                Name = SmallGGroupName,
                Alleles = Alleles
            }};
            smallGGroupsBuilder.BuildSmallGGroups(default).ReturnsForAnyArgs(gGroups);

            var result = alleleGroupsService.GetAlleleGroupsMetadata("version").ToList();

            result.Select(grp => grp.Locus).Should().BeEquivalentTo(locus);
        }

        [Test]
        public void GetAlleleGroupsMetadata_SetsPGroupAlleles()
        {
            var pGroups = new List<HlaNomP> { new HlaNomP(TypingLocus, PGroupName, Alleles) };

            wmdaDataRepository.GetWmdaDataset(default).ReturnsForAnyArgs(
                WmdaDatasetBuilder.New
                    .With(w => w.PGroups, pGroups)
                    .Build());

            var result = alleleGroupsService.GetAlleleGroupsMetadata("version").ToList();

            result.SelectMany(grp => grp.AllelesInGroup).Should().BeEquivalentTo(Alleles);
        }

        [Test]
        public void GetAlleleGroupsMetadata_SetsGGroupAlleles()
        {
            var gGroups = new List<HlaNomG> { new HlaNomG(TypingLocus, GGroupName, Alleles) };

            wmdaDataRepository.GetWmdaDataset(default).ReturnsForAnyArgs(
                WmdaDatasetBuilder.New
                    .With(w => w.GGroups, gGroups)
                    .Build());

            var result = alleleGroupsService.GetAlleleGroupsMetadata("version").ToList();

            result.SelectMany(grp => grp.AllelesInGroup).Should().BeEquivalentTo(Alleles);
        }

        [Test]
        public void GetAlleleGroupsMetadata_SetsSmallGGroupAlleles()
        {
            var gGroups = new[] { new SmallGGroup
            {
                Locus = DefaultLocus,
                Name = SmallGGroupName,
                Alleles = Alleles
            }};
            smallGGroupsBuilder.BuildSmallGGroups(default).ReturnsForAnyArgs(gGroups);

            var result = alleleGroupsService.GetAlleleGroupsMetadata("version").ToList();

            result.SelectMany(grp => grp.AllelesInGroup).Should().BeEquivalentTo(Alleles);
        }

        [Test]
        public void GetAlleleGroupsMetadata_OnlyReturnsAlleleGroupMetadata()
        {
            var pGroups = new[]
            {
                new HlaNomP(TypingLocus, PGroupName, Alleles),
                new HlaNomP(TypingLocus, AlleleName, Alleles)
            };

            var gGroups = new[]
            {
                new HlaNomG(TypingLocus, GGroupName, Alleles),
                new HlaNomG(TypingLocus, AlleleName, Alleles)
            };

            wmdaDataRepository.GetWmdaDataset(default).ReturnsForAnyArgs(
                WmdaDatasetBuilder.New
                    .With(w => w.PGroups, pGroups)
                    .With(w => w.GGroups, gGroups)
                    .Build());

            var smallGGroups = new[] 
            { 
                new SmallGGroup { Locus = DefaultLocus, Name = SmallGGroupName, Alleles = Alleles },
                new SmallGGroup { Locus = DefaultLocus, Name = AlleleName, Alleles = Alleles }
            };
            smallGGroupsBuilder.BuildSmallGGroups(default).ReturnsForAnyArgs(smallGGroups);

            var result = alleleGroupsService.GetAlleleGroupsMetadata("version").ToList();

            result.Should().HaveCount(3);
            result.Select(grp => grp.LookupName).Should().BeEquivalentTo(PGroupName, GGroupName, SmallGGroupName);
        }
    }
}
