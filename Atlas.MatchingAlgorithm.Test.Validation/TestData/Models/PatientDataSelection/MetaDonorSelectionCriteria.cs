using System.Collections.Generic;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.PatientDataSelection
{
    public class MetaDonorSelectionCriteria
    {
        public DonorType MatchingDonorType { get; set; } = DonorType.Adult;

        /// <summary>
        /// Determines how many fields the matching meta-donor's genotype should have at each position
        /// </summary>
        public PhenotypeInfo<TgsHlaTypingCategory> MatchingTgsTypingCategories { get; set; } =
            new PhenotypeInfo<TgsHlaTypingCategory>(TgsHlaTypingCategory.Arbitrary);

        /// <summary>
        /// The match level of the expected matching donor (if a match is expected)
        /// Necessary for meta-donor selection as we must ensure the genotype is valid for the specified match type
        /// e.g. for a p-group match, ensure that other alleles in the same p-group exist in our dataset
        /// </summary>
        public PhenotypeInfo<MatchLevel> MatchLevels { get; set; }

        /// <summary>
        /// Determines to what resolutions / match levels the expected matched donor is typed
        /// Necessary for meta-donor selection to ensure the selected meta-donor contains donors at the expected resolution
        /// </summary>
        public List<DatabaseDonorSpecification> DatabaseDonorDetailsSets { get; set; } = new List<DatabaseDonorSpecification>();

        /// <summary>
        /// Determines whether the expected meta-donor should be homozygous at each locus
        /// </summary>
        public LociInfo<bool> IsHomozygous { get; set; } = new LociInfo<bool>(false);

        /// <summary>
        /// Determines whether the expected meta-donor should have allele strings guaranteed to contain different groups
        /// </summary>
        public PhenotypeInfo<bool> AlleleStringContainsDifferentAntigenGroups { get; set; } = new PhenotypeInfo<bool>(false);

        /// <summary>
        /// Determines whether the expected meta-donor should have alleles with a non null expression suffix
        /// </summary>
        public PhenotypeInfo<bool> HasNonNullExpressionSuffix { get; set; } = new PhenotypeInfo<bool>(false);
        
        /// <summary>
        /// Determines whether the expected meta-donor should have null expressing alleles (i.e. an 'N' suffix)
        /// </summary>
        public PhenotypeInfo<bool> IsNullExpressing { get; set; } = new PhenotypeInfo<bool>(false);
        
        /// <summary>
        /// Determines how many matching meta-donors to ignore
        /// To be used in the case when multiple patients are to be tested, each against a meta-donor with otherwise identical criteria
        /// Note: This assumes the meta donor selection implementation will always return the meta-donors in the same order
        /// </summary>
        public int MetaDonorsToSkip { get; set; }
    }
}