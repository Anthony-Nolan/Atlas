using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Matching.Services;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.MatchPrediction.Config;
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
            ISet<Locus> allowedPatientLoci,
            ISet<Locus> allowedDonorLoci);
    }

    internal class MatchCalculationService : IMatchCalculationService
    {
        private readonly ILocusHlaConverter locusHlaConverter;
        private readonly ILocusMatchCalculator locusMatchCalculator;

        public MatchCalculationService(
            ILocusHlaConverter locusHlaConverter, 
            ILocusMatchCalculator locusMatchCalculator)
        {
            this.locusHlaConverter = locusHlaConverter;
            this.locusMatchCalculator = locusMatchCalculator;
        }

        public async Task<GenotypeMatchDetails> MatchAtPGroupLevel(
            PhenotypeInfo<string> patientGenotype,
            PhenotypeInfo<string> donorGenotype,
            string hlaNomenclatureVersion,
            ISet<Locus> allowedPatientLoci,
            ISet<Locus> allowedDonorLoci)
        {
            const TargetHlaCategory matchingResolution = TargetHlaCategory.PGroup;

            var patientGenotypeAsPGroups =
                await locusHlaConverter.ConvertHla(patientGenotype, matchingResolution, hlaNomenclatureVersion, allowedPatientLoci);
            var donorGenotypeAsPGroups =
                await locusHlaConverter.ConvertHla(donorGenotype, matchingResolution, hlaNomenclatureVersion, allowedDonorLoci);

            var allowedLoci = LocusSettings.MatchPredictionLoci.ToList();

            var matchCounts = new LociInfo<int?>().Map((locus, matchCount) =>
                allowedLoci.Contains(locus)
                    ? locusMatchCalculator.MatchCount(
                        patientGenotypeAsPGroups.GetLocus(locus).Map(x => x as IEnumerable<string>),
                        donorGenotypeAsPGroups.GetLocus(locus).Map(x => x as IEnumerable<string>))
                    : (int?) null);

            return new GenotypeMatchDetails{MatchCounts = matchCounts, PatientGenotype = patientGenotype, DonorGenotype = donorGenotype};
        }
    }
}