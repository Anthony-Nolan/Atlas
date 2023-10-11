using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.MatchProbability
{
    /// <summary>
    /// Tests specific to null allele that are part of a larger "expressing" G group.
    /// </summary>
    [TestFixture]
    public class MatchingOnNullAlleleThatHasRelatedAllelesTests : MatchProbabilityTestsBase
    {
        private const Locus LocusUnderTest = Locus.A;
        private const LocusPosition NullAllelePosition = LocusPosition.Two;

        // both alleles are from 03:01:01G
        private const string NullAllele = "*03:129N";
        private const string ExpressingAlleleFromSameGGroupAsNull = "*03:20";

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            // For this test fixture it doesn't matter if the subjects can be explained by haplotypes as they are all unambiguously typed,
            // and we only assert the predicted match counts.
            // But the tests do need at least one HF set in the db.
            await ImportFrequencies(new List<HaplotypeFrequency>
            {
                DefaultHaplotypeFrequency1.WithFrequency(DefaultHaplotypeFrequency).Build(),
                DefaultHaplotypeFrequency2.WithFrequency(DefaultHaplotypeFrequency).Build()
            });
        }

        [Test]
        public async Task CalculateMatchProbability_PatientHasNullAllele_OneDonorAlleleMatchesExpressing_AndOtherIsFromSameGGroupAsNull_PredictsMatchCountOf9()
        {
            var patientHla = DefaultUnambiguousAllelesBuilder.WithDataAt(LocusUnderTest, NullAllelePosition, NullAllele).Build();
            var donorHla = DefaultUnambiguousAllelesBuilder.WithDataAt(LocusUnderTest, NullAllelePosition, ExpressingAlleleFromSameGGroupAsNull).Build();

            var input = DefaultInputBuilder.WithPatientHla(patientHla).WithDonorHla(donorHla).Build();
            var matchProbability = await MatchProbabilityService.CalculateMatchProbability(input);

            matchProbability.OverallMatchCount.Should().Be(9);
        }

        [Test]
        public async Task CalculateMatchProbability_PatientHasNullAllele_AndExpressingAlleleFromSameGGroupAsNull_DonorIsHeterozygous_AndOneAlleleMatchesExpressing_PredictsMatchCountOf9()
        {
            var patientHla = DefaultUnambiguousAllelesBuilder.WithDataAt(LocusUnderTest, ExpressingAlleleFromSameGGroupAsNull, NullAllele).Build();
            var donorHla = DefaultUnambiguousAllelesBuilder.WithDataAt(LocusUnderTest, NullAllelePosition, ExpressingAlleleFromSameGGroupAsNull).Build();

            var input = DefaultInputBuilder.WithPatientHla(patientHla).WithDonorHla(donorHla).Build();
            var matchProbability = await MatchProbabilityService.CalculateMatchProbability(input);

            matchProbability.OverallMatchCount.Should().Be(9);
        }
    }
}