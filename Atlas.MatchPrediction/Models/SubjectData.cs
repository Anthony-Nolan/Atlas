using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;

// A subject's typing as submitted - allele, MAC, XX code or serology, whatever the request carried. Any resolution.
// Fed straight to CompressedPhenotypeExpanderInput.Phenotype.
using SubmittedPhenotype = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.PhenotypeInfo<string>;

namespace Atlas.MatchPrediction.Models
{
    public class SubjectData
    {
        public SubmittedPhenotype HlaTyping { get; set; }
        public SubjectFrequencySet SubjectFrequencySet { get; set; }

        public SubjectData(SubmittedPhenotype hlaTyping, SubjectFrequencySet subjectFrequencySet)
        {
            HlaTyping = hlaTyping;
            SubjectFrequencySet = subjectFrequencySet;
        }
    }

    public class SubjectFrequencySet
    {
        public HaplotypeFrequencySet FrequencySet { get; set; }
        public string SubjectLogDescription { get; set; }

        public SubjectFrequencySet(HaplotypeFrequencySet frequencySet, string subjectLogDescription)
        {
            FrequencySet = frequencySet;
            SubjectLogDescription = subjectLogDescription;
        }
    }
}
