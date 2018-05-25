using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using NUnit.Framework;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Repositories.Wmda
{
    [TestFixtureSource(typeof(WmdaRepositoryTestFixtureArgs), nameof(WmdaRepositoryTestFixtureArgs.RelDnaSerTestArgs))]
    public class RelDnaSerTest : WmdaRepositoryTestBase<RelDnaSer>
    {
        public RelDnaSerTest(IEnumerable<RelDnaSer> relDnaSer, IEnumerable<string> matchLoci)
            : base(relDnaSer, matchLoci)
        {
        }

        [Test]
        public void RelDnaSer_SuccessfullyCaptured()
        {
            var alleleWithUnambiguous = new RelDnaSer("A*", "01:01:01:01", new List<SerologyAssignment>
            {
                new SerologyAssignment("1", Assignment.Unambiguous)
            });

            var alleleWithPossible = new RelDnaSer("B*", "07:31", new List<SerologyAssignment>
            {
                new SerologyAssignment("42", Assignment.Possible),
                new SerologyAssignment("7", Assignment.Possible)
            });

            var alleleWithAssumed = new RelDnaSer("C*", "04:04:01:01", new List<SerologyAssignment>
            {
                new SerologyAssignment("4", Assignment.Assumed)
            });

            var alleleWithExpert = new RelDnaSer("C*", "14:02:01:01", new List<SerologyAssignment>
            {
                new SerologyAssignment("1", Assignment.Expert)
            });

            var alleleWithMultiple = new RelDnaSer("A*", "02:55", new List<SerologyAssignment>
            {
                new SerologyAssignment("2", Assignment.Assumed),
                new SerologyAssignment("28", Assignment.Expert)
            });

            var alleleWithNone = new RelDnaSer("B*", "83:01", new List<SerologyAssignment>());

            var nullAllele = new RelDnaSer("DQB1*", "02:18N", new List<SerologyAssignment>());

            var lowAllele = new RelDnaSer("B*", "39:01:01:02L", new List<SerologyAssignment>
            {
                new SerologyAssignment("3901", Assignment.Possible)
            });

            var questionableAllele = new RelDnaSer("C*", "07:121Q", new List<SerologyAssignment>
            {
                new SerologyAssignment("7", Assignment.Assumed)
            });

            var secretedAllele = new RelDnaSer("B*", "44:02:01:02S", new List<SerologyAssignment>
            {
                new SerologyAssignment("44", Assignment.Expert)
            });

            Assert.AreEqual(alleleWithUnambiguous, GetSingleWmdaHlaTyping("A*", "01:01:01:01"));
            Assert.AreEqual(alleleWithPossible, GetSingleWmdaHlaTyping("B*", "07:31"));
            Assert.AreEqual(alleleWithAssumed, GetSingleWmdaHlaTyping("C*", "04:04:01:01"));
            Assert.AreEqual(alleleWithExpert, GetSingleWmdaHlaTyping("C*", "14:02:01:01"));
            Assert.AreEqual(alleleWithMultiple, GetSingleWmdaHlaTyping("A*", "02:55"));
            Assert.AreEqual(alleleWithNone, GetSingleWmdaHlaTyping("B*", "83:01"));

            Assert.AreEqual(nullAllele, GetSingleWmdaHlaTyping("DQB1*", "02:18N"));
            Assert.AreEqual(lowAllele, GetSingleWmdaHlaTyping("B*", "39:01:01:02L"));
            Assert.AreEqual(questionableAllele, GetSingleWmdaHlaTyping("C*", "07:121Q"));
            Assert.AreEqual(secretedAllele, GetSingleWmdaHlaTyping("B*", "44:02:01:02S"));
        }
    }
}
