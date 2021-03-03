using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.Services.DataGeneration.Generators;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.DataGeneration
{
    [TestFixture]
    public class SmallGGroupsBuilderTests
    {
        private const string LocusName = "A*";
        private const Locus ExpectedLocus = Locus.A;
        private const string DefaultHlaVersion = "hla-version";

        private IWmdaDataRepository wmdaDataRepository;
        private ISmallGGroupsBuilder smallGGroupsBuilder;

        [SetUp]
        public void SetUp()
        {
            wmdaDataRepository = Substitute.For<IWmdaDataRepository>();
            smallGGroupsBuilder = new SmallGGroupsBuilder(wmdaDataRepository);
        }

        [Test]
        public void BuildSmallGGroups_PGroupMapsToGGroupWithNoNulls_SmallGGroupOnlyHasPGroupAlleles()
        {
            const string sharedAllele = "01:01";
            var pGroupAlleles = new[] { sharedAllele, "01:02L", "01:03S", "01:04Q" };

            var dataset = WmdaDatasetBuilder.New
                .AddPGroup(LocusName, "01:01P", pGroupAlleles)
                .AddGGroup(LocusName, "G-group", sharedAllele);

            wmdaDataRepository.GetWmdaDataset(default).ReturnsForAnyArgs(dataset);

            var result = smallGGroupsBuilder.BuildSmallGGroups(DefaultHlaVersion);
            var smallG = result.Single();

            smallG.Alleles.Should().BeEquivalentTo(pGroupAlleles);
        }

        [Test]
        public void BuildSmallGGroups_PGroupMapsToGGroupsWithNulls_SmallGGroupHasPGroupAllelesAndNullsFromGGroups()
        {
            const string sharedAllele = "01:01";
            const string nullAllele1 = "01:03N";
            const string nullAllele2 = "01:04N";
            var pGroupAlleles = new[] { sharedAllele, "01:02L", "01:03S", "01:04Q" };

            var dataset = WmdaDatasetBuilder.New
                .AddPGroup(LocusName, "01:01P", pGroupAlleles)
                .AddGGroup(LocusName, "G-group-1", new[] { sharedAllele, nullAllele1 })
                .AddGGroup(LocusName, "G-group-2", new[] { sharedAllele, nullAllele2 });

            wmdaDataRepository.GetWmdaDataset(default).ReturnsForAnyArgs(dataset);

            var result = smallGGroupsBuilder.BuildSmallGGroups(DefaultHlaVersion);
            var smallG = result.Single();

            smallG.Alleles.Should().BeEquivalentTo(
                new List<string>(pGroupAlleles) { nullAllele1, nullAllele2 });
        }

        [Test]
        public void BuildSmallGGroups_PGroupMapsToGGroup_OnlyGroupsAllelesFromTheSameLocus()
        {
            const string sharedAllele = "01:01";
            const string nullAllele = "01:03N";
            var pGroupAlleles = new[] { sharedAllele, "01:02L", "01:03S", "01:04Q" };

            const string differentLocusName = "B*";
            const Locus differentLocus = Locus.B;

            var dataset = WmdaDatasetBuilder.New
                .AddPGroup(LocusName, "01:01P", pGroupAlleles)
                .AddGGroup(LocusName, "G-group", new[] { sharedAllele, nullAllele })
                .AddPGroup(differentLocusName, "99:99P", sharedAllele)
                .AddGGroup(differentLocusName, "different-locus-G-group", sharedAllele);

            wmdaDataRepository.GetWmdaDataset(default).ReturnsForAnyArgs(dataset);

            var result = smallGGroupsBuilder.BuildSmallGGroups(DefaultHlaVersion)
                .OrderByDescending(g => g.Alleles.Count)
                .ToList();
            var firstSmallG = result[0];
            var secondSmallG = result[1];
            
            firstSmallG.Locus.Should().Be(ExpectedLocus);
            firstSmallG.Alleles.Should().BeEquivalentTo(new List<string>(pGroupAlleles) { nullAllele });

            secondSmallG.Locus.Should().Be(differentLocus);
            secondSmallG.Alleles.Should().BeEquivalentTo(sharedAllele);
        }

        [Test]
        public void BuildSmallGGroups_PGroupMapsToGGroup_SetsPGroup()
        {
            const string pGroupName = "01:01P";

            var dataset = WmdaDatasetBuilder.New
                .AddPGroup(LocusName, pGroupName , "01:01:01");

            wmdaDataRepository.GetWmdaDataset(default).ReturnsForAnyArgs(dataset);

            var result = smallGGroupsBuilder.BuildSmallGGroups(DefaultHlaVersion);
            var smallG = result.Single();
            
            smallG.PGroup.Should().Be(pGroupName);
        }

        [TestCase("")]
        [TestCase("L")]
        [TestCase("S")]
        [TestCase("Q")]
        public void BuildSmallGGroups_ExpressingAlleleNotAssignedToAPGroup_SmallGGroupOnlyHasExpressingAllele(string expressionLetter)
        {
            var alleleName = "01:01" + expressionLetter;

            var dataset = WmdaDatasetBuilder.New
                .AddPGroup(LocusName, alleleName, alleleName)
                .AddGGroup(LocusName, alleleName, alleleName);

            wmdaDataRepository.GetWmdaDataset(default).ReturnsForAnyArgs(dataset);

            var result = smallGGroupsBuilder.BuildSmallGGroups(DefaultHlaVersion);
            var smallG = result.Single();

            smallG.Alleles.Should().BeEquivalentTo(alleleName);
        }

        [TestCase("")]
        [TestCase("L")]
        [TestCase("S")]
        [TestCase("Q")]
        public void BuildSmallGGroups_ExpressingAlleleNotAssignedToAPGroup_PGroupIsExpressingAlleleName(string expressionLetter)
        {
            var alleleName = "01:01" + expressionLetter;

            var dataset = WmdaDatasetBuilder.New
                .AddPGroup(LocusName, alleleName, alleleName)
                .AddGGroup(LocusName, alleleName, alleleName);

            wmdaDataRepository.GetWmdaDataset(default).ReturnsForAnyArgs(dataset);

            var result = smallGGroupsBuilder.BuildSmallGGroups(DefaultHlaVersion);
            var smallG = result.Single();

            smallG.PGroup.Should().BeEquivalentTo(alleleName);
        }

        [Test]
        public void BuildSmallGGroups_NullAllelesThatDoNotMapToPGroups_GroupsNullAllelesByFirst2Fields()
        {
            const string nSuffix = "N";

            const string twoFields = "01:01";
            const string threeFieldNullAllele1 = twoFields + ":01" + nSuffix;
            const string threeFieldNullAllele2 = twoFields + ":02" + nSuffix;
            const string fourFieldNullAllele1 = twoFields + ":01:01" + nSuffix;
            const string fourFieldNullAllele2 = twoFields + ":01:02" + nSuffix;

            const string nullAlleleWithDifferentTwoFields = "99:99" + nSuffix;

            var dataset = WmdaDatasetBuilder.New
                .AddGGroup(LocusName, threeFieldNullAllele1, threeFieldNullAllele1)
                .AddGGroup(LocusName, threeFieldNullAllele2, threeFieldNullAllele2)
                .AddGGroup(LocusName, "G-group-with-only-nulls", new[] { fourFieldNullAllele1, fourFieldNullAllele2 })
                .AddGGroup(LocusName, nullAlleleWithDifferentTwoFields, nullAlleleWithDifferentTwoFields);

            wmdaDataRepository.GetWmdaDataset(default).ReturnsForAnyArgs(dataset);

            var result = smallGGroupsBuilder.BuildSmallGGroups(DefaultHlaVersion)
                .OrderByDescending(g => g.Alleles.Count)
                .ToList();
            var firstSmallG = result[0];
            var secondSmallG = result[1];

            firstSmallG.Alleles.Should().BeEquivalentTo(
                threeFieldNullAllele1, threeFieldNullAllele2, fourFieldNullAllele1, fourFieldNullAllele2);
            secondSmallG.Alleles.Should().BeEquivalentTo(nullAlleleWithDifferentTwoFields);
        }

        [Test]
        public void BuildSmallGGroups_NullAllelesThatDoNotMapToPGroups_OnlyGroupsAllelesFromTheSameLocus()
        {
            const string nullAllele = "01:01N";
            const string differentLocusName = "B*";
            const Locus differentLocus = Locus.B;

            var dataset = WmdaDatasetBuilder.New
                .AddGGroup(LocusName, nullAllele, nullAllele)
                .AddGGroup(differentLocusName, nullAllele, nullAllele);

            wmdaDataRepository.GetWmdaDataset(default).ReturnsForAnyArgs(dataset);

            var result = smallGGroupsBuilder.BuildSmallGGroups(DefaultHlaVersion)
                .OrderBy(g => g.Locus)
                .ToList();
            var firstSmallG = result[0];
            var secondSmallG = result[1];

            firstSmallG.Locus.Should().Be(ExpectedLocus);
            firstSmallG.Alleles.Should().BeEquivalentTo(nullAllele);

            secondSmallG.Locus.Should().Be(differentLocus);
            secondSmallG.Alleles.Should().BeEquivalentTo(nullAllele);
        }

        [Test]
        public void BuildSmallGGroups_NullAllelesThatDoNotMapToPGroups_PGroupIsNull()
        {
            const string nSuffix = "N";
            const string twoFields = "01:01";
            const string nullAllele1 = twoFields + ":01" + nSuffix;
            const string nullAllele2 = twoFields + ":02" + nSuffix;

            var dataset = WmdaDatasetBuilder.New
                .AddGGroup(LocusName, nullAllele1, nullAllele1)
                .AddGGroup(LocusName, nullAllele2, nullAllele2);

            wmdaDataRepository.GetWmdaDataset(default).ReturnsForAnyArgs(dataset);

            var result = smallGGroupsBuilder.BuildSmallGGroups(DefaultHlaVersion);
            var smallG = result.Single();

            smallG.PGroup.Should().BeNull();
        }

        [TestCase(
            new[] { "01:01:01", "01:01:02L", "01:01:03:01S", "01:01:03:02Q" },
            new object[] { },
            "01:01")]
        [TestCase(
            new[] { "01:01:01", "01:01:02L", "01:01:03:01S", "01:01:03:02Q" },
            new[] { "01:01:04N", "01:01:05:01N" },
            "01:01")]

        public void BuildSmallGGroups_AllelesInSmallGGroupHaveSameFirst2Fields_NamesWithFirst2Fields(
            object[] expressingAlleles,
            object[] nullAlleles,
            string firstTwoFields)
        {
            var pGroupAlleles = expressingAlleles.Select(a => a.ToString()).ToList();
            var gGroupAlleles = pGroupAlleles.Concat(nullAlleles.Select(a => a.ToString())).ToList();

            var dataset = WmdaDatasetBuilder.New
                .AddPGroup(LocusName, "P-group", pGroupAlleles)
                .AddGGroup(LocusName, "G-group", gGroupAlleles);

            wmdaDataRepository.GetWmdaDataset(default).ReturnsForAnyArgs(dataset);

            var result = smallGGroupsBuilder.BuildSmallGGroups(DefaultHlaVersion);
            var smallG = result.Single();

            smallG.Name.Should().Be(firstTwoFields);

            smallG.Locus.Should().Be(ExpectedLocus);
            smallG.Alleles.Count.Should().Be(gGroupAlleles.Count);
        }

        [TestCase(
            new[] { "01:01", "01:02L", "01:03:01S", "01:04:01:01Q" },
            new object[] { },
            "111:111")]
        [TestCase(
            new[] { "01:01", "01:02L", "01:03:01S", "01:04:01:01Q" },
            new[] { "01:05N", "01:06:01N", "01:07:01:01N" },
            "999:999")]
        public void BuildSmallGGroups_AllelesInSmallGGroupHaveDifferentFirst2Fields_NamesAfterPGroup(
            object[] expressingAlleles,
            object[] nullAlleles,
            string pGroupNameWithoutSuffix)
        {
            var pGroupAlleles = expressingAlleles.Select(a => a.ToString()).ToList();
            var gGroupAlleles = pGroupAlleles.Concat(nullAlleles.Select(a => a.ToString())).ToList();

            var dataset = WmdaDatasetBuilder.New
                .AddPGroup(LocusName, pGroupNameWithoutSuffix + "P", pGroupAlleles)
                .AddGGroup(LocusName, "G-group", gGroupAlleles);

            wmdaDataRepository.GetWmdaDataset(default).ReturnsForAnyArgs(dataset);

            var result = smallGGroupsBuilder.BuildSmallGGroups(DefaultHlaVersion);
            var smallG = result.Single();

            smallG.Name.Should().Be(pGroupNameWithoutSuffix + "g");

            smallG.Locus.Should().Be(ExpectedLocus);
            smallG.Alleles.Count.Should().Be(gGroupAlleles.Count);
        }

        [TestCase("")]
        [TestCase("L")]
        [TestCase("S")]
        [TestCase("Q")]
        public void BuildSmallGGroups_SmallGGroupOnlyHasOneAllele_Expressing_NamesWithAlleleName(string expressionLetter)
        {
            var alleleName = "01:01" + expressionLetter;

            var dataset = WmdaDatasetBuilder.New
                .AddPGroup(LocusName, alleleName, alleleName)
                .AddGGroup(LocusName, alleleName, alleleName);

            wmdaDataRepository.GetWmdaDataset(default).ReturnsForAnyArgs(dataset);

            var result = smallGGroupsBuilder.BuildSmallGGroups(DefaultHlaVersion);
            var smallG = result.Single();

            smallG.Name.Should().BeEquivalentTo(alleleName);

            smallG.Locus.Should().Be(ExpectedLocus);
            smallG.Alleles.Count.Should().Be(1);
        }

        [TestCase("01:01N", "01:01N")]
        [TestCase("01:01:01N", "01:01N")]
        [TestCase("01:01:01:01N", "01:01N")]
        public void BuildSmallGGroups_SmallGGroupOnlyHasOneAllele_Null_NamesWithFirst2FieldsAndExpressionSuffix(
            string nullAllele,
            string expectedName)
        {
            var dataset = WmdaDatasetBuilder.New.AddGGroup(LocusName, nullAllele, nullAllele);

            wmdaDataRepository.GetWmdaDataset(default).ReturnsForAnyArgs(dataset);

            var result = smallGGroupsBuilder.BuildSmallGGroups(DefaultHlaVersion);
            var smallG = result.Single();

            smallG.Name.Should().Be(expectedName);

            smallG.Locus.Should().Be(ExpectedLocus);
            smallG.Alleles.Count.Should().Be(1);
        }

        [Test]
        public void BuildSmallGGroups_AllelesInSmallGGroupAreAllNull_NamesWithFirst2Fields()
        {
            const string nSuffix = "N";

            const string twoFields = "01:01";
            const string threeFieldNullAllele1 = twoFields + ":01" + nSuffix;
            const string threeFieldNullAllele2 = twoFields + ":02" + nSuffix;
            const string fourFieldNullAllele1 = twoFields + ":01:01" + nSuffix;
            const string fourFieldNullAllele2 = twoFields + ":01:02" + nSuffix;

            var dataset = WmdaDatasetBuilder.New
                .AddGGroup(LocusName, threeFieldNullAllele1, threeFieldNullAllele1)
                .AddGGroup(LocusName, threeFieldNullAllele2, threeFieldNullAllele2)
                .AddGGroup(LocusName, "G-group-with-only-nulls", new[] { fourFieldNullAllele1, fourFieldNullAllele2 });

            wmdaDataRepository.GetWmdaDataset(default).ReturnsForAnyArgs(dataset);

            var result = smallGGroupsBuilder.BuildSmallGGroups(DefaultHlaVersion);
            var smallG = result.Single();

            smallG.Name.Should().Be(twoFields);

            smallG.Locus.Should().Be(ExpectedLocus);
            smallG.Alleles.Count.Should().Be(4);
        }
    }
}