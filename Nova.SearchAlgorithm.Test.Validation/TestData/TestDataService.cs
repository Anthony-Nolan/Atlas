using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Data.Entity;
using Nova.SearchAlgorithm.Test.Validation.TestData.Builders;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Repositories;

namespace Nova.SearchAlgorithm.Test.Validation.TestData
{
    public static class TestDataService
    {
        public static void SetupTestData()
        {
            var donors = new List<Donor>
            {
                new DonorBuilder(DonorGenotypeRepository.DonorGenotypes.First())
                    .WithFullTypingCategory(HlaTypingCategory.Tgs)
                    .OfType(DonorType.Adult)
                    .AtRegistry(RegistryCode.AN)
                    .Build(),
                new DonorBuilder(DonorGenotypeRepository.DonorGenotypes.First())
                    .WithFullTypingCategory(HlaTypingCategory.Tgs)
                    .OfType(DonorType.Cord)
                    .AtRegistry(RegistryCode.AN)
                    .Build(),
                new DonorBuilder(DonorGenotypeRepository.DonorGenotypes.First())
                    .WithFullTypingCategory(HlaTypingCategory.TwoField)
                    .OfType(DonorType.Adult)
                    .AtRegistry(RegistryCode.AN)
                    .Build(),
            };

            TestDataRepository.SetupDatabase();
            TestDataRepository.AddTestDonors(donors);
        }
    }
}