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
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using GenotypeOfKnownTypingCategory = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.PhenotypeInfo<Atlas.MatchPrediction.ExternalInterface.Models.HlaAtKnownTypingCategory>;

namespace Atlas.MatchPrediction.Services.MatchProbability
{
    internal class GenotypeConverterInput
    {
        public PhenotypeInfo<string> CompressedPhenotype { get; set; }
        public ISet<Locus> AllowedLoci { get; set; }
        public ISet<GenotypeOfKnownTypingCategory> Genotypes { get; set; }
        public IReadOnlyDictionary<PhenotypeInfo<string>, decimal> GenotypeLikelihoods { get; set; }
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
        private readonly ILogger logger;
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
                return (await Task.WhenAll(input.Genotypes.Select(async g => await ConvertGenotypeToPGroups(
                    noNullAllelesInCompressedPhenotype,
                    nullAlleleInfoByPosition, 
                    g, 
                    hfSetHmd, 
                    matchingHmd, 
                    input.GenotypeLikelihoods[g.ToHlaNames()])
                ))).ToList();
            }
        }

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

            if (!allowedLoci.Contains(locus) || hla == null || !categoriser.IsNullAllele(hla))
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

        private async Task<GenotypeAtDesiredResolutions> ConvertGenotypeToPGroups(
            bool noNullAllelesInCompressedPhenotype,
            PhenotypeInfo<(bool, IEnumerable<HlaAtKnownTypingCategory>)> nullAlleleInfoByPosition,
            GenotypeOfKnownTypingCategory genotype,
            IHlaMetadataDictionary hfSetHmd,
            IHlaMetadataDictionary matchingHmd,
            decimal genotypeLikelihood)
        {
            var genotypeToConvert = noNullAllelesInCompressedPhenotype
                ? genotype
                : AccountForNullAlleleInCompressedPhenotype(genotype, nullAlleleInfoByPosition);

            var stringMatchableGenotype = (await genotypeToConvert.MapAsync(async (locus, _, hla) =>
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