using System;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.MatchCalculation;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using AutoFixture;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.MatchPrediction;

namespace Atlas.MatchPrediction.Test.Services.MatchProbability
{
    [TestFixture]
    internal class GenotypeMatcherTests
    {
        private IGenotypeImputationService genotypeImputer;
        private IGenotypeConverter genotypeConverter;
        private IMatchCalculationService matchCalculationService;
        private IPreparationMatcher preparationMatcher;
        private IMatchPredictionLogger<MatchProbabilityLoggingContext> logger;
        private IGenotypeMatcher genotypeMatcher;

        private readonly Fixture fixture = new();
        private GenotypeMatcherInput input; 

        [SetUp]
        public void SetUp()
        {
            genotypeImputer = Substitute.For<IGenotypeImputationService>();
            genotypeConverter = Substitute.For<IGenotypeConverter>();
            matchCalculationService = Substitute.For<IMatchCalculationService>();
            preparationMatcher = Substitute.For<IPreparationMatcher>();
            logger = Substitute.For<IMatchPredictionLogger<MatchProbabilityLoggingContext>>();

            genotypeMatcher = new GenotypeMatcher(genotypeImputer, genotypeConverter, matchCalculationService, preparationMatcher, logger);

            genotypeImputer.Impute(default).ReturnsForAnyArgs(new ImputedGenotypesBuilder().Default().Build());

            var genotype = new GenotypeAtDesiredResolutionsBuilder().Default().Build();
            genotypeConverter.ConvertGenotypesForMatchCalculation(default)
                .ReturnsForAnyArgs(new List<GenotypeAtDesiredResolutions> { genotype });

            matchCalculationService.CalculateMatchCounts_Fast(default, default, default)
                .ReturnsForAnyArgs(new LociInfo<int?>(0));

            input = BuildDefaultInput();
            preparationMatcher.UpdateRenamedHla(input.PatientData, input.MatchPredictionParameters).Returns(input.PatientData);
            preparationMatcher.UpdateRenamedHla(input.DonorData, input.MatchPredictionParameters).Returns(input.DonorData);
        }

        [Test]
        public async Task MatchPatientDonorGenotypes_ImputesGenotypes()
        {
            await genotypeMatcher.MatchPatientDonorGenotypes(input);

            await genotypeImputer.Received().Impute(Arg.Is<ImputationInput>(x =>
                x.MatchPredictionParameters.AllowedLoci.SetEquals(input.MatchPredictionParameters.AllowedLoci) &&
                x.SubjectData.HlaTyping.Equals(input.PatientData.HlaTyping) &&
                x.SubjectData.SubjectFrequencySet.FrequencySet.Id == input.PatientData.SubjectFrequencySet.FrequencySet.Id
            ));

            await genotypeImputer.Received().Impute(Arg.Is<ImputationInput>(x =>
                x.MatchPredictionParameters.AllowedLoci.SetEquals(input.MatchPredictionParameters.AllowedLoci) &&
                x.SubjectData.HlaTyping.Equals(input.DonorData.HlaTyping) &&
                x.SubjectData.SubjectFrequencySet.FrequencySet.Id == input.DonorData.SubjectFrequencySet.FrequencySet.Id
            ));
        }

        [Test]
        public async Task MatchPatientDonorGenotypes_PatientHasNoGenotypes_ReturnsPatientIsUnrepresented()
        {
            genotypeImputer
                .Impute(Arg.Is<ImputationInput>(x => x.SubjectData.SubjectFrequencySet.SubjectLogDescription == input.PatientData.SubjectFrequencySet.SubjectLogDescription))
                .Returns(new ImputedGenotypesBuilder().Build());

            var result = await genotypeMatcher.MatchPatientDonorGenotypes(input);

            result.PatientResult.IsUnrepresented.Should().BeTrue();
            result.PatientResult.SumOfLikelihoods.Should().Be(0);

            result.DonorResult.IsUnrepresented.Should().BeFalse();
            result.DonorResult.SumOfLikelihoods.Should().BeGreaterThan(0);
        }

        [Test]
        public async Task MatchPatientDonorGenotypes_DonorHasNoGenotypes_ReturnsDonorIsUnrepresented()
        {
            genotypeImputer
                .Impute(Arg.Is<ImputationInput>(x => x.SubjectData.SubjectFrequencySet.SubjectLogDescription == input.DonorData.SubjectFrequencySet.SubjectLogDescription))
                .Returns(new ImputedGenotypesBuilder().Build());

            var result = await genotypeMatcher.MatchPatientDonorGenotypes(input);

            result.DonorResult.IsUnrepresented.Should().BeTrue();
            result.DonorResult.SumOfLikelihoods.Should().Be(0);

            result.PatientResult.IsUnrepresented.Should().BeFalse();
            result.PatientResult.SumOfLikelihoods.Should().BeGreaterThan(0);
        }

        [Test]
        public async Task MatchPatientDonorGenotypes_ReturnsPatientDonorGenotypePairs()
        {
            var genotype1 = new GenotypeAtDesiredResolutionsBuilder().Default().Build();
            var genotype2 = new GenotypeAtDesiredResolutionsBuilder().Default().Build();

            genotypeConverter.ConvertGenotypesForMatchCalculation(default)
                .ReturnsForAnyArgs(new List<GenotypeAtDesiredResolutions> { genotype1, genotype2 });

            var result = await genotypeMatcher.MatchPatientDonorGenotypes(input);

            result.GenotypeMatchDetails.Count().Should().Be(4);
        }

        [Test]
        public async Task MatchPatientDonorGenotypes_OnlyCalculatesGenotypeMatchesOnEnumerationOfResults()
        {
            var genotype1 = new GenotypeAtDesiredResolutionsBuilder().Default().Build();
            var genotype2 = new GenotypeAtDesiredResolutionsBuilder().Default().Build();

            genotypeConverter.ConvertGenotypesForMatchCalculation(default)
                .ReturnsForAnyArgs(new List<GenotypeAtDesiredResolutions> { genotype1, genotype2 });

            var result = await genotypeMatcher.MatchPatientDonorGenotypes(input);

            // before enumeration
            matchCalculationService.DidNotReceiveWithAnyArgs().CalculateMatchCounts_Fast(default, default, default);

            _ = result.GenotypeMatchDetails.ToList();

            // after enumeration
            matchCalculationService.ReceivedWithAnyArgs(4).CalculateMatchCounts_Fast(default, default, default);
        }

        private GenotypeMatcherInput BuildDefaultInput()
        {
            var allowedLoci = new[] { Locus.A, Locus.B, Locus.Drb1 }.ToHashSet();

            var patientHla = new PhenotypeInfoBuilder<string>("patient-hla").Build();
            var patientFrequencySet = fixture.Create<SubjectFrequencySet>();

            var donorHla = new PhenotypeInfoBuilder<string>("donor-hla").Build();
            var donorFrequencySet = fixture.Create<SubjectFrequencySet>();

            return new GenotypeMatcherInput
            {
                MatchPredictionParameters = new MatchPredictionParameters(allowedLoci),
                PatientData = new SubjectData(patientHla, patientFrequencySet),
                DonorData = new SubjectData(donorHla, donorFrequencySet)
            };
        }
    }
}