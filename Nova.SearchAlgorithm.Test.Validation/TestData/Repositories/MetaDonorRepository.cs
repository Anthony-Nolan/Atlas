using System.Collections.Generic;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Repositories
{
    public static class MetaDonorRepository
    {
        public static readonly IEnumerable<MetaDonor> MetaDonors = new List<MetaDonor>
        {
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                Registry = RegistryCode.AN,
                Genotype = GenotypeRepository.NextGenotype(),
                HlaTypingCategorySets = new List<PhenotypeInfo<HlaTypingCategory>>
                {
                    MetaDonor.FullHlaAtTypingCategory(HlaTypingCategory.TgsFourFieldAllele),
                    MetaDonor.FullHlaAtTypingCategory(HlaTypingCategory.ThreeFieldTruncatedAllele),
                    MetaDonor.FullHlaAtTypingCategory(HlaTypingCategory.TwoFieldTruncatedAllele),
                    MetaDonor.FullHlaAtTypingCategory(HlaTypingCategory.XxCode),
                    MetaDonor.FullHlaAtTypingCategory(HlaTypingCategory.NmdpCode),
                    MetaDonor.FullHlaAtTypingCategory(HlaTypingCategory.Serology),
                }
            },
            new MetaDonor
            {
                DonorType = DonorType.Cord,
                Registry = RegistryCode.AN,
                Genotype = GenotypeRepository.NextGenotype()
            },
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                Registry = RegistryCode.DKMS,
                Genotype = GenotypeRepository.NextGenotype()
            },
            new MetaDonor
            {
                DonorType = DonorType.Cord,
                Registry = RegistryCode.DKMS,
                Genotype = GenotypeRepository.NextGenotype()
            }
        };
    }
}