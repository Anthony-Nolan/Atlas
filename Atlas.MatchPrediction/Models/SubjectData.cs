using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;

namespace Atlas.MatchPrediction.Models
{
    public class SubjectData
    {
        public PhenotypeInfo<string> HlaTyping { get; set; }
        public SubjectFrequencySet SubjectFrequencySet { get; set; }

        public SubjectData(PhenotypeInfo<string> hlaTyping, SubjectFrequencySet subjectFrequencySet)
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