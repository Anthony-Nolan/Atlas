using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchCalculation;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;
using AutoFixture;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using HaplotypeFrequencySet = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet.HaplotypeFrequencySet;

namespace Atlas.MatchPrediction.Test.Services.MatchProbability
{
    [TestFixture]
    public class MatchProbabilityServiceTests
    {
        private IMatchCalculationService matchCalculationService;
        private IGenotypeConverter genotypeConverter;
        private IMatchProbabilityCalculator matchProbabilityCalculator;
        private IHaplotypeFrequencyService haplotypeFrequencyService;
        private IGenotypeImputationService genotypeImputationService;
        private IMatchPredictionLogger<MatchProbabilityLoggingContext> logger;
        private MatchProbabilityLoggingContext matchProbabilityLoggingContext;
        private readonly Fixture fixture = new();

        private IMatchProbabilityService matchProbabilityService;

        [SetUp]
        public void SetUp()
        {
            genotypeConverter = Substitute.For<IGenotypeConverter>();
            matchCalculationService = Substitute.For<IMatchCalculationService>();
            matchProbabilityCalculator = Substitute.For<IMatchProbabilityCalculator>();
            haplotypeFrequencyService = Substitute.For<IHaplotypeFrequencyService>();
            genotypeImputationService = Substitute.For<IGenotypeImputationService>();
            logger = Substitute.For<IMatchPredictionLogger<MatchProbabilityLoggingContext>>();
            matchProbabilityLoggingContext = new MatchProbabilityLoggingContext();

            var hmd = Substitute.For<IHlaMetadataDictionary>();
            var hmdFactory = Substitute.For<IHlaMetadataDictionaryFactory>();
            hmdFactory.BuildDictionary(default).ReturnsForAnyArgs(hmd);

            matchProbabilityService = new MatchProbabilityService(
                genotypeConverter,
                matchCalculationService,
                matchProbabilityCalculator,
                haplotypeFrequencyService,
                genotypeImputationService,
                logger,
                matchProbabilityLoggingContext);

            haplotypeFrequencyService.GetAllHaplotypeFrequencies(default).ReturnsForAnyArgs(
                new ConcurrentDictionary<LociInfo<string>, HaplotypeFrequency>(
                    new Dictionary<LociInfo<string>, HaplotypeFrequency> { { new LociInfo<string>(), HaplotypeFrequencyBuilder.New.Build() } })
                );

            genotypeImputationService.Impute(default).ReturnsForAnyArgs(new ImputedGenotypes
            {
                GenotypeLikelihoods = new Dictionary<PhenotypeInfo<string>, decimal> { { new PhenotypeInfo<string>("hla"), 0.01m } },
                Genotypes = new HashSet<PhenotypeInfo<HlaAtKnownTypingCategory>> { new(new HlaAtKnownTypingCategory("hla", HaplotypeTypingCategory.SmallGGroup)) }
            });

            genotypeConverter.ConvertGenotypes(default, default, default, default).ReturnsForAnyArgs(
                new List<GenotypeAtDesiredResolutions>()
            );

            matchProbabilityCalculator.CalculateMatchProbability(default, default, default, default).ReturnsForAnyArgs(
                new MatchProbabilityResponse());
        }

        [Test]
        public async Task CalculateMatchProbability_ImputesGenotypesForPatientAndDonorHlaTypings()
        {
            const int patientHfSetId = 123, donorHfSetId = 456;

            haplotypeFrequencyService.GetHaplotypeFrequencySets(default, default).ReturnsForAnyArgs(
                new HaplotypeFrequencySetResponse
                {
                    PatientSet = new HaplotypeFrequencySet { Id = patientHfSetId },
                    DonorSet = new HaplotypeFrequencySet { Id = donorHfSetId }
                }
            );

            var input = SingleDonorMatchProbabilityInputBuilder.Valid.Build();
            await matchProbabilityService.CalculateMatchProbability(input);

            await genotypeImputationService.Received(1).Impute(Arg.Is<ImputationInput>(x =>
                x.SubjectLogDescription == "patient" &&
                x.AllowedMatchPredictionLoci.SetEquals(new[] { Locus.A, Locus.B, Locus.C, Locus.Dqb1, Locus.Drb1 }) &&
                x.FrequencySet.Id == patientHfSetId));

            await genotypeImputationService.Received(1).Impute(Arg.Is<ImputationInput>(x =>
                x.SubjectLogDescription == "donor" &&
                x.AllowedMatchPredictionLoci.SetEquals(new[] { Locus.A, Locus.B, Locus.C, Locus.Dqb1, Locus.Drb1 }) &&
                x.FrequencySet.Id == donorHfSetId));
        }

        [Test]
        public async Task CalculateMatchProbability_WhenPatientIsUnrepresented_ReturnsUnrepresentedResponse()
        {
            const int patientHfSetId = 123, donorHfSetId = 456;

            haplotypeFrequencyService.GetHaplotypeFrequencySets(default, default).ReturnsForAnyArgs(
                new HaplotypeFrequencySetResponse
                {
                    PatientSet = new HaplotypeFrequencySet { Id = patientHfSetId },
                    DonorSet = new HaplotypeFrequencySet { Id = donorHfSetId }
                }
            );

            genotypeImputationService.Impute(Arg.Is<ImputationInput>(x => x.SubjectLogDescription == "patient"))
                .Returns(new ImputedGenotypes());

            var input = SingleDonorMatchProbabilityInputBuilder.Valid.Build();
            var result = await matchProbabilityService.CalculateMatchProbability(input);

            result.IsPatientPhenotypeUnrepresented.Should().BeTrue();
            result.IsDonorPhenotypeUnrepresented.Should().BeFalse();
            result.PatientHaplotypeFrequencySet.Id.Should().Be(patientHfSetId);
            result.DonorHaplotypeFrequencySet.Id.Should().Be(donorHfSetId);
            result.MatchProbabilities.MatchCategory.Should().BeNull();
        }

        [Test]
        public async Task CalculateMatchProbability_WhenDonorIsUnrepresented_ReturnsUnrepresentedResponse()
        {
            const int patientHfSetId = 123, donorHfSetId = 456;

            haplotypeFrequencyService.GetHaplotypeFrequencySets(default, default).ReturnsForAnyArgs(
                new HaplotypeFrequencySetResponse
                {
                    PatientSet = new HaplotypeFrequencySet { Id = patientHfSetId },
                    DonorSet = new HaplotypeFrequencySet { Id = donorHfSetId }
                }
            );

            genotypeImputationService.Impute(Arg.Is<ImputationInput>(x => x.SubjectLogDescription == "donor"))
                .Returns(new ImputedGenotypes());

            var input = SingleDonorMatchProbabilityInputBuilder.Valid.Build();
            var result = await matchProbabilityService.CalculateMatchProbability(input);

            result.IsPatientPhenotypeUnrepresented.Should().BeFalse();
            result.IsDonorPhenotypeUnrepresented.Should().BeTrue();
            result.PatientHaplotypeFrequencySet.Id.Should().Be(patientHfSetId);
            result.DonorHaplotypeFrequencySet.Id.Should().Be(donorHfSetId);
            result.MatchProbabilities.MatchCategory.Should().BeNull();
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