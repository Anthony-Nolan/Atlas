using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Models.PatientDataSelection
{
    public class PatientHlaSelectionCriteria
    {
        // TODO: NOVA-1642 - patient typing resolutions to be set by step; currently defaulting to TGS
        public PhenotypeInfo<HlaTypingResolution> PatientTypingResolutions = new PhenotypeInfo<HlaTypingResolution>(HlaTypingResolution.Tgs);
        
        /// <summary>
        /// Determines whether each position should have a donor match
        /// </summary>
        public PhenotypeInfo<bool> HlaMatches { get; set; } = new PhenotypeInfo<bool>(true);
        
        /// <summary>
        /// The match level of the expected matching donor (if a match is expected)
        /// e.g. If PGroup, an different allele in the same p-group as the donor will be selected
        /// This may converge with MatchGrades in the future
        /// </summary>
        public PhenotypeInfo<MatchLevel> MatchLevels { get; set; }
        
        /// <summary>
        /// Determines whether the patient should be homozygous at each locus
        /// </summary>
        public LocusInfo<bool> IsHomozygous = new LocusInfo<bool>(false);
    }
}