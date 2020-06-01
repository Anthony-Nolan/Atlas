using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.HlaMetadataDictionary.Models.Lookups.Dpb1TceGroupLookup;
using Atlas.HlaMetadataDictionary.Services.DataGeneration;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.Dpb1TceGroups
{
    public class Dpb1TceGroupsServiceTest
    {
        private List<IDpb1TceGroupsLookupResult> tceGroupsLookupResults;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                var dataRepository = SharedTestDataCache.GetWmdaDataRepository();

                tceGroupsLookupResults = new Dpb1TceGroupsService(dataRepository)
                    .GetDpb1TceGroupLookupResults(SharedTestDataCache.HlaNomenclatureVersionToTest)
                    .ToList();
            });
        }

        [Test]
        public void Dpb1TceGroupsService_GetDpb1TceGroupLookupResults_DoesNotGenerateDuplicateLookupResults()
        {
            tceGroupsLookupResults
                .GroupBy(lookupResult => lookupResult.LookupName)
                .Any(group => group.Count() > 1)
                .Should()
                .BeFalse();
        }

        [TestCase("01:01:01:01", "3", Description = "Assignment V1 & 2 are the same; single allele name")]
        [TestCase("01:01", "3", Description = "Assignment V1 & 2 are the same; NMDP code allele name")]
        [TestCase("01:XX", "3", Description = "Assignment V1 & 2 are the same; XX code name")]
        [TestCase("06:01:01:03", "2", Description = "Assignment changed between V1 & 2; single allele name")]
        [TestCase("06:01", "2", Description = "Assignment changed between V1 & 2; NMDP code allele name")]
        [TestCase("06:XX", "2", Description = "Assignment changed between V1 & 2; XX code name")]
        [TestCase("30:01:01:01", "1", Description = "Assignment based on functional distance scores; single allele name")]
        [TestCase("30:01", "1", Description = "Assignment based on functional distance scores; NMDP code allele name")]
        [TestCase("30:XX", "1", Description = "Assignment based on functional distance scores; XX code name")]
        [TestCase("85:01:01:01", "3", Description = "No Assignment in V1; single allele name")]
        [TestCase("85:01", "3", Description = "No Assignment in V1; NMDP code allele name")]
        [TestCase("85:XX", "3", Description = "No Assignment in V1; XX code name")]
        [TestCase("548:01", "", Description = "No Assignments in V1 & V2; single/NMDP code allele name")]
        [TestCase("548:XX", "", Description = "No Assignments in V1 & V2; XX code name")]
        [TestCase("04:01:01:24N", "", Description = "Null allele; single allele name")]
        [TestCase("04:01", "3", Description = "Group of alleles with same lookup name & assignment contains a null allele; NMDP code allele name")]
        [TestCase("04:XX", "3", Description = "Group of alleles with same lookup name & assignment contains a null allele; XX code name")]
        public void Dpb1TceGroupsService_GetDpb1TceGroupLookupResults_TceGroupIsAsExpected(
            string lookupName, string expectedTceGroup)
        {
            var actualLookupResult = GetDpb1TceGroupsLookupResult(lookupName);

            actualLookupResult.TceGroup.Should().Be(expectedTceGroup);
        }

        private IDpb1TceGroupsLookupResult GetDpb1TceGroupsLookupResult(string lookupName)
        {
            return tceGroupsLookupResults
                .Single(alleleName => alleleName.LookupName.Equals(lookupName));
        }
    }
}
