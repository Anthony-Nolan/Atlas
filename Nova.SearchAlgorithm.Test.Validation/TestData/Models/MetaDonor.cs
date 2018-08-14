using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Entity;
using Nova.SearchAlgorithm.Test.Validation.TestData.Builders;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;

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
        
        /// <summary>
        /// Flag will be set if this meta-donor is guaranteed to have a p-group that corresponds to multiple test alleles,
        /// such that patient data can be chosen that does not exactliy match but does share a p-group
        /// </summary>
        public bool HasNonUniquePGroups { get; set; }

        public List<Donor> DatabaseDonors { get; set; }

        /// <summary>
        /// Determines to what typing levels each hla will be set at in the database
        /// </summary>
        public List<PhenotypeInfo<HlaTypingCategory>> HlaTypingCategorySets { get; set; } = new List<PhenotypeInfo<HlaTypingCategory>>
        {
            new HlaTypingCategorySetBuilder().Build()
        };

        public IEnumerable<Donor> GetDatabaseDonors()
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
    }
}