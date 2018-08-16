using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Models.PatientDataSelection
{
    public class PatientHlaSelectionCriteria
    {
        // TODO: NOVA-1642 - patient typing resolutions to be set by step; currently defaulting to TGS
        public readonly PhenotypeInfo<HlaTypingResolution> PatientTypingResolutions = new PhenotypeInfo<HlaTypingResolution>
        {
            A_1 = HlaTypingResolution.Tgs,
            A_2 = HlaTypingResolution.Tgs,
            B_1 = HlaTypingResolution.Tgs,
            B_2 = HlaTypingResolution.Tgs,
            C_1 = HlaTypingResolution.Tgs,
            C_2 = HlaTypingResolution.Tgs,
            DPB1_1 = HlaTypingResolution.Tgs,
            DPB1_2 = HlaTypingResolution.Tgs,
            DQB1_1 = HlaTypingResolution.Tgs,
            DQB1_2 = HlaTypingResolution.Tgs,
            DRB1_1 = HlaTypingResolution.Tgs,
            DRB1_2 = HlaTypingResolution.Tgs,
        };
        
        /// <summary>
        /// Determines whether each position should have a donor match
        /// </summary>
        public PhenotypeInfo<bool> HlaMatches { get; set; } = new PhenotypeInfo<bool>();
        
        /// <summary>
        /// The match level of the expected matching donor (if a match is expected)
        /// e.g. If PGroup, an different allele in the same p-group as the donor will be selected
        /// This may converge with MatchGrades in the future
        /// </summary>
        public PhenotypeInfo<MatchLevel> MatchLevels { get; set; }
    }
}