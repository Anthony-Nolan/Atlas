using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Matching.Services;
using static Atlas.Common.Maths.Combinations;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchCalculation;
using Atlas.MatchPrediction.Utils;
using Atlas.MatchPrediction.Validators;
using FluentValidation;
using LoggingStopwatch;
using HaplotypeFrequencySet = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet.HaplotypeFrequencySet;
using TypedGenotype = Atlas.Common.GeneticData.PhenotypeInfo.PhenotypeInfo<Atlas.MatchPrediction.ExternalInterface.Models.HlaAtKnownTypingCategory>;
using StringGenotype = Atlas.Common.GeneticData.PhenotypeInfo.PhenotypeInfo<string>;

// ReSharper disable ParameterTypeCanBeEnumerable.Local
// ReSharper disable SuggestBaseTypeForParameter

namespace Atlas.MatchPrediction.Services.MatchProbability
{
    public interface IMatchProbabilityService
    {
        public Task<MatchProbabilityResponse> CalculateMatchProbability(SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput);
    }

    internal class Buckets<T>
    {
        public ISet<T> Twos { get; } = new HashSet<T>();
        public ISet<T> Ones { get; } = new HashSet<T>();
        public ISet<T> Zeros { get; } = new HashSet<T>();

        public void AddToBucket(T hlaPair, int matchCount)
        {
            switch (matchCount)
            {
                case 0:
                    Zeros.Add(hlaPair);
                    return;
                case 1:
                    Ones.Add(hlaPair);
                    return;
                case 2:
                    Twos.Add(hlaPair);
                    return;
                default:
                    throw new ArgumentException("match count per locus can only be 0, 1, or 2", nameof(matchCount));
            }
        }
        public void AddToBucket(HashSet<T> hlaPairs, int matchCount)
        {
            switch (matchCount)
            {
                case 0:
                    Zeros.UnionWith(hlaPairs);
                    return;
                case 1:
                    Ones.UnionWith(hlaPairs);
                    return;
                case 2:
                    Twos.UnionWith(hlaPairs);
                    return;
                default:
                    throw new ArgumentException("match count per locus can only be 0, 1, or 2", nameof(matchCount));
            }
        }

        public ISet<T> GetBucket(int matchCount)
        {
            return matchCount switch
            {
                0 => Zeros,
                1 => Ones,
                2 => Twos,
                _ => throw new ArgumentException("match count per locus can only be 0, 1, or 2", nameof(matchCount))
            };
        }
    }

    internal class GenotypeAtDesiredResolutions
    {
        // Dictionary - quick way to check both (a) existence (b) quick lookup of index (vs. index of)
        // Add to it in order so .Keys gives same indexes as values!
        public static readonly Dictionary<LocusInfo<string>, int> LocusIndexes = new Dictionary<LocusInfo<string>, int>();

        public static readonly LociInfo<Dictionary<LocusInfo<string>, int>> PerLocusPairIndexes =
            new LociInfo<Dictionary<LocusInfo<string>, int>>(_ => new Dictionary<LocusInfo<string>, int>());

        /// <summary>
        /// HLA at the resolution at which they were stored.
        /// i.e. G group, or P group if any null alleles are present in the haplotype.
        /// </summary>
        public StringGenotype HaplotypeResolution { get; }

        /// <summary>
        /// HLA at a resolution at which it is possible to calculate match counts using string comparison only, no expansion.
        /// i.e. P group, or G group for null expressing alleles. 
        /// </summary>
        public StringGenotype StringMatchableResolution { get; }

        /// <summary>
        /// Each value = integer representation of an allele pair at a locus
        /// </summary>
        public LociInfo<int?> IndexResolution { get; }

        /// <summary>
        /// Likelihood of this genotype.
        ///
        /// Stored with the genotype to avoid dictionary lookups when calculating final likelihoods, as looking up the same genotype multiple times
        /// for different patient/donor pairs is inefficient 
        /// </summary>
        public decimal GenotypeLikelihood { get; }

        private GenotypeAtDesiredResolutions(TypedGenotype haplotypeResolution, StringGenotype stringMatchableResolution, decimal genotypeLikelihood)
        {
            HaplotypeResolution = haplotypeResolution.ToHlaNames();
            StringMatchableResolution = stringMatchableResolution;

            IndexResolution =
                stringMatchableResolution.Map((l, hla) => hla == null
                    ? null as int?
                    : LocusIndexes.GetOrAdd(hla, () =>
                    {
                        PerLocusPairIndexes.GetLocus(l).GetOrAdd(hla, () => LocusIndexes.Count);
                        return LocusIndexes.Count;
                    }));

            GenotypeLikelihood = genotypeLikelihood;
        }

        public static async Task<GenotypeAtDesiredResolutions> FromHaplotypeResolutions(
            TypedGenotype haplotypeResolutions,
            IHlaMetadataDictionary hlaMetadataDictionary,
            decimal genotypeLikelihood)
        {
            var stringMatchableResolutions = (await haplotypeResolutions.MapAsync(async (locus, _, hla) =>
            {
                if (hla?.Hla == null)
                {
                    return null;
                }

                return hla.TypingCategory switch
                {
                    HaplotypeTypingCategory.GGroup => await hlaMetadataDictionary.ConvertGGroupToPGroup(locus, hla.Hla),
                    HaplotypeTypingCategory.PGroup => hla.Hla,
                    _ => throw new ArgumentOutOfRangeException(nameof(hla.TypingCategory))
                };
            })).CopyExpressingAllelesToNullPositions();

            return new GenotypeAtDesiredResolutions(haplotypeResolutions, stringMatchableResolutions, genotypeLikelihood);
        }
    }

    internal class MatchProbabilityService : IMatchProbabilityService
    {
        private const string LoggingPrefix = "MatchPrediction: ";

        private readonly ICompressedPhenotypeExpander compressedPhenotypeExpander;
        private readonly IGenotypeLikelihoodService genotypeLikelihoodService;
        private readonly IMatchCalculationService matchCalculationService;
        private readonly IMatchProbabilityCalculator matchProbabilityCalculator;
        private readonly IHaplotypeFrequencyService haplotypeFrequencyService;
        private readonly ILogger logger;
        private readonly MatchPredictionLoggingContext matchPredictionLoggingContext;
        private readonly IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory;
        private readonly IStringBasedLocusMatchCalculator stringBasedLocusMatchCalculator;

        public MatchProbabilityService(
            ICompressedPhenotypeExpander compressedPhenotypeExpander,
            IGenotypeLikelihoodService genotypeLikelihoodService,
            IMatchCalculationService matchCalculationService,
            IMatchProbabilityCalculator matchProbabilityCalculator,
            IHaplotypeFrequencyService haplotypeFrequencyService,
            // ReSharper disable once SuggestBaseTypeForParameter
            IMatchPredictionLogger logger,
            MatchPredictionLoggingContext matchPredictionLoggingContext,
            IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory,
            IStringBasedLocusMatchCalculator stringBasedLocusMatchCalculator)
        {
            this.compressedPhenotypeExpander = compressedPhenotypeExpander;
            this.genotypeLikelihoodService = genotypeLikelihoodService;
            this.matchCalculationService = matchCalculationService;
            this.matchProbabilityCalculator = matchProbabilityCalculator;
            this.haplotypeFrequencyService = haplotypeFrequencyService;
            this.logger = logger;
            this.matchPredictionLoggingContext = matchPredictionLoggingContext;
            this.hlaMetadataDictionaryFactory = hlaMetadataDictionaryFactory;
            this.stringBasedLocusMatchCalculator = stringBasedLocusMatchCalculator;
        }

        private static List<LociInfo<int?>> GetMatchCountsOfEightOrBetter()
        {
            var counts = new List<LociInfo<int?>>();
            for (var a = 0; a <= 2; a++)
            {
                for (var b = 0; b <= 2; b++)
                {
                    for (var c = 0; c <= 2; c++)
                    {
                        for (var dqb1 = 0; dqb1 <= 2; dqb1++)
                        {
                            for (var drb1 = 0; drb1 <= 2; drb1++)
                            {
                                if (a + b + c + dqb1 + drb1 >= 8)
                                {
                                    counts.Add(new LociInfo<int?>(a, b, c, null, dqb1, drb1));
                                }
                            }
                        }
                    }
                }
            }

            return counts;
        }

        public async Task<MatchProbabilityResponse> CalculateMatchProbability(SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput)
        {
            await new MatchProbabilityInputValidator().ValidateAndThrowAsync(singleDonorMatchProbabilityInput);

            matchPredictionLoggingContext.Initialise(singleDonorMatchProbabilityInput);

            var allowedLoci = LocusSettings.MatchPredictionLoci.Except(singleDonorMatchProbabilityInput.ExcludedLoci).ToHashSet();
            var hlaNomenclatureVersion = singleDonorMatchProbabilityInput.HlaNomenclatureVersion;

            var frequencySets = await haplotypeFrequencyService.GetHaplotypeFrequencySets(
                singleDonorMatchProbabilityInput.DonorInput.DonorFrequencySetMetadata,
                singleDonorMatchProbabilityInput.PatientFrequencySetMetadata
            );

            var patientGenotypes = await ExpandToGenotypes(
                singleDonorMatchProbabilityInput.PatientHla.ToPhenotypeInfo(),
                frequencySets.PatientSet.Id,
                allowedLoci,
                hlaNomenclatureVersion,
                "patient"
            );

            var donorGenotypes = await ExpandToGenotypes(
                singleDonorMatchProbabilityInput.DonorInput.DonorHla.ToPhenotypeInfo(),
                frequencySets.DonorSet.Id,
                allowedLoci,
                hlaNomenclatureVersion,
                "donor"
            );

            if (donorGenotypes.IsNullOrEmpty() || patientGenotypes.IsNullOrEmpty())
            {
                if (donorGenotypes.IsNullOrEmpty())
                {
                    logger.SendTrace($"{LoggingPrefix}Donor genotype unrepresented.", LogLevel.Verbose);
                }

                if (patientGenotypes.IsNullOrEmpty())
                {
                    logger.SendTrace($"{LoggingPrefix}Patient genotype unrepresented.", LogLevel.Verbose);
                }

                return new MatchProbabilityResponse(null, allowedLoci)
                {
                    IsDonorPhenotypeUnrepresented = donorGenotypes.IsNullOrEmpty(),
                    IsPatientPhenotypeUnrepresented = patientGenotypes.IsNullOrEmpty()
                };
            }

            if (NumberOfPairsOfCartesianProduct(patientGenotypes, donorGenotypes) > 40_000_000)
            {
                logger.SendTrace(
                    $"{LoggingPrefix} Calculating the MatchCounts of provided donor patient pairs is expected to take upwards of 1 minute." +
                    $"[{donorGenotypes.Count * patientGenotypes.Count} pairs to calculate.]"
                    , LogLevel.Warn);
            }

            var hlaMetadataDictionary = hlaMetadataDictionaryFactory.BuildDictionary(hlaNomenclatureVersion);

            var patientStringGenotypes = patientGenotypes.Select(g => g.ToHlaNames()).ToHashSet();
            var donorStringGenotypes = donorGenotypes.Select(g => g.ToHlaNames()).ToHashSet();


            // TODO: ATLAS-233: Re-introduce hardcoded 100% probability for guaranteed match but no represented genotypes
            var patientGenotypeLikelihoods = await CalculateGenotypeLikelihoods(patientStringGenotypes, frequencySets.PatientSet, allowedLoci);
            var donorGenotypeLikelihoods = await CalculateGenotypeLikelihoods(donorStringGenotypes, frequencySets.DonorSet, allowedLoci);

            async Task<List<GenotypeAtDesiredResolutions>> ConvertGenotypes(
                ISet<TypedGenotype> genotypes,
                string subjectLogDescription,
                IReadOnlyDictionary<PhenotypeInfo<string>, decimal> genotypeLikelihoods)
            {
                using (logger.RunTimed($"Convert genotypes for matching: {subjectLogDescription}", LogLevel.Verbose))
                {
                    return (await Task.WhenAll(genotypes.Select(async g => await GenotypeAtDesiredResolutions.FromHaplotypeResolutions(
                        g,
                        hlaMetadataDictionary,
                        genotypeLikelihoods[g.ToHlaNames()]
                    )))).ToList();
                }
            }

            var convertedPatientGenotypes = await ConvertGenotypes(patientGenotypes, "patient", patientGenotypeLikelihoods);
            var convertedDonorGenotypes = await ConvertGenotypes(donorGenotypes, "donor", donorGenotypeLikelihoods);
            var allPatientDonorCombinations = CombineGenotypes(convertedPatientGenotypes, convertedDonorGenotypes);

            var matchCounts = GenotypeAtDesiredResolutions.LocusIndexes.Keys.Select(locusHlaOuter =>
                GenotypeAtDesiredResolutions.LocusIndexes.Keys
                    .Select(locusHlaInner => stringBasedLocusMatchCalculator.MatchCount(locusHlaOuter, locusHlaInner)).ToList()
            ).ToList();

            var flatMatchCounts = matchCounts.SelectMany(x => x).ToArray();


            var loggerBuckets = logger.RunTimed("Calculate buckets");
            var buckets = GenotypeAtDesiredResolutions.PerLocusPairIndexes.Map((l, hla) =>
            {
                var perLocusHashTable = new Dictionary<LocusInfo<string>, Buckets<LocusInfo<string>>>();

                foreach (var hlaPair in hla.Keys)
                {
                    var buckets = new Buckets<LocusInfo<string>>();

                    var others = GenotypeAtDesiredResolutions.PerLocusPairIndexes.GetLocus(l);
                    // TODO: This double counts, can improve 2x with cleverness
                    foreach (var other in others.Keys)
                    {
                        var matchCount = stringBasedLocusMatchCalculator.MatchCount(hlaPair, other);
                        buckets.AddToBucket(other, matchCount);
                    }

                    perLocusHashTable[hlaPair] = buckets;
                }

                return perLocusHashTable;
            });
            loggerBuckets.Dispose();

            var loggerMatchCounts = logger.RunTimed("Calculate allowed match counts");
            var allowedMatchCounts = GetMatchCountsOfEightOrBetter();
            loggerMatchCounts.Dispose();

            
            var loggerBuckets2 = logger.RunTimed("Calculate donor buckets");

            var loggerDonorBuckets = logger.RunLongOperationWithTimer("creating donor buckets", new LongLoggingSettings
            {
                ExpectedNumberOfIterations = buckets.ToEnumerable().Aggregate(0, (x, y) => x + y.Count),
                InnerOperationLoggingPeriod = 1000
            });
            var donorBuckets = buckets.Map((l, bucketDict) =>
            {
                // This is probably double counting pairs and can be more efficient
                return bucketDict.ToDictionary(k => k.Key, kvp =>
                {
                    using (loggerDonorBuckets.TimeInnerOperation())
                    {
                        var singleDonorBuckets = new Buckets<GenotypeAtDesiredResolutions>();
                        var twos = convertedDonorGenotypes.Where(dg => kvp.Value.Twos.Contains(dg.StringMatchableResolution.GetLocus(l))).ToHashSet();
                        var ones = convertedDonorGenotypes.Where(dg => kvp.Value.Ones.Contains(dg.StringMatchableResolution.GetLocus(l))).ToHashSet();
                        var zeros = convertedDonorGenotypes.Where(dg => kvp.Value.Zeros.Contains(dg.StringMatchableResolution.GetLocus(l))).ToHashSet();
                        singleDonorBuckets.AddToBucket(twos, 2);
                        singleDonorBuckets.AddToBucket(ones, 1);
                        singleDonorBuckets.AddToBucket(zeros, 0);
                        return singleDonorBuckets;
                    }
                });
            });
            loggerBuckets2.Dispose();
            loggerDonorBuckets.Dispose();


            static HashSet<StringGenotype> CombineToGenotypes(LociInfo<ISet<LocusInfo<string>>> options)
            {
                var genotypes = new HashSet<StringGenotype>();
                
                foreach (var a in options.A)
                {
                    foreach (var b in options.B)
                    {
                        foreach (var c in options.C)
                        {
                            foreach (var dqb1 in options.Dqb1)
                            {
                                foreach (var drb1 in options.Drb1)
                                {
                                    genotypes.Add(new StringGenotype(a, b, c, null, dqb1, drb1));
                                }
                            }
                        }
                    }
                }

                return genotypes;
            }

            HashSet<GenotypeAtDesiredResolutions> GetAllMatchingGenotypes(StringGenotype genotype)
            {
                var allGenotypes = new HashSet<GenotypeAtDesiredResolutions>();
                
                foreach (var matchCountSet in allowedMatchCounts)
                {
                    var bucketsToCombine = matchCountSet.Map((l, mc) =>
                        mc == null
                            ? null
                            : donorBuckets.GetLocus(l)[genotype.GetLocus(l)].GetBucket(mc.Value));

                    var allDonorsToCombine = bucketsToCombine.ToEnumerable().SelectMany(x => x ?? new HashSet<GenotypeAtDesiredResolutions>()).ToHashSet();
                    allGenotypes.UnionWith(allDonorsToCombine);
                }

                return allGenotypes;
            }

            IEnumerable<(GenotypeAtDesiredResolutions, GenotypeAtDesiredResolutions)> GetAllMatchingPairs()
            {
                var totalMatches = 0;
                using (var longLog = logger.RunLongOperationWithTimer("get all matching pairs", new LongLoggingSettings
                {
                    ExpectedNumberOfIterations = convertedPatientGenotypes.Count,
                    InnerOperationLoggingPeriod = 250
                }))
                {
                    foreach (var patientGenotype in convertedPatientGenotypes)
                    {
                        using (longLog.TimeInnerOperation())
                        {
                            var matchingDonorGenotypes = GetAllMatchingGenotypes(patientGenotype.StringMatchableResolution);
                            totalMatches += matchingDonorGenotypes.Count;
                            Console.WriteLine($"Cumulative match pairs: {totalMatches}");
                            foreach (var matchingDonorGenotype in matchingDonorGenotypes)
                            {
                                yield return (patientGenotype, matchingDonorGenotype);
                            }
                        }
                    }
                }
            }

            var loggerGenotypePairs = logger.RunTimed("Calculate match pairs");
            var pairs = GetAllMatchingPairs().ToList();
            loggerGenotypePairs.Dispose();

            using (var matchCountLogger = MatchCountLogger(NumberOfPairsOfCartesianProduct(convertedDonorGenotypes, convertedPatientGenotypes)))
            {
                var patientDonorMatchDetails = CalculatePairsMatchCounts(allPatientDonorCombinations, allowedLoci, matchCountLogger, matchCounts,
                    flatMatchCounts, matchCounts.Count);

                // Sum likelihoods outside of loop, so they are not calculated millions of times
                var sumOfPatientLikelihoods = patientGenotypeLikelihoods.Values.SumDecimals();
                var sumOfDonorLikelihoods = donorGenotypeLikelihoods.Values.SumDecimals();

                using (logger.RunTimed("Calculate match probability", LogLevel.Verbose))
                {
                    var matchProbability = matchProbabilityCalculator.CalculateMatchProbability(
                        sumOfPatientLikelihoods,
                        sumOfDonorLikelihoods,
                        patientDonorMatchDetails,
                        allowedLoci
                    );

                    matchProbability.DonorFrequencySetNomenclatureVersion = frequencySets.DonorSet.HlaNomenclatureVersion;
                    matchProbability.PatientFrequencySetNomenclatureVersion = frequencySets.PatientSet.HlaNomenclatureVersion;

                    return matchProbability;
                }
            }
        }

        private async Task<ISet<TypedGenotype>> ExpandToGenotypes(
            StringGenotype phenotype,
            int frequencySetId,
            ISet<Locus> allowedLoci,
            string hlaNomenclatureVersion,
            string subjectLogDescription = null)
        {
            var haplotypeFrequencies = await haplotypeFrequencyService.GetAllHaplotypeFrequencies(frequencySetId);
            using (logger.RunTimed($"{LoggingPrefix}Expand {subjectLogDescription} phenotype", LogLevel.Verbose))
            {
                return await compressedPhenotypeExpander.ExpandCompressedPhenotype(
                    new ExpandCompressedPhenotypeInput
                    {
                        Phenotype = phenotype,
                        AllowedLoci = allowedLoci,
                        HlaNomenclatureVersion = hlaNomenclatureVersion,
                        AllHaplotypes = new DataByResolution<IReadOnlyCollection<LociInfo<string>>>
                        {
                            GGroup = haplotypeFrequencies
                                .Where(h => h.Value.TypingCategory == HaplotypeTypingCategory.GGroup)
                                .Select(h => h.Value.Hla)
                                .ToList(),
                            PGroup = haplotypeFrequencies
                                .Where(h => h.Value.TypingCategory == HaplotypeTypingCategory.PGroup)
                                .Select(h => h.Value.Hla)
                                .ToList()
                        }
                    }
                );
            }
        }

        private IEnumerable<Tuple<GenotypeAtDesiredResolutions, GenotypeAtDesiredResolutions>> CombineGenotypes(
            List<GenotypeAtDesiredResolutions> patientGenotypes,
            List<GenotypeAtDesiredResolutions> donorGenotypes)
        {
            using (logger.RunTimed("Combining patient/donor genotypes", LogLevel.Verbose))
            {
                var combinations = patientGenotypes.SelectMany(patientHla =>
                    donorGenotypes.Select(donorHla =>
                        new Tuple<GenotypeAtDesiredResolutions, GenotypeAtDesiredResolutions>(patientHla, donorHla)));

                logger.SendTrace($"Patient/donor pairs: {NumberOfPairsOfCartesianProduct(patientGenotypes, donorGenotypes):n0}");

                return combinations;
            }
        }

        private ILongOperationLoggingStopwatch MatchCountLogger(long patientDonorPairCount) => logger.RunLongOperationWithTimer(
            "Calculate match counts.",
            new LongLoggingSettings
            {
                ExpectedNumberOfIterations = patientDonorPairCount,
                // The higher this number, the slower the algorithm will be. Every 500,000 seems to be a good balance opf performance vs. Information.
                InnerOperationLoggingPeriod = 500_000
            },
            LogLevel.Verbose
        );

        // This needs to return an IEnumerable to enable lazy evaluation - the list of combinations will be very long, so we do not want to enumerate 
        // it more than once, so we leave it as an IEnumerable until match probability aggregation.
        private IEnumerable<GenotypeMatchDetails> CalculatePairsMatchCounts(
            IEnumerable<Tuple<GenotypeAtDesiredResolutions, GenotypeAtDesiredResolutions>> allPatientDonorCombinations,
            ISet<Locus> allowedLoci,
            ILongOperationLoggingStopwatch stopwatch,
            List<List<int>> matchCounts,
            int[] flatMatchCounts,
            int nestedArrayCount)
        {
            return allPatientDonorCombinations
                .Select(pd =>
                {
                    using (stopwatch.TimeInnerOperation())
                    {
                        var (patient, donor) = pd;
                        return new GenotypeMatchDetails
                        {
                            AvailableLoci = allowedLoci,
                            DonorGenotype = donor.HaplotypeResolution,
                            DonorGenotypeLikelihood = donor.GenotypeLikelihood,
                            PatientGenotype = patient.HaplotypeResolution,
                            PatientGenotypeLikelihood = patient.GenotypeLikelihood,
                            MatchCounts = matchCalculationService.CalculateMatchCounts_Faster_Still(
                                patient.IndexResolution,
                                donor.IndexResolution,
                                allowedLoci,
                                (flatMatchCounts, nestedArrayCount)
                            )
                        };
                    }
                });
        }

        private async Task<Dictionary<StringGenotype, decimal>> CalculateGenotypeLikelihoods(
            ISet<StringGenotype> genotypes,
            HaplotypeFrequencySet frequencySet,
            ISet<Locus> allowedLoci)
        {
            using (logger.RunTimed($"{LoggingPrefix}Calculate likelihoods for genotypes", LogLevel.Verbose))
            {
                var genotypeLikelihoodTasks = genotypes.Select(genotype => CalculateLikelihood(genotype, frequencySet, allowedLoci)).ToList();
                var genotypeLikelihoods = await Task.WhenAll(genotypeLikelihoodTasks);
                return genotypeLikelihoods.ToDictionary();
            }
        }

        private async Task<KeyValuePair<StringGenotype, decimal>> CalculateLikelihood(
            StringGenotype genotype,
            HaplotypeFrequencySet frequencySet,
            ISet<Locus> allowedLoci)
        {
            var likelihood = await genotypeLikelihoodService.CalculateLikelihood(genotype, frequencySet, allowedLoci);
            return new KeyValuePair<StringGenotype, decimal>(genotype, likelihood);
        }
    }
}