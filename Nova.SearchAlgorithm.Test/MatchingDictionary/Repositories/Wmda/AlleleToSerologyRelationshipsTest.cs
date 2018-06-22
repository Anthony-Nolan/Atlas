using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using NUnit.Framework;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Repositories.Wmda
{
    [TestFixtureSource(typeof(WmdaRepositoryTestFixtureArgs), nameof(WmdaRepositoryTestFixtureArgs.RelDnaSerTestArgs))]
    public class AlleleToSerologyRelationshipsTest : WmdaRepositoryTestBase<RelDnaSer>
    {
        public AlleleToSerologyRelationshipsTest(IEnumerable<RelDnaSer> relDnaSer, IEnumerable<string> matchLoci)
            : base(relDnaSer, matchLoci)
        {
        }

        [TestCase("A*", "01:01:01:01", new[] { "1" }, new[] { Assignment.Unambiguous })]
        [TestCase("B*", "07:31", new[] { "42", "7" }, new[] { Assignment.Possible, Assignment.Possible })]
        [TestCase("C*", "04:04:01:01", new[] { "4" }, new[] { Assignment.Assumed })]
        [TestCase("C*", "14:02:01:01", new[] { "1" }, new[] { Assignment.Expert })]
        [TestCase("A*", "02:55", new[] { "2", "28" }, new[] { Assignment.Assumed, Assignment.Expert })]
        [TestCase("B*", "39:01:01:02L", new[] { "3901" }, new[] { Assignment.Possible })]
        [TestCase("C*", "07:121Q", new[] { "7" }, new[] { Assignment.Assumed })]
        [TestCase("B*", "44:02:01:02S", new[] { "44" }, new[] { Assignment.Expert })]
        public void WmdaDataRepository_WhenAlleleHasRelatedSerology_RelationshipsSuccessfullyCaptured(
            string molecularLocus,
            string alleleName,
            string[] expectedSerologyNames,
            Assignment[] expectedAssignments)
        {
            var serologyAssignments = new List<SerologyAssignment>();
            for (var i = 0; i < expectedSerologyNames.Length; i++)
            {
                var serologyName = expectedSerologyNames[i];
                var assignment = expectedAssignments[i];
                serologyAssignments.Add(new SerologyAssignment(serologyName, assignment));
            }

            var expectedRelationship = new RelDnaSer(molecularLocus, alleleName, serologyAssignments);
            var actualRelationship = GetSingleWmdaHlaTyping(molecularLocus, alleleName);

            Assert.AreEqual(expectedRelationship, actualRelationship);
        }

        [TestCase("B*", "83:01")]
        [TestCase("DQB1*", "02:18N")]
        public void WmdaDataRepository_WhenAlleleHasNoRelatedSerology_RelationshipsSuccessfullyCaptured(
            string molecularLocus,
            string alleleName)
        {
            var expectedRelationship = new RelDnaSer(molecularLocus, alleleName, new List<SerologyAssignment>());
            var actualRelationship = GetSingleWmdaHlaTyping(molecularLocus, alleleName);

            Assert.AreEqual(expectedRelationship, actualRelationship);
        }
    }
}
