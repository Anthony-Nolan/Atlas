using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.HlaMetadataDictionary.InternalModels.HLATypings;
using Atlas.HlaMetadataDictionary.Services.DataGeneration.Generators;
using Atlas.HlaMetadataDictionary.Test.UnitTests;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.Tests
{
    public class SmallGGroupsBuilderTests
    {
        private List<SmallGGroup> allSmallGGroups;
        private WmdaDataset wmdaDataset;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                var repo = SharedTestDataCache.GetWmdaDataRepository();
                const string version = SharedTestDataCache.HlaNomenclatureVersionForImportingTestWmdaRepositoryFiles;

                allSmallGGroups = new SmallGGroupsBuilder(repo).BuildSmallGGroups(version).ToList();
                wmdaDataset = repo.GetWmdaDataset(version);
            });
        }

        [Test]
        public void BuildSmallGGroups_DoesNotGenerateDuplicateSmallGGroups()
        {
            allSmallGGroups
                .GroupBy(metadata => new { metadata.Locus, metadata.Name })
                .Any(group => group.Count() > 1)
                .Should()
                .BeFalse();
        }

        [TestCase(Locus.A)]
        [TestCase(Locus.B)]
        [TestCase(Locus.C)]
        [TestCase(Locus.Dpb1)]
        [TestCase(Locus.Dqb1)]
        [TestCase(Locus.Drb1)]
        public void BuildSmallGGroups_EveryNonConfidentialAlleleHasBeenAssignedToASmallGGroup(Locus locus)
        {
            var typingLocus = $"{locus}*";

            var alleles = wmdaDataset.Alleles
                .Where(a => a.TypingLocus == typingLocus && a.IsDeleted == false && IsNotConfidential(typingLocus, a.Name))
                .Select(a => a.Name);

            var smallGAlleles = allSmallGGroups.Where(g => g.Locus == locus).SelectMany(g => g.Alleles);

            // Due to manual curation, the hla_nom file stored in the test Resources folder has fewer alleles
            // than the _g and _p files, and so the direction of comparison here is important.
            var allelesWithoutSmallGGroup = alleles.Except(smallGAlleles).ToList();

            allelesWithoutSmallGGroup.Should().BeEmpty();
        }

        [TestCase(Locus.A, "01:52",
            new object[] { "01:52:01N", "01:52:02N" },
            Description = "All Null alleles within small g group")]
        [TestCase(Locus.Drb1, "04:94N",
            new object[] { "04:94:01N" },
            Description = "Single null allele with more than 2 fields")]
        [TestCase(Locus.A, "02:04g",
            new object[] { "02:04:01", "02:04:02", "02:664", "02:710N" },
            Description = "One Null allele within small g group")]
        [TestCase(Locus.A, "34:01g",
            new object[] { "34:01:01:01", "34:01:01:02", "34:01:01:03", "34:01:01:04", "34:01:02", "34:01:03", "34:01:04", "34:01:05", "34:01:06Q", "34:01:07", "34:01:08", "34:18" },
            Description = "Other Expression Letters within small g group")]
        [TestCase(Locus.A, "02:22g",
            new object[] { "02:22:01:01", "02:22:01:02", "02:22:02", "02:104", "02:929" },
            Description = "Mixture of 2,3 and 4 field typing resolutions within small g group")]
        [TestCase(Locus.B, "44:192",
            new object[] { "44:192:01", "44:192:02", "44:192:03", "44:192:04" },
            Description = "Changes in third field typing within small g group")]
        [TestCase(Locus.C, "04:13",
            new object[] { "04:13:01:01", "04:13:01:02" },
            Description = "Changes in fourth field typing within small g group")]
        [TestCase(Locus.Dpb1, "26:01g",
            new object[] { "26:01:01", "26:01:02:01", "26:01:02:02", "26:01:02:03", "26:01:02:04", "26:01:02:05", "26:01:03", "1088:01" },
            Description = "Changes allele family within small g group")]
        [TestCase(Locus.Drb1, "01:03g",
            new object[] { "01:03:01:01", "01:03:01:02", "01:03:02", "01:03:03", "01:03:04", "01:03:05", "01:102", "01:155" },
            Description = "Returned small g group is locus specific")]
        public void BuildSmallGGroups_SmallGGroupIsAsExpected(Locus locus, string name, object[] expectedAlleles)
        {
            var smallGGroup = GetSmallGGroup(locus, name);

            smallGGroup.Alleles.Should().BeEquivalentTo(expectedAlleles);
        }

        [TestCase(Locus.A, "01:52", null,
            Description = "All Null alleles within small g group")]
        [TestCase(Locus.A, "02:04g", "02:04P",
            Description = "One Null allele within small g group")]
        [TestCase(Locus.B, "38:01g", "38:01P",
            Description = "Other Expression Letters within small g group")]
        [TestCase(Locus.A, "02:22g", "02:22P",
            Description = "Mixture of 2,3 and 4 field typing resolutions within small g group")]
        [TestCase(Locus.B, "44:192", "44:192P",
            Description = "Changes in third field typing within small g group")]
        [TestCase(Locus.C, "04:13", "04:13P",
            Description = "Changes in fourth field typing within small g group")]
        [TestCase(Locus.Dpb1, "02:02g", "02:02P",
            Description = "Changes allele family within small g group")]
        public void BuildSmallGGroups_PGroupIsAsExpected(Locus locus, string name, string expectedPGroup)
        {
            var smallGGroup = GetSmallGGroup(locus, name);

            smallGGroup.PGroup.Should().Be(expectedPGroup);
        }
        
        private SmallGGroup GetSmallGGroup(Locus locus, string lookupName)
        {
            return allSmallGGroups.Single(name => name.Locus.Equals(locus) && name.Name.Equals(lookupName));
        }

        private bool IsNotConfidential(string typingLocus, string name)
        {
            return !wmdaDataset.ConfidentialAlleles.Any(c => c.TypingLocus == typingLocus && c.Name.Equals(name));
        }
    }
}