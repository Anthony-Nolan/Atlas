using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Repositories.Wmda
{
    [TestFixtureSource(typeof(WmdaRepositoryTestFixtureArgs), nameof(WmdaRepositoryTestFixtureArgs.RelDnaSerTestArgs))]
    public class AlleleToSerologyRelationshipsTest : WmdaRepositoryTestBase<RelDnaSer>
    {
        public AlleleToSerologyRelationshipsTest(IEnumerable<RelDnaSer> relDnaSer, IEnumerable<string> matchLoci)
            : base(relDnaSer, matchLoci)
        {
        }

        [TestCase(
            "A*",
            "01:01:01:01",
            new object[] { new object[] { "1", Assignment.Unambiguous } }
            )]
        [TestCase(
            "B*",
            "07:31",
            new object[]
            {
                new object[] { "42", Assignment.Possible },
                new object[] { "7", Assignment.Possible }
            }
            )]
        [TestCase(
            "C*",
            "04:04:01:01",
            new object[] { new object[] { "4", Assignment.Assumed } }
            )]
        [TestCase(
            "C*",
            "14:02:01:01",
            new object[] { new object[] { "1", Assignment.Expert } }
            )]
        [TestCase(
            "A*",
            "02:55",
            new object[]
            {
                new object[] { "2", Assignment.Assumed },
                new object[] { "28", Assignment.Expert }
            }
            )]
        [TestCase(
            "B*",
            "39:01:01:02L",
            new object[] { new object[] { "3901", Assignment.Possible } }
            )]
        [TestCase(
            "C*",
            "07:121Q",
            new object[] { new object[] { "7", Assignment.Assumed } }
            )]
        [TestCase(
            "B*",
            "44:02:01:02S",
            new object[] { new object[] { "44", Assignment.Expert } }
            )]
        public void WmdaDataRepository_WhenAlleleHasRelatedSerology_SerologyAssignmentsSuccessfullyCaptured(
            string molecularLocus,
            string alleleName,
            object[] expectedSerologyAssignments)
        {
            var actualRelationship = GetSingleWmdaHlaTyping(molecularLocus, alleleName);

            var expectedAssignments = expectedSerologyAssignments
                .Select(x => (object[])x)
                .Select(x => new SerologyAssignment(x[0].ToString(), (Assignment) x[1]));

            actualRelationship.Assignments.ShouldBeEquivalentTo(expectedAssignments);
        }

        [TestCase("B*", "83:01")]
        [TestCase("DQB1*", "02:18N")]
        public void WmdaDataRepository_WhenAlleleHasNoRelatedSerology_NoSerologyAssignmentsAreCaptured(
            string molecularLocus,
            string alleleName)
        {
            var actualRelationship = GetSingleWmdaHlaTyping(molecularLocus, alleleName);

            actualRelationship.Assignments.ShouldBeEquivalentTo(new List<SerologyAssignment>());
        }
    }
}
