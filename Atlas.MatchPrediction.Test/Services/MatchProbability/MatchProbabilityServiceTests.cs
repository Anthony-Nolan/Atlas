using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Client.Models.MatchProbability;
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

            genotypeMatcher.PairsWithTenOutOfTenMatch(
                Arg.Any<HashSet<PhenotypeInfo<string>>>(),
                Arg.Any<HashSet<PhenotypeInfo<string>>>(),
                HlaNomenclatureVersion)
                .Returns(new HashSet<Tuple<PhenotypeInfo<string>, PhenotypeInfo<string>>>());

            genotypeLikelihoodService.CalculateLikelihood(Arg.Any<PhenotypeInfo<string>>()).Returns(0.5m);

            matchProbabilityService = new MatchProbabilityService(
                compressedPhenotypeExpander,
                genotypeLikelihoodService,
                genotypeMatcher, 
                matchProbabilityCalculator);
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

            genotypeMatcher.PairsWithTenOutOfTenMatch(
                    Arg.Any<HashSet<PhenotypeInfo<string>>>(),
                    Arg.Any<HashSet<PhenotypeInfo<string>>>(),
                    HlaNomenclatureVersion)
                .Returns(new HashSet<Tuple<PhenotypeInfo<string>, PhenotypeInfo<string>>> 
                    {new Tuple<PhenotypeInfo<string>, PhenotypeInfo<string>>(PatientHla, PatientHla)});

            const decimal probability = 0.5m;

            matchProbabilityCalculator.CalculateMatchProbability(
                    Arg.Any<HashSet<PhenotypeInfo<string>>>(),
                    Arg.Any<HashSet<PhenotypeInfo<string>>>(),
                    Arg.Any<HashSet<Tuple<PhenotypeInfo<string>, PhenotypeInfo<string>>>>(),
                    Arg.Any<Dictionary<PhenotypeInfo<string>, decimal>>())
                .Returns(probability);

            var actualResponse = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            actualResponse.ZeroMismatchProbability.Should().Be(probability);
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
                .Returns(new HashSet<PhenotypeInfo<string>>{DonorGenotype});

            compressedPhenotypeExpander.ExpandCompressedPhenotype(PatientHla, HlaNomenclatureVersion)
                .Returns(new HashSet<PhenotypeInfo<string>>{PatientHla});

            var actualResponse = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            actualResponse.ZeroMismatchProbability.Should().Be(0m);
        }
    }
}