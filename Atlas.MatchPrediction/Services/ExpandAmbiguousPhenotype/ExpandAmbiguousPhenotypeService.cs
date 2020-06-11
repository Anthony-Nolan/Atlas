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
        private readonly IAmbiguousPhenotypeExpander ambiguousPhenotypeExpander;

        public ExpandAmbiguousPhenotypeService(
            IHlaMetadataDictionaryFactory metadataDictionaryFactory,
            IAmbiguousPhenotypeExpander ambiguousPhenotypeExpander)
        {
            this.metadataDictionaryFactory = metadataDictionaryFactory;
            this.ambiguousPhenotypeExpander = ambiguousPhenotypeExpander;
        }

        public async Task<IEnumerable<PhenotypeInfo<string>>> ExpandPhenotype(PhenotypeInfo<string> phenotype, string hlaNomenclatureVersion)
        {
            var hlaMetadataDictionary = metadataDictionaryFactory.BuildDictionary(hlaNomenclatureVersion);

            var allelesPerLocus = await phenotype.MapAsync(async (locus, position, hla) =>
                await hlaMetadataDictionary.GetTwoFieldAllelesForAmbiguousHla(locus, hla));

            var genotypes = ambiguousPhenotypeExpander.ExpandPhenotype(allelesPerLocus);

            return genotypes;
        }
    }
}