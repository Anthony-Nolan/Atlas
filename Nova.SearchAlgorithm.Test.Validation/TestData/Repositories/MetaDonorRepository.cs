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
                    new PhenotypeInfo<HlaTypingCategory>
                    {
                        A_1 = HlaTypingCategory.TgsFourFieldAllele,
                        A_2 = HlaTypingCategory.TgsFourFieldAllele,
                        B_1 = HlaTypingCategory.TgsFourFieldAllele,
                        B_2 = HlaTypingCategory.TgsFourFieldAllele,
                        C_1 = HlaTypingCategory.Untyped,
                        C_2 = HlaTypingCategory.Untyped,
                        DPB1_1 = HlaTypingCategory.TgsFourFieldAllele,
                        DPB1_2 = HlaTypingCategory.TgsFourFieldAllele,
                        DQB1_1 = HlaTypingCategory.TgsFourFieldAllele,
                        DQB1_2 = HlaTypingCategory.TgsFourFieldAllele,
                        DRB1_1 = HlaTypingCategory.TgsFourFieldAllele,
                        DRB1_2 = HlaTypingCategory.TgsFourFieldAllele,
                    }
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