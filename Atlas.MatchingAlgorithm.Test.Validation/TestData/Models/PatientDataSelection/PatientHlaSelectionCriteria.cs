using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.PatientDataSelection
{
    public class PatientHlaSelectionCriteria
    {
        public PhenotypeInfo<HlaTypingResolution> PatientTypingResolutions = new PhenotypeInfo<HlaTypingResolution>(HlaTypingResolution.Tgs);
        
        /// <summary>
        /// Determines whether each position should have a donor match
        /// </summary>
        public PhenotypeInfo<PatientHlaSource> HlaSources { get; set; } = new PhenotypeInfo<PatientHlaSource>(PatientHlaSource.Match);

        public PhenotypeInfo<bool> HlaMatches => HlaSources.Map((l, p, source) => source == PatientHlaSource.Match);
        
        /// <summary>
        /// The match level of the expected matching donor (if a match is expected)
        /// e.g. If PGroup, an different allele in the same p-group as the donor will be selected
        /// This may converge with MatchGrades in the future
        /// </summary>
        public PhenotypeInfo<MatchLevel> MatchLevels { get; set; } = new PhenotypeInfo<MatchLevel>(MatchLevel.Allele);
        
        /// <summary>
        /// Determines whether the patient should be homozygous at each locus
        /// </summary>
        public LociInfo<bool> IsHomozygous = new LociInfo<bool>(false);
        
        /// <summary>
        /// Determines which match orientation will be used when selecting patient hla
        /// If arbitrary, either Direct or Cross will be chosen and used consistently
        /// </summary>
        public LociInfo<MatchOrientation> Orientations { get; set; } = new LociInfo<MatchOrientation>(MatchOrientation.Arbitrary);
    }
}