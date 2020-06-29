using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;

namespace Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype
{
    public interface ICompressedPhenotypeExpander
    {
        public Task<ISet<PhenotypeInfo<string>>> ExpandCompressedPhenotype(PhenotypeInfo<string> phenotype, string hlaNomenclatureVersion);

        /// <returns>
        /// The number of genotypes that would be returned if <see cref="ExpandCompressedPhenotype"/> were to be called on this phenotype.
        /// </returns>>
        public Task<long> CalculateNumberOfPermutations(PhenotypeInfo<string> phenotype, string hlaNomenclatureVersion);
    }

    internal class CompressedPhenotypeExpander : ICompressedPhenotypeExpander
    {
        private const TargetHlaCategory FrequencyResolution = TargetHlaCategory.GGroup;

        private readonly IAmbiguousPhenotypeExpander ambiguousPhenotypeExpander;
        private readonly ILocusHlaConverter locusHlaConverter;

        public CompressedPhenotypeExpander(
            IAmbiguousPhenotypeExpander ambiguousPhenotypeExpander,
            ILocusHlaConverter locusHlaConverter)
        {
            this.ambiguousPhenotypeExpander = ambiguousPhenotypeExpander;
            this.locusHlaConverter = locusHlaConverter;
        }

        public async Task<ISet<PhenotypeInfo<string>>> ExpandCompressedPhenotype(
            PhenotypeInfo<string> phenotype,
            string hlaNomenclatureVersion)
        {
            var allelesPerLocus = await locusHlaConverter.ConvertHla(phenotype, FrequencyResolution, hlaNomenclatureVersion);

            var genotypes = ambiguousPhenotypeExpander.ExpandPhenotype(allelesPerLocus);

            return genotypes;
        }

        /// <inheritdoc />
        public async Task<long> CalculateNumberOfPermutations(PhenotypeInfo<string> phenotype, string hlaNomenclatureVersion)
        {
            var allelesPerLocus = await locusHlaConverter.ConvertHla(phenotype, FrequencyResolution, hlaNomenclatureVersion);

            return allelesPerLocus.Reduce(
                (l, p, alleles, count) => l == Locus.Dpb1 ? count : count * alleles.Count,
                1L
            );
        }
    }
}