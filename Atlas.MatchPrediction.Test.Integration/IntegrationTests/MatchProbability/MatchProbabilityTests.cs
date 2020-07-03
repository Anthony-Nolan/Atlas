using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.FrequencySetFile;
using FluentAssertions;
using LochNessBuilder;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchProbability
{
    public class MatchProbabilityTests
    {
        private IMatchProbabilityService matchProbabilityService;
        private IFrequencySetService importService;

        private const string HlaNomenclatureVersion = Constants.SnapshotHlaNomenclatureVersion;

        private const string A1 = "02:09";
        private const string A2 = "02:66";
        private const string B1 = "08:182";
        private const string B2 = "15:146";
        private const string C1 = "01:03";
        private const string C2 = "03:05";
        private const string Dqb11 = "03:09";
        private const string Dqb12 = "02:04";
        private const string Drb11 = "03:124";
        private const string Drb12 = "11:129";

        private const string GGroupA1 = "02:01:01G";
        private const string GGroupA2 = "02:01:01G";
        private const string GGroupB1 = "15:01:01G";
        private const string GGroupB2 = "15:01:01G";
        private const string GGroupC1 = "01:03:01G";
        private const string GGroupC2 = "03:05:01G";
        private const string GGroupDqb11 = "03:01:01G";
        private const string GGroupDqb12 = "02:01:01G";
        private const string GGroupDrb11 = "03:01:01G";
        private const string GGroupDrb12 = "11:06:01G";

        [SetUp]
        public void SetUp()
        {
            matchProbabilityService = DependencyInjection.DependencyInjection.Provider.GetService<IMatchProbabilityService>();
            importService = DependencyInjection.DependencyInjection.Provider.GetService<IFrequencySetService>();
        }

        [Test]
        public async Task CalculateMatchProbability_WhenIdenticalGenotypes_OneHundredPercentProbability()
        {
            var matchProbabilityInput = new MatchProbabilityInput
            {
                PatientHla = NewHla,
                DonorHla = NewHla,
                HlaNomenclatureVersion = HlaNomenclatureVersion
            };

            var expectedProbabilityPerLocus = new LociInfo<decimal?> {A = 1, B = 1, C = 1, Dpb1 = null, Dqb1 = 1, Drb1 = 1};

            var matchDetails = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.ZeroMismatchProbability.Should().Be(1m);
            matchDetails.ZeroMismatchProbabilityPerLocus.Should().Be(expectedProbabilityPerLocus);
        }

        [Test]
        public async Task CalculateMatchProbability_WhenGenotypesAreNonMatching_ZeroPercentProbability()
        {
            var donorHla = NewHla.With(h => h.A, new LocusInfo<string> {Position1 = "23:17", Position2 = "23:18"});

            var matchProbabilityInput = new MatchProbabilityInput
            {
                PatientHla = NewHla,
                DonorHla = donorHla,
                HlaNomenclatureVersion = HlaNomenclatureVersion
            };

            var expectedProbabilityPerLocus = new LociInfo<decimal?> {A = 0, B = 0, C = 0, Dpb1 = null, Dqb1 = 0, Drb1 = 0};

            var matchDetails = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.ZeroMismatchProbability.Should().Be(0m);
            matchDetails.ZeroMismatchProbabilityPerLocus.Should().Be(expectedProbabilityPerLocus);
        }

        [Test]
        public async Task CalculateMatchProbability_WhenAmbiguousHla_ReturnsProbability()
        {
            var allPossibleHaplotypes = new List<HaplotypeFrequency>
            {
                new HaplotypeFrequency {A = GGroupA1, B = "15:12:01G", C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb12, Frequency = 0.10m},
                new HaplotypeFrequency {A = GGroupA1, B = "15:12:01G", C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb12, Frequency = 0.09m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB2, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb12, Frequency = 0.08m},
                new HaplotypeFrequency {A = GGroupA2, B = GGroupB2, C = GGroupC2, DQB1 = GGroupDqb12, DRB1 = GGroupDrb11, Frequency = 0.07m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB2, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.06m},
                new HaplotypeFrequency {A = GGroupA2, B = GGroupB2, C = GGroupC2, DQB1 = GGroupDqb12, DRB1 = GGroupDrb12, Frequency = 0.05m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB2, C = GGroupC1, DQB1 = GGroupDqb12, DRB1 = GGroupDrb12, Frequency = 0.04m},
                new HaplotypeFrequency {A = GGroupA2, B = GGroupB2, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.03m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB2, C = GGroupC1, DQB1 = GGroupDqb12, DRB1 = GGroupDrb11, Frequency = 0.02m},
                new HaplotypeFrequency {A = GGroupA2, B = GGroupB2, C = GGroupC2, DQB1 = GGroupDqb11, DRB1 = GGroupDrb12, Frequency = 0.01m}
            };

            await ImportFrequencies(allPossibleHaplotypes);

            var patientHla = NewHla.With(h => h.B, new LocusInfo<string> {Position1 = "15:12/146", Position2 = B2});
            var donorHla = NewHla.With(h => h.B, new LocusInfo<string> {Position1 = "15:12", Position2 = B2});

            var matchProbabilityInput = new MatchProbabilityInput
            {
                PatientHla = patientHla,
                DonorHla = donorHla,
                HlaNomenclatureVersion = HlaNomenclatureVersion
            };

            var expectedProbabilityPerLocus = new LociInfo<decimal?> {A = 1, B = 0.4430379746835443037974683544m, C = 1, Dpb1 = null, Dqb1 = 1, Drb1 = 1};

            var matchDetails = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.ZeroMismatchProbability.Should().Be(0.4430379746835443037974683544m);
            matchDetails.ZeroMismatchProbabilityPerLocus.Should().Be(expectedProbabilityPerLocus);
        }

        private static Builder<PhenotypeInfo<string>> NewHla => Builder<PhenotypeInfo<string>>.New
            .With(h => h.A, new LocusInfo<string> {Position1 = A1, Position2 = A2})
            .With(h => h.B, new LocusInfo<string> {Position1 = B1, Position2 = B2})
            .With(h => h.C, new LocusInfo<string> {Position1 = C1, Position2 = C2})
            .With(h => h.Dpb1, new LocusInfo<string> {Position1 = null, Position2 = null })
            .With(h => h.Dqb1, new LocusInfo<string> {Position1 = Dqb11, Position2 = Dqb12})
            .With(h => h.Drb1, new LocusInfo<string> {Position1 = Drb11, Position2 = Drb12});

        private async Task ImportFrequencies(IEnumerable<HaplotypeFrequency> haplotypes)
        {
            using var file = FrequencySetFileBuilder.New(haplotypes).Build();
            await importService.ImportFrequencySet(file);
        }
    }
}
