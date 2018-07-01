using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.Test.MatchingDictionary.Repositories.Wmda;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.AlleleNames
{
    public class AlleleNamesServiceTest
    {
        private List<AlleleNameEntry> alleleNameEntries;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            alleleNameEntries = new AlleleNamesService(WmdaRepositoryTestFixtureArgs.WmdaDataRepository)
                .GetAlleleNames()
                .ToList();
        }

        [TestCase(MatchLocus.A, "01:01:01:01", "01:01:01:01", Description = "Lookup name equals current name")]
        [TestCase(MatchLocus.A, "02:30", "02:30:01", Description = "2 field to 3 field")]
        [TestCase(MatchLocus.C, "07:06", "07:06:01:01", Description = "2 field to 4 field")]
        [TestCase(MatchLocus.B, "08:01:01", "08:01:01:01", Description = "3 field to 4 field")]
        [TestCase(MatchLocus.B, "07:44", "07:44N", Description = "Addition of expression suffix")]
        [TestCase(MatchLocus.B, "13:08Q", "13:08", Description = "Removal of expression suffix")]
        [TestCase(MatchLocus.A, "23:19Q", "23:19N", Description = "Change in expression suffix")]
        public void AlleleNamesService_WhenExactAlleleNameHasBeenInHlaNom_CurrentAlleleNameIsAsExpected(
            MatchLocus matchLocus, string lookupName, string expectedCurrentAlleleName)
        {
            var actualAlleleName = GetAlleleNameEntry(matchLocus, lookupName);

            actualAlleleName.CurrentAlleleName.Should().Be(expectedCurrentAlleleName);
        }

        private AlleleNameEntry GetAlleleNameEntry(MatchLocus matchLocus, string lookupName)
        {
            return alleleNameEntries
                .First(alleleName => 
                    alleleName.MatchLocus.Equals(matchLocus) && 
                    alleleName.LookupName.Equals(lookupName));
        }
    }
}
