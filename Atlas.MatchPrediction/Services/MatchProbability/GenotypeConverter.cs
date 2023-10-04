using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HlaConversion;
using Atlas.MatchPrediction.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenotypeAsStrings = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.PhenotypeInfo<string>;
using GenotypeOfKnownTypingCategory = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.PhenotypeInfo<Atlas.MatchPrediction.ExternalInterface.Models.HlaAtKnownTypingCategory>;

namespace Atlas.MatchPrediction.Services.MatchProbability
{
    internal class GenotypeConverterInput
    {
        public ISet<GenotypeOfKnownTypingCategory> Genotypes { get; set; }
        public IReadOnlyDictionary<GenotypeAsStrings, decimal> GenotypeLikelihoods { get; set; }
        public string HfSetHlaNomenclatureVersion { get; set; }
        public string MatchingAlgorithmHlaNomenclatureVersion { get; set; }
        public string SubjectLogDescription { get; set; }
    }

    internal interface IGenotypeConverter
    {
        Task<ICollection<GenotypeAtDesiredResolutions>> ConvertGenotypes(GenotypeConverterInput input);
    }

    internal class GenotypeConverter : IGenotypeConverter
    {
        private const string StageToLog = "Convert genotypes for match calculation";
        private readonly ILogger logger;
        private readonly IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory;
        private readonly IGGroupToPGroupConverter gGroupConverter;
        private readonly ISmallGGroupToPGroupConverter smallGGroupConverter;

        public GenotypeConverter(
            // ReSharper disable once SuggestBaseTypeForParameterInConstructor
            IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory,
            IGGroupToPGroupConverter gGroupConverter,
            ISmallGGroupToPGroupConverter smallGGroupConverter,
            IMatchPredictionLogger<MatchProbabilityLoggingContext> logger)
        {
            this.logger = logger;
            this.hlaMetadataDictionaryFactory = hlaMetadataDictionaryFactory;
            this.gGroupConverter = gGroupConverter;
            this.smallGGroupConverter = smallGGroupConverter;
        }

        public async Task<ICollection<GenotypeAtDesiredResolutions>> ConvertGenotypes(GenotypeConverterInput input)
        {
            var hfSetHmd = hlaMetadataDictionaryFactory.BuildDictionary(input.HfSetHlaNomenclatureVersion);
            var matchingHmd = input.MatchingAlgorithmHlaNomenclatureVersion == null
                ? null
                : hlaMetadataDictionaryFactory.BuildDictionary(input.MatchingAlgorithmHlaNomenclatureVersion);

            using (logger.RunTimed($"{StageToLog}: {input.SubjectLogDescription}", LogLevel.Verbose))
            {
                return (await Task.WhenAll(input.Genotypes.Select(async g => await ConvertGenotypeToPGroups(
                    g,
                    hfSetHmd,
                    matchingHmd,
                    input.GenotypeLikelihoods[g.ToHlaNames()]
                )))).ToList();
            }
        }

        private async Task<GenotypeAtDesiredResolutions> ConvertGenotypeToPGroups(
            GenotypeOfKnownTypingCategory genotype,
            IHlaMetadataDictionary hfSetHmd,
            IHlaMetadataDictionary matchingHmd,
            decimal genotypeLikelihood)
        {
            var stringMatchableGenotype = (await genotype.MapAsync(async (locus, _, hla) =>
            {
                if (hla?.Hla == null)
                {
                    return null;
                }

                async Task<string> ConvertHlaToPGroup(IHlaConverter converter)
                {
                    var converterInput = new HlaConverterInput
                    {
                        HfSetHmd = hfSetHmd,
                        MatchingAlgorithmHmd = matchingHmd,
                        StageToLog = StageToLog,
                        TargetHlaCategory = TargetHlaCategory.PGroup
                    };

                    return (await converter.ConvertHlaWithLoggingAndRetryOnFailure(converterInput, locus, hla.Hla)).SingleOrDefault();
                }

                return hla.TypingCategory switch
                {
                    HaplotypeTypingCategory.PGroup => hla.Hla,
                    HaplotypeTypingCategory.GGroup => await ConvertHlaToPGroup(gGroupConverter),
                    HaplotypeTypingCategory.SmallGGroup => await ConvertHlaToPGroup(smallGGroupConverter),
                    _ => throw new ArgumentOutOfRangeException(nameof(hla.TypingCategory))
                };
            })).CopyExpressingAllelesToNullPositions();

            return new GenotypeAtDesiredResolutions(genotype, stringMatchableGenotype, genotypeLikelihood);
        }
    }
}