using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface;

namespace Atlas.MatchPrediction.Services.ImputeToGenotypes
{
    public interface IImputeToGenotypesService
    {
        public Task<IEnumerable<PhenotypeInfo<string>>> ImputePhenotype(
            PhenotypeInfo<string> phenotype,
            string hlaNomenclatureVersion);
    }

    public class ImputeToGenotypesService : IImputeToGenotypesService
    {
        private readonly IHlaMetadataDictionaryFactory metadataDictionaryFactory;

        public ImputeToGenotypesService(IHlaMetadataDictionaryFactory metadataDictionaryFactory)
        {
            this.metadataDictionaryFactory = metadataDictionaryFactory;
        }

        public async Task<IEnumerable<PhenotypeInfo<string>>> ImputePhenotype(
            PhenotypeInfo<string> phenotype,
            string hlaNomenclatureVersion)
        {
            var hlaMetadataDictionary = metadataDictionaryFactory.BuildDictionary(hlaNomenclatureVersion);

            var allelesPerLocus = await phenotype.MapAsync(async (locus, position, hla) =>
                await hlaMetadataDictionary.GetTwoFieldAllelesForAmbiguousHla(locus, hla));

            //TODO: ATLAS-20: Impute to genotypes

            return new List<PhenotypeInfo<string>>();
        }
    }
}