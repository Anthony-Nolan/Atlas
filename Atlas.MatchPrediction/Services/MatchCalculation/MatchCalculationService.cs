using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Matching.Services;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.MatchPrediction.Client.Models.MatchCalculation;

namespace Atlas.MatchPrediction.Services.MatchCalculation
{
    public interface IMatchCalculationService
    {
        public Task<MatchCalculationResponse> MatchAtPGroupLevel(
            PhenotypeInfo<string> patientGenotype,
            PhenotypeInfo<string> donorGenotype,
            string hlaNomenclatureVersion);
    }

    public class MatchCalculationService : IMatchCalculationService
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

        public async Task<MatchCalculationResponse> MatchAtPGroupLevel(
            PhenotypeInfo<string> patientGenotype,
            PhenotypeInfo<string> donorGenotype,
            string hlaNomenclatureVersion)
        {
            const TargetHlaCategory matchingResolution = TargetHlaCategory.PGroup;

            var patientGenotypeAsPGroups =
                await locusHlaConverter.ConvertHla(patientGenotype, matchingResolution, hlaNomenclatureVersion);
            var donorGenotypeAsPGroups =
                await locusHlaConverter.ConvertHla(donorGenotype, matchingResolution, hlaNomenclatureVersion);

            // TODO: ATLAS-217/ATLAS-417: Return MatchHla & 10/10 result

            return new MatchCalculationResponse();
        }
    }
}