using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using NUnit.Framework;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Repositories.Wmda
{
    [TestFixtureSource(typeof(WmdaRepositoryTestFixtureArgs), nameof(WmdaRepositoryTestFixtureArgs.AllelesStatusesTestArgs))]
    public class AlleleStatusesTest : WmdaRepositoryTestBase<AlleleStatus>
    {
        public AlleleStatusesTest(IEnumerable<AlleleStatus> hlaNomAlleleStatuses, IEnumerable<string> matchLoci)
            : base(hlaNomAlleleStatuses, matchLoci)
        {
        }

        [TestCase("A*", "01:26", "Partial", "cDNA")]
        [TestCase("A*", "01:27N", "Partial", "cDNA")]
        [TestCase("DRB1*", "07:04", "Full", "cDNA")]
        [TestCase("A*","68:113", "Partial", "gDNA")]
        [TestCase("B*", "07:05:06", "Partial", "cDNA")]
        [TestCase("C*", "07:491:01N", "Partial", "cDNA")]
        [TestCase("C*", "07:01:53", "Full", "cDNA")]
        [TestCase("DQB1*", "03:01:01:07", "Full", "gDNA")]
        [TestCase("C*", "07:01:01:14Q", "Full", "gDNA")]
        public void WmdaDataRepository_AlleleStatuses_SuccessfullyCaptured(string locus, string alleleName, string sequenceStatus, string dnaCategory)
        {
            var actualAlleleStatus = GetSingleWmdaHlaTyping(locus, alleleName);
            var expectedAlleleStatus = new AlleleStatus(locus, alleleName, sequenceStatus, dnaCategory);
            Assert.AreEqual(actualAlleleStatus, expectedAlleleStatus);
        }
    }
}
