using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface;

namespace Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype
{
    public interface IExpandAmbiguousPhenotypeService
    {
        public Task<IEnumerable<PhenotypeInfo<string>>> ExpandPhenotype(PhenotypeInfo<string> phenotype, string hlaNomenclatureVersion);
    }

    public class ExpandAmbiguousPhenotypeService : IExpandAmbiguousPhenotypeService
    {
        private readonly IHlaMetadataDictionaryFactory metadataDictionaryFactory;

        public ExpandAmbiguousPhenotypeService(IHlaMetadataDictionaryFactory metadataDictionaryFactory)
        {
            this.metadataDictionaryFactory = metadataDictionaryFactory;
        }

        public async Task<IEnumerable<PhenotypeInfo<string>>> ExpandPhenotype(PhenotypeInfo<string> phenotype, string hlaNomenclatureVersion)
        {
            var hlaMetadataDictionary = metadataDictionaryFactory.BuildDictionary(hlaNomenclatureVersion);

            var allelesPerLocus = await phenotype.MapAsync(async (locus, position, hla) =>
                await hlaMetadataDictionary.GetTwoFieldAllelesForAmbiguousHla(locus, hla));

            //TODO: ATLAS-20: expand to genotypes

            return new List<PhenotypeInfo<string>>();
        }
    }
}