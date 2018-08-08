using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
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

            var metaDonors = new List<GenotypeDonor>
            {
                new GenotypeDonor
                {
                    DonorType = DonorType.Adult,
                    Registry = RegistryCode.AN,
                    Genotype = GenotypeRepository.NextGenotype(),
                    HlaTypingCategorySets = new List<PhenotypeInfo<HlaTypingCategory>>
                    {
                        GenotypeDonor.FullHlaAtTypingCategory(HlaTypingCategory.Tgs),
                        GenotypeDonor.FullHlaAtTypingCategory(HlaTypingCategory.ThreeField),
                        GenotypeDonor.FullHlaAtTypingCategory(HlaTypingCategory.TwoField),
                        GenotypeDonor.FullHlaAtTypingCategory(HlaTypingCategory.XxCode),
                        GenotypeDonor.FullHlaAtTypingCategory(HlaTypingCategory.NmdpCode),
                        GenotypeDonor.FullHlaAtTypingCategory(HlaTypingCategory.Serology),
                    }
                },
                new GenotypeDonor
                {
                    DonorType = DonorType.Cord,
                    Registry = RegistryCode.AN,
                    Genotype = GenotypeRepository.NextGenotype()
                },
                new GenotypeDonor
                {
                    DonorType = DonorType.Adult,
                    Registry = RegistryCode.DKMS,
                    Genotype = GenotypeRepository.NextGenotype()
                },
                new GenotypeDonor
                {
                    DonorType = DonorType.Cord,
                    Registry = RegistryCode.DKMS,
                    Genotype = GenotypeRepository.NextGenotype()
                }
            };

            TestDataRepository.SetupDatabase();
            TestDataRepository.AddTestDonors(metaDonors.SelectMany(md => md.GetDatabaseDonors()));
        }
    }
}