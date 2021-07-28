using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.Common.Utils.Extensions;
using Atlas.Common.Utils.Models;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Test.Integration.Resources.Alleles;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;
using FluentAssertions;
using NUnit.Framework;

// ReSharper disable InconsistentNaming - want to avoid calling "G groups" "gGroup", as "g" groups are a distinct thing 

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.MatchProbability
{
    public class MatchProbabilityTests : MatchProbabilityTestsBase
    {
        [Test]
        public async Task CalculateMatchProbability_ReturnsCorrectNomenclatureVersionForDonorAndPatient()
        {
            const string patientNomenclatureVersion = "3330";
            const string donorNomenclatureVersion = "3400";

            var matchProbabilityInput = DefaultInputBuilder
                .WithPatientMetadata(new FrequencySetMetadata {EthnicityCode = "patient-ethnicity", RegistryCode = "patient-registry"})
                .WithDonorMetadata(new FrequencySetMetadata {EthnicityCode = "donor-ethnicity", RegistryCode = "donor-registry"}).Build();

            var possibleHaplotypes = new List<HaplotypeFrequency>
            {
                DefaultHaplotypeFrequency1.With(h => h.Frequency, 0.00002m).Build(),
                DefaultHaplotypeFrequency2.With(h => h.Frequency, 0.00001m).Build(),
            };

            await ImportFrequencies(possibleHaplotypes, "patient-registry", "patient-ethnicity", patientNomenclatureVersion);
            await ImportFrequencies(possibleHaplotypes, "donor-registry", "donor-ethnicity", donorNomenclatureVersion);

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.PatientFrequencySetNomenclatureVersion.Should().Be(patientNomenclatureVersion);
            matchDetails.DonorFrequencySetNomenclatureVersion.Should().Be(donorNomenclatureVersion);
        }

        [Test]
        public async Task CalculateMatchProbability_WhenIdenticalGenotypes_UnrepresentedInFrequencySet_ReturnsZeroPercent()
        {
            var matchProbabilityInput = DefaultInputBuilder.Build();

            await ImportFrequencies(new List<HaplotypeFrequency>
            {
                DefaultHaplotypeFrequency1.WithDataAt(Locus.A, "68:24").With(h => h.Frequency, 0.00001m).Build()
            });

            var expectedMismatchProbabilityPerLocus = new LociInfo<Probability>((Probability) null);

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.MatchProbabilities.ZeroMismatchProbability.Should().Be(null);
            matchDetails.MatchProbabilities.OneMismatchProbability.Should().Be(null);
            matchDetails.MatchProbabilities.TwoMismatchProbability.Should().Be(null);
            matchDetails.ZeroMismatchProbabilityPerLocus.Should().Be(expectedMismatchProbabilityPerLocus);
            matchDetails.OneMismatchProbabilityPerLocus.Should().Be(expectedMismatchProbabilityPerLocus);
            matchDetails.TwoMismatchProbabilityPerLocus.Should().Be(expectedMismatchProbabilityPerLocus);
        }

        [Test]
        public async Task CalculateMatchProbability_WhenIdenticalGenotypes_RepresentedInFrequencySet_ReturnsOneHundredPercent()
        {
            var matchProbabilityInput = DefaultInputBuilder.Build();

            var possibleHaplotypes = new List<HaplotypeFrequency>
            {
                DefaultHaplotypeFrequency1.With(h => h.Frequency, 0.00002m).Build(),
                DefaultHaplotypeFrequency2.With(h => h.Frequency, 0.00001m).Build(),
            };

            await ImportFrequencies(possibleHaplotypes);

            var expectedZeroMismatchProbabilityPerLocus = new LociInfo<decimal?>(1).SetLocus(Locus.Dpb1, null);
            var expectedOneMismatchProbabilityPerLocus = new LociInfo<decimal?>(0).SetLocus(Locus.Dpb1, null);
            var expectedTwoMismatchProbabilityPerLocus = new LociInfo<decimal?>(0).SetLocus(Locus.Dpb1, null);

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.MatchProbabilities.ZeroMismatchProbability.Decimal.Should().Be(1m);
            matchDetails.MatchProbabilities.OneMismatchProbability.Decimal.Should().Be(0m);
            matchDetails.MatchProbabilities.TwoMismatchProbability.Decimal.Should().Be(0m);
            matchDetails.ZeroMismatchProbabilityPerLocus.ToDecimals().Should().Be(expectedZeroMismatchProbabilityPerLocus);
            matchDetails.OneMismatchProbabilityPerLocus.ToDecimals().Should().Be(expectedOneMismatchProbabilityPerLocus);
            matchDetails.TwoMismatchProbabilityPerLocus.ToDecimals().Should().Be(expectedTwoMismatchProbabilityPerLocus);
        }

        [Test]
        public async Task CalculateMatchProbability_WhenGenotypesAreNonMatching_ZeroPercentProbability()
        {
            const string gGroupA = "23:01:01G";
            const string gGroupB = "07:05:01G";
            const string gGroupC = "07:01:01G";
            const string gGroupDqb1 = "06:01:01G";
            const string gGroupDrb1 = "01:01:01G";

            var possibleHaplotypes = new List<HaplotypeFrequency>
            {
                DefaultHaplotypeFrequency1.With(h => h.Frequency, 0.00001m).Build(),
                DefaultHaplotypeFrequency2.With(h => h.Frequency, 0.00002m).Build(),
                HaplotypeFrequencyBuilder.New
                    .WithHaplotype(new LociInfo<string>(gGroupA, gGroupB, gGroupC, null, gGroupDqb1, gGroupDrb1))
                    .With(h => h.Frequency, 0.00003m)
                    .Build()
            };

            await ImportFrequencies(possibleHaplotypes);

            var patientHla = DefaultUnambiguousAllelesBuilder.WithDataAt(
                    Locus.A,
                    Alleles.UnambiguousAlleleDetails.A.Position1.Allele,
                    Alleles.UnambiguousAlleleDetails.A.Position2.Allele)
                .Build();

            var donorHla = new PhenotypeInfoBuilder<string>()
                .WithDataAt(Locus.A, gGroupA)
                .WithDataAt(Locus.B, gGroupB)
                .WithDataAt(Locus.C, gGroupC)
                .WithDataAt(Locus.Dqb1, gGroupDqb1)
                .WithDataAt(Locus.Drb1, gGroupDrb1).Build();

            var matchProbabilityInput = DefaultInputBuilder
                .WithPatientHla(patientHla)
                .WithDonorHla(donorHla)
                .Build();

            var expectedZeroMismatchProbabilityPerLocus = new LociInfo<decimal?>(0).SetLocus(Locus.Dpb1, null);
            var expectedOneMismatchProbabilityPerLocus = new LociInfo<decimal?>(0).SetLocus(Locus.Dpb1, null);
            var expectedTwoMismatchProbabilityPerLocus = new LociInfo<decimal?>(1).SetLocus(Locus.Dpb1, null);

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.MatchProbabilities.ZeroMismatchProbability.Decimal.Should().Be(0m);
            matchDetails.MatchProbabilities.OneMismatchProbability.Decimal.Should().Be(0m);
            matchDetails.MatchProbabilities.TwoMismatchProbability.Decimal.Should().Be(0m);
            matchDetails.ZeroMismatchProbabilityPerLocus.ToDecimals().Should().Be(expectedZeroMismatchProbabilityPerLocus);
            matchDetails.OneMismatchProbabilityPerLocus.ToDecimals().Should().Be(expectedOneMismatchProbabilityPerLocus);
            matchDetails.TwoMismatchProbabilityPerLocus.ToDecimals().Should().Be(expectedTwoMismatchProbabilityPerLocus);
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

            var matchProbabilityInput = DefaultInputBuilder.WithPatientHla(patientHla).Build();

            var expectedZeroMismatchProbabilityPerLocus = new LociInfo<decimal?>
            (
                0.0823045267489711934156378601m,
                0.7777777777777777777777777778m,
                0.8148148148148148148148148148m,
                null,
                0.8518518518518518518518518519m,
                0.8888888888888888888888888889m
            );

            var expectedOneMismatchProbabilityPerLocus = new LociInfo<decimal?>
            (
                0.6872427983539094650205761317m,
                0.2222222222222222222222222222m,
                0.1851851851851851851851851852m,
                null,
                0.1481481481481481481481481481m,
                0.1111111111111111111111111111m
            );

            var expectedTwoMismatchProbabilityPerLocus = new LociInfo<decimal?>
            (
                0.2304526748971193415637860082m,
                0m,
                0m,
                null,
                0m,
                0m
            );

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.MatchProbabilities.ZeroMismatchProbability.Decimal.Should().Be(0.008230452674897119341563786m);
            matchDetails.MatchProbabilities.OneMismatchProbability.Decimal.Should().Be(0.1687242798353909465020576132m);
            matchDetails.MatchProbabilities.TwoMismatchProbability.Decimal.Should().Be(0.8230452674897119341563786008m);
            matchDetails.ZeroMismatchProbabilityPerLocus.ToDecimals().Should().Be(expectedZeroMismatchProbabilityPerLocus);
            matchDetails.OneMismatchProbabilityPerLocus.ToDecimals().Should().Be(expectedOneMismatchProbabilityPerLocus);
            matchDetails.TwoMismatchProbabilityPerLocus.ToDecimals().Should().Be(expectedTwoMismatchProbabilityPerLocus);
        }

        [Test]
        public async Task CalculateMatchProbability_WhenAmbiguousHlaAndMissingLoci_ReturnsProbability()
        {
            const string alleleStringA = "01:37";
            const string GGroupA = "01:01:01G";
            const string anotherAlleleStringA = "23:17";
            const string anotherGGroupA = "23:01:01G";

            const string alleleStringB = "08:182";
            const string GGroupB = "08:01:01G";

            const string alleleStringC = "04:82";

            const string alleleStringDrb1 = "11:129";
            const string GGroupDrb1 = "11:06:01G";

            var possibleHaplotypes = new List<HaplotypeFrequency>
            {
                DefaultHaplotypeFrequency2.With(h => h.A, anotherGGroupA).With(h => h.Frequency, 0.00008m).Build(),
                DefaultHaplotypeFrequency1.With(h => h.A, GGroupA).With(h => h.Frequency, 0.00007m).Build(),
                DefaultHaplotypeFrequency1.With(h => h.B, GGroupB).With(h => h.Frequency, 0.00006m).Build(),
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
                .WithDataAt(Locus.Dqb1, null as string)
                .WithDataAt(Locus.Drb1, LocusPosition.One, $"{Alleles.UnambiguousAlleleDetails.Drb1.Position1.Allele}/{alleleStringDrb1}")
                .Build();

            var matchProbabilityInput = new SingleDonorMatchProbabilityInput
            {
                PatientHla = patientHla.ToPhenotypeInfoTransfer(),
                DonorInput = new DonorInput
                {
                    DonorHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.C, null as string).Build().ToPhenotypeInfoTransfer(),
                    DonorFrequencySetMetadata = new FrequencySetMetadata {EthnicityCode = DefaultEthnicityCode, RegistryCode = DefaultRegistryCode},
                },
                MatchingAlgorithmHlaNomenclatureVersion = HlaNomenclatureVersion,
                PatientFrequencySetMetadata = new FrequencySetMetadata {EthnicityCode = DefaultEthnicityCode, RegistryCode = DefaultRegistryCode}
            };

            var expectedZeroMismatchProbabilityPerLocus = new LociInfo<decimal?>
            (
                0.0679012345679012345679012346m,
                0.6666666666666666666666666667m,
                1m,
                null,
                1m,
                0.8333333333333333333333333333m
            );

            var expectedOneMismatchProbabilityPerLocus = new LociInfo<decimal?>
            (
                0.5864197530864197530864197531m,
                0.3333333333333333333333333333m,
                0m,
                null,
                0m,
                0.1666666666666666666666666667m
            );

            var expectedTwoMismatchProbabilityPerLocus = new LociInfo<decimal?>
            (
                0.3456790123456790123456790123m,
                0m,
                0m,
                null,
                0m,
                0m
            );

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.MatchProbabilities.ZeroMismatchProbability.Decimal.Should().Be(0.012345679012345679012345679m);
            matchDetails.MatchProbabilities.OneMismatchProbability.Decimal.Should().Be(0.1975308641975308641975308642m);
            matchDetails.MatchProbabilities.TwoMismatchProbability.Decimal.Should().Be(0.7901234567901234567901234568m);
            matchDetails.ZeroMismatchProbabilityPerLocus.ToDecimals().Should().Be(expectedZeroMismatchProbabilityPerLocus);
            matchDetails.OneMismatchProbabilityPerLocus.ToDecimals().Should().Be(expectedOneMismatchProbabilityPerLocus);
            matchDetails.TwoMismatchProbabilityPerLocus.ToDecimals().Should().Be(expectedTwoMismatchProbabilityPerLocus);
        }

        [Test]
        public async Task CalculateMatchProbability_WithAmbiguousHomozygousHlaAtSingleLocus_ReturnsCorrectProbability()
        {
            var patientHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, "02:XX").Build();
            var donorHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, LocusPosition.Two, "11:03/02:01").Build();

            await ImportFrequencies(new List<HaplotypeFrequency>
            {
                DefaultHaplotypeFrequency1.WithFrequency(0.05m),
                DefaultHaplotypeFrequency2.WithDataAt(Locus.A, "02:01:01G").WithFrequency(0.02m),
                DefaultHaplotypeFrequency2.WithFrequency(0.07m),
            });
            var matchProbabilityInput = DefaultInputBuilder
                .WithPatientHla(patientHla)
                .WithDonorHla(donorHla)
                .Build();

            var matchProbability = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchProbability.MatchProbabilities.ZeroMismatchProbability.Percentage.Should().Be(22);
        }

        // These test cases were calculated by hand to ensure we are asserting the correct percentages
        [TestCase(10, 90, 5)]
        [TestCase(90, 10, 82)]
        public async Task CalculateMatchProbability_WhenOnlyMatchingGenotypeIsHomozygousAtAllLoci_ReturnsCorrectProbability(
            int sharedHaplotypeFrequencyAsPercentage,
            int otherHaplotypeFrequencyAsPercentage,
            int expectedZeroMismatchPercentage
        )
        {
            var sharedHaplotypeHla = Alleles.UnambiguousAlleleDetails.GGroups().Split().Item1;

            // patientHla entirely homozygous
            var patientHla = new PhenotypeInfo<string>(sharedHaplotypeHla, sharedHaplotypeHla);

            const Locus ambiguousLocus = Locus.B;
            const LocusPosition ambiguousPosition = LocusPosition.Two;

            var alleleDetailsAtAmbiguousLocus = Alleles.UnambiguousAlleleDetails.GetLocus(ambiguousLocus);
            var donorHla = new PhenotypeInfoBuilder<string>(patientHla)
                .WithDataAt(ambiguousLocus, ambiguousPosition,
                    $"{alleleDetailsAtAmbiguousLocus.Position1.Allele}/{alleleDetailsAtAmbiguousLocus.Position2.Allele}")
                .Build();

            var matchProbabilityInput = DefaultInputBuilder
                .WithPatientHla(patientHla)
                .WithDonorHla(donorHla)
                .Build();
            await ImportFrequencies(new List<HaplotypeFrequency>
            {
                DefaultHaplotypeFrequency1
                    .WithFrequencyAsPercentage(sharedHaplotypeFrequencyAsPercentage),
                DefaultHaplotypeFrequency1
                    .WithDataAt(ambiguousLocus, alleleDetailsAtAmbiguousLocus.Position2.GGroup)
                    .WithFrequencyAsPercentage(otherHaplotypeFrequencyAsPercentage),
            });

            var matchProbability = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchProbability.MatchProbabilities.ZeroMismatchProbability.Percentage.Should().Be(expectedZeroMismatchPercentage);
        }
    }
}