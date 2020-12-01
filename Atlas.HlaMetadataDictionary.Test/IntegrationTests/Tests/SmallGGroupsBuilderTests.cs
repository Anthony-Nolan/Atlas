using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Atlas.Common.GeneticData;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.HlaMetadataDictionary.InternalModels.HLATypings;
using Atlas.HlaMetadataDictionary.Services.DataGeneration;
using Atlas.HlaMetadataDictionary.Test.UnitTests;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.Tests
{
    public class SmallGGroupsBuilderTests
    {
        private List<SmallGGroup> smallGdata;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                var dataRepository = SharedTestDataCache.GetWmdaDataRepository();

                smallGdata = new SmallGGroupsBuilder(dataRepository)
                    .BuildSmallGGroups(SharedTestDataCache.HlaNomenclatureVersionForImportingTestWmdaRepositoryFiles)
                    .ToList();
            });
        }

        [Test]
        public void SmallGGroupsBuilder_BuildSmallGGroups_DoesNotGenerateDuplicateMetadata()
        {
            smallGdata
                .GroupBy(metadata => new { metadata.Name, metadata.Locus })
                .Any(group => group.Count() > 1)
                .Should()
                .BeFalse();
        }

        [TestCase(Locus.A, "01:52", "01:52:01N,01:52:02N", Description = "All Null expressing alleles within g Group")]
        [TestCase(Locus.A, "02:04g", "02:04,02:664,02:710N", Description = "One Null expressing allele within g Group")]
        [TestCase(Locus.B, "38:01g", "38:01:01:01,38:01:01:02,38:01:02,38:01:03,38:01:04,38:01:05,38:01:06,38:01:07,38:01:08,38:01:09,38:01:10,38:01:11,38:01:12,38:68Q", Description = "Other Expression Letters within g Group")]
        [TestCase(Locus.A, "02:22g", "02:22:01:01,02:22:01:02,02:22:02,02:104", Description = "Mixture of 2,3 and 4 field typing resolutions within g Group")]
        [TestCase(Locus.B, "44:192", "44:192:01,44:192:02,44:192:03", Description = "Changes in third field typing within g Group")]
        [TestCase(Locus.C, "04:13", "04:13:01:01,04:13:01:02", Description = "Changes in fourth field typing within g Group")]
        [TestCase(Locus.Dpb1, "02:02g", "02:02:01:01,02:02:01:02,02:02:01:03,02:02:01:04,02:02:01:05,02:02:01:06,02:02:01:07,547:01", Description = "Changes allele family within g Group")]
        [TestCase(Locus.Drb1, "01:03", "01:03:01,01:03:02", Description = "Returned g Group is locus specific")]
        public void SmallGGroupsBuilder_BuildSmallGGroups_SmallGGroupIsAsExpected(
            Locus locus, string name, string alleles)
        {
            var actualMetadata = BuildSmallGGroups(name, locus);

            var alleleStrings = string.Join(",", actualMetadata.Alleles);

            alleleStrings.Should().Be(alleles);
        }

        private SmallGGroup BuildSmallGGroups(string lookupName, Locus locus)
        {
            return smallGdata
                .Single(name => name.Name.Equals(lookupName) && name.Locus.Equals(locus));
        }
    }
}
