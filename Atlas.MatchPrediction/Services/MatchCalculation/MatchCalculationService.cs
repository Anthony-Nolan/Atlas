using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Matching.Services;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchPrediction.Utils;

namespace Atlas.MatchPrediction.Services.MatchCalculation
{
    public interface IMatchCalculationService
    {
        /// <returns>
        /// null for non calculated, 0, 1, or 2 if calculated representing the match count.
        ///
        /// Patient genotype and donor genotype *MUST* be provided at G Group typing resolution.
        /// </returns>
        public Task<LociInfo<int?>> CalculateMatchCounts(
            PhenotypeInfo<string> patientGenotype,
            PhenotypeInfo<string> donorGenotype,
            string hlaNomenclatureVersion,
            ISet<Locus> allowedLoci);

        /// <returns>
        /// null for non calculated, 0, 1, or 2 if calculated representing the match count.
        ///
        /// This is a performance-optimised version of <see cref="CalculateMatchCounts"/>, and as such has some more stringent requirements.
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
        private readonly IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory;

        public MatchCalculationService(
            IStringBasedLocusMatchCalculator stringBasedLocusMatchCalculator,
            IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory)
        {
            this.stringBasedLocusMatchCalculator = stringBasedLocusMatchCalculator;
            this.hlaMetadataDictionaryFactory = hlaMetadataDictionaryFactory;
        }

        public async Task<LociInfo<int?>> CalculateMatchCounts(
            PhenotypeInfo<string> patientGenotype,
            PhenotypeInfo<string> donorGenotype,
            string hlaNomenclatureVersion,
            ISet<Locus> allowedLoci)
        {
            var hlaMetadataDictionary = hlaMetadataDictionaryFactory.BuildDictionary(hlaNomenclatureVersion);

            var patientGenotypeAsSinglePGroups = await hlaMetadataDictionary.ConvertGGroupsToPGroups(patientGenotype, allowedLoci);
            var donorGenotypeAsSinglePGroups = await hlaMetadataDictionary.ConvertGGroupsToPGroups(donorGenotype, allowedLoci);

            return CalculateMatchCounts_Fast(patientGenotypeAsSinglePGroups, donorGenotypeAsSinglePGroups, allowedLoci);
        }

        // This method will be called millions of times in match prediction, and needs to stay as fast as possible. 
        public LociInfo<int?> CalculateMatchCounts_Fast(
            PhenotypeInfo<string> patientGenotype,
            PhenotypeInfo<string> donorGenotype,
            ISet<Locus> allowedLoci)
        {
            return new LociInfo<int?>(
                allowedLoci.Contains(Locus.A) ? stringBasedLocusMatchCalculator.MatchCount(patientGenotype.A, donorGenotype.A) : (int?) null,
                allowedLoci.Contains(Locus.B) ? stringBasedLocusMatchCalculator.MatchCount(patientGenotype.B, donorGenotype.B) : (int?) null,
                allowedLoci.Contains(Locus.C) ? stringBasedLocusMatchCalculator.MatchCount(patientGenotype.C, donorGenotype.C) : (int?) null,
                null,
                allowedLoci.Contains(Locus.Dqb1) ? stringBasedLocusMatchCalculator.MatchCount(patientGenotype.Dqb1, donorGenotype.Dqb1) : (int?) null,
                allowedLoci.Contains(Locus.Drb1) ? stringBasedLocusMatchCalculator.MatchCount(patientGenotype.Drb1, donorGenotype.Drb1) : (int?) null
            );
        }
    }
}