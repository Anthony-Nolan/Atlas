using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Services.CompressedPhenotypeExpansion;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchCalculation;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;
using NSubstitute;
using NUnit.Framework;
using HaplotypeFrequencySet = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet.HaplotypeFrequencySet;

namespace Atlas.MatchPrediction.Test.Services.MatchProbability
{
    [TestFixture]
    public class MatchProbabilityServiceTests
    {
        private ICompressedPhenotypeExpander compressedPhenotypeExpander;
        private IGenotypeLikelihoodService genotypeLikelihoodService;
        private IMatchCalculationService matchCalculationService;
        private IGenotypeConverter genotypeConverter;
        private IMatchProbabilityCalculator matchProbabilityCalculator;
        private IHaplotypeFrequencyService haplotypeFrequencyService;
        private IMatchPredictionLogger logger;
        private MatchPredictionLoggingContext matchPredictionLoggingContext;

        private IMatchProbabilityService matchProbabilityService;

        [SetUp]
        public void SetUp()
        {
            compressedPhenotypeExpander = Substitute.For<ICompressedPhenotypeExpander>();
            genotypeLikelihoodService = Substitute.For<IGenotypeLikelihoodService>();
            genotypeConverter = Substitute.For<IGenotypeConverter>();
            matchCalculationService = Substitute.For<IMatchCalculationService>();
            matchProbabilityCalculator = Substitute.For<IMatchProbabilityCalculator>();
            haplotypeFrequencyService = Substitute.For<IHaplotypeFrequencyService>();
            logger = Substitute.For<IMatchPredictionLogger>();
            matchPredictionLoggingContext = new MatchPredictionLoggingContext();

            var hmd = Substitute.For<IHlaMetadataDictionary>();
            var hmdFactory = Substitute.For<IHlaMetadataDictionaryFactory>();
            hmdFactory.BuildDictionary(default).ReturnsForAnyArgs(hmd);

            matchProbabilityService = new MatchProbabilityService(
                compressedPhenotypeExpander,
                genotypeLikelihoodService,
                genotypeConverter,
                matchCalculationService,
                matchProbabilityCalculator,
                haplotypeFrequencyService,
                logger,
                matchPredictionLoggingContext);

            haplotypeFrequencyService.GetAllHaplotypeFrequencies(default).ReturnsForAnyArgs(
                new ConcurrentDictionary<LociInfo<string>, HaplotypeFrequency>(
                    new Dictionary<LociInfo<string>, HaplotypeFrequency> {{new LociInfo<string>(), HaplotypeFrequencyBuilder.New.Build()}})
                );

            compressedPhenotypeExpander.ExpandCompressedPhenotype(default).ReturnsForAnyArgs(
                new HashSet<PhenotypeInfo<HlaAtKnownTypingCategory>>(new List<PhenotypeInfo<HlaAtKnownTypingCategory>>
                {
                    new PhenotypeInfo<HlaAtKnownTypingCategory>(new HlaAtKnownTypingCategory("hla", HaplotypeTypingCategory.SmallGGroup))
                })
            );

            genotypeLikelihoodService.CalculateLikelihood(default, default, default).ReturnsForAnyArgs(0.01m);

            genotypeConverter.ConvertGenotypes(default, default, default, default).ReturnsForAnyArgs(
                new List<GenotypeAtDesiredResolutions>()
            );

            matchProbabilityCalculator.CalculateMatchProbability(default, default, default, default).ReturnsForAnyArgs(
                new MatchProbabilityResponse());
        }

        [Test]
        public async Task CalculateMatchProbability_ExpandsPhenotypesUsingHFSetNomenclatureVersions()
        {
            const string patientHfSetVersion = "3400", donorHfSetVersion = "3410";

            haplotypeFrequencyService.GetHaplotypeFrequencySets(default, default).ReturnsForAnyArgs(
                new HaplotypeFrequencySetResponse
                {
                    PatientSet = new HaplotypeFrequencySet { HlaNomenclatureVersion = patientHfSetVersion },
                    DonorSet = new HaplotypeFrequencySet { HlaNomenclatureVersion = donorHfSetVersion }
                }
            );

            var input = SingleDonorMatchProbabilityInputBuilder.Default.Build();
            await matchProbabilityService.CalculateMatchProbability(input);


            await compressedPhenotypeExpander.Received(1).ExpandCompressedPhenotype(Arg.Is<ExpandCompressedPhenotypeInput>(x =>
                x.HlaNomenclatureVersion == donorHfSetVersion));

            await compressedPhenotypeExpander.Received(1).ExpandCompressedPhenotype(Arg.Is<ExpandCompressedPhenotypeInput>(x =>
                x.HlaNomenclatureVersion == patientHfSetVersion));
        }

        [Test]
        public async Task CalculateMatchProbability_ConvertsGenotypesUsingHFSetNomenclatureVersions()
        {
            const string patientHfSetVersion = "3400", donorHfSetVersion = "3410";

            haplotypeFrequencyService.GetHaplotypeFrequencySets(default, default).ReturnsForAnyArgs(
                new HaplotypeFrequencySetResponse
                {
                    PatientSet = new HaplotypeFrequencySet { HlaNomenclatureVersion = patientHfSetVersion },
                    DonorSet = new HaplotypeFrequencySet { HlaNomenclatureVersion = donorHfSetVersion }
                }
            );

            var input = SingleDonorMatchProbabilityInputBuilder.Default.Build();
            await matchProbabilityService.CalculateMatchProbability(input);


            await genotypeConverter.Received(1).ConvertGenotypes(
                Arg.Any<ISet<PhenotypeInfo<HlaAtKnownTypingCategory>>>(), 
                "patient", 
                Arg.Any<IReadOnlyDictionary<PhenotypeInfo<string>, decimal>>(),
                patientHfSetVersion
                );

            await genotypeConverter.Received(1).ConvertGenotypes(
                Arg.Any<ISet<PhenotypeInfo<HlaAtKnownTypingCategory>>>(), 
                "donor", 
                Arg.Any<IReadOnlyDictionary<PhenotypeInfo<string>, decimal>>(),
                donorHfSetVersion
            );
        }
    }
}
