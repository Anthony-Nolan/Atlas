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
        public LociInfo<int?> CalculateMatchCounts_Fast(
            PhenotypeInfo<string> patientGenotype,
            PhenotypeInfo<string> donorGenotype,
            ISet<Locus> allowedLoci)
        {
            return new LociInfo<int?>(l => allowedLoci.Contains(l)
                    ? stringBasedLocusMatchCalculator.MatchCount(patientGenotype.GetLocus(l), donorGenotype.GetLocus(l))
                    : null);
        }
    }
}