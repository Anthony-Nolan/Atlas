using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;
using FluentAssertions;
using NUnit.Framework;
// ReSharper disable InconsistentNaming

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.MatchProbability
{
    [TestFixtureSource(nameof(NullAlleles))]
    public class MatchingOnNullAlleleTests : MatchProbabilityTestsBase
    {
        #region Static Test Data

        private const Locus LocusUnderTest = Locus.A;

        private const LocusPosition NullAllelePosition = LocusPosition.Two;
        private const LocusPosition OtherPosition = LocusPosition.One;

        private static readonly string ExpressingAllele = DefaultUnambiguousAllelesBuilder.Build().GetPosition(LocusUnderTest, OtherPosition);
        private const string MismatchedAllele = "01:01";

        private const string NullAllele_HasNoRelatedAlleles = "01:11N";
        private const string NullAllele_HasRelatedAlleles = "03:129N";
        public static object[] NullAlleles = { NullAllele_HasNoRelatedAlleles, NullAllele_HasRelatedAlleles };

        #endregion

        private readonly string nullAllele;

        public MatchingOnNullAlleleTests(string nullAllele)
        {
            this.nullAllele = nullAllele;
        }

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
        public async Task CalculateMatchProbability_PatientAndDonorHaveSameNullAndExpressingAlleles_PredictsMatchCountOf10()
        {
            var patientHla = BuildHlaWithNullAllelePositionOf(nullAllele);
            var donorHla = BuildHlaWithNullAllelePositionOf(nullAllele);

            var matchProbability = await MatchProbabilityService.CalculateMatchProbability(BuildInput(patientHla, donorHla));

            matchProbability.OverallMatchCount.Should().Be(10);
        }

        [Test]
        public async Task CalculateMatchProbability_PatientHasNullAllele_DonorHomozygousForExpressing_PredictsMatchCountOf10()
        {
            var patientHla = BuildHlaWithNullAllelePositionOf(nullAllele);
            var donorHla = DefaultUnambiguousAllelesBuilder.WithDataAt(LocusUnderTest, ExpressingAllele, ExpressingAllele).Build();

            var matchProbability = await MatchProbabilityService.CalculateMatchProbability(BuildInput(patientHla, donorHla));

            matchProbability.OverallMatchCount.Should().Be(10);
        }

        [Test]
        public async Task CalculateMatchProbability_PatientHasNullAllele_DonorIsHeterozygous_AndOnlyOneDonorAlleleMatchesExpressing_PredictsMatchCountOf9()
        {
            var patientHla = BuildHlaWithNullAllelePositionOf(nullAllele);
            var donorHla = DefaultUnambiguousAllelesBuilder.Build();

            var matchProbability = await MatchProbabilityService.CalculateMatchProbability(BuildInput(patientHla, donorHla));

            matchProbability.OverallMatchCount.Should().Be(9);
        }

        [Test]
        public async Task CalculateMatchProbability_PatientHasNullAllele_DonorIsHeterozygous_AndNeitherDonorAlleleMatchesExpressing_PredictsMatchCountOf8()
        {
            var patientHla = DefaultUnambiguousAllelesBuilder.WithDataAt(LocusUnderTest, MismatchedAllele, nullAllele).Build();
            var donorHla = DefaultUnambiguousAllelesBuilder.Build();

            var matchProbability = await MatchProbabilityService.CalculateMatchProbability(BuildInput(patientHla, donorHla));

            matchProbability.OverallMatchCount.Should().Be(8);
        }

        [Test]
        public async Task CalculateMatchProbability_PatientHasNullAllele_DonorIsHomozygous_AndNeitherDonorAlleleMatchesExpressing_PredictsMatchCountOf8()
        {
            var patientHla = BuildHlaWithNullAllelePositionOf(nullAllele);
            var donorHla = DefaultUnambiguousAllelesBuilder.WithDataAt(LocusUnderTest, MismatchedAllele, MismatchedAllele).Build();

            var matchProbability = await MatchProbabilityService.CalculateMatchProbability(BuildInput(patientHla, donorHla));

            matchProbability.OverallMatchCount.Should().Be(8);
        }

        [Test]
        public async Task CalculateMatchProbability_PatientAndDonorHaveNullAllele_MismatchedOnExpressingAlleles_PredictsMatchCountOf8()
        {
            var patientHla = BuildHlaWithNullAllelePositionOf(nullAllele);
            var donorHla = DefaultUnambiguousAllelesBuilder.WithDataAt(LocusUnderTest, MismatchedAllele, nullAllele).Build();

            var matchProbability = await MatchProbabilityService.CalculateMatchProbability(BuildInput(patientHla, donorHla));

            matchProbability.OverallMatchCount.Should().Be(8);
        }

        private static PhenotypeInfo<string> BuildHlaWithNullAllelePositionOf(string hla) =>
            DefaultUnambiguousAllelesBuilder.WithDataAt(LocusUnderTest, NullAllelePosition, hla).Build();

        private static SingleDonorMatchProbabilityInput BuildInput(PhenotypeInfo<string> patientHla, PhenotypeInfo<string> donorHla) =>
            DefaultInputBuilder.WithPatientHla(patientHla).WithDonorHla(donorHla).Build();
    }
}