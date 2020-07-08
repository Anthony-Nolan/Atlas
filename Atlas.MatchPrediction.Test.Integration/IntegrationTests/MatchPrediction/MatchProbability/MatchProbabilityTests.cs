using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Test.Integration.Resources;
using Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.FrequencySetFile;
using FluentAssertions;
using LochNessBuilder;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

// ReSharper disable InconsistentNaming - want to avoid calling "G groups" "gGroup", as "g" groups are a distinct thing 

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.MatchProbability
{
    public class MatchProbabilityTests
    {
        private IMatchProbabilityService matchProbabilityService;
        private IHaplotypeFrequencyService importService;

        private const string HlaNomenclatureVersion = Constants.SnapshotHlaNomenclatureVersion;

        private static readonly string GGroupA1 = Alleles.UnambiguousAlleleDetails.A.Position1.GGroup;
        private static readonly string GGroupA2 = Alleles.UnambiguousAlleleDetails.A.Position2.GGroup;
        private static readonly string GGroupB1 = Alleles.UnambiguousAlleleDetails.B.Position1.GGroup;
        private static readonly string GGroupB2 = Alleles.UnambiguousAlleleDetails.B.Position2.GGroup;
        private static readonly string GGroupC1 = Alleles.UnambiguousAlleleDetails.C.Position1.GGroup;
        private static readonly string GGroupC2 = Alleles.UnambiguousAlleleDetails.C.Position2.GGroup;
        private static readonly string GGroupDqb11 = Alleles.UnambiguousAlleleDetails.Dqb1.Position1.GGroup;
        private static readonly string GGroupDqb12 = Alleles.UnambiguousAlleleDetails.Dqb1.Position2.GGroup;
        private static readonly string GGroupDrb11 = Alleles.UnambiguousAlleleDetails.Drb1.Position1.GGroup;
        private static readonly string GGroupDrb12 = Alleles.UnambiguousAlleleDetails.Drb1.Position2.GGroup;
        
        private readonly string DefaultRegistryCode = "default-registry-code";
        private readonly string DefaultEthnicityCode = "default-ethnicity-code";

        [SetUp]
        public void SetUp()
        {
            matchProbabilityService = DependencyInjection.DependencyInjection.Provider.GetService<IMatchProbabilityService>();
            importService = DependencyInjection.DependencyInjection.Provider.GetService<IHaplotypeFrequencyService>();
        }

        [Test]
        public async Task CalculateMatchProbability_WhenIdenticalGenotypes_OneHundredPercentProbability()
        {
            var matchProbabilityInput = new MatchProbabilityInput
            {
                PatientHla = DefaultUnambiguousAllelesBuilder.Build(),
                DonorHla = DefaultUnambiguousAllelesBuilder.Build(),
                HlaNomenclatureVersion = HlaNomenclatureVersion
            };

            var expectedProbabilityPerLocus = new LociInfo<decimal?> {A = 1, B = 1, C = 1, Dpb1 = null, Dqb1 = 1, Drb1 = 1};

            var matchDetails = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.ZeroMismatchProbability.Decimal.Should().Be(1m);
            matchDetails.OneMismatchProbability.Decimal.Should().Be(0m);
            matchDetails.TwoMismatchProbability.Decimal.Should().Be(0m);
            matchDetails.ZeroMismatchProbabilityPerLocus.ToDecimals().Should().Be(expectedProbabilityPerLocus);
        }

        [Test]
        public async Task CalculateMatchProbability_WhenGenotypesAreNonMatching_ZeroPercentProbability()
        {
            const string alleleStringA1 = "23:17";
            const string alleleStringA2 = "23:18";

            var possibleHaplotypes = new List<HaplotypeFrequency>
            {
                DefaultHaplotypeFrequency1.With(h => h.Frequency, 0.00002m).Build(),
                DefaultHaplotypeFrequency2.With(h => h.Frequency, 0.00001m).Build(),
                DefaultHaplotypeFrequency1.With(h => h.A, alleleStringA1).With(h => h.Frequency, 0.00002m).Build(),
                DefaultHaplotypeFrequency2.With(h => h.A, alleleStringA2).With(h => h.Frequency, 0.00001m).Build()
            };

            await ImportFrequencies(possibleHaplotypes);


            var patientHla = DefaultUnambiguousAllelesBuilder.WithDataAt(
                Locus.A,
                Alleles.UnambiguousAlleleDetails.A.Position1.Allele,
                Alleles.UnambiguousAlleleDetails.A.Position2.Allele)
                .Build();
            var donorHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, alleleStringA1, alleleStringA2).Build();

            var matchProbabilityInput = new MatchProbabilityInput
            {
                PatientHla = patientHla,
                DonorHla = donorHla,
                HlaNomenclatureVersion = HlaNomenclatureVersion,
                DonorFrequencySetMetadata = new FrequencySetMetadata { EthnicityCode = DefaultEthnicityCode, RegistryCode = DefaultRegistryCode },
                PatientFrequencySetMetadata = new FrequencySetMetadata { EthnicityCode = DefaultEthnicityCode, RegistryCode = DefaultRegistryCode }
            };

            var expectedProbabilityPerLocus = new LociInfo<decimal?> {A = 0, B = 0, C = 0, Dpb1 = null, Dqb1 = 0, Drb1 = 0};

            var matchDetails = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.ZeroMismatchProbability.Decimal.Should().Be(0m);
            matchDetails.OneMismatchProbability.Decimal.Should().Be(0m);
            matchDetails.TwoMismatchProbability.Decimal.Should().Be(0m);
            matchDetails.ZeroMismatchProbabilityPerLocus.ToDecimals().Should().Be(expectedProbabilityPerLocus);
        }

        [Test]
        public async Task CalculateMatchProbability_WhenAmbiguousHla_ReturnsProbability()
        {
            const string alleleStringA = "01:37";
            const string GGroupA = "01:01:01G";
            const string anotherAlleleStringA = "23:17";
            const string anotherGGroupA = "23:01:01G";

            const string alleleStringB = "08:182";
            const string GGroupB = "08:01:01G";

            const string alleleStringC = "04:82";
            const string GGroupC = "04:01:01G";

            const string alleleStringDqb1 = "06:39";
            const string GGroupDqb1 = "06:04:01G";

            const string alleleStringDrb1 = "11:129";
            const string GGroupDrb1 = "11:06:01G";
            
            var possibleHaplotypes = new List<HaplotypeFrequency>
            {
                DefaultHaplotypeFrequency2.With(h => h.A, anotherGGroupA).With(h => h.Frequency, 0.00008m).Build(),
                DefaultHaplotypeFrequency1.With(h => h.A, GGroupA).With(h => h.Frequency, 0.00007m).Build(),
                DefaultHaplotypeFrequency1.With(h => h.B, GGroupB).With(h => h.Frequency, 0.00006m).Build(),
                DefaultHaplotypeFrequency1.With(h => h.C, GGroupC).With(h => h.Frequency, 0.00005m).Build(),
                DefaultHaplotypeFrequency1.With(h => h.DQB1, GGroupDqb1).With(h => h.Frequency, 0.00004m).Build(),
                DefaultHaplotypeFrequency1.With(h => h.DRB1, GGroupDrb1).With(h => h.Frequency, 0.00003m).Build(),
                DefaultHaplotypeFrequency1.With(h => h.Frequency, 0.00002m).Build(),
                DefaultHaplotypeFrequency2.With(h => h.Frequency, 0.00001m).Build()
            };

            await ImportFrequencies(possibleHaplotypes);

            var patientHla = DefaultUnambiguousAllelesBuilder
                    .WithDataAt(
                        Locus.A,
                        $"{Alleles.UnambiguousAlleleDetails.A.Position1.Allele}/{alleleStringA}",
                        $"{Alleles.UnambiguousAlleleDetails.A.Position2.Allele}/{anotherAlleleStringA}")
                    .WithDataAt(Locus.B, LocusPosition.One, $"{Alleles.UnambiguousAlleleDetails.B.Position1.Allele}/{alleleStringB}")
                    .WithDataAt(Locus.C, LocusPosition.One, $"{Alleles.UnambiguousAlleleDetails.C.Position1.Allele}/{alleleStringC}")
                    .WithDataAt(Locus.Dqb1, LocusPosition.One, $"{Alleles.UnambiguousAlleleDetails.Dqb1.Position1.Allele}/{alleleStringDqb1}")
                    .WithDataAt(Locus.Drb1, LocusPosition.One, $"{Alleles.UnambiguousAlleleDetails.Drb1.Position1.Allele}/{alleleStringDrb1}")
                    .Build();

            var matchProbabilityInput = new MatchProbabilityInput
            {
                PatientHla = patientHla,
                DonorHla = DefaultUnambiguousAllelesBuilder.Build(),
                HlaNomenclatureVersion = HlaNomenclatureVersion,
                DonorFrequencySetMetadata = new FrequencySetMetadata { EthnicityCode = DefaultEthnicityCode, RegistryCode = DefaultRegistryCode},
                PatientFrequencySetMetadata = new FrequencySetMetadata { EthnicityCode = DefaultEthnicityCode, RegistryCode = DefaultRegistryCode}
            };

            var expectedProbabilityPerLocus = new LociInfo<decimal?>
            {
                A = 0.0823045267489711934156378601m,
                B = 0.7777777777777777777777777778m,
                C = 0.8148148148148148148148148148m,
                Dpb1 = null,
                Dqb1 = 0.8518518518518518518518518519m, 
                Drb1 = 0.8888888888888888888888888889m
            };

            var matchDetails = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.ZeroMismatchProbability.Decimal.Should().Be(0.008230452674897119341563786m);
            matchDetails.OneMismatchProbability.Decimal.Should().Be(0.1687242798353909465020576132m);
            matchDetails.TwoMismatchProbability.Decimal.Should().Be(0.8230452674897119341563786008m);
            matchDetails.ZeroMismatchProbabilityPerLocus.ToDecimals().Should().Be(expectedProbabilityPerLocus);
        }

        private async Task ImportFrequencies(IEnumerable<HaplotypeFrequency> haplotypes)
        {
            using var file = FrequencySetFileBuilder.New(DefaultRegistryCode, DefaultEthnicityCode, haplotypes).Build();
            await importService.ImportFrequencySet(file);
        }

        private static PhenotypeInfoBuilder<string> DefaultUnambiguousAllelesBuilder =>
            new PhenotypeInfoBuilder<string>(Alleles.UnambiguousAlleleDetails.Alleles());

        private static Builder<HaplotypeFrequency> DefaultHaplotypeFrequency1 => Builder<HaplotypeFrequency>.New
            .With(r => r.A, GGroupA1)
            .With(r => r.B, GGroupB1)
            .With(r => r.C, GGroupC1)
            .With(r => r.DQB1, GGroupDqb11)
            .With(r => r.DRB1, GGroupDrb11);

        public Builder<HaplotypeFrequency> DefaultHaplotypeFrequency2 => Builder<HaplotypeFrequency>.New
            .With(r => r.A, GGroupA2)
            .With(r => r.B, GGroupB2)
            .With(r => r.C, GGroupC2)
            .With(r => r.DQB1, GGroupDqb12)
            .With(r => r.DRB1, GGroupDrb12);
    }
}
