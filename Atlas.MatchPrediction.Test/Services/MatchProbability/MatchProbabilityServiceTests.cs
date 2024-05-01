using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;
using AutoFixture;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using HaplotypeFrequencySet = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet.HaplotypeFrequencySet;

namespace Atlas.MatchPrediction.Test.Services.MatchProbability
{
    [TestFixture]
    public class MatchProbabilityServiceTests
    {
        private IMatchProbabilityCalculator matchProbabilityCalculator;
        private IGenotypeMatcher genotypeMatcher;
        private IHaplotypeFrequencyService haplotypeFrequencyService;
        private IMatchPredictionLogger<MatchProbabilityLoggingContext> logger;
        private MatchProbabilityLoggingContext matchProbabilityLoggingContext;
        private readonly Fixture fixture = new();

        private IMatchProbabilityService matchProbabilityService;

        [SetUp]
        public void SetUp()
        {
            matchProbabilityCalculator = Substitute.For<IMatchProbabilityCalculator>();
            genotypeMatcher = Substitute.For<IGenotypeMatcher>();
            haplotypeFrequencyService = Substitute.For<IHaplotypeFrequencyService>();
            logger = Substitute.For<IMatchPredictionLogger<MatchProbabilityLoggingContext>>();
            matchProbabilityLoggingContext = new MatchProbabilityLoggingContext();

            var hmd = Substitute.For<IHlaMetadataDictionary>();
            var hmdFactory = Substitute.For<IHlaMetadataDictionaryFactory>();
            hmdFactory.BuildDictionary(default).ReturnsForAnyArgs(hmd);

            matchProbabilityService = new MatchProbabilityService(
                matchProbabilityCalculator,
                haplotypeFrequencyService,
                genotypeMatcher,
                logger,
                matchProbabilityLoggingContext);

            haplotypeFrequencyService.GetHaplotypeFrequencySets(default, default).ReturnsForAnyArgs(
                new HaplotypeFrequencySetResponse
                {
                    PatientSet = new HaplotypeFrequencySet(),
                    DonorSet = new HaplotypeFrequencySet()
                });

            genotypeMatcher.MatchPatientDonorGenotypes(default).ReturnsForAnyArgs(new GenotypeMatcherResult
            {
                PatientResult = new GenotypeMatcherResult.SubjectResult(false, 1, 0m),
                DonorResult = new GenotypeMatcherResult.SubjectResult(false, 1, 0m),
                GenotypeMatchDetails = new List<GenotypeMatchDetails>()
            });

            matchProbabilityCalculator.CalculateMatchProbability(default, default, default, default).ReturnsForAnyArgs(
                new MatchProbabilityResponse());
        }

        [Test]
        public async Task CalculateMatchProbability_MatchesPatientAndDonorGenotypes()
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

            await genotypeMatcher.Received(1).MatchPatientDonorGenotypes(Arg.Is<GenotypeMatcherInput>(x =>
                x.PatientData.SubjectFrequencySet.SubjectLogDescription == "patient" &&
                x.PatientData.SubjectFrequencySet.FrequencySet.Id == patientHfSetId &&
                x.PatientData.HlaTyping.Equals(input.PatientHla.ToPhenotypeInfo()) &&
                x.DonorData.SubjectFrequencySet.SubjectLogDescription == "donor" &&
                x.DonorData.SubjectFrequencySet.FrequencySet.Id == donorHfSetId &&
                x.DonorData.HlaTyping.Equals(input.Donor.DonorHla.ToPhenotypeInfo()) &&
                x.MatchPredictionParameters.AllowedLoci.SetEquals(new[] { Locus.A, Locus.B, Locus.C, Locus.Dqb1, Locus.Drb1 })));
        }

        [Test]
        public async Task CalculateMatchProbability_PatientIsUnrepresented_DoesNotCalculateMatchProbability()
        {
            genotypeMatcher.MatchPatientDonorGenotypes(default).ReturnsForAnyArgs(new GenotypeMatcherResult
            {
                PatientResult = new GenotypeMatcherResult.SubjectResult(true, 0, 0m),
                DonorResult = new GenotypeMatcherResult.SubjectResult(false, 1, 0.1m),
                GenotypeMatchDetails = new List<GenotypeMatchDetails>()
            });

            var input = SingleDonorMatchProbabilityInputBuilder.Valid.Build();
            await matchProbabilityService.CalculateMatchProbability(input);

            matchProbabilityCalculator.DidNotReceiveWithAnyArgs().CalculateMatchProbability(default, default, default, default);
        }

        [Test]
        public async Task CalculateMatchProbability_PatientIsUnrepresented_ReturnsUnrepresentedResponse()
        {
            const int patientHfSetId = 123, donorHfSetId = 456;

            haplotypeFrequencyService.GetHaplotypeFrequencySets(default, default).ReturnsForAnyArgs(
                new HaplotypeFrequencySetResponse
                {
                    PatientSet = new HaplotypeFrequencySet { Id = patientHfSetId },
                    DonorSet = new HaplotypeFrequencySet { Id = donorHfSetId }
                }
            );

            genotypeMatcher.MatchPatientDonorGenotypes(default).ReturnsForAnyArgs(new GenotypeMatcherResult
            {
                PatientResult = new GenotypeMatcherResult.SubjectResult(true, 0, 0m),
                DonorResult = new GenotypeMatcherResult.SubjectResult(false, 1, 0.1m),
                GenotypeMatchDetails = new List<GenotypeMatchDetails>()
            });

            var input = SingleDonorMatchProbabilityInputBuilder.Valid.Build();
            var result = await matchProbabilityService.CalculateMatchProbability(input);

            result.IsPatientPhenotypeUnrepresented.Should().BeTrue();
            result.IsDonorPhenotypeUnrepresented.Should().BeFalse();
            result.PatientHaplotypeFrequencySet.Id.Should().Be(patientHfSetId);
            result.DonorHaplotypeFrequencySet.Id.Should().Be(donorHfSetId);
            result.MatchProbabilities.MatchCategory.Should().BeNull();
        }

        [Test]
        public async Task CalculateMatchProbability_DonorIsUnrepresented_DoesNotCalculateMatchProbability()
        {
            genotypeMatcher.MatchPatientDonorGenotypes(default).ReturnsForAnyArgs(new GenotypeMatcherResult
            {
                PatientResult = new GenotypeMatcherResult.SubjectResult(false, 1, 0.1m),
                DonorResult = new GenotypeMatcherResult.SubjectResult(true, 0, 0m),
                GenotypeMatchDetails = new List<GenotypeMatchDetails>()
            });

            var input = SingleDonorMatchProbabilityInputBuilder.Valid.Build();
            await matchProbabilityService.CalculateMatchProbability(input);

            matchProbabilityCalculator.DidNotReceiveWithAnyArgs().CalculateMatchProbability(default, default, default, default);
        }

        [Test]
        public async Task CalculateMatchProbability_DonorIsUnrepresented_ReturnsUnrepresentedResponse()
        {
            const int patientHfSetId = 123, donorHfSetId = 456;

            haplotypeFrequencyService.GetHaplotypeFrequencySets(default, default).ReturnsForAnyArgs(
                new HaplotypeFrequencySetResponse
                {
                    PatientSet = new HaplotypeFrequencySet { Id = patientHfSetId },
                    DonorSet = new HaplotypeFrequencySet { Id = donorHfSetId }
                }
            );

            genotypeMatcher.MatchPatientDonorGenotypes(default).ReturnsForAnyArgs(new GenotypeMatcherResult
            {
                PatientResult = new GenotypeMatcherResult.SubjectResult(false, 1, 0.1m),
                DonorResult = new GenotypeMatcherResult.SubjectResult(true,0, 0m),
                GenotypeMatchDetails = new List<GenotypeMatchDetails>()
            });

            var input = SingleDonorMatchProbabilityInputBuilder.Valid.Build();
            var result = await matchProbabilityService.CalculateMatchProbability(input);

            result.IsPatientPhenotypeUnrepresented.Should().BeFalse();
            result.IsDonorPhenotypeUnrepresented.Should().BeTrue();
            result.PatientHaplotypeFrequencySet.Id.Should().Be(patientHfSetId);
            result.DonorHaplotypeFrequencySet.Id.Should().Be(donorHfSetId);
            result.MatchProbabilities.MatchCategory.Should().BeNull();
        }

        [Test]
        public async Task CalculateMatchProbability_CalculatesMatchProbability()
        {
            const decimal patientLikelihood = 0.1m, donorLikelihood = 0.5m;

            genotypeMatcher.MatchPatientDonorGenotypes(default).ReturnsForAnyArgs(new GenotypeMatcherResult
            {
                PatientResult = new GenotypeMatcherResult.SubjectResult(false, 1, patientLikelihood),
                DonorResult = new GenotypeMatcherResult.SubjectResult(false, 1, donorLikelihood),
                GenotypeMatchDetails = new List<GenotypeMatchDetails>()
            });

            var input = SingleDonorMatchProbabilityInputBuilder.Valid.Build();
            await matchProbabilityService.CalculateMatchProbability(input);

            matchProbabilityCalculator.Received(1).CalculateMatchProbability(
                patientLikelihood,
                donorLikelihood,
                Arg.Any<IEnumerable<GenotypeMatchDetails>>(),
                Arg.Is<HashSet<Locus>>(x => x.SetEquals(new[] { Locus.A, Locus.B, Locus.C, Locus.Dqb1, Locus.Drb1 }))
            );
        }

        [Test]
        public async Task CalculateMatchProbability_ReturnsMatchProbabilityResponse()
        {
            const decimal probability = 0.1m;

            genotypeMatcher.MatchPatientDonorGenotypes(default).ReturnsForAnyArgs(new GenotypeMatcherResult
            {
                PatientResult = new GenotypeMatcherResult.SubjectResult(false, 1, 0.1m),
                DonorResult = new GenotypeMatcherResult.SubjectResult(false, 1, 0.1m),
                GenotypeMatchDetails = new List<GenotypeMatchDetails>()
            });

            matchProbabilityCalculator.CalculateMatchProbability(default, default, default, default)
                .ReturnsForAnyArgs(new MatchProbabilityResponse
                {
                    IsDonorPhenotypeUnrepresented = false,
                    IsPatientPhenotypeUnrepresented = false,
                    MatchProbabilities = MatchProbabilitiesBuilder.New.WithAllProbabilityValuesSetTo(probability)
                });

            var input = SingleDonorMatchProbabilityInputBuilder.Valid.Build();
            var result = await matchProbabilityService.CalculateMatchProbability(input);

            result.IsPatientPhenotypeUnrepresented.Should().BeFalse();
            result.IsDonorPhenotypeUnrepresented.Should().BeFalse();
            result.MatchProbabilities.OneMismatchProbability.Decimal.Should().Be(probability);
        }

        [Test]
        public async Task CalculateMatchProbability_ReturnsCompleteHaplotypeFrequencySet()
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