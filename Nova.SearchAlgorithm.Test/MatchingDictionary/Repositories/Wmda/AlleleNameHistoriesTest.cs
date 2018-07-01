using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Repositories.Wmda
{
    [TestFixtureSource(typeof(WmdaRepositoryTestFixtureArgs), nameof(WmdaRepositoryTestFixtureArgs.AllelesNameHistoriesTestArgs))]
    public class AlleleNameHistoriesTest : WmdaRepositoryTestBase<AlleleNameHistory>
    {
        private static readonly string[] AllExpectedHlaDatabaseVersions =
        {
            "3310","3300","3290","3280","3270","3260","3250","3240","3230","3220",
            "3210","3200","3190","3180","3170","3160","3150","3140","3131","3120",
            "3110","3100","3090","3080","3070","3060","3050","3040","3030","3020",
            "3010","3000"
        };

        public AlleleNameHistoriesTest(IEnumerable<AlleleNameHistory> alleleNameHistories, IEnumerable<string> matchLoci)
            : base(alleleNameHistories, matchLoci)
        {
        }

        [Test]
        public void WmdaDataRepository_WhenUnassignedAlleleName_NoAlleleNameHistoriesCaptured()
        {
            const string locus = "A*";
            const string alleleName = "02:100";

            Assert.IsEmpty(HlaTypings.Where(typing =>
                typing.Locus.Equals(locus) &&
                typing.VersionedAlleleNames.Select(x => x.AlleleName).Contains(alleleName)
                ));
        }

        [TestCase("A*", "HLA00001", new[] { "3310" }, new[] { "01:01:01:01" }, Description = "Allele with same name from latest version to v3.0.0")]
        [TestCase("B*", "HLA13015", new[] { "3310", "3190" }, new[] { "07:242", null }, Description = "Allele discovered after v3.0.0")]
        [TestCase("A*", "HLA00003", new[] { "3310", "3270" }, new[] { "01:03:01:01", "01:03" }, Description = "Allele that has been renamed")]
        [TestCase("DQB1*", "HLA16167", new[] { "3310", "3290", "3260" }, new[] { null, "04:02:01:02", null }, Description = "Allele that was shown to be identical to another allele")]
        public void WmdaDataRepository_WhenAssignedAlleleName_AlleleNameHistoriesSuccessfullyCaptured(
            string locus, string hlaId, string[] versionsWhenAlleleNameChanged, string[] alleleNames)
        {
            var actualAlleleNameHistory = GetSingleWmdaHlaTyping(locus, hlaId);
            var expectedVersionedAlleleNames = GetExpectedVersionedAlleleNames(versionsWhenAlleleNameChanged, alleleNames);
            var expectedCurrentAlleleName = alleleNames[0];
            var expectedDistinctAlleleNames = alleleNames.Where(name => !string.IsNullOrEmpty(name));
            var expectedMostRecentAlleleName = expectedDistinctAlleleNames.First();

            actualAlleleNameHistory.TypingMethod.Should().Be(TypingMethod.Molecular);
            actualAlleleNameHistory.Locus.Should().Be(locus);
            actualAlleleNameHistory.Name.Should().Be(hlaId);
            actualAlleleNameHistory.VersionedAlleleNames.ShouldBeEquivalentTo(expectedVersionedAlleleNames);
            actualAlleleNameHistory.CurrentAlleleName.Should().Be(expectedCurrentAlleleName);
            actualAlleleNameHistory.DistinctAlleleNames.ShouldBeEquivalentTo(expectedDistinctAlleleNames);
            actualAlleleNameHistory.MostRecentAlleleName.Should().Be(expectedMostRecentAlleleName);
        }

        private static IEnumerable<VersionedAlleleName> GetExpectedVersionedAlleleNames(
            IReadOnlyList<string> versionsWhenAlleleNameChanged, IReadOnlyList<string> alleleNames)
        {
            var currentIndex = 0;
            var alleleName = "";
            var expectedVersionedAlleleNames = new List<VersionedAlleleName>();

            foreach (var version in AllExpectedHlaDatabaseVersions)
            {
                if (versionsWhenAlleleNameChanged.Count > currentIndex &&
                    version.Equals(versionsWhenAlleleNameChanged[currentIndex]))
                {
                    alleleName = alleleNames[currentIndex];
                    currentIndex++;
                }

                expectedVersionedAlleleNames.Add(new VersionedAlleleName(version, alleleName));
            }

            return expectedVersionedAlleleNames;
        }
    }
}
