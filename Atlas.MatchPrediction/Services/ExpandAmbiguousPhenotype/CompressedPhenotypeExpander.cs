using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Maths;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Utils;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Models;

// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable ParameterTypeCanBeEnumerable.Local

namespace Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype
{
    public interface ICompressedPhenotypeExpander
    {
        /// <summary>
        /// Expands an ambiguous phenotype to GGroup resolution, then transforms into all possible permutations of the given hla representations.
        /// Does not consider phase - so the results cannot necessarily be considered Diplotypes.
        /// </summary>
        /// <param name="phenotype">Given phenotype. Can be of any supported hla resolution.</param>
        /// <param name="hlaNomenclatureVersion">HLA nomenclature version to be used</param>
        /// <param name="allowedLoci">Loci that should be considered for expansion.</param>
        /// <param name="allGGroupHaplotypes">
        /// A subset of all available haplotypes, which are typed at G group resolution - should be taken from the corresponding haplotype frequency set for a given search.
        /// Together with <see cref="allPGroupHaplotypes"/>, represents all possibilities for haplotypes for the current frequency set.
        /// </param>
        /// <param name="allPGroupHaplotypes">
        /// A subset of all available haplotypes, which are typed at P group resolution - should be taken from the corresponding haplotype frequency set for a given search.
        /// Together with <see cref="allGGroupHaplotypes"/>, represents all possibilities for haplotypes for the current frequency set.
        /// </param>
        public Task<ISet<PhenotypeInfo<HlaAtKnownTypingCategory>>> ExpandCompressedPhenotype(
            PhenotypeInfo<string> phenotype,
            string hlaNomenclatureVersion,
            ISet<Locus> allowedLoci,
            IReadOnlyCollection<LociInfo<string>> allGGroupHaplotypes,
            IReadOnlyCollection<LociInfo<string>> allPGroupHaplotypes
        );
    }

    internal class CompressedPhenotypeExpander : ICompressedPhenotypeExpander
    {
        private readonly ILogger logger;
        private readonly IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory;

        public CompressedPhenotypeExpander(
            // ReSharper disable once SuggestBaseTypeForParameter
            IMatchPredictionLogger logger,
            IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory)
        {
            this.logger = logger;
            this.hlaMetadataDictionaryFactory = hlaMetadataDictionaryFactory;
        }

        /// <inheritdoc />
        public async Task<ISet<PhenotypeInfo<HlaAtKnownTypingCategory>>> ExpandCompressedPhenotype(
            PhenotypeInfo<string> phenotype,
            string hlaNomenclatureVersion,
            ISet<Locus> allowedLoci,
            IReadOnlyCollection<LociInfo<string>> allGGroupHaplotypes,
            IReadOnlyCollection<LociInfo<string>> allPGroupHaplotypes
        )
        {
            if (allGGroupHaplotypes == null || allPGroupHaplotypes == null)
            {
                throw new ArgumentException("Haplotypes must be provided for phenotype expansion to complete in a reasonable timeframe.");
            }

            var hlaMetadataDictionary = hlaMetadataDictionaryFactory.BuildDictionary(hlaNomenclatureVersion);

            var gGroupsPerPosition = await hlaMetadataDictionary.ConvertAllHla(phenotype, TargetHlaCategory.GGroup, allowedLoci);
            var gGroupsPerLocus = CombineSetsAtLoci(gGroupsPerPosition);

            var pGroupsPerPosition = await hlaMetadataDictionary.ConvertAllHla(phenotype, TargetHlaCategory.PGroup, allowedLoci);
            var pGroupsPerLocus = CombineSetsAtLoci(pGroupsPerPosition);

            var allowedHaplotypes = GetAllowedHaplotypes(allowedLoci, allGGroupHaplotypes, allPGroupHaplotypes, gGroupsPerLocus, pGroupsPerLocus);

            var allowedHaplotypesExcludingLoci = new HashSet<LociInfo<HlaAtKnownTypingCategory>>(allowedHaplotypes.Select(h =>
                h.Map((l, hla) => allowedLoci.Contains(l) ? hla : null)
            ));

            var allowedDiplotypes = Combinations.AllPairs(allowedHaplotypesExcludingLoci, true).ToList();
            var filteredDiplotypes = FilterDiplotypes(allowedDiplotypes, gGroupsPerPosition, pGroupsPerPosition, allowedLoci);

            logger.SendTrace($"Filtered expanded genotypes: {filteredDiplotypes.Count}");
            return filteredDiplotypes.Select(dp => new PhenotypeInfo<HlaAtKnownTypingCategory>(dp.Item1, dp.Item2)).ToHashSet();
        }

        private static IEnumerable<LociInfo<HlaAtKnownTypingCategory>> GetAllowedHaplotypes(
            ISet<Locus> allowedLoci,
            IReadOnlyCollection<LociInfo<string>> allGGroupHaplotypes,
            IReadOnlyCollection<LociInfo<string>> allPGroupHaplotypes,
            LociInfo<ISet<string>> gGroupsPerLocus,
            LociInfo<ISet<string>> pGroupsPerLocus)
        {
            var allowedGGroupHaplotypes = allGGroupHaplotypes.Where(haplotype =>
                    allowedLoci.All(locus => gGroupsPerLocus.GetLocus(locus).Contains(haplotype.GetLocus(locus)))
                )
                .Select(haplotype => haplotype.Map(hla => new HlaAtKnownTypingCategory(hla, HaplotypeTypingCategory.GGroup)))
                .ToList();

            var allowedPGroupHaplotypes = allPGroupHaplotypes.Where(haplotype =>
                    allowedLoci.All(locus => pGroupsPerLocus.GetLocus(locus).Contains(haplotype.GetLocus(locus)))
                )
                .Select(haplotype => haplotype.Map(hla => new HlaAtKnownTypingCategory(hla, HaplotypeTypingCategory.PGroup)))
                .ToList();

            return allowedGGroupHaplotypes.Concat(allowedPGroupHaplotypes);
        }

        /// <summary>
        /// Filters a collection of diplotypes down to only those which are possible for an input phenotype, typed to G-Group resolution
        /// </summary>
        /// <param name="allowedDiplotypes">Source of diplotypes to filter.</param>
        /// <param name="gGroupsPerPosition">GGroups present in the phenotype being expanded.</param>
        /// <param name="pGroupsPerPosition">PGroups present in the phenotype being expanded.</param>
        /// <param name="allowedLoci">List of loci that are being considered.</param>
        private static List<Tuple<LociInfo<HlaAtKnownTypingCategory>, LociInfo<HlaAtKnownTypingCategory>>> FilterDiplotypes(
            List<Tuple<LociInfo<HlaAtKnownTypingCategory>, LociInfo<HlaAtKnownTypingCategory>>> allowedDiplotypes,
            PhenotypeInfo<IReadOnlyCollection<string>> gGroupsPerPosition,
            PhenotypeInfo<IReadOnlyCollection<string>> pGroupsPerPosition,
            ISet<Locus> allowedLoci)
        {
            bool IsRepresentedInTargetPhenotype(HlaAtKnownTypingCategory hla, Locus locus, LocusPosition position)
            {
                return hla.TypingCategory switch
                {
                    HaplotypeTypingCategory.GGroup => gGroupsPerPosition.GetPosition(locus, position).Contains(hla.Hla),
                    HaplotypeTypingCategory.PGroup => pGroupsPerPosition.GetPosition(locus, position).Contains(hla.Hla),
                    _ => throw new ArgumentOutOfRangeException(nameof(hla.TypingCategory))
                };
            }

            // ReSharper disable once UseDeconstructionOnParameter
            return allowedDiplotypes.Where(diplotype =>
            {
                var (haplotype1, haplotype2) = diplotype;
                return !new LociInfo<bool>().Reduce((locus, _, toExclude) =>
                {
                    if (toExclude)
                    {
                        return true;
                    }

                    if (!allowedLoci.Contains(locus))
                    {
                        return false;
                    }

                    var hla1 = haplotype1.GetLocus(locus);
                    var hla2 = haplotype2.GetLocus(locus);

                    var representedDirectly =
                        IsRepresentedInTargetPhenotype(hla1, locus, LocusPosition.One) &&
                        IsRepresentedInTargetPhenotype(hla2, locus, LocusPosition.Two);
                    
                    var representedInverted =
                        IsRepresentedInTargetPhenotype(hla1, locus, LocusPosition.Two) &&
                        IsRepresentedInTargetPhenotype(hla2, locus, LocusPosition.One);

                    return !(representedDirectly || representedInverted);
                }, false);
            }).ToList();
        }

        private static LociInfo<ISet<string>> CombineSetsAtLoci(PhenotypeInfo<IReadOnlyCollection<string>> phenotypeInfo)
        {
            return phenotypeInfo.ToLociInfo((_, values1, values2)
                => values1 != null && values2 != null ? (ISet<string>) new HashSet<string>(values1.Concat(values2)) : null
            );
        }
    }
}