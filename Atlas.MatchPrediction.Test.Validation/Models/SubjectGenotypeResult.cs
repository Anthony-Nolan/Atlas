using System.Collections.Generic;
using Atlas.MatchPrediction.Test.Validation.Data.Models;
using Atlas.MatchPrediction.Test.Validation.Data.Models.Homework;

namespace Atlas.MatchPrediction.Test.Validation.Models
{
    internal class SubjectGenotypeResult
    {
        public bool HasMissingHla { get; }

        public SubjectInfo SubjectInfo { get; }

        /// <summary>
        /// Will be `null` if the subject has not yet been imputed.
        /// </summary>
        public IEnumerable<SubjectGenotype> Genotypes { get; set; }

        public SubjectGenotypeResult(bool hasMissingHla, SubjectInfo subjectInfo)
        {
            HasMissingHla = hasMissingHla;
            SubjectInfo = subjectInfo;
        }
    }
}