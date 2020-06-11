using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface;

namespace Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype
{
    public interface ICompressedPhenotypeExpander
    {
        public Task<IEnumerable<PhenotypeInfo<string>>> ExpandCompressedPhenotype(PhenotypeInfo<string> phenotype, string hlaNomenclatureVersion);
    }

    public class CompressedPhenotypeExpander : ICompressedPhenotypeExpander
    {
        private readonly IHlaMetadataDictionaryFactory metadataDictionaryFactory;
        private readonly IAmbiguousPhenotypeExpander ambiguousPhenotypeExpander;

        public CompressedPhenotypeExpander(
            IHlaMetadataDictionaryFactory metadataDictionaryFactory,
            IAmbiguousPhenotypeExpander ambiguousPhenotypeExpander)
        {
            this.metadataDictionaryFactory = metadataDictionaryFactory;
            this.ambiguousPhenotypeExpander = ambiguousPhenotypeExpander;
        }

        public async Task<IEnumerable<PhenotypeInfo<string>>> ExpandCompressedPhenotype(PhenotypeInfo<string> phenotype, string hlaNomenclatureVersion)
        {
            var hlaMetadataDictionary = metadataDictionaryFactory.BuildDictionary(hlaNomenclatureVersion);

            var allelesPerLocus = await phenotype.MapAsync(async (locus, position, hla) =>
                await hlaMetadataDictionary.GetTwoFieldAllelesForAmbiguousHla(locus, hla));

            var genotypes = ambiguousPhenotypeExpander.ExpandPhenotype(allelesPerLocus);

            return genotypes;
        }
    }
}