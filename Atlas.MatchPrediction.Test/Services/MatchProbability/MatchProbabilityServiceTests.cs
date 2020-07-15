using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.Common.Utils.Models;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchCalculation;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using HaplotypeHla = Atlas.Common.GeneticData.PhenotypeInfo.LociInfo<string>;

namespace Atlas.MatchPrediction.Test.Services.MatchProbability
{
    public class MatchProbabilityServiceTests
    {
        private ICompressedPhenotypeExpander compressedPhenotypeExpander;
        private IGenotypeLikelihoodService genotypeLikelihoodService;
        private IMatchCalculationService matchCalculationService;
        private IMatchProbabilityCalculator matchProbabilityCalculator;
        private IHaplotypeFrequencyService haplotypeFrequencyService;

        private IMatchProbabilityService matchProbabilityService;

        private const string HlaNomenclatureVersion = FileBackedHlaMetadataRepositoryBaseReader.OlderTestHlaVersion;

        private const string PatientLocus = "patientHla";
        private const string DonorLocus = "donorHla";

        private static readonly PhenotypeInfo<string> PatientHla = new PhenotypeInfoBuilder<string>().WithDataAt(Locus.A, PatientLocus).Build();

        private static readonly PhenotypeInfo<string> DonorHla = new PhenotypeInfoBuilder<string>().WithDataAt(Locus.A, DonorLocus).Build();

        [SetUp]
        public void Setup()
        {
            compressedPhenotypeExpander = Substitute.For<ICompressedPhenotypeExpander>();
            genotypeLikelihoodService = Substitute.For<IGenotypeLikelihoodService>();
            matchCalculationService = Substitute.For<IMatchCalculationService>();
            matchProbabilityCalculator = Substitute.For<IMatchProbabilityCalculator>();
            haplotypeFrequencyService = Substitute.For<IHaplotypeFrequencyService>();
            var logger = Substitute.For<ILogger>();

            matchCalculationService.MatchAtPGroupLevel(default, default, default, default, default)
                .ReturnsForAnyArgs(new GenotypeMatchDetails {MatchCounts = new MatchCountsBuilder().ZeroOutOfTen().Build()});

            genotypeLikelihoodService.CalculateLikelihood(default, default, default).Returns(0.5m);

            haplotypeFrequencyService.GetHaplotypeFrequencySets(default, default).ReturnsForAnyArgs(new HaplotypeFrequencySetResponse
            {
                DonorSet = new HaplotypeFrequencySet(),
                PatientSet = new HaplotypeFrequencySet()
            });

            haplotypeFrequencyService.GetAllHaplotypeFrequencies(default).ReturnsForAnyArgs(new Dictionary<LociInfo<string>, decimal>());

            matchProbabilityService = new MatchProbabilityService(
                compressedPhenotypeExpander,
                genotypeLikelihoodService,
                matchCalculationService,
                matchProbabilityCalculator,
                haplotypeFrequencyService,
                logger);
        }

        [Test]
        public async Task CalculateMatchProbability_ReturnsMatchProbability()
        {
            var matchProbabilityInput = new MatchProbabilityInput
            {
                DonorFrequencySetMetadata = new FrequencySetMetadata(),
                PatientFrequencySetMetadata = new FrequencySetMetadata(),
                DonorHla = DonorHla,
                PatientHla = PatientHla,
                HlaNomenclatureVersion = HlaNomenclatureVersion
            };

            compressedPhenotypeExpander.ExpandCompressedPhenotype(DonorHla, HlaNomenclatureVersion)
                .Returns(new HashSet<PhenotypeInfo<string>> {DonorHla, PatientHla});

            compressedPhenotypeExpander.ExpandCompressedPhenotype(PatientHla, HlaNomenclatureVersion)
                .Returns(new HashSet<PhenotypeInfo<string>> {PatientHla});

            matchCalculationService.MatchAtPGroupLevel(PatientHla, DonorHla, Arg.Any<string>(), default, default)
                .Returns(new GenotypeMatchDetails
                {
                    MatchCounts = new MatchCountsBuilder().TenOutOfTen().Build()
                });

            matchProbabilityCalculator.CalculateMatchProbability(
                    default,
                    default,
                    default
                )
                .ReturnsForAnyArgs(new MatchProbabilityResponse {ZeroMismatchProbability = new Probability(0.5m)});

            haplotypeFrequencyService.GetHaplotypeFrequencySets(default, default)
                .ReturnsForAnyArgs(new HaplotypeFrequencySetResponse
                    {
                        PatientSet = new HaplotypeFrequencySet(),
                        DonorSet = new HaplotypeFrequencySet()
                    }
                );

            var actualResponse = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            actualResponse.ZeroMismatchProbability.Decimal.Should().Be(0.5m);
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

            compressedPhenotypeExpander
                .ExpandCompressedPhenotype(PatientHla, HlaNomenclatureVersion, Arg.Any<IReadOnlyCollection<HaplotypeHla>>(), Arg.Any<ISet<Locus>>())
                .Returns(Enumerable.Range(1, numberOfPatientGenotypes)
                    .Select(i => new PhenotypeInfo<string>($"patient${i}")).ToHashSet());

            compressedPhenotypeExpander
                .ExpandCompressedPhenotype(DonorHla, HlaNomenclatureVersion, Arg.Any<IReadOnlyCollection<HaplotypeHla>>(), Arg.Any<ISet<Locus>>())
                .Returns(Enumerable.Range(1, numberOfDonorGenotypes)
                    .Select(i => new PhenotypeInfo<string>($"donor${i}")).ToHashSet());

            await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            await matchCalculationService.Received(numberOfPossibleCombinations).MatchAtPGroupLevel(
                    Arg.Any<PhenotypeInfo<string>>(),
                    Arg.Any<PhenotypeInfo<string>>(),
                    Arg.Any<string>(),
                    Arg.Any<ISet<Locus>>(),
                    Arg.Any<ISet<Locus>>());
        }
    }
}