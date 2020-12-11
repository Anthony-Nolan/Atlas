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
using Atlas.HlaMetadataDictionary.Services.DataGeneration.Generators;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.DataGeneration
{
    [TestFixture]
    public class AlleleGroupsServiceTests
    {
        private const string Locus = "A*";
        private const string PGroupName = "p-group";
        private const string GGroupName = "g-group";
        private const string AlleleName = "allele";
        private readonly ICollection<string> alleles = new List<string> { AlleleName };

        private IWmdaDataRepository wmdaDataRepository;
        private IHlaCategorisationService hlaCategorisationService;
        private IAlleleGroupsService alleleGroupsService;

        [SetUp]
        public void SetUp()
        {
            wmdaDataRepository = Substitute.For<IWmdaDataRepository>();
            hlaCategorisationService = Substitute.For<IHlaCategorisationService>();

            alleleGroupsService = new AlleleGroupsService(wmdaDataRepository, hlaCategorisationService);

            wmdaDataRepository.GetWmdaDataset(default).ReturnsForAnyArgs(WmdaDatasetBuilder.New.Build());
            hlaCategorisationService.GetHlaTypingCategory(AlleleName).Returns(HlaTypingCategory.Allele);
            hlaCategorisationService.GetHlaTypingCategory(PGroupName).Returns(HlaTypingCategory.PGroup);
            hlaCategorisationService.GetHlaTypingCategory(GGroupName).Returns(HlaTypingCategory.GGroup);
        }

        [Test]
        public void GetPGroupsMetadata_GetsWmdaDatasetOfRequiredVersion()
        {
            const string version = "version";

            alleleGroupsService.GetAlleleGroupsMetadata(version);

            wmdaDataRepository.Received().GetWmdaDataset(version);
        }

        [TestCase("A*", Common.GeneticData.Locus.A)]
        [TestCase("B*", Common.GeneticData.Locus.B)]
        [TestCase("C*", Common.GeneticData.Locus.C)]
        [TestCase("DPB1*", Common.GeneticData.Locus.Dpb1)]
        [TestCase("DQB1*", Common.GeneticData.Locus.Dqb1)]
        [TestCase("DRB1*", Common.GeneticData.Locus.Drb1)]
        public void GetAlleleGroupsMetadata_SetsPGroupLocus(string typingLocus, Locus expectedLocus)
        {
            var pGroups = new List<HlaNomP> { new HlaNomP(typingLocus, PGroupName, alleles) };

            wmdaDataRepository.GetWmdaDataset(default).ReturnsForAnyArgs(
                WmdaDatasetBuilder.New
                .With(w => w.PGroups, pGroups)
                .Build());

            var result = alleleGroupsService.GetAlleleGroupsMetadata("version").ToList();

            result.Select(grp => grp.Locus).Should().BeEquivalentTo(expectedLocus);
        }

        [TestCase("A*", Common.GeneticData.Locus.A)]
        [TestCase("B*", Common.GeneticData.Locus.B)]
        [TestCase("C*", Common.GeneticData.Locus.C)]
        [TestCase("DPB1*", Common.GeneticData.Locus.Dpb1)]
        [TestCase("DQB1*", Common.GeneticData.Locus.Dqb1)]
        [TestCase("DRB1*", Common.GeneticData.Locus.Drb1)]
        public void GetAlleleGroupsMetadata_SetsGGroupLocus(string typingLocus, Locus expectedLocus)
        {
            var gGroups = new List<HlaNomG> { new HlaNomG(typingLocus, GGroupName, alleles) };

            wmdaDataRepository.GetWmdaDataset(default).ReturnsForAnyArgs(
                WmdaDatasetBuilder.New
                .With(w => w.GGroups, gGroups)
                .Build());

            var result = alleleGroupsService.GetAlleleGroupsMetadata("version").ToList();

            result.Select(grp => grp.Locus).Should().BeEquivalentTo(expectedLocus);
        }

        [Test]
        public void GetAlleleGroupsMetadata_SetsPGroupAlleles()
        {
            var pGroups = new List<HlaNomP> { new HlaNomP(Locus, PGroupName, alleles) };

            wmdaDataRepository.GetWmdaDataset(default).ReturnsForAnyArgs(
                WmdaDatasetBuilder.New
                    .With(w => w.PGroups, pGroups)
                    .Build());

            var result = alleleGroupsService.GetAlleleGroupsMetadata("version").ToList();

            result.SelectMany(grp => grp.AllelesInGroup).Should().BeEquivalentTo(alleles);
        }

        [Test]
        public void GetAlleleGroupsMetadata_SetsGGroupAlleles()
        {
            var gGroups = new List<HlaNomG> { new HlaNomG(Locus, GGroupName, alleles) };

            wmdaDataRepository.GetWmdaDataset(default).ReturnsForAnyArgs(
                WmdaDatasetBuilder.New
                    .With(w => w.GGroups, gGroups)
                    .Build());

            var result = alleleGroupsService.GetAlleleGroupsMetadata("version").ToList();

            result.SelectMany(grp => grp.AllelesInGroup).Should().BeEquivalentTo(alleles);
        }

        [Test]
        public void GetAlleleGroupsMetadata_OnlyReturnsAlleleGroupMetadata()
        {
            var pGroups = new List<HlaNomP>
            {
                new HlaNomP(Locus, PGroupName, alleles),
                new HlaNomP(Locus, AlleleName, alleles)
            };

            var gGroups = new List<HlaNomG>
            {
                new HlaNomG(Locus, GGroupName, alleles),
                new HlaNomG(Locus, AlleleName, alleles)
            };

            wmdaDataRepository.GetWmdaDataset(default).ReturnsForAnyArgs(
                WmdaDatasetBuilder.New
                    .With(w => w.PGroups, pGroups)
                    .With(w => w.GGroups, gGroups)
                    .Build());

            var result = alleleGroupsService.GetAlleleGroupsMetadata("version").ToList();

            result.Should().HaveCount(2);
            result.Select(grp => grp.LookupName).Should().BeEquivalentTo(PGroupName, GGroupName);
        }
    }
}
