using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Maths;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;

namespace Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype
{
    public interface ICompressedPhenotypeExpander
    {
        // TODO: ATLAS-400: Either use this conditionally or remove 
        public Task<ISet<PhenotypeInfo<string>>> ExpandCompressedPhenotype(PhenotypeInfo<string> phenotype, string hlaNomenclatureVersion);

        public Task<ISet<PhenotypeInfo<string>>> ExpandCompressedPhenotype(
            PhenotypeInfo<string> phenotype,
            string hlaNomenclatureVersion,
            IReadOnlyCollection<LociInfo<string>> allPossibleHaplotypes,
            ISet<Locus> allowedLoci);

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
        private readonly ILogger logger;

        public CompressedPhenotypeExpander(
            IAmbiguousPhenotypeExpander ambiguousPhenotypeExpander,
            ILocusHlaConverter locusHlaConverter,
            ILogger logger)
        {
            this.ambiguousPhenotypeExpander = ambiguousPhenotypeExpander;
            this.locusHlaConverter = locusHlaConverter;
            this.logger = logger;
        }

        public async Task<ISet<PhenotypeInfo<string>>> ExpandCompressedPhenotype(PhenotypeInfo<string> phenotype, string hlaNomenclatureVersion)
        {
            var gGroupsPerLocus = await locusHlaConverter.ConvertHla(phenotype, FrequencyResolution, hlaNomenclatureVersion);
            return ambiguousPhenotypeExpander.ExpandPhenotype(gGroupsPerLocus);
        }

        // TODO: ATLAS-400: Tests for this method
        /// <inheritdoc />
        public async Task<ISet<PhenotypeInfo<string>>> ExpandCompressedPhenotype(
            PhenotypeInfo<string> phenotype,
            string hlaNomenclatureVersion,
            IReadOnlyCollection<LociInfo<string>> allPossibleHaplotypes,
            ISet<Locus> allowedLoci)
        {
            var gGroupsPerPosition = await locusHlaConverter.ConvertHla(phenotype, FrequencyResolution, hlaNomenclatureVersion);
            var gGroupsPerLocus = gGroupsPerPosition.ToLociInfo((l, gGroups1, gGroups2)
                => gGroups1 != null && gGroups2 != null ? new HashSet<string>(gGroups1.Concat(gGroups2)) : null
            );

            var allowedHaplotypes = allPossibleHaplotypes.Where(h =>
                gGroupsPerLocus.A.Contains(h.A)
                && gGroupsPerLocus.B.Contains(h.B)
                && gGroupsPerLocus.C.Contains(h.C)
                && gGroupsPerLocus.Dqb1.Contains(h.Dqb1)
                && gGroupsPerLocus.Drb1.Contains(h.Drb1)
            ).ToList();

            // TODO: ATLAS-400: Filtering when still Enumerable to save memory
            // TODO: ATLAS-400: De-duplicate as unordered pairs! Currently double counting diplotypes. 
            var allowedDiplotypes = Combinations.AllPairs(allowedHaplotypes.ToArray(), true).ToList();

            logger.SendTrace($"(Filtered) Possible allowed haplotypes: {allowedHaplotypes.Count}");
            logger.SendTrace($"(Filtered) Possible allowed *diplotypes* (calculated): {Combinations.NumberOfPairs(allowedHaplotypes.Count, true)}");
            logger.SendTrace($"(Filtered) Possible allowed *diplotypes* (actually expanded): {allowedDiplotypes.Count()}");
            logger.SendTrace($"(Unfiltered) Possible genotypes: {await CalculateNumberOfPermutations(phenotype, hlaNomenclatureVersion)}");
            
            var expanded = ambiguousPhenotypeExpander.ExpandPhenotype(gGroupsPerPosition, allowedHaplotypes);
            logger.SendTrace($"Expanded genotypes: {expanded.Count}");

            return expanded;
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