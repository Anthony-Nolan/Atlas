using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
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
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using HaplotypeFrequencySet = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet.HaplotypeFrequencySet;
using AutoFixture;

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
        private IMatchPredictionLogger<MatchProbabilityLoggingContext> logger;
        private MatchProbabilityLoggingContext matchProbabilityLoggingContext;
        private readonly Fixture fixture = new();

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
            logger = Substitute.For<IMatchPredictionLogger<MatchProbabilityLoggingContext>>();
            matchProbabilityLoggingContext = new MatchProbabilityLoggingContext();

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
                matchProbabilityLoggingContext);

            haplotypeFrequencyService.GetAllHaplotypeFrequencies(default).ReturnsForAnyArgs(
                new ConcurrentDictionary<LociInfo<string>, HaplotypeFrequency>(
                    new Dictionary<LociInfo<string>, HaplotypeFrequency> {{new LociInfo<string>(), HaplotypeFrequencyBuilder.New.Build()}})
                );

            compressedPhenotypeExpander.ExpandCompressedPhenotype(default).ReturnsForAnyArgs(
                new HashSet<PhenotypeInfo<HlaAtKnownTypingCategory>>(new List<PhenotypeInfo<HlaAtKnownTypingCategory>>
                {
                    new(new HlaAtKnownTypingCategory("hla", HaplotypeTypingCategory.SmallGGroup))
                })
            );

            genotypeLikelihoodService.CalculateLikelihoodForDiplotype(default, default, default).ReturnsForAnyArgs(0.01m);

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

        [Test]
        public async Task CalculateMatchProbability_ShouldReturnCompleteHaplotypeFrequencySet()
        {
            var patientSet = fixture.Create<HaplotypeFrequencySet>();
            var donorSet = fixture.Create<HaplotypeFrequencySet>();

            haplotypeFrequencyService.GetHaplotypeFrequencySets(default, default).ReturnsForAnyArgs(
                new HaplotypeFrequencySetResponse
                {
                    PatientSet = patientSet,
                    DonorSet = donorSet
                }
            );

            var input = SingleDonorMatchProbabilityInputBuilder.Default.Build();
            var result = await matchProbabilityService.CalculateMatchProbability(input);

            result.DonorHaplotypeFrequencySet.Id.Should().Be(donorSet.Id);
            result.DonorHaplotypeFrequencySet.RegistryCode.Should().Be(donorSet.RegistryCode);
            result.DonorHaplotypeFrequencySet.EthnicityCode.Should().Be(donorSet.EthnicityCode);
            result.DonorHaplotypeFrequencySet.HlaNomenclatureVersion.Should().Be(donorSet.HlaNomenclatureVersion);
            result.DonorHaplotypeFrequencySet.PopulationId.Should().Be(donorSet.PopulationId);
            result.DonorHaplotypeFrequencySet.HlaNomenclatureVersion.Should().Be(donorSet.HlaNomenclatureVersion);

            result.PatientHaplotypeFrequencySet.Id.Should().Be(patientSet.Id);
            result.PatientHaplotypeFrequencySet.RegistryCode.Should().Be(patientSet.RegistryCode);
            result.PatientHaplotypeFrequencySet.EthnicityCode.Should().Be(patientSet.EthnicityCode);
            result.PatientHaplotypeFrequencySet.HlaNomenclatureVersion.Should().Be(patientSet.HlaNomenclatureVersion);
            result.PatientHaplotypeFrequencySet.PopulationId.Should().Be(patientSet.PopulationId);
            result.PatientHaplotypeFrequencySet.HlaNomenclatureVersion.Should().Be(patientSet.HlaNomenclatureVersion);
        }
    }
}