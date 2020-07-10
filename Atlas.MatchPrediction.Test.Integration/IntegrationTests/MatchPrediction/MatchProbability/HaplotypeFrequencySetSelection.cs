using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Test.Integration.Resources;
using FluentAssertions;
using NUnit.Framework;
using HaplotypeFrequencySet = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet.HaplotypeFrequencySet;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.MatchProbability
{
    public class HaplotypeFrequencySetSelection : MatchProbabilityTestsBase
    {
        
        private const string AlleleStringB = "08:182";
            
        private const string SpecificRegistryCode = "specific-registry-code";
        private const string SpecificEthnicityCode = "specific-ethnicity-code";
        
        private static readonly List<HaplotypeFrequency> WrongSetToUse = new List<HaplotypeFrequency>
        {
            DefaultHaplotypeFrequency1.With(h => h.Frequency, 0.1m).Build(),
            DefaultHaplotypeFrequency1.With(h => h.A, GGroupA2).With(h => h.Frequency, 0.1m).Build(),
            DefaultHaplotypeFrequency1.With(h => h.Frequency, 0.1m).With(h => h.B, GGroupB2).Build(),
            DefaultHaplotypeFrequency2.With(h => h.Frequency, 0.1m).Build(),
        };
        
        private static readonly List<HaplotypeFrequency> CorrectSetToUse = new List<HaplotypeFrequency>
        {
            DefaultHaplotypeFrequency1.With(h => h.Frequency, 0.5m).Build(),
            DefaultHaplotypeFrequency1.With(h => h.A, GGroupA2).With(h => h.Frequency, 0.5m).Build(),
            DefaultHaplotypeFrequency1.With(h => h.Frequency, 0.75m).With(h => h.B, GGroupB2).Build(),
            DefaultHaplotypeFrequency2.With(h => h.Frequency, 0.5m).Build(),
        };


        [Test]
        public async Task CalculateMatchProbability_WhenUsingSpecificRegistryAndEthnicity_UsesSpecificHaplotypeFrequencySet()
        {
            await ImportFrequencies(WrongSetToUse, null, null);
            
            await ImportFrequencies(CorrectSetToUse, SpecificEthnicityCode, SpecificRegistryCode);

            var patientHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.B, LocusPosition.One, $"{Alleles.UnambiguousAlleleDetails.B.Position1.Allele}/{AlleleStringB}").Build();
            var donorHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, GGroupA2, GGroupA2).Build();

            var matchProbabilityInput = new MatchProbabilityInput
            {
                PatientHla = patientHla,
                DonorHla = donorHla,
                HlaNomenclatureVersion = HlaNomenclatureVersion,
                DonorFrequencySetMetadata = new FrequencySetMetadata {EthnicityCode = SpecificEthnicityCode, RegistryCode = SpecificRegistryCode},
                PatientFrequencySetMetadata = new FrequencySetMetadata {EthnicityCode = SpecificEthnicityCode, RegistryCode = SpecificRegistryCode}
            };

            var matchDetails = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.OneMismatchProbability.Percentage.Should().Be(40);
        }
        
        [Test]
        public async Task CalculateMatchProbability_WhenUsingRegistryButNotEthnicityMatchedHaplotypeSet_UsesRegistryOnlyHaplotypeFrequencySet()
        {
            await ImportFrequencies(WrongSetToUse, null, null);
            
            await ImportFrequencies(CorrectSetToUse, null, SpecificRegistryCode);
            
            var patientHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.B, LocusPosition.One, $"{Alleles.UnambiguousAlleleDetails.B.Position1.Allele}/{AlleleStringB}").Build();
            var donorHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, GGroupA2, GGroupA2).Build();

            var matchProbabilityInput = new MatchProbabilityInput
            {
                PatientHla = patientHla,
                DonorHla = donorHla,
                HlaNomenclatureVersion = HlaNomenclatureVersion,
                DonorFrequencySetMetadata = new FrequencySetMetadata {EthnicityCode = "unrepresented-donor-ethnicity", RegistryCode = SpecificRegistryCode},
                PatientFrequencySetMetadata = new FrequencySetMetadata {EthnicityCode =  "unrepresented-patient-ethnicity", RegistryCode = SpecificRegistryCode}
            };

            var matchDetails = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.OneMismatchProbability.Percentage.Should().Be(40);
        }
        
        [Test]
        public async Task CalculateMatchProbability_WhenUsingRegistryAndEthnicityCodesAreNotRepresented_UsesGlobalHaplotypeFrequencySet()
        {
            await ImportFrequencies(WrongSetToUse, SpecificEthnicityCode, null);
            
            await ImportFrequencies(CorrectSetToUse, null, null);
            
            var patientHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.B, LocusPosition.One, $"{Alleles.UnambiguousAlleleDetails.B.Position1.Allele}/{AlleleStringB}").Build();
            var donorHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, GGroupA2, GGroupA2).Build();

            var matchProbabilityInput = new MatchProbabilityInput
            {
                PatientHla = patientHla,
                DonorHla = donorHla,
                HlaNomenclatureVersion = HlaNomenclatureVersion,
                DonorFrequencySetMetadata = new FrequencySetMetadata {EthnicityCode = "unrepresented-donor-ethnicity", RegistryCode = "unrepresented-donor-registry"},
                PatientFrequencySetMetadata = new FrequencySetMetadata {EthnicityCode =  "unrepresented-patient-ethnicity", RegistryCode = "unrepresented-patient-registry"}
            };

            var matchDetails = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.OneMismatchProbability.Percentage.Should().Be(40);
        }

        /*
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
        }*/
    }
    
    
}