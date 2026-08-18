using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Repositories.Wmda
{
    internal class PGroupsTest : WmdaRepositoryTestBase<HlaNomP>
    {
        protected override IEnumerable<HlaNomP> SelectTestDataTypings(WmdaDataset dataset) => dataset.PGroups;
        protected override string[] ApplicableLoci => MolecularLoci;

        [TestCase("A*", "26:20P", new[] { "26:20:01", "26:20:02" }, Description = "P group of alleles of same subtype")]
        [TestCase("C*", "03:14P", new[] { "03:14", "03:361" }, Description = "P group of alleles of different subtypes")]
        [TestCase("DQB1*", "05:04P", new[] { "05:04:01", "05:04:02", "05:132Q", "05:262" }, Description = "P group where allele has expression suffix")]
        [TestCase("A*", "02:65P", new[] { "02:65", "74:21" }, Description = "P group of alleles from different families")]
        [TestCase("DQB1*", "05:02P", new[] {
                "05:02:01:01", "05:02:01:02", "05:02:01:03", "05:02:01:04", "05:02:01:05", "05:02:01:06", "05:02:01:07", "05:02:01:08", "05:02:01:09", "05:02:01:10",
                "05:02:01:11", "05:02:01:12", "05:02:01:13", "05:02:01:14", "05:02:01:15", "05:02:01:16", "05:02:01:17", "05:02:01:18", "05:02:01:19", "05:02:01:20", "05:02:01:21",
                "05:02:02", "05:02:03", "05:02:04", "05:02:05", "05:02:06", "05:02:07", "05:02:08", "05:02:09", "05:02:10", "05:02:11", "05:02:12", "05:02:13", "05:02:14", "05:02:15",
                "05:02:16", "05:02:17", "05:02:18", "05:02:19", "05:02:20", "05:02:21", "05:02:22", "05:02:23", "05:02:24", "05:02:25", "05:02:26", "05:02:27", "05:02:28", "05:02:29",
                "05:02:30", "05:02:31", "05:02:32",
                "05:14", "05:17", "05:35", "05:36", "05:37", "05:46", "05:47", "05:57",
                "05:87Q",
                "05:102", "05:106", "05:136", "05:165", "05:174", "05:178", "05:186", "05:192", "05:196", "05:198", "05:199", "05:222", "05:227", "05:231", "05:239", "05:241", "05:250",
                "05:251", "05:253", "05:257", "05:278", "05:284", "05:287", "05:309", "05:311:01", "05:311:02", "05:339", "05:361", "05:364", "05:381" },
            Description = "P group with many alleles of different properties")]
        [TestCase("B*", "08:100", new[] { "08:100" }, Description = "Hla-nom-p entry is single allele, not P group")]
        [TestCase("A*", "30:14L", new[] { "30:14L" }, Description = "Hla-nom-p entry is single allele with expression suffix, not P group")]
        public void WmdaDataRepository_PGroups_SuccessfullyCaptured(string locus, string pGroupName, IEnumerable<string> expectedAlleles)
        {
            var expectedPGroup = new HlaNomP(locus, pGroupName, expectedAlleles);

            var actualPGroup = GetSingleWmdaHlaTyping(locus, pGroupName);

            actualPGroup.Should().BeEquivalentTo(expectedPGroup);
        }
    }
}