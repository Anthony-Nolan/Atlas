using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Test.Integration.Resources;
using FluentAssertions;
using NUnit.Framework;

// ReSharper disable InconsistentNaming - want to avoid calling "G groups" "gGroup", as "g" groups are a distinct thing 

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.MatchProbability
{
    public class MatchProbabilityTests : MatchProbabilityTestsBase
    {
        [Test]
        public async Task CalculateMatchProbability_WhenIdenticalGenotypes_UnrepresentedInFrequencySet_ReturnsZeroPercent()
        {
            var matchProbabilityInput = new MatchProbabilityInput
            {
                PatientHla = DefaultUnambiguousAllelesBuilder.Build(),
                DonorHla = DefaultUnambiguousAllelesBuilder.Build(),
                HlaNomenclatureVersion = HlaNomenclatureVersion
            };

            await ImportFrequencies(new List<HaplotypeFrequency> {Builder<HaplotypeFrequency>.New.Build()}, null, null);

            var expectedProbabilityPerLocus = new LociInfo<decimal?> {A = 0, B = 0, C = 0, Dpb1 = null, Dqb1 = 0, Drb1 = 0};

            var matchDetails = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.ZeroMismatchProbability.Decimal.Should().Be(0m);
            matchDetails.OneMismatchProbability.Decimal.Should().Be(0m);
            matchDetails.TwoMismatchProbability.Decimal.Should().Be(0m);
            matchDetails.ZeroMismatchProbabilityPerLocus.ToDecimals().Should().Be(expectedProbabilityPerLocus);
        }
        
        [Test]
        public async Task CalculateMatchProbability_WhenIdenticalGenotypes_RepresentedInFrequencySet_ReturnsOneHundredPercent()
        {
            var matchProbabilityInput = new MatchProbabilityInput
            {
                PatientHla = DefaultUnambiguousAllelesBuilder.Build(),
                DonorHla = DefaultUnambiguousAllelesBuilder.Build(),
                HlaNomenclatureVersion = HlaNomenclatureVersion
            };

            var possibleHaplotypes = new List<HaplotypeFrequency>
            {
                DefaultHaplotypeFrequency1.With(h => h.Frequency, 0.00002m).Build(),
                DefaultHaplotypeFrequency2.With(h => h.Frequency, 0.00001m).Build(),
            };
            
            await ImportFrequencies(possibleHaplotypes, null, null);

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

            await ImportFrequencies(possibleHaplotypes, DefaultEthnicityCode, DefaultRegistryCode);

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
                DonorFrequencySetMetadata = new FrequencySetMetadata {EthnicityCode = DefaultEthnicityCode, RegistryCode = DefaultRegistryCode},
                PatientFrequencySetMetadata = new FrequencySetMetadata {EthnicityCode = DefaultEthnicityCode, RegistryCode = DefaultRegistryCode}
            };

            var expectedProbabilityPerLocus = new LociInfo<decimal?> {A = 0, B = 0, C = 0, Dpb1 = null, Dqb1 = 0, Drb1 = 0};

            var matchDetails = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.ZeroMismatchProbability.Decimal.Should().Be(0m);
            matchDetails.OneMismatchProbability.Decimal.Should().Be(0m);
            matchDetails.TwoMismatchProbability.Decimal.Should().Be(0m);
            matchDetails.ZeroMismatchProbabilityPerLocus.ToDecimals().Should().Be(expectedProbabilityPerLocus);
        }

        // Warning - this test takes a long time to run (~30s)
        // TODO: ATLAS-400 - can this be quicker?
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

            await ImportFrequencies(possibleHaplotypes, DefaultEthnicityCode, DefaultRegistryCode);

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
                DonorFrequencySetMetadata = new FrequencySetMetadata {EthnicityCode = DefaultEthnicityCode, RegistryCode = DefaultRegistryCode},
                PatientFrequencySetMetadata = new FrequencySetMetadata {EthnicityCode = DefaultEthnicityCode, RegistryCode = DefaultRegistryCode}
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
        
        [Test]
        public async Task CalculateMatchProbability_WhenUsingSpecificHaplotypeSet_ReturnsProbability()
        {
            const string alleleStringA = "01:37";
            const string GGroupA = "01:01:01G";
            const string alleleStringB = "08:182";
            const string GGroupB = "08:01:01G";
            const string alleleStringC = "04:82";
            const string GGroupC = "04:01:01G";
            const string alleleStringDqb1 = "06:39";
            const string GGroupDqb1 = "06:04:01G";
            const string alleleStringDrb1 = "11:129";
            const string GGroupDrb1 = "11:06:01G";

            const string specificRegistryCode = "specific-registry-code";
            const string specificEthnicityCode = "specific-ethnicity-code";

            var genericHaplotypeSet = new List<HaplotypeFrequency>
            {
                new HaplotypeFrequency {A = GGroupA, B = GGroupB1, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.9m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.9m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB1, C = GGroupC, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.9m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB1, C = GGroupC1, DQB1 = GGroupDqb1, DRB1 = GGroupDrb11, Frequency = 0.9m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB1, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb1, Frequency = 0.9m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB1, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.9m},
                new HaplotypeFrequency {A = GGroupA2, B = GGroupB2, C = GGroupC2, DQB1 = GGroupDqb12, DRB1 = GGroupDrb12, Frequency = 0.9m}
            };
            
            await ImportFrequencies(genericHaplotypeSet, DefaultEthnicityCode, DefaultRegistryCode);
            
            var specificHaplotypeSet = new List<HaplotypeFrequency>
            {
                new HaplotypeFrequency {A = GGroupA, B = GGroupB1, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.1m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.1m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB1, C = GGroupC, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.1m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB1, C = GGroupC1, DQB1 = GGroupDqb1, DRB1 = GGroupDrb11, Frequency = 0.1m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB1, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb1, Frequency = 0.1m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB1, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.1m},
                new HaplotypeFrequency {A = GGroupA2, B = GGroupB2, C = GGroupC2, DQB1 = GGroupDqb12, DRB1 = GGroupDrb12, Frequency = 0.1m}
            };

            await ImportFrequencies(specificHaplotypeSet, specificEthnicityCode, specificRegistryCode);

            var patientHla = DefaultUnambiguousAllelesBuilder
                    .WithDataAt(Locus.A, LocusPosition.One, $"{Alleles.UnambiguousAlleleDetails.A.Position1.Allele}/{alleleStringA}")
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
                DonorFrequencySetMetadata = new FrequencySetMetadata { EthnicityCode = specificEthnicityCode, RegistryCode = specificRegistryCode},
                PatientFrequencySetMetadata = new FrequencySetMetadata { EthnicityCode = specificEthnicityCode, RegistryCode = specificRegistryCode}
            };

            var matchDetails = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.ZeroMismatchProbability.Should().Be(0.1666666666666666666666666667m);
        }
        
        [Test]
        public async Task CalculateMatchProbability_WhenUsingRegistryButNotEthnicityMatchedHaplotypeSet_ReturnsProbability()
        {
            const string alleleStringA = "01:37";
            const string GGroupA = "01:01:01G";
            const string alleleStringB = "08:182";
            const string GGroupB = "08:01:01G";
            const string alleleStringC = "04:82";
            const string GGroupC = "04:01:01G";
            const string alleleStringDqb1 = "06:39";
            const string GGroupDqb1 = "06:04:01G";
            const string alleleStringDrb1 = "11:129";
            const string GGroupDrb1 = "11:06:01G";

            const string specificRegistryCode = "specific-registry-code";
            const string specificEthnicityCode = "specific-ethnicity-code";

            var genericHaplotypeSet = new List<HaplotypeFrequency>
            {
                new HaplotypeFrequency {A = GGroupA, B = GGroupB1, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.9m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.9m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB1, C = GGroupC, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.9m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB1, C = GGroupC1, DQB1 = GGroupDqb1, DRB1 = GGroupDrb11, Frequency = 0.9m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB1, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb1, Frequency = 0.9m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB1, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.9m},
                new HaplotypeFrequency {A = GGroupA2, B = GGroupB2, C = GGroupC2, DQB1 = GGroupDqb12, DRB1 = GGroupDrb12, Frequency = 0.9m}
            };
            
            await ImportFrequencies(genericHaplotypeSet, DefaultEthnicityCode, DefaultRegistryCode);
            
            var genericEthnicitySet = new List<HaplotypeFrequency>
            {
                new HaplotypeFrequency {A = GGroupA, B = GGroupB1, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.1m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.1m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB1, C = GGroupC, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.1m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB1, C = GGroupC1, DQB1 = GGroupDqb1, DRB1 = GGroupDrb11, Frequency = 0.1m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB1, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb1, Frequency = 0.1m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB1, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.1m},
                new HaplotypeFrequency {A = GGroupA2, B = GGroupB2, C = GGroupC2, DQB1 = GGroupDqb12, DRB1 = GGroupDrb12, Frequency = 0.1m}
            };

            await ImportFrequencies(genericEthnicitySet, null, specificRegistryCode);
            
            var specificEthnicitySet = new List<HaplotypeFrequency>
            {
                new HaplotypeFrequency {A = GGroupA, B = GGroupB1, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.9m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.9m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB1, C = GGroupC, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.9m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB1, C = GGroupC1, DQB1 = GGroupDqb1, DRB1 = GGroupDrb11, Frequency = 0.9m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB1, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb1, Frequency = 0.9m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB1, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.9m},
                new HaplotypeFrequency {A = GGroupA2, B = GGroupB2, C = GGroupC2, DQB1 = GGroupDqb12, DRB1 = GGroupDrb12, Frequency = 0.9m}
            };

            await ImportFrequencies(specificEthnicitySet, specificEthnicityCode, specificRegistryCode);

            var patientHla = DefaultUnambiguousAllelesBuilder
                    .WithDataAt(Locus.A, LocusPosition.One, $"{Alleles.UnambiguousAlleleDetails.A.Position1.Allele}/{alleleStringA}")
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
                DonorFrequencySetMetadata = new FrequencySetMetadata { EthnicityCode = "unrepresented-donor-code", RegistryCode = specificRegistryCode},
                PatientFrequencySetMetadata = new FrequencySetMetadata { EthnicityCode = "unrepresented-patient-code", RegistryCode = specificRegistryCode}
            };

            var matchDetails = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.ZeroMismatchProbability.Should().Be(0.1666666666666666666666666667m);
        }
        
                [Test]
        public async Task CalculateMatchProbability_WhenUsingRegistryAndEthnicityCodesAreNotRepresented_ReturnsProbability()
        {
            const string alleleStringA = "01:37";
            const string GGroupA = "01:01:01G";
            const string alleleStringB = "08:182";
            const string GGroupB = "08:01:01G";
            const string alleleStringC = "04:82";
            const string GGroupC = "04:01:01G";
            const string alleleStringDqb1 = "06:39";
            const string GGroupDqb1 = "06:04:01G";
            const string alleleStringDrb1 = "11:129";
            const string GGroupDrb1 = "11:06:01G";

            const string specificRegistryCode = "specific-registry-code";
            const string specificEthnicityCode = "specific-ethnicity-code";

            var globalHaplotypeSet = new List<HaplotypeFrequency>
            {
                new HaplotypeFrequency {A = GGroupA, B = GGroupB1, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.1m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.1m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB1, C = GGroupC, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.1m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB1, C = GGroupC1, DQB1 = GGroupDqb1, DRB1 = GGroupDrb11, Frequency = 0.1m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB1, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb1, Frequency = 0.1m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB1, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.1m},
                new HaplotypeFrequency {A = GGroupA2, B = GGroupB2, C = GGroupC2, DQB1 = GGroupDqb12, DRB1 = GGroupDrb12, Frequency = 0.1m}
            };
            
            await ImportFrequencies(globalHaplotypeSet, null, null);
            
            var genericEthnicitySet = new List<HaplotypeFrequency>
            {
                new HaplotypeFrequency {A = GGroupA, B = GGroupB1, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.9m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.9m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB1, C = GGroupC, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.9m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB1, C = GGroupC1, DQB1 = GGroupDqb1, DRB1 = GGroupDrb11, Frequency = 0.9m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB1, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb1, Frequency = 0.9m},
                new HaplotypeFrequency {A = GGroupA1, B = GGroupB1, C = GGroupC1, DQB1 = GGroupDqb11, DRB1 = GGroupDrb11, Frequency = 0.9m},
                new HaplotypeFrequency {A = GGroupA2, B = GGroupB2, C = GGroupC2, DQB1 = GGroupDqb12, DRB1 = GGroupDrb12, Frequency = 0.9m}
            };

            await ImportFrequencies(genericEthnicitySet, null, DefaultRegistryCode);

            var patientHla = DefaultUnambiguousAllelesBuilder
                    .WithDataAt(Locus.A, LocusPosition.One, $"{Alleles.UnambiguousAlleleDetails.A.Position1.Allele}/{alleleStringA}")
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
                DonorFrequencySetMetadata = new FrequencySetMetadata { EthnicityCode = "unrepresented-donor-code", RegistryCode = "unrepresented-donor-code"},
                PatientFrequencySetMetadata = new FrequencySetMetadata { EthnicityCode = "unrepresented-patient-code", RegistryCode = "unrepresented-patient-code"}
            };

            var matchDetails = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.ZeroMismatchProbability.Should().Be(0.1666666666666666666666666667m);
        }

        private async Task ImportFrequencies(IEnumerable<HaplotypeFrequency> haplotypes, string ethnicityCode, string registryCode)
        {
            using var file = FrequencySetFileBuilder.New(registryCode, ethnicityCode, haplotypes).Build();
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

        private Builder<HaplotypeFrequency> DefaultHaplotypeFrequency2 => Builder<HaplotypeFrequency>.New
            .With(r => r.A, GGroupA2)
            .With(r => r.B, GGroupB2)
            .With(r => r.C, GGroupC2)
            .With(r => r.DQB1, GGroupDqb12)
            .With(r => r.DRB1, GGroupDrb12);
    }
}