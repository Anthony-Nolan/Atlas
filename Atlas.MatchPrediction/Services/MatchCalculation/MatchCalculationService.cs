using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Matching.Services;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.MatchPrediction.Models;

namespace Atlas.MatchPrediction.Services.MatchCalculation
{
    public interface IMatchCalculationService
    {
        /// <returns>
        /// null for non calculated, 0, 1, or 2 if calculated representing the match count.
        /// </returns>
        public Task<GenotypeMatchDetails> MatchAtPGroupLevel(
            PhenotypeInfo<string> patientGenotype,
            PhenotypeInfo<string> donorGenotype,
            string hlaNomenclatureVersion,
            ISet<Locus> allowedLoci);
    }

    internal class MatchCalculationService : IMatchCalculationService
    {
        private readonly ILocusHlaConverter locusHlaConverter;
        private readonly IStringBasedLocusMatchCalculator stringBasedLocusMatchCalculator;

        public MatchCalculationService(
            ILocusHlaConverter locusHlaConverter,
            IStringBasedLocusMatchCalculator stringBasedLocusMatchCalculator)
        {
            this.locusHlaConverter = locusHlaConverter;
            this.stringBasedLocusMatchCalculator = stringBasedLocusMatchCalculator;
        }

        public async Task<GenotypeMatchDetails> MatchAtPGroupLevel(
            PhenotypeInfo<string> patientGenotype,
            PhenotypeInfo<string> donorGenotype,
            string hlaNomenclatureVersion,
            ISet<Locus> allowedLoci)
        {
            const TargetHlaCategory matchingResolution = TargetHlaCategory.PGroup;

            var patientGenotypeAsSinglePGroups =
                await locusHlaConverter.ConvertGroupsToPGroups(patientGenotype, hlaNomenclatureVersion, allowedLoci);

            var donorGenotypeAsSinglePGroups =
                await locusHlaConverter.ConvertGroupsToPGroups(donorGenotype, hlaNomenclatureVersion, allowedLoci);

            var matchCounts = new LociInfo<int?>().Map((locus, matchCount) =>
            {
                var patientHla = patientGenotypeAsSinglePGroups.GetLocus(locus);
                var donorHla = donorGenotypeAsSinglePGroups.GetLocus(locus);
                return allowedLoci.Contains(locus) ? stringBasedLocusMatchCalculator.MatchCount(patientHla, donorHla) : (int?) null;
            });

            return new GenotypeMatchDetails
            {
                MatchCounts = matchCounts,
                PatientGenotype = patientGenotype,
                DonorGenotype = donorGenotype,
                AvailableLoci = allowedLoci
            };
        }
    }
}