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
        /// <summary>
        /// Expands an ambiguous phenotype to GGroup resolution, then transforms into all possible permutations of the given hla representations.
        /// Does not consider phase - so the results cannot necessarily be considered Diplotypes.
        ///
        /// Without <see cref="allPossibleHaplotypes"/> provided, this process is extremely slow, and not fit for any but the least ambiguous phenotypes.
        /// </summary>
        /// <param name="phenotype">Given phenotype. Can be of any supported hla resolution.</param>
        /// <param name="hlaNomenclatureVersion">HLA nomenclature version to be used</param>
        /// <param name="allowedLoci">Loci that should be considered for expansion.</param>
        /// <param name="allPossibleHaplotypes">
        /// A list of all available haplotypes - should be taken from the corresponding haplotype frequency set for a given search.
        /// When not provided, all possible genotypes will be imputed - with any ambiguity the number of permutations increases massively.
        ///
        /// It is recommended to always provide a collection of haplotypes to use for filtering. 
        /// </param>
        public Task<ISet<PhenotypeInfo<string>>> ExpandCompressedPhenotype(
            PhenotypeInfo<string> phenotype,
            string hlaNomenclatureVersion,
            ISet<Locus> allowedLoci,
            // ReSharper disable once ParameterTypeCanBeEnumerable.Global
            IReadOnlyCollection<LociInfo<string>> allPossibleHaplotypes = null);
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

        // TODO: ATLAS-400: Tests for this method
        /// <inheritdoc />
        public async Task<ISet<PhenotypeInfo<string>>> ExpandCompressedPhenotype(
            PhenotypeInfo<string> phenotype,
            string hlaNomenclatureVersion,
            ISet<Locus> allowedLoci,
            IReadOnlyCollection<LociInfo<string>> allPossibleHaplotypes)
        {
            var gGroupsPerPosition = await locusHlaConverter.ConvertHla(phenotype, FrequencyResolution, hlaNomenclatureVersion, allowedLoci);
            var gGroupsPerLocus = gGroupsPerPosition.ToLociInfo((l, gGroups1, gGroups2)
                => gGroups1 != null && gGroups2 != null ? new HashSet<string>(gGroups1.Concat(gGroups2)) : null
            );

            // TODO: ATLAS-400: Return smallest of two sets - sometimes allowedDiplotypes is bigger than fully expanding the input...
            if (allPossibleHaplotypes == null)
            {
                return ambiguousPhenotypeExpander.ExpandPhenotype(gGroupsPerPosition);
            }
            
            var allowedHaplotypes = allPossibleHaplotypes.Where(h =>
                allowedLoci.All(l => gGroupsPerLocus.GetLocus(l).Contains(h.GetLocus(l)))
            ).ToList();
            
            // TODO: ATLAS-400: Filtering when still Enumerable to save memory
            // TODO: ATLAS-400: De-duplicate as unordered pairs! Currently double counting diplotypes. 
            var allowedDiplotypes = Combinations.AllPairs(allowedHaplotypes.ToArray(), true)
                .Select(p => new UnorderedPair<LociInfo<string>>(p.Item1, p.Item2))
                .Distinct()
                .ToList();

            // TODO: ATLAS-400: No really, unit test this.
            var filteredDiplotypes = allowedDiplotypes.Where(diplotype =>
            {
                return !gGroupsPerPosition.Reduce((l, gGroups, toExclude) =>
                {
                    if (toExclude)
                    {
                        return true;
                    }

                    // TODO: ATLAS-400: Replace with cleaner config backed version
                    if (l == Locus.Dpb1)
                    {
                        return false;
                    }

                    var diplotypeGGroup1 = diplotype.Item1.GetLocus(l);
                    var diplotypeGGroup2 = diplotype.Item2.GetLocus(l);

                    return !((gGroups.Position1.Contains(diplotypeGGroup1) && gGroups.Position2.Contains(diplotypeGGroup2)) ||
                             (gGroups.Position1.Contains(diplotypeGGroup2) && gGroups.Position2.Contains(diplotypeGGroup1)));
                }, false);
            }).ToList();

            logger.SendTrace($"Filtered diplotypes: {filteredDiplotypes.Count}");

            // TODO: ATLAS-400: Prove to myself that this gives the same answers! - it looks good on the benchmarks, check existing int. tests once the build is fixed

            // TODO: ATLAS-400: Check how this copes with homozygous loci, it might not be good!
            // TODO: ATLAS-400: If we can break this down to diplotypes here, can we *not* times by two and get rid of the homozygous correction factor altogether?
            // TODO: ATLAS-400: Is the count of this calculatable mathematically? In case we want to decide to instead fully impute if that gives us fewer options 
            var finalDiplotypes = new HashSet<PhenotypeInfo<string>>(filteredDiplotypes
                .Select(dp => new PhenotypeInfo<string>(dp.Item1, dp.Item2))
                .SelectMany(d => new[] {d})
                .ToList()
            );

            logger.SendTrace($"Final diplotype based response: {finalDiplotypes.Count}");
            return finalDiplotypes;
        }

        private async Task<long> CalculateNumberOfPermutations(
            PhenotypeInfo<string> phenotype,
            string hlaNomenclatureVersion,
            ISet<Locus> allowedLoci)
        {
            var allelesPerLocus = await locusHlaConverter.ConvertHla(phenotype, FrequencyResolution, hlaNomenclatureVersion, allowedLoci);

            return allelesPerLocus.Reduce((l, p, alleles, count) => allowedLoci.Contains(l) ? count * alleles.Count : count, 1L);
        }
    }
}