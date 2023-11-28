using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Maths;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;

// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable ParameterTypeCanBeEnumerable.Local

namespace Atlas.MatchPrediction.Services.CompressedPhenotypeExpansion
{
    internal class CompressedPhenotypeExpanderInput
    {
        /// <summary>
        /// Given phenotype. Can be of any supported HLA resolution.
        /// </summary>
        public PhenotypeInfo<string> Phenotype { get; set; }

        /// <summary>
        /// Haplotype Frequency Set Id - used to fetch haplotypes, if needed
        /// </summary>
        public int HfSetId { get; set; }

        /// <summary>
        /// HLA nomenclature version of Haplotype Frequency Set
        /// </summary>
        public string HfSetHlaNomenclatureVersion { get; set; }

        /// <inheritdoc cref="Models.MatchPredictionParameters" />
        public MatchPredictionParameters MatchPredictionParameters { get; set; }
    }

    internal interface ICompressedPhenotypeExpander
    {
        /// <summary>
        /// Expands an ambiguous phenotype to GGroup resolution, then transforms into all possible permutations of the given hla representations.
        /// Does not consider phase - so the results cannot necessarily be considered Diplotypes.
        /// </summary>
        public Task<ISet<PhenotypeInfo<HlaAtKnownTypingCategory>>> ExpandCompressedPhenotype(CompressedPhenotypeExpanderInput input);
    }

    internal class CompressedPhenotypeExpander : ICompressedPhenotypeExpander
    {
        private readonly ILogger logger;
        private readonly ICompressedPhenotypeConverter converter;
        private readonly IHaplotypeFrequencyService haplotypeFrequencyService;

        public CompressedPhenotypeExpander(
            // ReSharper disable once SuggestBaseTypeForParameterInConstructor
            IMatchPredictionLogger<MatchProbabilityLoggingContext> logger,
            ICompressedPhenotypeConverter converter,
            IHaplotypeFrequencyService haplotypeFrequencyService)
        {
            this.logger = logger;
            this.converter = converter;
            this.haplotypeFrequencyService = haplotypeFrequencyService;
        }

        public async Task<ISet<PhenotypeInfo<HlaAtKnownTypingCategory>>> ExpandCompressedPhenotype(CompressedPhenotypeExpanderInput input)
        {
            var allowedLoci = input.MatchPredictionParameters.AllowedLoci;
            var groupsPerPosition = await converter.ConvertPhenotype(input);

            if (IsUnambiguousAtAllowedLoci(allowedLoci, groupsPerPosition))
            {
                return BuildSingleSmallGGenotype(groupsPerPosition);
            }

            var haplotypesForAllowedLoci = await GetHaplotypesForAllowedLoci(input.HfSetId, allowedLoci, groupsPerPosition);
            var filteredDiplotypes = GetFilteredDiplotypes(haplotypesForAllowedLoci, groupsPerPosition, allowedLoci);

            logger.SendTrace($"Filtered expanded genotypes: {filteredDiplotypes.Count}");
            return filteredDiplotypes.Select(dp => new PhenotypeInfo<HlaAtKnownTypingCategory>(dp.Item1, dp.Item2)).ToHashSet();
        }

        private static ISet<PhenotypeInfo<HlaAtKnownTypingCategory>> BuildSingleSmallGGenotype(DataByResolution<PhenotypeInfo<ISet<string>>> groupsPerPosition)
        {
            return new HashSet<PhenotypeInfo<HlaAtKnownTypingCategory>>
            {
                groupsPerPosition.SmallGGroup.Map((_, __, v) =>
                    v == null ? null : new HlaAtKnownTypingCategory(v.Single(), HaplotypeTypingCategory.SmallGGroup))
            };
        }

        private static bool IsUnambiguousAtAllowedLoci(
            ISet<Locus> allowedLoci, 
            DataByResolution<PhenotypeInfo<ISet<string>>> groupsPerPosition)
        {
            return allowedLoci.All(l =>
            {
                var groupsAtLocus = groupsPerPosition.SmallGGroup.GetLocus(l);
                return groupsAtLocus.Position1?.Count == 1 && groupsAtLocus.Position2?.Count == 1;
            });
        }

        private async Task<IEnumerable<LociInfo<HlaAtKnownTypingCategory>>> GetHaplotypesForAllowedLoci(
            int frequencySetId,
            ISet<Locus> allowedLoci,
            DataByResolution<PhenotypeInfo<ISet<string>>> groupsPerPosition)
        {
            var allHaplotypes = await FetchHaplotypesGroupedByTypingCategory(frequencySetId);

            if (allHaplotypes?.GGroup == null || allHaplotypes.PGroup == null || allHaplotypes.SmallGGroup == null)
            {
                throw new Exception( $"No haplotypes could be found for set id {frequencySetId}.");
            }

            var groupsPerLocus = groupsPerPosition.Map(CombineSetsAtLoci);

            var haplotypesFilteredBySubjectHla = allHaplotypes.Map((category, haplotypes) =>
            {
                return haplotypes.Where(haplotype => allowedLoci.All(locus =>
                    {
                        var hlaGroups = groupsPerLocus.GetByCategory(category).GetLocus(locus);
                        return hlaGroups == null || hlaGroups.Contains(haplotype.GetLocus(locus));
                    }))
                    .Select(haplotype => haplotype.Map(hla => new HlaAtKnownTypingCategory(hla, category)))
                    .ToList();
            });

            var mergedHaplotypes = haplotypesFilteredBySubjectHla.GGroup
                .Concat(haplotypesFilteredBySubjectHla.PGroup)
                .Concat(haplotypesFilteredBySubjectHla.SmallGGroup);

            return new HashSet<LociInfo<HlaAtKnownTypingCategory>>(mergedHaplotypes.Select(h =>
                h.Map((l, hla) => allowedLoci.Contains(l) ? hla : null)));
        }

        private async Task<DataByResolution<IReadOnlyCollection<LociInfo<string>>>> FetchHaplotypesGroupedByTypingCategory(int frequencySetId)
        {
            var haplotypeFrequencies = await haplotypeFrequencyService.GetAllHaplotypeFrequencies(frequencySetId);

            var groupedFrequencies = haplotypeFrequencies
                .GroupBy(f => f.Value.TypingCategory)
                .Select(g =>
                    new KeyValuePair<HaplotypeTypingCategory, IReadOnlyCollection<LociInfo<string>>>(g.Key, g.Select(f => f.Value.Hla).ToList())
                )
                .ToDictionary();

            return new DataByResolution<IReadOnlyCollection<LociInfo<string>>>
            {
                GGroup = groupedFrequencies.GetValueOrDefault(HaplotypeTypingCategory.GGroup, new List<LociInfo<string>>()),
                PGroup = groupedFrequencies.GetValueOrDefault(HaplotypeTypingCategory.PGroup, new List<LociInfo<string>>()),
                SmallGGroup = groupedFrequencies.GetValueOrDefault(HaplotypeTypingCategory.SmallGGroup, new List<LociInfo<string>>()),
            };
        }

        /// <summary>
        /// Filters a collection of haplotypes down to only those which are possible for an input phenotype, typed to G-Group resolution
        /// </summary>
        /// <param name="haplotypes">Source of haplotypes to filter.</param>
        /// <param name="groupsPerPosition">GGroups and PGroups present in the phenotype being expanded.</param>
        /// <param name="allowedLoci">List of loci that are being considered.</param>
        /// <returns>Set of diplotypes (pairs of haplotypes) which are possible for an input phenotype</returns>
        private static List<Tuple<LociInfo<HlaAtKnownTypingCategory>, LociInfo<HlaAtKnownTypingCategory>>> GetFilteredDiplotypes(
            IEnumerable<LociInfo<HlaAtKnownTypingCategory>> haplotypes,
            DataByResolution<PhenotypeInfo<ISet<string>>> groupsPerPosition,
            ISet<Locus> allowedLoci)
        {
            var diplotypes = Combinations.AllPairs(haplotypes, true).ToList();

            bool IsRepresentedInTargetPhenotype(HlaAtKnownTypingCategory hla, Locus locus, LocusPosition position)
            {
                var groups = groupsPerPosition.GetByCategory(hla.TypingCategory).GetPosition(locus, position);
                return groups == null || groups.Contains(hla.Hla);
            }

            // ReSharper disable once UseDeconstructionOnParameter
            return diplotypes.Where(diplotype =>
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
                => values1 != null && values2 != null ? (ISet<string>)new HashSet<string>(values1.Concat(values2)) : null
            );
        }
    }
}