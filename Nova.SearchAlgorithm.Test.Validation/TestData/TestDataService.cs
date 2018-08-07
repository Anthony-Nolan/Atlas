using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Data.Entity;

namespace Nova.SearchAlgorithm.Test.Validation.TestData
{
    public static class TestDataService
    {
        public static void SetupTestData()
        {
            var donors = new List<Donor>
            {
                DonorGenotypeRepository.DonorGenotypes.First().BuildTgsTypedDonor(1, DonorType.Adult, RegistryCode.AN),
                DonorGenotypeRepository.DonorGenotypes.First().BuildTgsTypedDonor(2, DonorType.Cord, RegistryCode.AN),
            };
            
            TestDataRepository.SetupDatabase();
            TestDataRepository.AddTestDonors(donors);
        }
    }
}