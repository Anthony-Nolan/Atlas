using Atlas.MatchPrediction.Test.Validation.Data.Models;

namespace Atlas.MatchPrediction.Test.Validation.Models
{
    internal class SubjectGenotypeResult
    {
        public bool HasMissingHla { get; }

        public SubjectInfo SubjectInfo { get; }

        public SubjectGenotypeResult(bool hasMissingHla, SubjectInfo subjectInfo)
        {
            HasMissingHla = hasMissingHla;
            SubjectInfo = subjectInfo;
        }
    }
}