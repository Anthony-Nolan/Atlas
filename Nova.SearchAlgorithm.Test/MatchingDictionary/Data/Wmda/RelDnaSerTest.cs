using System;
using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Data.Wmda
{
    [TestFixtureSource(typeof(MolecularTestFixtureArgs), "Args")]
    public class RelDnaSerTest : WmdaDataTestBase<RelDnaSer>
    {
        public RelDnaSerTest(Func<IWmdaHlaType, bool> filter, IEnumerable<string> matchLoci)
            : base(filter, matchLoci)
        {
        }

        [Test]
        public void RelDnaSerRegexCapturesMappingAsExpected()
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

            Assert.AreEqual(alleleWithUnambiguous, GetSingleWmdaHlaType("A*", "01:01:01:01"));
            Assert.AreEqual(alleleWithPossible, GetSingleWmdaHlaType("B*", "07:31"));
            Assert.AreEqual(alleleWithAssumed, GetSingleWmdaHlaType("C*", "04:04:01:01"));
            Assert.AreEqual(alleleWithExpert, GetSingleWmdaHlaType("C*", "14:02:01:01"));
            Assert.AreEqual(alleleWithMultiple, GetSingleWmdaHlaType("A*", "02:55"));
            Assert.AreEqual(alleleWithNone, GetSingleWmdaHlaType("B*", "83:01"));

            Assert.AreEqual(nullAllele, GetSingleWmdaHlaType("DQB1*", "02:18N"));
            Assert.AreEqual(lowAllele, GetSingleWmdaHlaType("B*", "39:01:01:02L"));
            Assert.AreEqual(questionableAllele, GetSingleWmdaHlaType("C*", "07:121Q"));
            Assert.AreEqual(secretedAllele, GetSingleWmdaHlaType("B*", "44:02:01:02S"));
        }
    }
}
