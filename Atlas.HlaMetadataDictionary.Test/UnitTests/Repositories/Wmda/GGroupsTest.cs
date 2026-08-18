using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Repositories.Wmda
{
    internal class GGroupsTest : WmdaRepositoryTestBase<HlaNomG>
    {
        protected override IEnumerable<HlaNomG> SelectTestDataTypings(WmdaDataset dataset) => dataset.GGroups;
        protected override string[] ApplicableLoci => MolecularLoci;

        [TestCase("C*", "02:14:01G", new[] { "02:14:01", "02:14:02" }, Description = "G group of alleles of same subtype")]
        [TestCase("C*", "01:03:01G", new[] { "01:03:01", "01:03:03", "01:24" }, Description = "G group of alleles of different subtypes")]
        [TestCase("DQB1*", "05:04:01G", new[] { "05:04:01", "05:04:02", "05:132Q", "05:262" }, Description = "G group where allele has expression suffix")]
        [TestCase("DQB1*", "05:02:01G", new[] {
                    "05:02:01:01", "05:02:01:02", "05:02:01:03", "05:02:01:04", "05:02:01:05", "05:02:01:06", "05:02:01:07", "05:02:01:08", "05:02:01:09", "05:02:01:10",
                    "05:02:01:11", "05:02:01:12", "05:02:01:13", "05:02:01:14", "05:02:01:15", "05:02:01:16", "05:02:01:17", "05:02:01:18", "05:02:01:19", "05:02:01:20", "05:02:01:21",
                    "05:02:03", "05:02:07", "05:02:11", "05:02:14", "05:02:15", "05:02:20", "05:02:22", "05:02:23", "05:02:24", "05:02:25", "05:02:26",
                    "05:02:28", "05:02:29", "05:02:30", "05:02:32",
                    "05:14", "05:17", "05:35", "05:36", "05:37", "05:46", "05:47", "05:57",
                    "05:87Q", "05:90N",
                    "05:102", "05:106", "05:136", "05:165", "05:174", "05:178", "05:186", "05:192", "05:196", "05:198", "05:199", "05:222", "05:227", "05:231", "05:239", "05:241", "05:250",
                    "05:251", "05:253", "05:257", "05:278", "05:284", "05:287", "05:309", "05:311:01", "05:311:02", "05:339", "05:361", "05:364", "05:365N", "05:381" },
            Description = "G group with many alleles of different properties")]
        [TestCase("A*", "24:02:34G", new[] { "24:02:34" }, Description = "G group with only one allele")]
        [TestCase("A*", "01:01:02", new[] { "01:01:02" }, Description = "Hla-nom-g entry is single allele, not G group")]
        [TestCase("B*", "37:33N", new[] { "37:33N" }, Description = "Hla-nom-g entry is single allele with expression suffix, not G group")]
        public void WmdaDataRepository_GGroups_SuccessfullyCaptured(string locus, string gGroupName, IEnumerable<string> expectedAlleles)
        {
            var expectedGGroup = new HlaNomG(locus, gGroupName, expectedAlleles);

            var actualGGroup = GetSingleWmdaHlaTyping(locus, gGroupName);

            actualGGroup.Should().BeEquivalentTo(expectedGGroup);
        }
    }
}
