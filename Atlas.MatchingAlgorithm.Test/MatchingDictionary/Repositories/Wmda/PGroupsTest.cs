using Atlas.HlaMetadataDictionary.Models.Wmda;
using NUnit.Framework;
using System.Collections.Generic;
using FluentAssertions;

namespace Atlas.MatchingAlgorithm.Test.HlaMetadataDictionary.Repositories.Wmda
{
    public class PGroupsTest : WmdaRepositoryTestBase<HlaNomP>
    {
        protected override void SetupTestData()
        {
            SetTestData(WmdaDataRepository.GetWmdaDataset(HlaDatabaseVersionToTest).PGroups, MolecularLoci);
        }

        [TestCase("DRB1*", "03:02P", new[] { "03:02:01", "03:02:02", "03:02:03" }, Description = "P group of alleles of same subtype")]
        [TestCase("C*", "03:14P", new[] { "03:14", "03:361" }, Description = "P group of alleles of different subtypes")]
        [TestCase("DQB1*", "05:04P", new[] { "05:04", "05:132Q" }, Description = "P group where allele has expression suffix")]
        [TestCase("A*", "02:65P", new[] { "02:65", "74:21" }, Description = "P group of alleles from different families")]
        [TestCase("DQB1*", "05:02P", new[] {
                "05:02:01:01", "05:02:01:02", "05:02:01:03",
                "05:02:02", "05:02:03", "05:02:04", "05:02:05", "05:02:06", "05:02:07", "05:02:08", "05:02:09", "05:02:10", "05:02:11", "05:02:12", "05:02:13", "05:02:14", "05:02:15",
                "05:14", "05:17", "05:35", "05:36", "05:37", "05:46", "05:47", "05:57",
                "05:87Q",
                "05:102", "05:106", "05:136" },
            Description = "P group with many alleles of different properties")]
        [TestCase("B*", "08:100", new[] { "08:100" }, Description = "Hla-nom-p entry is single allele, not P group")]
        [TestCase("A*", "30:14L", new[] { "30:14L" }, Description = "Hla-nom-p entry is single allele with expression suffix, not P group")]
        public void WmdaDataRepository_PGroups_SuccessfullyCaptured(string locus, string pGroupName, IEnumerable<string> expectedAlleles)
        {
            var expectedPGroup = new HlaNomP(locus, pGroupName, expectedAlleles);

            var actualPGroup = GetSingleWmdaHlaTyping(locus, pGroupName);

            actualPGroup.ShouldBeEquivalentTo(expectedPGroup);
        }
    }
}