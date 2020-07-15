using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchingAlgorithm.Client.Models.Scoring
{
    public class ScoringRequest<T>
    {
        public PhenotypeInfo<string> PatientHla { get; set; }
        public IReadOnlyCollection<Locus> LociToScore { get; set; }
        public IReadOnlyCollection<Locus> LociToExcludeFromAggregateScoring { get; set; }
        public T DonorData { get; set; }
    }
}