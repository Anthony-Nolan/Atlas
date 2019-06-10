using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Repositories.Wmda
{
    public class AlleleStatusesTest : WmdaRepositoryTestBase<AlleleStatus>
    {
        protected override void SetupTestData()
        {
            SetTestData(WmdaDataRepository.GetWmdaDataset(HlaDatabaseVersionToTest).AlleleStatuses, MolecularLoci);
        }
        
        [TestCase("A*", "01:26", "Partial", "cDNA")]
        [TestCase("A*", "01:27N", "Partial", "cDNA")]
        [TestCase("DRB1*", "07:04", "Full", "cDNA")]
        [TestCase("A*","68:113", "Partial", "gDNA")]
        [TestCase("B*", "07:05:06", "Partial", "cDNA")]
        [TestCase("DPB1*", "01:01:03", "Partial", "cDNA")]
        [TestCase("C*", "07:491:01N", "Partial", "cDNA")]
        [TestCase("C*", "07:01:53", "Full", "cDNA")]
        [TestCase("DQB1*", "03:01:01:07", "Full", "gDNA")]
        [TestCase("C*", "07:01:01:14Q", "Full", "gDNA")]
        public void WmdaDataRepository_AlleleStatuses_SuccessfullyCaptured(string locus, string alleleName, string sequenceStatus, string dnaCategory)
        {
            var expectedAlleleStatus = new AlleleStatus(locus, alleleName, sequenceStatus, dnaCategory);

            var actualAlleleStatus = GetSingleWmdaHlaTyping(locus, alleleName);

            Assert.AreEqual(expectedAlleleStatus, actualAlleleStatus);
        }
    }
}
