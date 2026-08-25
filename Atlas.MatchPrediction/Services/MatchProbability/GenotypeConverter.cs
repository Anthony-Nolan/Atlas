using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.GeneticData.Hla.Services.AlleleNameUtils;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HlaConversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenotypeOfKnownTypingCategory = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.PhenotypeInfo<Atlas.MatchPrediction.ExternalInterface.Models.HlaAtKnownTypingCategory>;
// This class holds two PhenotypeInfo<string> that mean different things, one field apart - the typing as submitted,
// and the imputation output named at the resolution the frequency set stores.
using SubmittedPhenotype = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.PhenotypeInfo<string>;
using HfSetGenotypeNames = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.PhenotypeInfo<string>;

namespace Atlas.MatchPrediction.Services.MatchProbability
{
    internal class GenotypeConverterInput
    {
        public SubmittedPhenotype CompressedPhenotype { get; set; }
        public ISet<Locus> AllowedLoci { get; set; }
        public ISet<GenotypeOfKnownTypingCategory> Genotypes { get; set; }
        public IReadOnlyDictionary<HfSetGenotypeNames, decimal> GenotypeLikelihoods { get; set; }
        public string HfSetHlaNomenclatureVersion { get; set; }
        public string MatchingAlgorithmHlaNomenclatureVersion { get; set; }
        public string SubjectLogDescription { get; set; }
    }

    internal interface IGenotypeConverter
    {
        Task<ICollection<GenotypeAtDesiredResolutions>> ConvertGenotypesForMatchCalculation(GenotypeConverterInput input);
    }

    internal class GenotypeConverter : IGenotypeConverter
    {
        private const string StageToLog = "Convert genotypes for match calculation";
        private readonly IAtlasLogger logger;
        private readonly IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory;
        private readonly IHlaCategorisationService categoriser;
        private readonly IHlaToTargetCategoryConverter hlaToTargetCategoryConverter;
        private readonly IGGroupToPGroupConverter gGroupConverter;
        private readonly ISmallGGroupToPGroupConverter smallGGroupConverter;

        public GenotypeConverter(
            // ReSharper disable once SuggestBaseTypeForParameterInConstructor
            IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory,
            IHlaCategorisationService categoriser,
            IHlaToTargetCategoryConverter hlaToTargetCategoryConverter,
            IGGroupToPGroupConverter gGroupConverter,
            ISmallGGroupToPGroupConverter smallGGroupConverter,
            IMatchPredictionLogger<MatchProbabilityLoggingContext> logger)
        {
            this.logger = logger;
            this.categoriser = categoriser;
            this.hlaToTargetCategoryConverter = hlaToTargetCategoryConverter;
            this.hlaMetadataDictionaryFactory = hlaMetadataDictionaryFactory;
            this.gGroupConverter = gGroupConverter;
            this.smallGGroupConverter = smallGGroupConverter;
        }

        public async Task<ICollection<GenotypeAtDesiredResolutions>> ConvertGenotypesForMatchCalculation(GenotypeConverterInput input)
        {
            var hfSetHmd = hlaMetadataDictionaryFactory.BuildDictionary(input.HfSetHlaNomenclatureVersion);
            var matchingHmd = input.MatchingAlgorithmHlaNomenclatureVersion == null
                ? null
                : hlaMetadataDictionaryFactory.BuildDictionary(input.MatchingAlgorithmHlaNomenclatureVersion);

            var nullAlleleInfoByPosition = await input.CompressedPhenotype.MapAsync(async (locus, _, hla) =>
                await GetNullAlleleInfo(hfSetHmd, matchingHmd, input.AllowedLoci, locus, hla));
            var noNullAllelesInCompressedPhenotype = nullAlleleInfoByPosition.AllAtLoci(NoNullAllelesAtLocus);

            using (logger.RunTimed($"{StageToLog}: {input.SubjectLogDescription}", LogLevel.Verbose))
            {
                // Two passes over the genotypes, not one conversion per genotype position.
                //
                // A P group is a pure function of (locus, group name, typing category) once the two HMDs are fixed, and
                // the genotypes are pairs drawn from the survivor pool - so the same triple recurs across genotypes as
                // often as its haplotype does. Converting per position instead means, at the capped shape, 2,000
                // genotypes x 10 typed positions = 20,000 conversions, each allocating its own HlaConverterInput, a
                // single-element array, an interpolated cache key and an async state machine.
                //
                // Pass 1 resolves each DISTINCT triple exactly once. The bound stops being the genotype count and
                // becomes the SURVIVOR count: five loci per survivor rather than ten positions per genotype, and lower
                // again because survivors share names heavily - that sharing is what makes a reduced allowed-loci key
                // collapse the survivor count in the first place. Pass 2 is then pure dictionary reads and allocates no
                // task at all.
                var pGroups = await ResolveDistinctPGroups(
                    input, noNullAllelesInCompressedPhenotype, nullAlleleInfoByPosition, hfSetHmd, matchingHmd);

                // Hoisted out of the loop deliberately: this closes over `pGroups` alone, so one delegate serves every
                // genotype. Written inline as MapByLocus's argument it would be a closure and a delegate per genotype.
                var toPGroupsAtLocus = ToPGroupsAtLocus(pGroups);

                var converted = new List<GenotypeAtDesiredResolutions>(input.Genotypes.Count);

                foreach (var genotype in input.Genotypes)
                {
                    var genotypeToConvert = noNullAllelesInCompressedPhenotype
                        ? genotype
                        : AccountForNullAlleleInCompressedPhenotype(genotype, nullAlleleInfoByPosition);

                    // One ToHlaNames() per genotype, not two: it keys the likelihood here, and it is what
                    // GenotypeAtDesiredResolutions stores as HaplotypeResolution, which is why that type takes the name
                    // form rather than re-deriving it from the same genotype.
                    var haplotypeResolution = genotype.ToHlaNames();

                    converted.Add(new GenotypeAtDesiredResolutions
                    {
                        HaplotypeResolution = haplotypeResolution,
                        StringMatchableResolution = genotypeToConvert.MapByLocus(toPGroupsAtLocus),
                        GenotypeLikelihood = input.GenotypeLikelihoods[haplotypeResolution]
                    });
                }

                return converted;
            }
        }

        /// <summary>
        /// The (locus, name, typing category) triples every kept genotype holds, each converted to its P group exactly
        /// one time.
        /// </summary>
        /// <remarks>
        /// The genotypes are walked twice - here and in the caller's build loop - and the null-allele adjustment is
        /// applied on both passes rather than being carried between them. The two passes are order-independent (this one
        /// only accumulates into a set), and the adjustment is a no-op unless the subject's own submitted typing carries
        /// a null-expressing allele, which is the rare case. Recomputing it there costs one map on those subjects only;
        /// carrying it would cost an array of every genotype on all of them.
        /// </remarks>
        private async Task<Dictionary<PGroupLookupKey, string>> ResolveDistinctPGroups(
            GenotypeConverterInput input,
            bool noNullAllelesInCompressedPhenotype,
            PhenotypeInfo<(bool, IEnumerable<HlaAtKnownTypingCategory>)> nullAlleleInfoByPosition,
            IHlaMetadataDictionary hfSetHmd,
            IHlaMetadataDictionary matchingHmd)
        {
            var distinct = new HashSet<PGroupLookupKey>();

            // Hoisted for the same reason as the caller's mapping delegate.
            Action<Locus, LocusPosition, HlaAtKnownTypingCategory> collect = (locus, _, hla) =>
            {
                if (hla?.Hla != null)
                {
                    distinct.Add(new PGroupLookupKey(locus, hla));
                }
            };

            foreach (var genotype in input.Genotypes)
            {
                var genotypeToConvert = noNullAllelesInCompressedPhenotype
                    ? genotype
                    : AccountForNullAlleleInCompressedPhenotype(genotype, nullAlleleInfoByPosition);

                genotypeToConvert.EachPosition(collect);
            }

            // One input for the whole request rather than one per position. Nothing mutates it after this point - the
            // target category is P group for every triple, whatever category the triple is written in.
            var converterInput = new HlaConverterInput
            {
                HfSetHmd = hfSetHmd,
                MatchingAlgorithmHmd = matchingHmd,
                StageToLog = StageToLog,
                TargetHlaCategory = TargetHlaCategory.PGroup
            };

            // Concurrently, not in a sequential loop: on a cold HMD cache these are table-storage round trips.
            var keys = distinct.ToArray();
            var pGroups = await Task.WhenAll(keys.Select(key => ConvertToPGroup(converterInput, key)));

            var resolved = new Dictionary<PGroupLookupKey, string>(keys.Length);
            for (var i = 0; i < keys.Length; i++)
            {
                resolved[keys[i]] = pGroups[i];
            }

            return resolved;
        }

        private async Task<string> ConvertToPGroup(HlaConverterInput converterInput, PGroupLookupKey key)
        {
            var (locus, hla) = key;

            async Task<string> ConvertHlaToPGroup(IHlaConverter converter) =>
                (await converter.ConvertHlaWithLoggingAndRetryOnFailure(converterInput, locus, hla.Hla)).SingleOrDefault();

            return hla.TypingCategory switch
            {
                HaplotypeTypingCategory.PGroup => hla.Hla,
                HaplotypeTypingCategory.GGroup => await ConvertHlaToPGroup(gGroupConverter),
                HaplotypeTypingCategory.SmallGGroup => await ConvertHlaToPGroup(smallGGroupConverter),
                _ => throw new ArgumentOutOfRangeException(nameof(hla.TypingCategory))
            };
        }

        /// <summary>
        /// Reads both positions' P groups out of <paramref name="pGroups"/> and applies the null-allele rule in the same
        /// step.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The rule is <c>README_MatchPredictionAlgorithm.md</c>'s: a null-expressing allele expresses no protein and so
        /// has no P group, and the position takes <i>"the P group of its paired allele … in keeping with the logic used
        /// in the matching algorithm"</i>. A locus with no P group at either position stays absent at both and is
        /// treated as untyped by the match calculator.
        /// </para>
        /// <para>
        /// Applied here rather than in a second pass building a second <see cref="PhenotypeInfo{T}"/>, because both
        /// positions are already in hand: <c>position1 ?? position2</c> is the paired allele's P group where this
        /// position has none, and both being absent leaves both absent.
        /// </para>
        /// </remarks>
        private static Func<Locus, LocusInfo<HlaAtKnownTypingCategory>, LocusInfo<string>> ToPGroupsAtLocus(
            Dictionary<PGroupLookupKey, string> pGroups)
        {
            return (locus, locusHla) =>
            {
                var position1 = PGroupOrAbsent(locusHla.Position1);
                var position2 = PGroupOrAbsent(locusHla.Position2);

                return new LocusInfo<string>(position1 ?? position2, position2 ?? position1);

                string PGroupOrAbsent(HlaAtKnownTypingCategory hla) =>
                    hla?.Hla == null ? null : pGroups[new PGroupLookupKey(locus, hla)];
            };
        }

        /// <summary>
        /// What a P group conversion depends on, and nothing else.
        /// </summary>
        /// <remarks>
        /// Keyed on the whole <see cref="HlaAtKnownTypingCategory"/> rather than on its two fields, which is the safe
        /// direction: it already has value equality over (name, category), and a field added to it later can only
        /// over-partition this cache - i.e. cost hits - where picking the fields out by hand could silently share one
        /// answer between two triples that no longer convert alike.
        /// </remarks>
        private readonly record struct PGroupLookupKey(Locus Locus, HlaAtKnownTypingCategory Hla);

        private async Task<(bool isNullAllele, IEnumerable<HlaAtKnownTypingCategory> nullAlleleGGroups)> GetNullAlleleInfo(
            IHlaMetadataDictionary hfSetHmd,
            IHlaMetadataDictionary matchingHmd,
            ICollection<Locus> allowedLoci,
            Locus locus,
            string hla)
        {
            const string nullAlleleStageName = "Handle null allele in compressed phenotype";
            var converterInput = new HlaConverterInput
            {
                HfSetHmd = hfSetHmd,
                MatchingAlgorithmHmd = matchingHmd,
                StageToLog = nullAlleleStageName
            };

            // TODO #1091 - stop stripping the molecular prefix once #1091 is done
            if (!allowedLoci.Contains(locus) || hla == null || !categoriser.IsNullAllele(AlleleSplitter.RemovePrefix(hla)))
            {
                return (false, new List<HlaAtKnownTypingCategory>());
            }

            async Task<HlaAtKnownTypingCategory> ConvertHla(HaplotypeTypingCategory category)
            {
                converterInput.TargetHlaCategory = category.ToHlaTypingCategory().ToTargetHlaCategory();
                var convertedHla = (await hlaToTargetCategoryConverter.ConvertHlaWithLoggingAndRetryOnFailure(converterInput, locus, hla)).Single();
                return new HlaAtKnownTypingCategory(convertedHla, category);
            }

            var smallGGroup = await ConvertHla(HaplotypeTypingCategory.SmallGGroup);
            var gGroup = await ConvertHla(HaplotypeTypingCategory.GGroup);

            return (true, new[] { smallGGroup, gGroup });
        }

        /// <summary>
        /// Will convert genotype locus to homozygous wherever there is a null allele in the compressed phenotype.
        /// </summary>
        /// <param name="genotype"></param>
        /// <param name="nullAlleleInfoByPosition"></param>
        /// <returns></returns>
        private static GenotypeOfKnownTypingCategory AccountForNullAlleleInCompressedPhenotype(
            GenotypeOfKnownTypingCategory genotype,
            PhenotypeInfo<(bool, IEnumerable<HlaAtKnownTypingCategory> nullAlleleGGroups)> nullAlleleInfoByPosition)
        {
            return genotype.MapByLocus((locus, genotypeLocusHla) =>
            {
                var locusNullAlleleInfo = nullAlleleInfoByPosition.GetLocus(locus);

                if (NoNullAllelesAtLocus(locusNullAlleleInfo))
                {
                    return genotypeLocusHla;
                }

                bool IsGGroupOfNullAllele(HlaAtKnownTypingCategory genotypeHla)
                {
                    return locusNullAlleleInfo.EitherPosition(v => v.nullAlleleGGroups.Contains(genotypeHla));
                }

                var pos1IsGGroupOfNullAllele = IsGGroupOfNullAllele(genotypeLocusHla.Position1);
                var pos2IsGGroupOfNullAllele = IsGGroupOfNullAllele(genotypeLocusHla.Position2);

                if (pos1IsGGroupOfNullAllele ^ pos2IsGGroupOfNullAllele)
                { 
                    return new LocusInfo<HlaAtKnownTypingCategory>(
                        pos1IsGGroupOfNullAllele ? genotypeLocusHla.Position2 : genotypeLocusHla.Position1,
                        pos2IsGGroupOfNullAllele ? genotypeLocusHla.Position1 : genotypeLocusHla.Position2);
                }

                return genotypeLocusHla;
            });
        }

        private static bool NoNullAllelesAtLocus(LocusInfo<(bool isNullAllele, IEnumerable<HlaAtKnownTypingCategory>)> locusInfo) =>
            locusInfo.BothPositions(v => !v.isNullAllele);
    }
}