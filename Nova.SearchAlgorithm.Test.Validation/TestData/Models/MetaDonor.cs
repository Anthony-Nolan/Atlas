using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Entity;
using Nova.SearchAlgorithm.Test.Validation.TestData.Builders;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.PatientDataSelection;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Models
{
    /// <summary>
    /// A 'meta-donor', with a genotype, registry, and donor type.
    /// The donor's genotype will have the resolution artifically reduced, and hence correspond to multiple database donors 
    /// </summary>
    public class MetaDonor
    {
        private Genotype genotype;
        private List<Donor> databaseDonors;

        public Genotype Genotype
        {
            get
            {
                if (genotype == null)
                {
                    genotype = GenotypeGenerator.GenerateGenotype(GenotypeCriteria);
                }

                return genotype;
            }
        }

        public RegistryCode Registry { get; set; }
        public DonorType DonorType { get; set; }

        /// <summary>
        /// Criteria for selecting a Genotype for this meta-donor
        /// </summary>
        public GenotypeCriteria GenotypeCriteria { get; set; }

        public List<Donor> DatabaseDonors
        {
            get
            {
                if (databaseDonors == null)
                {
                    throw new Exception("Tried to get database donors for meta-donor, but found null. Have the donors been added to the database?");
                }

                return databaseDonors;
            }
            set => databaseDonors = value;
        }

        /// <summary>
        /// Determines to what typing levels each hla will be set at in the database
        /// </summary>
        public List<DatabaseDonorSpecification> DatabaseDonorSpecifications { get; set; } = new List<DatabaseDonorSpecification>
        {
            new DatabaseDonorSpecification
            {
                MatchingTypingResolutions = new PhenotypeInfo<HlaTypingResolution>(HlaTypingResolution.Tgs),
            }
        };

        public IEnumerable<Donor> GetDatabaseDonors()
        {
            if (databaseDonors == null)
            {
                DatabaseDonors = DatabaseDonorSpecifications.Select(databaseDonorCriteria => new DonorBuilder(Genotype)
                        .AtRegistry(Registry)
                        .OfType(DonorType)
                        .WithTypingCategories(databaseDonorCriteria.MatchingTypingResolutions)
                        .Build())
                    .ToList();
            }

            return DatabaseDonors;
        }
    }
}