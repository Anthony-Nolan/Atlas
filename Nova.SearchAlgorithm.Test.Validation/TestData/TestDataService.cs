using System;
using System.Linq;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Builders;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Repositories;

namespace Nova.SearchAlgorithm.Test.Validation.TestData
{
    public static class TestDataService
    {
        public static void SetupTestData()
        {
            var allDonorTypes = Enum.GetValues(typeof(DonorType)).Cast<DonorType>();
            var allRegistries = Enum.GetValues(typeof(RegistryCode)).Cast<RegistryCode>();

            var donors = GenotypeRepository.Genotypes
                .SelectMany(genotype => HlaTypingCategoryHelper.AllCategories()
                    .SelectMany(category => allDonorTypes
                        .SelectMany(donorType => allRegistries.Select(registry =>
                                new DonorBuilder(genotype)
                                    .WithFullTypingCategory(category)
                                    .AtRegistry(registry)
                                    .OfType(donorType)
                                    .Build()
                            )
                        )
                    )
                );

            TestDataRepository.SetupDatabase();
            TestDataRepository.AddTestDonors(donors);
        }
    }
}