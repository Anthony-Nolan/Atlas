using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Models.PatientDataSelection
{
    public class MetaDonorSelectionCriteria
    {
        public DonorType MatchingDonorType { get; set; }
        public RegistryCode MatchingRegistry { get; set; }
        
        /// <summary>
        /// Determines how many fields the matching meta-donor's genotype should have at each position
        /// </summary>
        public PhenotypeInfo<TgsHlaTypingCategory> MatchingTgsTypingCategories { get; set; } = new PhenotypeInfo<TgsHlaTypingCategory>();
        
        /// <summary>
        /// The match level of the expected matching donor (if a match is expected)
        /// e.g. If PGroup, an different allele in the same p-group as the donor will be selected
        /// This may converge with MatchGrades in the future
        /// </summary>
        public PhenotypeInfo<MatchLevel> MatchLevels { get; set; } = new PhenotypeInfo<MatchLevel>
        {
            A_1 = MatchLevel.Allele,
            A_2 = MatchLevel.Allele,
            B_1 = MatchLevel.Allele,
            B_2 = MatchLevel.Allele,
            C_1 = MatchLevel.Allele,
            C_2 = MatchLevel.Allele,
            DPB1_1 = MatchLevel.Allele,
            DPB1_2 = MatchLevel.Allele,
            DQB1_1 = MatchLevel.Allele,
            DQB1_2 = MatchLevel.Allele,
            DRB1_1 = MatchLevel.Allele,
            DRB1_2 = MatchLevel.Allele,
        };
    }
}