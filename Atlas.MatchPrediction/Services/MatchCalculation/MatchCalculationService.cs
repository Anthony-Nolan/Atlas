using System.Collections.Generic;
using Atlas.Common.Matching.Services;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Services.MatchCalculation
{
    public interface IMatchCalculationService
    {
        /// <returns>
        /// null for non calculated, 0, 1, or 2 if calculated representing the match count.
        /// 
        /// Patient genotype and donor genotype *MUST* be provided at a resolution for which match counts can be calculated by string comparison.
        /// i.e. PGroups, or G-Groups when a null allele is present.
        /// </returns>
        public LociInfo<int?> CalculateMatchCounts_Fast(
            PhenotypeInfo<string> patientGenotype,
            PhenotypeInfo<string> donorGenotype,
            ISet<Locus> allowedLoci);
    }

    internal class MatchCalculationService : IMatchCalculationService
    {
        private readonly IStringBasedLocusMatchCalculator stringBasedLocusMatchCalculator;

        public MatchCalculationService(IStringBasedLocusMatchCalculator stringBasedLocusMatchCalculator)
        {
            this.stringBasedLocusMatchCalculator = stringBasedLocusMatchCalculator;
        }

        // This method will be called millions of times in match prediction, and needs to stay as fast as possible.
        // The explicit, unrolled form below is deliberate: it avoids the per-call closure allocation, the six
        // Func<Locus, int?> delegate invocations, and the GetLocus(..) switch lookups that the lambda-based
        // LociInfo constructor incurred. Accessing the A/B/C.. properties directly is allocation-free and inlinable.
        public LociInfo<int?> CalculateMatchCounts_Fast(
            PhenotypeInfo<string> patientGenotype,
            PhenotypeInfo<string> donorGenotype,
            ISet<Locus> allowedLoci)
        {
            return new LociInfo<int?>(
                valueA: allowedLoci.Contains(Locus.A) ? stringBasedLocusMatchCalculator.MatchCount(patientGenotype.A, donorGenotype.A) : null,
                valueB: allowedLoci.Contains(Locus.B) ? stringBasedLocusMatchCalculator.MatchCount(patientGenotype.B, donorGenotype.B) : null,
                valueC: allowedLoci.Contains(Locus.C) ? stringBasedLocusMatchCalculator.MatchCount(patientGenotype.C, donorGenotype.C) : null,
                valueDpb1: allowedLoci.Contains(Locus.Dpb1) ? stringBasedLocusMatchCalculator.MatchCount(patientGenotype.Dpb1, donorGenotype.Dpb1) : null,
                valueDqb1: allowedLoci.Contains(Locus.Dqb1) ? stringBasedLocusMatchCalculator.MatchCount(patientGenotype.Dqb1, donorGenotype.Dqb1) : null,
                valueDrb1: allowedLoci.Contains(Locus.Drb1) ? stringBasedLocusMatchCalculator.MatchCount(patientGenotype.Drb1, donorGenotype.Drb1) : null
            );
        }
    }
}