using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Repositories.Wmda
{
    internal class AlleleNameHistoriesTest : WmdaRepositoryTestBase<AlleleNameHistory>
    {
        protected override IEnumerable<AlleleNameHistory> SelectTestDataTypings(WmdaDataset dataset) => dataset.AlleleNameHistories;
        protected override string[] ApplicableLoci => MolecularLoci;

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
                    new[] { versionedAlleleName.HlaNomenclatureVersion, versionedAlleleName.AlleleName })
                .ToArray();

            actualVersionedAlleleNames.Should().BeEquivalentTo(expectedVersionedAlleleNames);
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

            actualAlleleNameHistory.DistinctAlleleNames.Should().BeEquivalentTo(expectedDistinctAlleleNames);
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
