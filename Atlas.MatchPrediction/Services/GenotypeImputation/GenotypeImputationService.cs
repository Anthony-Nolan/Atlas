using System.Threading.Tasks;
using Atlas.MatchPrediction.Client.Models.GenotypeImputation;
using Atlas.HlaMetadataDictionary.ExternalInterface;

namespace Atlas.MatchPrediction.Services.GenotypeImputation
{
    public interface IGenotypeImputationService
    {
        public Task<GenotypeImputationResponse> ImputeGenotype(GenotypeImputationInput phenotype);
    }

    public class GenotypeImputationService : IGenotypeImputationService
    {
        private readonly IHlaMetadataDictionary hlaMetadataDictionary;

        public GenotypeImputationService(IHlaMetadataDictionaryFactory factory)
        {
            this.hlaMetadataDictionary = factory.BuildDictionary();
        }

        public async Task<GenotypeImputationResponse> ImputeGenotype(GenotypeImputationInput phenotype)
        {
            var allelesPerLocus = await phenotype.Phenotype.MapAsync(async (locus, position, hla) =>
                await hlaMetadataDictionary.GetTwoFieldAllelesForAmbiguousHla(locus, hla));

            return new GenotypeImputationResponse();
        }
    }
}