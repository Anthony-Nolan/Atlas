using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.Models.Wmda;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Repositories.Wmda
{
    internal class ConfidentialAlleleTest : WmdaRepositoryTestBase<ConfidentialAllele>
    {
        protected override void SetupTestData()
        {
            SetTestData(WmdaDataRepository.GetWmdaDataset(HlaDatabaseVersionToTest).ConfidentialAlleles, MolecularLoci);
        }
        
        [Test]
        public void WmdaDataRepository_ConfidentialAlleles_SuccessfullyCaptured()
        {
            var expectedConfidentialAlleles = new List<ConfidentialAllele>
            {
                new ConfidentialAllele("A*", "02:741"),
                new ConfidentialAllele("B*", "40:01:51"),
                new ConfidentialAllele("B*", "40:366"),
                new ConfidentialAllele("C*", "02:02:41"),
                new ConfidentialAllele("DQB1*", "03:279"),
                new ConfidentialAllele("DQB1*", "06:02:29")
            };

            WmdaHlaTypings.Should().BeEquivalentTo(expectedConfidentialAlleles);
        }
    }
}
