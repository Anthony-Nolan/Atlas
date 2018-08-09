using System.Collections.Generic;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Repositories
{
    public static class MetaDonorRepository
    {
        public static readonly IEnumerable<GenotypeDonor> MetaDonors = new List<GenotypeDonor>
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
                    new PhenotypeInfo<HlaTypingCategory>
                    {
                        A_1 = HlaTypingCategory.Tgs,
                        A_2 = HlaTypingCategory.Tgs,
                        B_1 = HlaTypingCategory.Tgs,
                        B_2 = HlaTypingCategory.Tgs,
                        C_1 = HlaTypingCategory.Untyped,
                        C_2 = HlaTypingCategory.Untyped,
                        DPB1_1 = HlaTypingCategory.Tgs,
                        DPB1_2 = HlaTypingCategory.Tgs,
                        DQB1_1 = HlaTypingCategory.Tgs,
                        DQB1_2 = HlaTypingCategory.Tgs,
                        DRB1_1 = HlaTypingCategory.Tgs,
                        DRB1_2 = HlaTypingCategory.Tgs,
                    }
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
    }
}