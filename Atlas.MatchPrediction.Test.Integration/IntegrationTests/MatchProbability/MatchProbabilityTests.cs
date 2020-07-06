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
        private const string A2 = "11:03";
        private const string B1 = "15:12";
        private const string B2 = "08:182";
        private const string C1 = "01:03";
        private const string C2 = "03:05";
        private const string Dqb11 = "03:09";
        private const string Dqb12 = "02:04";
        private const string Drb11 = "03:124";
        private const string Drb12 = "11:129";

        private const string GGroupA1 = "02:01:01G";
        private const string GGroupA2 = "11:03:01G";
        private const string GGroupB1 = "15:12:01G";
        private const string GGroupB2 = "08:01:01G";
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
                new HaplotypeFrequency {A = "01:01:01G", B = GGroupB1, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.7m},
                new HaplotypeFrequency {A = GGroupA1, B = "08:01:01G", C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.6m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB1, C = "04:01:01G", DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.5m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB1, C = GGroupC1, DQB1 = "06:04:01G", DRB1 = GGroupDrb11, Frequency = 0.4m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB1, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = "11:06:01G", Frequency = 0.3m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB1, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.2m},
                new HaplotypeFrequency {A = GGroupA2, B = GGroupB2, C = GGroupC2, DQB1 = GGroupDqb12, DRB1 = GGroupDrb12, Frequency = 0.1m}
            };

            await ImportFrequencies(allPossibleHaplotypes);

            var patientHla = NewHla
                .With(h => h.A, new LocusInfo<string> {Position1 = $"{A1}/01:37", Position2 = A2})
                .With(h => h.B, new LocusInfo<string> {Position1 = $"{B1}/08:182", Position2 = B2})
                .With(h => h.C, new LocusInfo<string> {Position1 = $"{C1}/04:82", Position2 = C2})
                .With(h => h.Dqb1, new LocusInfo<string> {Position1 = $"{Dqb11}/06:39", Position2 = Dqb12})
                .With(h => h.Drb1, new LocusInfo<string> {Position1 = $"{Drb11}/11:129", Position2 = Drb12});

            var donorHla = NewHla;

            var matchProbabilityInput = new MatchProbabilityInput
            {
                PatientHla = patientHla,
                DonorHla = donorHla,
                HlaNomenclatureVersion = HlaNomenclatureVersion
            };

            var expectedProbabilityPerLocus = new LociInfo<decimal?>
            {
                A = 0.7407407407407407407407407407m,
                B = 0.7777777777777777777777777778m,
                C = 0.8148148148148148148148148148m,
                Dpb1 = null,
                Dqb1 = 0.8518518518518518518518518519m, 
                Drb1 = 0.8888888888888888888888888889m
            };

            var matchDetails = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.ZeroMismatchProbability.Should().Be(0.0740740740740740740740740741m);
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
