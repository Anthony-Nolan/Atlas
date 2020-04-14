using FluentAssertions;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.HLATypings;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Wmda;
using NUnit.Framework;
using System.Linq;

namespace Atlas.MatchingAlgorithm.Test.MatchingDictionary.Repositories.Wmda
{
    public class AlleleNameHistoriesTest : WmdaRepositoryTestBase<AlleleNameHistory>
    {
        protected override void SetupTestData()
        {
            SetTestData(WmdaDataRepository.GetWmdaDataset(HlaDatabaseVersionToTest).AlleleNameHistories, MolecularLoci);
        }
        
        [Test]
        public void WmdaDataRepository_WhenUnassignedAlleleName_NoAlleleNameHistoriesCaptured()
        {
            const string locus = "A*";
            const string alleleName = "02:100";

            Assert.IsEmpty(WmdaHlaTypings.Where(typing =>
                typing.TypingLocus.Equals(locus) &&
                typing.VersionedAlleleNames.Select(x => x.AlleleName).Contains(alleleName)
                ));
        }

        [TestCaseSource(typeof(AlleleNameHistoriesTestCaseSources), nameof(AlleleNameHistoriesTestCaseSources.SequencesToTest))]
        public void WmdaDataRepository_WhenAssignedAlleleName_TypingMethodIsMolecular(
            string[] sequenceToTest
        )
        {
            var actualAlleleNameHistory = GetSingleWmdaHlaTyping(sequenceToTest[0], sequenceToTest[1]);

            actualAlleleNameHistory.TypingMethod.Should().Be(TypingMethod.Molecular);
        }

        [TestCaseSource(typeof(AlleleNameHistoriesTestCaseSources), nameof(AlleleNameHistoriesTestCaseSources.ExpectedVersionedAlleleNames))]
        public void WmdaDataRepository_WhenAssignedAlleleName_VersionedAllelesNamesSuccessfullyCaptured(
            string[] sequenceToTest,
            string[][] expectedVersionedAlleleNames
        )
        {
            var actualAlleleNameHistory = GetSingleWmdaHlaTyping(sequenceToTest[0], sequenceToTest[1]);

            var actualVersionedAlleleNames = actualAlleleNameHistory
                .VersionedAlleleNames
                .Select(versionedAlleleName =>
                    new[] { versionedAlleleName.HlaDatabaseVersion, versionedAlleleName.AlleleName })
                .ToArray();

            actualVersionedAlleleNames.ShouldBeEquivalentTo(expectedVersionedAlleleNames);
        }

        [TestCaseSource(typeof(AlleleNameHistoriesTestCaseSources), nameof(AlleleNameHistoriesTestCaseSources.ExpectedCurrentAlleleNames))]
        public void WmdaDataRepository_WhenAssignedAlleleName_CurrentAlleleNameIsCorrect(
            string[] sequenceToTest,
            string expectedCurrentAlleleName
        )
        {
            var actualAlleleNameHistory = GetSingleWmdaHlaTyping(sequenceToTest[0], sequenceToTest[1]);

            actualAlleleNameHistory.CurrentAlleleName.Should().Be(expectedCurrentAlleleName);
        }

        [TestCaseSource(typeof(AlleleNameHistoriesTestCaseSources), nameof(AlleleNameHistoriesTestCaseSources.ExpectedDistinctAlleleNames))]
        public void WmdaDataRepository_WhenAssignedAlleleName_DistinctAlleleNamesIsCorrect(
            string[] sequenceToTest,
            string[] expectedDistinctAlleleNames
        )
        {
            var actualAlleleNameHistory = GetSingleWmdaHlaTyping(sequenceToTest[0], sequenceToTest[1]);

            actualAlleleNameHistory.DistinctAlleleNames.ShouldBeEquivalentTo(expectedDistinctAlleleNames);
        }

        [TestCaseSource(typeof(AlleleNameHistoriesTestCaseSources), nameof(AlleleNameHistoriesTestCaseSources.ExpectedMostRecentAlleleNames))]
        public void WmdaDataRepository_WhenAssignedAlleleName_MostRecentAlleleNameIsCorrect(
            string[] sequenceToTest,
            string expectedMostRecentAlleleName
        )
        {
            var actualAlleleNameHistory = GetSingleWmdaHlaTyping(sequenceToTest[0], sequenceToTest[1]);

            actualAlleleNameHistory.MostRecentAlleleName.Should().Be(expectedMostRecentAlleleName);
        }
    }
}
