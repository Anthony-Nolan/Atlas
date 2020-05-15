using Atlas.HlaMetadataDictionary.Models.Wmda;
using NUnit.Framework;
using System.Collections.Generic;
using FluentAssertions;

namespace Atlas.MatchingAlgorithm.Test.HlaMetadataDictionary.Repositories.Wmda
{
    public class GGroupsTest : WmdaRepositoryTestBase<HlaNomG>
    {
        protected override void SetupTestData()
        {
            SetTestData(WmdaDataRepository.GetWmdaDataset(HlaDatabaseVersionToTest).GGroups, MolecularLoci);
        }

        [TestCase("C*", "02:14:01G", new[] { "02:14:01", "02:14:02" }, Description = "G group of alleles of same subtype")]
        [TestCase("C*", "01:03:01G", new[] { "01:03", "01:24" }, Description = "G group of alleles of different subtypes")]
        [TestCase("DQB1*", "05:04:01G", new[] { "05:04", "05:132Q" }, Description = "G group where allele has expression suffix")]
        [TestCase("DQB1*", "05:02:01G", new[] {
                    "05:02:01:01", "05:02:01:02", "05:02:01:03",
                    "05:02:03", "05:02:07", "05:02:11", "05:02:14", "05:02:15",
                    "05:14", "05:17", "05:35", "05:36", "05:37", "05:46", "05:47", "05:57",
                    "05:87Q", "05:90N",
                    "05:102", "05:106", "05:136" },
            Description = "G group with many alleles of different properties")]
        [TestCase("DRB1*", "11:11:01G", new[] { "11:11:01" }, Description = "G group with only one allele")]
        [TestCase("A*", "01:01:02", new[] { "01:01:02" }, Description = "Hla-nom-g entry is single allele, not G group")]
        [TestCase("B*", "37:33N", new[] { "37:33N" }, Description = "Hla-nom-g entry is single allele with expression suffix, not G group")]
        public void WmdaDataRepository_GGroups_SuccessfullyCaptured(string locus, string gGroupName, IEnumerable<string> expectedAlleles)
        {
            var expectedGGroup = new HlaNomG(locus, gGroupName, expectedAlleles);

            var actualGGroup = GetSingleWmdaHlaTyping(locus, gGroupName);

            actualGGroup.ShouldBeEquivalentTo(expectedGGroup);
        }
    }
}
