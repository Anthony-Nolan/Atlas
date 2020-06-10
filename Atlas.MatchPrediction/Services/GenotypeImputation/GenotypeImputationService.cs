using System.Threading.Tasks;
using Atlas.MatchPrediction.Client.Models.GenotypeImputation;
using Atlas.HlaMetadataDictionary.ExternalInterface;

namespace Atlas.MatchPrediction.Services.GenotypeImputation
{
    public interface IGenotypeImputationService
    {
        public Task<GenotypeImputationResponse> ImputeGenotype(GenotypeImputationInput genotypeImputationInput);
    }

    public class GenotypeImputationService : IGenotypeImputationService
    {
        private readonly IHlaMetadataDictionary hlaMetadataDictionary;

        public GenotypeImputationService(string nomenclatureVersion, IHlaMetadataDictionaryFactory factory)
        {
            this.hlaMetadataDictionary = factory.BuildDictionary(nomenclatureVersion);
        }

        public async Task<GenotypeImputationResponse> ImputeGenotype(GenotypeImputationInput genotypeImputationInput)
        {
            var allelesPerLocus = await genotypeImputationInput.Phenotype.MapAsync(async (locus, position, hla) =>
                await hlaMetadataDictionary.GetTwoFieldAllelesForAmbiguousHla(locus, hla));

            return new GenotypeImputationResponse();
        }
    }
}