using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;

namespace Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype
{
    public interface ICompressedPhenotypeExpander
    {
        public Task<IEnumerable<PhenotypeInfo<string>>> ExpandCompressedPhenotype(PhenotypeInfo<string> phenotype, string hlaNomenclatureVersion);

        /// <returns>
        /// The number of genotypes that would be returned if <see cref="ExpandCompressedPhenotype"/> were to be called on this phenotype.
        /// </returns>>
        public Task<long> CalculateNumberOfPermutations(PhenotypeInfo<string> phenotype, string hlaNomenclatureVersion);
    }

    public class CompressedPhenotypeExpander : ICompressedPhenotypeExpander
    {
        private const TargetHlaCategory FrequencyResolution = TargetHlaCategory.GGroup;

        private readonly IHlaMetadataDictionaryFactory metadataDictionaryFactory;
        private readonly IAmbiguousPhenotypeExpander ambiguousPhenotypeExpander;

        public CompressedPhenotypeExpander(
            IHlaMetadataDictionaryFactory metadataDictionaryFactory,
            IAmbiguousPhenotypeExpander ambiguousPhenotypeExpander)
        {
            this.metadataDictionaryFactory = metadataDictionaryFactory;
            this.ambiguousPhenotypeExpander = ambiguousPhenotypeExpander;
        }

        public async Task<IEnumerable<PhenotypeInfo<string>>> ExpandCompressedPhenotype(
            PhenotypeInfo<string> phenotype,
            string hlaNomenclatureVersion)
        {
            var allelesPerLocus = await ExpandAllelesPerLocus(phenotype, hlaNomenclatureVersion);

            var genotypes = ambiguousPhenotypeExpander.ExpandPhenotype(allelesPerLocus);

            return genotypes;
        }

        /// <inheritdoc />
        public async Task<long> CalculateNumberOfPermutations(PhenotypeInfo<string> phenotype, string hlaNomenclatureVersion)
        {
            var allelesPerLocus = await ExpandAllelesPerLocus(phenotype, hlaNomenclatureVersion);

            return allelesPerLocus.Reduce(
                (l, p, alleles, count) => l == Locus.Dpb1 ? count : count * alleles.Count,
                1L
            );
        }

        private async Task<PhenotypeInfo<IReadOnlyCollection<string>>> ExpandAllelesPerLocus(
            PhenotypeInfo<string> phenotype,
            string hlaNomenclatureVersion)
        {
            var hlaMetadataDictionary = metadataDictionaryFactory.BuildDictionary(hlaNomenclatureVersion);

            return await phenotype.MapAsync(async (locus, position, hla) =>
                {
                    if (locus == Locus.Dpb1)
                    {
                        return null;
                    }

                    return await hlaMetadataDictionary.ConvertHla(locus, hla, FrequencyResolution);
                }
            );
        }
    }
}