using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Builders;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.PatientDataSelection;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Repositories;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Services;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Models
{
    /// <summary>
    /// A 'meta-donor', with a genotype, and donor type.
    /// The donor's genotype will have the resolution artificially reduced, and hence correspond to multiple database donors 
    /// </summary>
    public class MetaDonor {
        private Genotype genotype;
        private List<Donor> databaseDonors;
        private readonly GenotypeGenerator genotypeGenerator;

        public MetaDonor()
        {
            var alleleRepository = new AlleleRepository();
            genotypeGenerator = new GenotypeGenerator(alleleRepository);
        }
        
        public Genotype Genotype
        {
            get
            {
                if (genotype == null)
                {
                    genotype = genotypeGenerator.GenerateGenotype(GenotypeCriteria);
                }

                return genotype;
            }
        }

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
                        .OfType(DonorType)
                        .WithTypingCategories(databaseDonorCriteria.MatchingTypingResolutions)
                        .WithShouldMatchGenotype(databaseDonorCriteria.ShouldMatchGenotype)
                        .Build())
                    .ToList();
            }

            return DatabaseDonors;
        }
    }
}