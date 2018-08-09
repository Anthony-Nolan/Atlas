using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Entity;
using Nova.SearchAlgorithm.Test.Validation.TestData.Builders;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Models
{
    /// <summary>
    /// A 'meta-donor', with a genotype, registry, and donor type.
    /// The donor's genotype will have the resolution artifically reduced, and hence correspond to multiple database donors 
    /// </summary>
    public class MetaDonor
    {
        public Genotype Genotype { get; set; }
        public RegistryCode Registry { get; set; }
        public DonorType DonorType { get; set; }

        public List<Donor> DatabaseDonors { get; set; }

        /// <summary>
        /// Determines to what typing levels each hla will be set at in the database
        /// </summary>
        public List<PhenotypeInfo<HlaTypingCategory>> HlaTypingCategorySets { get; set; } = new List<PhenotypeInfo<HlaTypingCategory>>
        {
            FullHlaAtTypingCategory(HlaTypingCategory.TgsFourFieldAllele)
        };

        public List<Donor> GetDatabaseDonors()
        {
            if (DatabaseDonors == null)
            {
                DatabaseDonors = HlaTypingCategorySets.Select(typingCategorySet => new DonorBuilder(Genotype)
                        .AtRegistry(Registry)
                        .OfType(DonorType)
                        .WithTypingCategories(typingCategorySet)
                        .Build())
                    .ToList();
            }

            return DatabaseDonors;
        }

        public static PhenotypeInfo<HlaTypingCategory> FullHlaAtTypingCategory(HlaTypingCategory category)
        {
            return new PhenotypeInfo<HlaTypingCategory>
            {
                A_1 = category,
                A_2 = category,
                B_1 = category,
                B_2 = category,
                C_1 = category,
                C_2 = category,
                DPB1_1 = category,
                DPB1_2 = category,
                DQB1_1 = category,
                DQB1_2 = category,
                DRB1_1 = category,
                DRB1_2 = category,
            };
        }
    }
}