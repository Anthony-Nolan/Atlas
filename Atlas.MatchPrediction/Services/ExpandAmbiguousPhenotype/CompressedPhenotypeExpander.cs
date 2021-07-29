using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Maths;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models;

// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable ParameterTypeCanBeEnumerable.Local

namespace Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype
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

        public CompressedPhenotypeExpander(
            // ReSharper disable once SuggestBaseTypeForParameter
            IMatchPredictionLogger logger,
            IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory)
        {
            this.logger = logger;
            this.hlaMetadataDictionaryFactory = hlaMetadataDictionaryFactory;
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
                await ConvertAllHla(hlaMetadataDictionary, input.Phenotype, category.ToHlaTypingCategory().ToTargetHlaCategory(), allowedLoci)
            );

            var groupsPerLocus = groupsPerPosition.Map(CombineSetsAtLoci);

            var allowedHaplotypes = GetAllowedHaplotypes(allowedLoci, input.AllHaplotypes, groupsPerLocus);

            var allowedHaplotypesExcludingLoci = new HashSet<LociInfo<HlaAtKnownTypingCategory>>(allowedHaplotypes.Select(h =>
                h.Map((l, hla) => allowedLoci.Contains(l) ? hla : null)
            ));

            var allowedDiplotypes = Combinations.AllPairs(allowedHaplotypesExcludingLoci, true).ToList();
            var filteredDiplotypes = FilterDiplotypes(allowedDiplotypes, groupsPerPosition, allowedLoci);

            logger.SendTrace($"Filtered expanded genotypes: {filteredDiplotypes.Count}");
            return filteredDiplotypes.Select(dp => new PhenotypeInfo<HlaAtKnownTypingCategory>(dp.Item1, dp.Item2)).ToHashSet();
        }

        /// <summary>
        /// Runs <see cref="IHlaMetadataDictionary.ConvertHla"/> for each HLA in a PhenotypeInfo, at selected loci.
        /// Excluded loci will not be converted, and will be set to `null`.
        /// Provided `null`s will be preserved.
        /// Any HLA that cannot be converted, e.g., an allele could not be found in the HMD due to being renamed in a later HLA version,
        /// will be assigned a default placeholder value to avoid issues in subsequent haplotype/diplotype filtering steps, as `null` has a special meaning.
        /// </summary>
        private async Task<PhenotypeInfo<IReadOnlyCollection<string>>> ConvertAllHla(
            IHlaMetadataDictionary hlaMetadataDictionary,
            PhenotypeInfo<string> hlaInfo,
            TargetHlaCategory targetHlaCategory,
            ISet<Locus> allowedLoci
        )
        {
            // placeholder text should not conform to any possible HLA naming pattern
            const string hlaConversionFailurePlaceholderText = "unconverted-HLA-typing";

            return await hlaInfo.MapAsync(async (locus, _, hla) =>
            {
                if (!allowedLoci.Contains(locus) || hla == null)
                {
                    return null;
                }

                try
                {
                    return await hlaMetadataDictionary.ConvertHla(locus, hla, targetHlaCategory);
                }
                // All HMD exceptions are being caught and suppressed here, under the assumption that the subject's HLA has already been
                // validated by the matching algorithm component, and the only reason the typing is missing
                // from the HMD is due to the matching algorithm and HF set being on different nomenclature versions.
                // See https://github.com/Anthony-Nolan/Atlas/issues/636 for more info.
                // Note: if the MPA endpoint is ever added to the Public API to allow it to be run independently of matching,
                // then the above assumption no longer stands; the possibility of invalid HLA being submitted to the MPA directly must be handled.
                catch (HlaMetadataDictionaryException exception)
                {
                    logger.SendEvent(new HlaConversionFailureEventModel(
                        locus,
                        hla,
                        hlaMetadataDictionary.ActiveHlaNomenclatureVersion,
                        targetHlaCategory, 
                        "Expansion of phenotype to HLA category of the HF set.",
                        exception));

                    //TODO issue #637 - re-attempt HLA conversion using other approaches

                    return new List<string> {hlaConversionFailurePlaceholderText};
                }
            });
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
            DataByResolution<PhenotypeInfo<IReadOnlyCollection<string>>> groupsPerPosition,
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

        private static LociInfo<ISet<string>> CombineSetsAtLoci(PhenotypeInfo<IReadOnlyCollection<string>> phenotypeInfo)
        {
            return phenotypeInfo.ToLociInfo((_, values1, values2)
                => values1 != null && values2 != null ? (ISet<string>)new HashSet<string>(values1.Concat(values2)) : null
            );
        }
    }
}