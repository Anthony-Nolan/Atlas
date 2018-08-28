using FluentAssertions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.Dpb1TceGroupLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.Dpb1TceGroups
{
    public class Dpb1TceGroupsServiceTest
    {
        private List<IDpb1TceGroupsLookupResult> tceGroupsLookupResults;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var dataRepository = SharedTestDataCache.GetWmdaDataRepository();

            tceGroupsLookupResults = new Dpb1TceGroupsService(dataRepository)
                .GetDpb1TceGroupLookupResults()
                .ToList();
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

        [TestCase("01:01:01:01", new[] { "3" }, Description = "Assignment V1 & 2 are the same; single allele name")]
        [TestCase("01:01", new[] { "3" }, Description = "Assignment V1 & 2 are the same; NMDP code allele name")]
        [TestCase("01", new[] { "3" }, Description = "Assignment V1 & 2 are the same; XX code name")]
        [TestCase("06:01:01:03", new[] { "2" }, Description = "Assignment changed between V1 & 2; single allele name")]
        [TestCase("06:01", new[] { "2" }, Description = "Assignment changed between V1 & 2; NMDP code allele name")]
        [TestCase("06", new[] { "2" }, Description = "Assignment changed between V1 & 2; XX code name")]
        [TestCase("30:01:01:01", new[] { "1" }, Description = "Assignment based on functional distance scores; single allele name")]
        [TestCase("30:01", new[] { "1" }, Description = "Assignment based on functional distance scores; NMDP code allele name")]
        [TestCase("30", new[] { "1" }, Description = "Assignment based on functional distance scores; XX code name")]
        [TestCase("85:01:01:01", new[] { "3" }, Description = "No Assignment in V1; single allele name")]
        [TestCase("85:01", new[] { "3" }, Description = "No Assignment in V1; NMDP code allele name")]
        [TestCase("85", new[] { "3" }, Description = "No Assignment in V1; XX code name")]
        [TestCase("548:01", new string[]{}, Description = "No Assignments in V1 & V2; single/NMPD code allele name")]
        [TestCase("548", new string[] { }, Description = "No Assignments in V1 & V2; XX code name")]
        public void Dpb1TceGroupsService_GetDpb1TceGroupLookupResults_TceGroupsAreAsExpected(
            string lookupName, IEnumerable<string> expectedTceGroups)
        {
            var actualLookupResult = GetDpb1TceGroupsLookupResult(lookupName);

            actualLookupResult.TceGroups.ShouldBeEquivalentTo(expectedTceGroups);
        }

        private IDpb1TceGroupsLookupResult GetDpb1TceGroupsLookupResult(string lookupName)
        {
            return tceGroupsLookupResults
                .Single(alleleName => alleleName.LookupName.Equals(lookupName));
        }
    }
}
