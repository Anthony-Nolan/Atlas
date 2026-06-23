using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.MatchCalculation;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using AutoFixture;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.MatchPrediction;

namespace Atlas.MatchPrediction.Test.Services.MatchProbability;

[TestFixture]
internal class GenotypeMatcherTests
{
    private IGenotypeSetService genotypeSetService;
    private IMatchCalculationService matchCalculationService;
    private IMatchPredictionLogger<MatchProbabilityLoggingContext> logger;
    private IGenotypeMatcher genotypeMatcher;

    private readonly Fixture fixture = new();

    [SetUp]
    public void SetUp()
    {
        genotypeSetService = Substitute.For<IGenotypeSetService>();
        matchCalculationService = Substitute.For<IMatchCalculationService>();
        logger = Substitute.For<IMatchPredictionLogger<MatchProbabilityLoggingContext>>();

        genotypeMatcher = new GenotypeMatcher(genotypeSetService, matchCalculationService, logger);

        var genotype = new GenotypeAtDesiredResolutionsBuilder().Default().Build();
        genotypeSetService.GetGenotypeSet(default, default)
            .ReturnsForAnyArgs(new SubjectGenotypeSet(false, new List<GenotypeAtDesiredResolutions> { genotype }, 0.5m));

        matchCalculationService.CalculateMatchCounts_Fast(default, default, default)
            .ReturnsForAnyArgs(new LociInfo<int?>(0));
    }

    [Test]
    public async Task MatchPatientDonorGenotypes_ExpandsDonorGenotypes()
    {
        var input = BuildDefaultInput();
        await genotypeMatcher.MatchPatientDonorGenotypes(input);

        await genotypeSetService.Received().GetGenotypeSet(
            Arg.Is<SubjectData>(x =>
                x.HlaTyping.Equals(input.DonorData.HlaTyping) &&
                x.SubjectFrequencySet.FrequencySet.Id == input.DonorData.SubjectFrequencySet.FrequencySet.Id),
            Arg.Is<MatchPredictionParameters>(x =>
                x.AllowedLoci.SetEquals(input.MatchPredictionParameters.AllowedLoci)));
    }

    [Test]
    public async Task MatchPatientDonorGenotypes_UsesProvidedPatientGenotypeSet_DoesNotExpandPatient()
    {
        var input = BuildDefaultInput();
        var patientGenotype = new GenotypeAtDesiredResolutionsBuilder().Default().Build();
        input.PatientGenotypeSet = new SubjectGenotypeSet(false, new List<GenotypeAtDesiredResolutions> { patientGenotype }, 0.1m);

        var result = await genotypeMatcher.MatchPatientDonorGenotypes(input);

        // Should only call GetGenotypeSet for donor, not patient
        await genotypeSetService.Received(1).GetGenotypeSet(
            Arg.Any<SubjectData>(),
            Arg.Any<MatchPredictionParameters>());

        await genotypeSetService.Received().GetGenotypeSet(
            Arg.Is<SubjectData>(x =>
                x.SubjectFrequencySet.SubjectLogDescription == input.DonorData.SubjectFrequencySet.SubjectLogDescription),
            Arg.Any<MatchPredictionParameters>());

        result.PatientResult.GenotypeCount.Should().Be(1);
        result.PatientResult.SumOfLikelihoods.Should().Be(0.1m);
    }

    [Test]
    public async Task MatchPatientDonorGenotypes_PatientIsUnrepresented_ReturnsPatientIsUnrepresented()
    {
        var input = BuildDefaultInput();
        input.PatientGenotypeSet = new SubjectGenotypeSet(true, new List<GenotypeAtDesiredResolutions>(), 0m);

        var result = await genotypeMatcher.MatchPatientDonorGenotypes(input);

        result.PatientResult.IsUnrepresented.Should().BeTrue();
        result.PatientResult.SumOfLikelihoods.Should().Be(0);

        result.DonorResult.IsUnrepresented.Should().BeFalse();
        result.DonorResult.SumOfLikelihoods.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task MatchPatientDonorGenotypes_DonorIsUnrepresented_ReturnsDonorIsUnrepresented()
    {
        var input = BuildDefaultInput();

        genotypeSetService.GetGenotypeSet(
                Arg.Is<SubjectData>(x =>
                    x.SubjectFrequencySet.SubjectLogDescription == input.DonorData.SubjectFrequencySet.SubjectLogDescription),
                Arg.Any<MatchPredictionParameters>())
            .Returns(new SubjectGenotypeSet(true, new List<GenotypeAtDesiredResolutions>(), 0m));

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

        var input = BuildDefaultInput();
        input.PatientGenotypeSet = new SubjectGenotypeSet(false, new List<GenotypeAtDesiredResolutions> { genotype1, genotype2 }, 0.5m);

        genotypeSetService.GetGenotypeSet(default, default)
            .ReturnsForAnyArgs(new SubjectGenotypeSet(false, new List<GenotypeAtDesiredResolutions> { genotype1, genotype2 }, 0.5m));

        var result = await genotypeMatcher.MatchPatientDonorGenotypes(input);

        result.GenotypeMatchDetails.Count().Should().Be(4);
    }

    [Test]
    public async Task MatchPatientDonorGenotypes_OnlyCalculatesGenotypeMatchesOnEnumerationOfResults()
    {
        var genotype1 = new GenotypeAtDesiredResolutionsBuilder().Default().Build();
        var genotype2 = new GenotypeAtDesiredResolutionsBuilder().Default().Build();

        var input = BuildDefaultInput();
        input.PatientGenotypeSet = new SubjectGenotypeSet(false, new List<GenotypeAtDesiredResolutions> { genotype1, genotype2 }, 0.5m);

        genotypeSetService.GetGenotypeSet(default, default)
            .ReturnsForAnyArgs(new SubjectGenotypeSet(false, new List<GenotypeAtDesiredResolutions> { genotype1, genotype2 }, 0.5m));

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
        var patientGenotype = new GenotypeAtDesiredResolutionsBuilder().Default().Build();

        var donorHla = new PhenotypeInfoBuilder<string>("donor-hla").Build();
        var donorFrequencySet = fixture.Create<SubjectFrequencySet>();

        return new GenotypeMatcherInput
        {
            MatchPredictionParameters = new MatchPredictionParameters(allowedLoci),
            PatientData = new SubjectData(patientHla, patientFrequencySet),
            DonorData = new SubjectData(donorHla, donorFrequencySet),
            PatientGenotypeSet = new SubjectGenotypeSet(false, new List<GenotypeAtDesiredResolutions> { patientGenotype }, 0.5m)
        };
    }
}