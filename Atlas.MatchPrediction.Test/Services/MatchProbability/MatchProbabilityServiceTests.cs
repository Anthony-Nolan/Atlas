using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using Atlas.MatchPrediction.Services.MatchCalculation;
using Atlas.MatchPrediction.Services.MatchProbability;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.MatchProbability
{
    public class MatchProbabilityServiceTests
    {
        private ICompressedPhenotypeExpander compressedPhenotypeExpander;
        private IGenotypeLikelihoodService genotypeLikelihoodService;
        private IMatchCalculationService matchCalculationService;
        private IMatchProbabilityCalculator matchProbabilityCalculator;

        private IMatchProbabilityService matchProbabilityService;

        private const string HlaNomenclatureVersion = "3330";

        private const string PatientLocus = "patientHla";
        private const string DonorLocus = "donorHla";

        private static readonly PhenotypeInfo<string> PatientHla = new PhenotypeInfoBuilder<string>()
            .WithDataAt(Locus.A, PatientLocus).Build();

        private static readonly PhenotypeInfo<string> DonorHla = new PhenotypeInfoBuilder<string>()
            .WithDataAt(Locus.A, DonorLocus).Build();

        [SetUp]
        public void Setup()
        {
            compressedPhenotypeExpander = Substitute.For<ICompressedPhenotypeExpander>();
            genotypeLikelihoodService = Substitute.For<IGenotypeLikelihoodService>();
            matchCalculationService = Substitute.For<IMatchCalculationService>();
            matchProbabilityCalculator = Substitute.For<IMatchProbabilityCalculator>();
            var logger = Substitute.For<ILogger>();

            matchCalculationService.MatchAtPGroupLevel(Arg.Any<PhenotypeInfo<string>>(), Arg.Any<PhenotypeInfo<string>>(), Arg.Any<string>())
                .Returns(new GenotypeMatchDetails
                    {MatchCounts = new LociInfo<int?> {A = 0, B = 0, C = 0, Dpb1 = null, Dqb1 = 0, Drb1 = 0}});

            genotypeLikelihoodService.CalculateLikelihood(Arg.Any<PhenotypeInfo<string>>()).Returns(0.5m);

            matchProbabilityService = new MatchProbabilityService(
                compressedPhenotypeExpander,
                genotypeLikelihoodService,
                matchCalculationService,
                matchProbabilityCalculator,
                logger);
        }

        [Test]
        public async Task CalculateMatchProbability_ReturnsMatchProbability()
        {
            var matchProbabilityInput = new MatchProbabilityInput
            {
                DonorHla = DonorHla,
                PatientHla = PatientHla,
                HlaNomenclatureVersion = HlaNomenclatureVersion
            };

            compressedPhenotypeExpander.ExpandCompressedPhenotype(DonorHla, HlaNomenclatureVersion)
                .Returns(new HashSet<PhenotypeInfo<string>> {DonorHla, PatientHla});

            compressedPhenotypeExpander.ExpandCompressedPhenotype(PatientHla, HlaNomenclatureVersion)
                .Returns(new HashSet<PhenotypeInfo<string>> {PatientHla});

            matchCalculationService.MatchAtPGroupLevel(PatientHla, DonorHla, Arg.Any<string>())
                .Returns(new GenotypeMatchDetails
                    {MatchCounts = new LociInfo<int?> {A = 2, B = 2, C = 2, Dpb1 = null, Dqb1 = 2, Drb1 = 2}});

            matchProbabilityCalculator.CalculateMatchProbability(
                    Arg.Any<HashSet<PhenotypeInfo<string>>>(),
                    Arg.Any<HashSet<PhenotypeInfo<string>>>(),
                    Arg.Any<HashSet<GenotypeMatchDetails>>(),
                    Arg.Any<Dictionary<PhenotypeInfo<string>, decimal>>())
                .Returns(new MatchProbabilityResponse {ZeroMismatchProbability = 0.5m});

            var actualResponse = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            actualResponse.ZeroMismatchProbability.Should().Be(0.5m);
        }

        [Test]
        public async Task CalculateMatchProbability_WhenAllPatientAndDonorGenotypeMatch_ReturnsOneHundredPercentMatchProbability()
        {
            var matchProbabilityInput = new MatchProbabilityInput
            {
                DonorHla = DonorHla,
                PatientHla = PatientHla,
                HlaNomenclatureVersion = HlaNomenclatureVersion
            };

            matchCalculationService.MatchAtPGroupLevel(Arg.Any<PhenotypeInfo<string>>(), Arg.Any<PhenotypeInfo<string>>(), Arg.Any<string>())
                .Returns(new GenotypeMatchDetails
                    {MatchCounts = new LociInfo<int?> {A = 2, B = 2, C = 2, Dpb1 = null, Dqb1 = 2, Drb1 = 2}});

            compressedPhenotypeExpander.ExpandCompressedPhenotype(DonorHla, HlaNomenclatureVersion)
                .Returns(new HashSet<PhenotypeInfo<string>> {DonorHla});

            compressedPhenotypeExpander.ExpandCompressedPhenotype(PatientHla, HlaNomenclatureVersion)
                .Returns(new HashSet<PhenotypeInfo<string>> {PatientHla});

            var actualResponse = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            actualResponse.ZeroMismatchProbability.Should().Be(1m);
        }

        [Test]
        public async Task CalculateMatchProbability_WhenNoTenOutOfTenMatch_ReturnsZeroPercentMatchProbability()
        {
            var matchProbabilityInput = new MatchProbabilityInput
            {
                DonorHla = DonorHla,
                PatientHla = PatientHla,
                HlaNomenclatureVersion = HlaNomenclatureVersion
            };

            compressedPhenotypeExpander.ExpandCompressedPhenotype(DonorHla, HlaNomenclatureVersion)
                .Returns(new HashSet<PhenotypeInfo<string>>());

            compressedPhenotypeExpander.ExpandCompressedPhenotype(PatientHla, HlaNomenclatureVersion)
                .Returns(new HashSet<PhenotypeInfo<string>>());

            var actualResponse = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            actualResponse.ZeroMismatchProbability.Should().Be(0m);
        }

        [TestCase(5, 1, 5)]
        [TestCase(4, 2, 8)]
        [TestCase(3, 3, 9)]
        [TestCase(2, 4, 8)]
        [TestCase(1, 5, 5)]
        public async Task CalculateMatchProbability_CalculatesMatchCountsForEachPatientDonorGenotypePair(
            int numberOfDonorGenotypes,
            int numberOfPatientGenotypes,
            int numberOfPossibleCombinations)
        {
            var matchProbabilityInput = new MatchProbabilityInput
            {
                DonorHla = DonorHla,
                PatientHla = PatientHla,
                HlaNomenclatureVersion = HlaNomenclatureVersion
            };

            compressedPhenotypeExpander.ExpandCompressedPhenotype(PatientHla, HlaNomenclatureVersion)
                .Returns(Enumerable.Range(1, numberOfPatientGenotypes).Select(i => new PhenotypeInfo<string>($"patient${i}")).ToHashSet());

            compressedPhenotypeExpander.ExpandCompressedPhenotype(DonorHla, HlaNomenclatureVersion)
                .Returns(Enumerable.Range(1, numberOfDonorGenotypes).Select(i => new PhenotypeInfo<string>($"donor${i}")).ToHashSet());

            await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            await matchCalculationService.Received(numberOfPossibleCombinations)
                .MatchAtPGroupLevel(Arg.Any<PhenotypeInfo<string>>(), Arg.Any<PhenotypeInfo<string>>(), Arg.Any<string>());
        }
    }
}