using FluentAssertions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using NUnit.Framework;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Repositories.Wmda
{
    public class ConfidentialAlleleTest : WmdaRepositoryTestBase<ConfidentialAllele>
    {
        protected override void SetupTestData()
        {
            SetTestData(WmdaDataRepository.ConfidentialAlleles, MolecularLoci);
        }
        
        [Test]
        public void WmdaDataRepository_ConfidentialAlleles_SuccessfullyCaptured()
        {
            var expectedConfidentialAlleles = new List<ConfidentialAllele>
            {
                new ConfidentialAllele("A*", "02:01:01:28"),
                new ConfidentialAllele("B*", "18:37:02"),
                new ConfidentialAllele("B*", "48:43"),
                new ConfidentialAllele("C*", "06:211N"),
                new ConfidentialAllele("DQB1*", "03:01:01:20"),
                new ConfidentialAllele("DQB1*", "03:23:03")
            };

            WmdaHlaTypings.ShouldAllBeEquivalentTo(expectedConfidentialAlleles);
        }
    }
}
