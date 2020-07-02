using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
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
        private IGenotypeMatcher genotypeMatcher;
        private IMatchProbabilityCalculator matchProbabilityCalculator;

        private IMatchProbabilityService matchProbabilityService;

        private const string HlaNomenclatureVersion = "3330";

        private const string PatientLocus = "patientHla";
        private const string DonorLocus = "donorHla";

        private static readonly PhenotypeInfo<string> PatientHla = PhenotypeInfoBuilder.New
            .With(d => d.A, new LocusInfo<string> {Position1 = PatientLocus, Position2 = PatientLocus}).Build();


        private static readonly PhenotypeInfo<string> DonorGenotype = PhenotypeInfoBuilder.New
            .With(d => d.A, new LocusInfo<string> {Position1 = DonorLocus, Position2 = DonorLocus}).Build();

        [SetUp]
        public void Setup()
        {
            compressedPhenotypeExpander = Substitute.For<ICompressedPhenotypeExpander>();
            genotypeLikelihoodService = Substitute.For<IGenotypeLikelihoodService>();
            genotypeMatcher = Substitute.For<IGenotypeMatcher>();
            matchProbabilityCalculator = Substitute.For<IMatchProbabilityCalculator>();
            var logger = Substitute.For<ILogger>();

            genotypeMatcher.PairsWithMatch(
                Arg.Any<HashSet<PhenotypeInfo<string>>>(),
                Arg.Any<HashSet<PhenotypeInfo<string>>>(),
                HlaNomenclatureVersion)
                .Returns(new HashSet<GenotypeMatchDetails>());

            genotypeLikelihoodService.CalculateLikelihood(Arg.Any<PhenotypeInfo<string>>()).Returns(0.5m);

            matchProbabilityService = new MatchProbabilityService(
                compressedPhenotypeExpander,
                genotypeLikelihoodService,
                genotypeMatcher,
                matchProbabilityCalculator,
                logger);
        }

        [Test]
        public async Task CalculateMatchProbability_ReturnsMatchProbability()
        {
            var matchProbabilityInput = new MatchProbabilityInput
            {
                DonorHla = DonorGenotype,
                PatientHla = PatientHla,
                HlaNomenclatureVersion = HlaNomenclatureVersion
            };

            compressedPhenotypeExpander.ExpandCompressedPhenotype(DonorGenotype, HlaNomenclatureVersion)
                .Returns(new HashSet<PhenotypeInfo<string>> {DonorGenotype, PatientHla});

            compressedPhenotypeExpander.ExpandCompressedPhenotype(PatientHla, HlaNomenclatureVersion)
                .Returns(new HashSet<PhenotypeInfo<string>> {PatientHla});

            genotypeMatcher.PairsWithMatch(
                    Arg.Any<HashSet<PhenotypeInfo<string>>>(),
                    Arg.Any<HashSet<PhenotypeInfo<string>>>(),
                    HlaNomenclatureVersion)
                .Returns(new HashSet<GenotypeMatchDetails> 
                    {new GenotypeMatchDetails
                    {
                        PatientGenotype = PatientHla,
                        DonorGenotype = PatientHla,
                        MatchCounts = new LociInfo<int?> {A = 2, B = 2, C = 2, Dpb1 = null, Dqb1 = 2, Drb1 = 2}
                    }});

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
                DonorHla = DonorGenotype,
                PatientHla = PatientHla,
                HlaNomenclatureVersion = HlaNomenclatureVersion
            };

            compressedPhenotypeExpander.ExpandCompressedPhenotype(DonorGenotype, HlaNomenclatureVersion)
                .Returns(new HashSet<PhenotypeInfo<string>>());

            compressedPhenotypeExpander.ExpandCompressedPhenotype(PatientHla, HlaNomenclatureVersion)
                .Returns(new HashSet<PhenotypeInfo<string>>());

            var actualResponse = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            actualResponse.ZeroMismatchProbability.Should().Be(1m);
        }

        [Test]
        public async Task CalculateMatchProbability_WhenNoPatientAndDonorGenotypeMatch_ReturnsZeroPercentMatchProbability()
        {
            var matchProbabilityInput = new MatchProbabilityInput
            {
                DonorHla = DonorGenotype,
                PatientHla = PatientHla,
                HlaNomenclatureVersion = HlaNomenclatureVersion
            };

            compressedPhenotypeExpander.ExpandCompressedPhenotype(DonorGenotype, HlaNomenclatureVersion)
                .Returns(new HashSet<PhenotypeInfo<string>> {DonorGenotype});

            compressedPhenotypeExpander.ExpandCompressedPhenotype(PatientHla, HlaNomenclatureVersion)
                .Returns(new HashSet<PhenotypeInfo<string>> {PatientHla});

            var actualResponse = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            actualResponse.ZeroMismatchProbability.Should().Be(0m);
        }
    }
}