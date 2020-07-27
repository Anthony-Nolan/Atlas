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

            return new LociInfo<int?>().Map((locus, matchCount) =>
            {
                var patientHla = patientGenotypeAsSinglePGroups.GetLocus(locus);
                var donorHla = donorGenotypeAsSinglePGroups.GetLocus(locus);
                return allowedLoci.Contains(locus)
                    ? stringBasedLocusMatchCalculator.MatchCount(patientHla, donorHla, UntypedLocusBehaviour.Throw)
                    : (int?) null;
            });
        }
    }
}