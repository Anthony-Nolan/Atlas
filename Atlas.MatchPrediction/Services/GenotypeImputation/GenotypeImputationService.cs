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
        private readonly IHlaMetadataDictionaryFactory metadataDictionaryFactory;

        public GenotypeImputationService(IHlaMetadataDictionaryFactory metadataDictionaryFactory)
        {
            this.metadataDictionaryFactory = metadataDictionaryFactory;
        }

        public async Task<GenotypeImputationResponse> ImputeGenotype(GenotypeImputationInput genotypeImputationInput)
        {
            var hlaMetadataDictionary = metadataDictionaryFactory.BuildDictionary(genotypeImputationInput.NomenclatureVersion);

            var allelesPerLocus = await genotypeImputationInput.Phenotype.MapAsync(async (locus, position, hla) =>
                await hlaMetadataDictionary.GetTwoFieldAllelesForAmbiguousHla(locus, hla));

            return new GenotypeImputationResponse();
        }
    }
}