using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Maths;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.MatchPrediction.Config;

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

            if (allPossibleHaplotypes == null)
            {
                return await NaivelyExpandPhenotype(phenotype, hlaNomenclatureVersion, allowedLoci, gGroupsPerPosition);
            }

            var allowedHaplotypes = allPossibleHaplotypes.Where(h =>
                allowedLoci.All(l => gGroupsPerLocus.GetLocus(l).Contains(h.GetLocus(l)))
            ).ToList();

            var allowedDiplotypes = Combinations.AllPairs(allowedHaplotypes.ToArray(), true).ToList();
            var filteredDiplotypes = FilterDiplotypes(allowedDiplotypes, gGroupsPerPosition);

            logger.SendTrace($"Filtered expanded genotypes: {filteredDiplotypes.Count}");
            return filteredDiplotypes.Select(dp => new PhenotypeInfo<string>(dp.Item1, dp.Item2)).ToHashSet();
        }

        /// <summary>
        /// Filters a collection of diplotypes down to only those which are possible for an input phenotype, typed to G-Group resolution
        /// </summary>
        /// <param name="diplotypes">Source of diplotypes to filter.</param>
        /// <param name="gGroupsPerPosition">GGroups present in the phenotype being expanded.</param>
        private static List<Tuple<LociInfo<string>, LociInfo<string>>> FilterDiplotypes(
            IReadOnlyCollection<Tuple<LociInfo<string>, LociInfo<string>>> diplotypes,
            PhenotypeInfo<IReadOnlyCollection<string>> gGroupsPerPosition
        )
        {
            // ReSharper disable once UseDeconstructionOnParameter
            return diplotypes.Where(diplotype =>
            {
                return !gGroupsPerPosition.Reduce((l, gGroups, toExclude) =>
                {
                    if (toExclude)
                    {
                        return true;
                    }

                    // If MPA does not support locus - e.g. DPB1: the haplotypes will have no data at such loci
                    // Distinct from the "allowedLoci", as there loci can be excluded even if supported
                    if (!LocusSettings.MatchPredictionLoci.Contains(l))
                    {
                        return false;
                    }

                    var diplotypeGGroup1 = diplotype.Item1.GetLocus(l);
                    var diplotypeGGroup2 = diplotype.Item2.GetLocus(l);

                    return !(gGroups.Position1.Contains(diplotypeGGroup1) && gGroups.Position2.Contains(diplotypeGGroup2) ||
                             gGroups.Position1.Contains(diplotypeGGroup2) && gGroups.Position2.Contains(diplotypeGGroup1));
                }, false);
            }).ToList();
        }

        /// <summary>
        /// Expands phenotype naively - i.e. with no haplotype filtering.
        /// Will throw an exception if input is too ambiguous. 
        /// </summary>
        private async Task<ISet<PhenotypeInfo<string>>> NaivelyExpandPhenotype(
            PhenotypeInfo<string> phenotype,
            string hlaNomenclatureVersion,
            ISet<Locus> allowedLoci,
            PhenotypeInfo<IReadOnlyCollection<string>> gGroupsPerPosition)
        {
            var numberOfGenotypes = await CalculateNumberOfPermutations(phenotype, hlaNomenclatureVersion, allowedLoci);
            // If long overflows, we will get a negative number - this number cannot be negative in other circumstances
            if (numberOfGenotypes < 0 || numberOfGenotypes > int.MaxValue)
            {
                throw new NotImplementedException(
                    "Imputation of provided phenotype would create an unfeasibly large number of permutations. This code path is not not supported for such ambiguous data."
                );
            }

            // TODO: ATLAS-235: Pass in excludedLoci here if lost in merge
            return ambiguousPhenotypeExpander.ExpandPhenotype(gGroupsPerPosition);
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