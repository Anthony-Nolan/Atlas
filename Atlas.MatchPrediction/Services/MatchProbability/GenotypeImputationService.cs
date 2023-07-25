using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.Services.CompressedPhenotypeExpansion;
using HaplotypeFrequencySet = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet.HaplotypeFrequencySet;
using PhenotypeOfStrings = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.PhenotypeInfo<string>;
using GenotypeOfKnownTypingCategory = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.PhenotypeInfo<Atlas.MatchPrediction.ExternalInterface.Models.HlaAtKnownTypingCategory>;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;

namespace Atlas.MatchPrediction.Services.MatchProbability
{
    public class ImputationInput
    {
        public PhenotypeOfStrings HlaTyping { get; set; }
        public HashSet<Locus> AllowedMatchPredictionLoci { get; set; }
        public HaplotypeFrequencySet FrequencySet { get; set; }
        public string SubjectLogDescription { get; set; }
    }

    public interface IGenotypeImputationService
    {
        /// <summary>
        /// Expands <see cref="ImputationInput.HlaTyping"/> to set of possible genotypes with their likelihoods.
        /// </summary>
        /// <returns>A set of the most likely genotypes, truncated from the full set of possible genotypes.
        /// <see cref="ExpandedGenotypeTruncater"/> for more info on set truncation.</returns>
        Task<ImputedGenotypes> Impute(ImputationInput input);
    }

    internal class GenotypeImputationService : IGenotypeImputationService
    {
        private const string LoggingPrefix = "MatchPrediction: ";
        private readonly IHaplotypeFrequencyService haplotypeFrequencyService;
        private readonly ICompressedPhenotypeExpander compressedPhenotypeExpander;
        private readonly IGenotypeLikelihoodService genotypeLikelihoodService;
        private readonly ILogger logger;

        public GenotypeImputationService(
            IHaplotypeFrequencyService haplotypeFrequencyService,
            ICompressedPhenotypeExpander compressedPhenotypeExpander,
            IGenotypeLikelihoodService genotypeLikelihoodService,
            // ReSharper disable once SuggestBaseTypeForParameterInConstructor
            IMatchPredictionLogger<MatchProbabilityLoggingContext> logger)
        {
            this.haplotypeFrequencyService = haplotypeFrequencyService;
            this.compressedPhenotypeExpander = compressedPhenotypeExpander;
            this.genotypeLikelihoodService = genotypeLikelihoodService;
            this.logger = logger;
        }

        /// <inheritdoc />
        public async Task<ImputedGenotypes> Impute(ImputationInput input)
        {
            var genotypes = await ExpandToGenotypes(input);

            if (genotypes.IsNullOrEmpty())
            {
                logger.SendTrace($"{LoggingPrefix}{input.SubjectLogDescription} genotype unrepresented.", LogLevel.Verbose);
                return new ImputedGenotypes();
            }

            var genotypeLikelihoods = await CalculateGenotypeLikelihoods(genotypes, input.FrequencySet, input.AllowedMatchPredictionLoci);

            return ExpandedGenotypeTruncater.TruncateGenotypes(genotypeLikelihoods, genotypes);
        }

        private async Task<ISet<GenotypeOfKnownTypingCategory>> ExpandToGenotypes(ImputationInput input)
        {
            var haplotypeFrequencies = await haplotypeFrequencyService.GetAllHaplotypeFrequencies(input.FrequencySet.Id);

            using (logger.RunTimed($"{LoggingPrefix}Expand {input.SubjectLogDescription} phenotype", LogLevel.Verbose))
            {
                var groupedFrequencies = haplotypeFrequencies
                    .GroupBy(f => f.Value.TypingCategory)
                    .Select(g =>
                        new KeyValuePair<HaplotypeTypingCategory, IReadOnlyCollection<LociInfo<string>>>(g.Key, g.Select(f => f.Value.Hla).ToList())
                    )
                    .ToDictionary();

                return await compressedPhenotypeExpander.ExpandCompressedPhenotype(
                    new ExpandCompressedPhenotypeInput
                    {
                        Phenotype = input.HlaTyping,
                        AllowedLoci = input.AllowedMatchPredictionLoci,
                        HlaNomenclatureVersion = input.FrequencySet.HlaNomenclatureVersion,
                        AllHaplotypes = new DataByResolution<IReadOnlyCollection<LociInfo<string>>>
                        {
                            GGroup = groupedFrequencies.GetValueOrDefault(HaplotypeTypingCategory.GGroup, new List<LociInfo<string>>()),
                            PGroup = groupedFrequencies.GetValueOrDefault(HaplotypeTypingCategory.PGroup, new List<LociInfo<string>>()),
                            SmallGGroup = groupedFrequencies.GetValueOrDefault(HaplotypeTypingCategory.SmallGGroup, new List<LociInfo<string>>()),
                        }
                    }
                );
            }
        }

        private async Task<Dictionary<PhenotypeOfStrings, decimal>> CalculateGenotypeLikelihoods(
            IEnumerable<GenotypeOfKnownTypingCategory> genotypes,
            HaplotypeFrequencySet frequencySet,
            ISet<Locus> allowedLoci)
        {
            using (logger.RunTimed($"{LoggingPrefix}Calculate likelihoods for genotypes", LogLevel.Verbose))
            {
                var genotypeLikelihoods = new List<KeyValuePair<PhenotypeOfStrings, decimal>>();

                var stringGenotypes = genotypes.Select(g => g.ToHlaNames()).ToHashSet();

                // If there is no ambiguity for an input genotype, we do not need to use haplotype frequencies to work out the likelihood of said genotype - it is already guaranteed! 
                if (stringGenotypes.Count == 1)
                {
                    genotypeLikelihoods.Add(new KeyValuePair<PhenotypeOfStrings, decimal>(stringGenotypes.Single(), 1));
                }
                else
                {
                    foreach (var genotype in stringGenotypes)
                    {
                        genotypeLikelihoods.Add(await CalculateLikelihood(genotype, frequencySet, allowedLoci));
                    }
                }

                return genotypeLikelihoods.ToDictionary();
            }
        }

        private async Task<KeyValuePair<PhenotypeOfStrings, decimal>> CalculateLikelihood(
            PhenotypeOfStrings diplotype,
            HaplotypeFrequencySet frequencySet,
            ISet<Locus> allowedLoci)
        {
            var likelihood = await genotypeLikelihoodService.CalculateLikelihoodForDiplotype(diplotype, frequencySet, allowedLoci);
            return new KeyValuePair<PhenotypeOfStrings, decimal>(diplotype, likelihood);
        }
    }
}