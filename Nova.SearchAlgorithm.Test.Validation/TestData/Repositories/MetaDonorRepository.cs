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