using System.Collections.Generic;
using Atlas.Common.Public.Models.GeneticData;

namespace Atlas.MatchPrediction.Functions.Models.Debug
{
    public class GenotypeMatcherRequest
    {
        public IEnumerable<Locus> AllowedLoci { get; set; }
        public SubjectInfo Patient { get; set; }
        public SubjectInfo Donor { get; set; }
    }
}