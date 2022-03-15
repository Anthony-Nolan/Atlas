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
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models;

// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable ParameterTypeCanBeEnumerable.Local

namespace Atlas.MatchPrediction.Services.CompressedPhenotypeExpansion
{
    internal class ExpandCompressedPhenotypeInput
    {
        /// <summary>
        /// Given phenotype. Can be of any supported hla resolution.
        /// </summary>
        public PhenotypeInfo<string> Phenotype { get; set; }

        /// <summary>
        /// HLA nomenclature version to be used
        /// </summary>
        public string HlaNomenclatureVersion { get; set; }

        /// <summary>
        /// Loci that should be considered for expansion.
        /// </summary>
        public ISet<Locus> AllowedLoci { get; set; }

        /// <summary>
        /// All available haplotypes, split by supported resolution - should be taken from the corresponding haplotype frequency set for a given search.
        /// Together, the properties represent all possibilities for haplotypes for the current frequency set.
        /// </summary>
        public DataByResolution<IReadOnlyCollection<LociInfo<string>>> AllHaplotypes { get; set; }
    }

    internal interface ICompressedPhenotypeExpander
    {
        /// <summary>
        /// Expands an ambiguous phenotype to GGroup resolution, then transforms into all possible permutations of the given hla representations.
        /// Does not consider phase - so the results cannot necessarily be considered Diplotypes.
        /// </summary>
        public Task<ISet<PhenotypeInfo<HlaAtKnownTypingCategory>>> ExpandCompressedPhenotype(ExpandCompressedPhenotypeInput input);
    }

    internal class CompressedPhenotypeExpander : ICompressedPhenotypeExpander
    {
        private readonly ILogger logger;
        private readonly IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory;
        private readonly ICompressedPhenotypeConverter converter;

        public CompressedPhenotypeExpander(
            // ReSharper disable once SuggestBaseTypeForParameter
            IMatchPredictionLogger logger,
            IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory,
            ICompressedPhenotypeConverter converter)
        {
            this.logger = logger;
            this.hlaMetadataDictionaryFactory = hlaMetadataDictionaryFactory;
            this.converter = converter;
        }

        /// <inheritdoc />
        public async Task<ISet<PhenotypeInfo<HlaAtKnownTypingCategory>>> ExpandCompressedPhenotype(ExpandCompressedPhenotypeInput input)
        {
            var allowedLoci = input.AllowedLoci;

            if (input.AllHaplotypes?.GGroup == null || input.AllHaplotypes?.PGroup == null || input.AllHaplotypes?.SmallGGroup == null)
            {
                throw new ArgumentException("Haplotypes must be provided for phenotype expansion to complete in a reasonable timeframe.");
            }

            var hlaMetadataDictionary = hlaMetadataDictionaryFactory.BuildDictionary(input.HlaNomenclatureVersion);

            var groupsPerPosition = await new DataByResolution<bool>().MapAsync(async (category, _) =>
                await converter.ConvertPhenotype(hlaMetadataDictionary, input.Phenotype, category.ToHlaTypingCategory().ToTargetHlaCategory(), allowedLoci)
            );

            var groupsPerLocus = groupsPerPosition.Map(CombineSetsAtLoci);

            var isUnambiguousAtSelectedLoci = allowedLoci.All(l =>
            {
                var groupsAtLocus = groupsPerPosition.SmallGGroup.GetLocus(l);
                return groupsAtLocus.Position1?.Count == 1 && groupsAtLocus.Position2?.Count == 1;
            });
            
            if (isUnambiguousAtSelectedLoci)
            {
                return new HashSet<PhenotypeInfo<HlaAtKnownTypingCategory>>
                {
                    groupsPerPosition.SmallGGroup.Map((_, __, v) =>
                        v == null ? null : new HlaAtKnownTypingCategory(v.Single(), HaplotypeTypingCategory.SmallGGroup))
                };
            }

            var allowedHaplotypes = GetAllowedHaplotypes(allowedLoci, input.AllHaplotypes, groupsPerLocus);

            var allowedHaplotypesExcludingLoci = new HashSet<LociInfo<HlaAtKnownTypingCategory>>(allowedHaplotypes.Select(h =>
                h.Map((l, hla) => allowedLoci.Contains(l) ? hla : null)
            ));

            var allowedDiplotypes = Combinations.AllPairs(allowedHaplotypesExcludingLoci, true).ToList();
            var filteredDiplotypes = FilterDiplotypes(allowedDiplotypes, groupsPerPosition, allowedLoci);

            logger.SendTrace($"Filtered expanded genotypes: {filteredDiplotypes.Count}");
            return filteredDiplotypes.Select(dp => new PhenotypeInfo<HlaAtKnownTypingCategory>(dp.Item1, dp.Item2)).ToHashSet();
        }

        private static IEnumerable<LociInfo<HlaAtKnownTypingCategory>> GetAllowedHaplotypes(
            ISet<Locus> allowedLoci,
            DataByResolution<IReadOnlyCollection<LociInfo<string>>> allHaplotypes,
            DataByResolution<LociInfo<ISet<string>>> groupsPerLocus)
        {
            var allowedHaplotypes = allHaplotypes.Map((category, haplotypes) =>
            {
                return haplotypes.Where(haplotype => allowedLoci.All(locus =>
                    {
                        var hlaGroups = groupsPerLocus.GetByCategory(category).GetLocus(locus);
                        return hlaGroups == null || hlaGroups.Contains(haplotype.GetLocus(locus));
                    }))
                    .Select(haplotype => haplotype.Map(hla => new HlaAtKnownTypingCategory(hla, category)))
                    .ToList();
            });

            return allowedHaplotypes.GGroup.Concat(allowedHaplotypes.PGroup).Concat(allowedHaplotypes.SmallGGroup);
        }

        /// <summary>
        /// Filters a collection of diplotypes down to only those which are possible for an input phenotype, typed to G-Group resolution
        /// </summary>
        /// <param name="allowedDiplotypes">Source of diplotypes to filter.</param>
        /// <param name="groupsPerPosition">GGroups and PGroups present in the phenotype being expanded.</param>
        /// <param name="allowedLoci">List of loci that are being considered.</param>
        private static List<Tuple<LociInfo<HlaAtKnownTypingCategory>, LociInfo<HlaAtKnownTypingCategory>>> FilterDiplotypes(
            List<Tuple<LociInfo<HlaAtKnownTypingCategory>, LociInfo<HlaAtKnownTypingCategory>>> allowedDiplotypes,
            DataByResolution<PhenotypeInfo<ISet<string>>> groupsPerPosition,
            ISet<Locus> allowedLoci)
        {
            bool IsRepresentedInTargetPhenotype(HlaAtKnownTypingCategory hla, Locus locus, LocusPosition position)
            {
                var groups = groupsPerPosition.GetByCategory(hla.TypingCategory).GetPosition(locus, position);
                return groups == null || groups.Contains(hla.Hla);
            }

            // ReSharper disable once UseDeconstructionOnParameter
            return allowedDiplotypes.Where(diplotype =>
            {
                var (haplotype1, haplotype2) = diplotype;
                return new LociInfo<bool>().AllAtLoci((locus, _) =>
                {
                    var hla1 = haplotype1.GetLocus(locus);
                    var hla2 = haplotype2.GetLocus(locus);

                    var representedDirectly =
                        IsRepresentedInTargetPhenotype(hla1, locus, LocusPosition.One) &&
                        IsRepresentedInTargetPhenotype(hla2, locus, LocusPosition.Two);

                    var representedInverted =
                        IsRepresentedInTargetPhenotype(hla1, locus, LocusPosition.Two) &&
                        IsRepresentedInTargetPhenotype(hla2, locus, LocusPosition.One);

                    return representedDirectly || representedInverted;
                }, allowedLoci);
            }).ToList();
        }

        private static LociInfo<ISet<string>> CombineSetsAtLoci(PhenotypeInfo<ISet<string>> phenotypeInfo)
        {
            return phenotypeInfo.ToLociInfo((_, values1, values2)
                => values1 != null && values2 != null ? (ISet<string>) new HashSet<string>(values1.Concat(values2)) : null
            );
        }
    }
}