using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Test.Integration.Resources.Alleles;
using Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.MatchProbability
{
    /// <summary>
    /// Covers the ability of match prediction to support HLA typings that are not found in the HF set HLA version,
    /// but are found in the matching algorithm HLA version.
    /// </summary>
    [TestFixture]
    internal class HlaVersionEffectOnRepresentationTests : MatchProbabilityTestsBase
    {
        #region Test Data

        private const Locus LocusUnderTest = Locus.A;
        private const LocusPosition PositionNotUnderTest = LocusPosition.One;
        private const LocusPosition PositionUnderTest = LocusPosition.Two;

        /// <summary>
        /// Builder based on <see cref="Alleles.UnambiguousAlleleDetails"/> but has a typing that maps to
        /// multiple G groups at position A_1, including the G group of allele A_1 in <see cref="Alleles.UnambiguousAlleleDetails"/>
        /// </summary>
        private static PhenotypeInfoBuilder<string> AmbiguousAtA1Builder =>
            new PhenotypeInfoBuilder<string>(Alleles.UnambiguousAlleleDetails.Alleles())
                .WithDataAt(LocusUnderTest, PositionNotUnderTest, "02:XX");

        private static readonly IEnumerable<PhenotypeInfoBuilder<string>> PhenotypeBuilders = new[]
        {
            DefaultUnambiguousAllelesBuilder, AmbiguousAtA1Builder
        };

        private static readonly IEnumerable<(PhenotypeInfoBuilder<string>, bool)> PhenotypeBuildersWithExpectedRepresentation = new[]
        {
            // unambiguous phenotype should always be represented (as long as the HLA is deemed valid)
            // as there is certainty of the underlying small g/G groups
            (DefaultUnambiguousAllelesBuilder, false),

            // ambiguous phenotype should be deemed unrepresented when no haplotypes can be found containing their small g/G groups
            (AmbiguousAtA1Builder, true)
        };

        #endregion

        [SetUp]
        public void Setup()
        {
            foreach (var phenotypeInfoBuilder in PhenotypeBuilders)
            {
                phenotypeInfoBuilder.ResetToStartingValues();
            }
        }

        [TestCaseSource(nameof(PhenotypeBuilders))]
        public async Task CalculateMatchProbability_HlaIsValidInHfSetHlaVersion_SubjectsAreRepresented(
            PhenotypeInfoBuilder<string> phenotypeBuilder)
        {
            await ImportDefaultHfSet();

            var phenotype = phenotypeBuilder.Build();
            var matchProbabilityInput = DefaultInputBuilder
                .WithDonorHla(phenotype)
                .WithPatientHla(phenotype)
                .Build();

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.IsDonorPhenotypeUnrepresented.Should().BeFalse();
            matchDetails.IsPatientPhenotypeUnrepresented.Should().BeFalse();
        }

        [TestCaseSource(nameof(PhenotypeBuilders))]
        public async Task CalculateMatchProbability_HlaInvalidInHFSetAndMatchingHlaVersions_SubjectsAreUnrepresented(
            PhenotypeInfoBuilder<string> phenotypeBuilder)
        {
            await ImportDefaultHfSet();

            // name that is valid according allele naming conventions but is completely made up so shouldn't be found in any nomenclature release
            const string hlaName = "9999:9999";
            var phenotype = phenotypeBuilder.WithDataAt(LocusUnderTest, PositionUnderTest, hlaName).Build();

            var matchProbabilityInput = DefaultInputBuilder
                .WithDonorHla(phenotype)
                .WithPatientHla(phenotype)
                .Build();

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.IsDonorPhenotypeUnrepresented.Should().BeTrue();
            matchDetails.IsPatientPhenotypeUnrepresented.Should().BeTrue();
        }

        [TestCaseSource(nameof(PhenotypeBuildersWithExpectedRepresentation))]
        public async Task CalculateMatchProbability_HlaInvalidInHfSetVersion_AndValidInMatchingVersion_ButGGroupNotInHfSetVersion_SubjectsAreExpectedRepresentation(
            (PhenotypeInfoBuilder<string> phenotypeBuilder, bool isRepresented) phenotypeBuilderWithExpectedRepresentation)
        {
            await ImportDefaultHfSet();

            // this allele was introduced in the matching HLA version v3400 (test HF set is on v3330, and test matching on v3400),
            // and maps to a G and P group not found in the HF set version (11:361)
            const string hlaName = "11:361";
            var phenotype = phenotypeBuilderWithExpectedRepresentation.phenotypeBuilder.WithDataAt(LocusUnderTest, PositionUnderTest, hlaName).Build();

            var matchProbabilityInput = DefaultInputBuilder
                .WithDonorHla(phenotype)
                .WithPatientHla(phenotype)
                .Build();

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            var expectedRepresentation = phenotypeBuilderWithExpectedRepresentation.isRepresented;
            matchDetails.IsDonorPhenotypeUnrepresented.Should().Be(expectedRepresentation);
            matchDetails.IsPatientPhenotypeUnrepresented.Should().Be(expectedRepresentation);
        }


        [TestCaseSource(nameof(PhenotypeBuildersWithExpectedRepresentation))]
        public async Task CalculateMatchProbability_MatchingVersionGGroupValidInHfSetVersion_ButNotFoundInHfSet_SubjectsAreExpectedRepresentation(
            (PhenotypeInfoBuilder<string> phenotypeBuilder, bool isRepresented) parameters)
        {
            await ImportDefaultHfSet();

            // This allele was introduced in the matching HLA version v3400 (test HF set is on v3330, and test matching on v3400).
            // It maps to a G/P group valid in the HF set HLA version (01:01:01G/01:01P) but the group is not found within the default test HF set.
            const string hlaName = "01:01:115";
            var phenotype = parameters.phenotypeBuilder.WithDataAt(LocusUnderTest, PositionUnderTest, hlaName).Build();

            var matchProbabilityInput = DefaultInputBuilder
                .WithDonorHla(phenotype)
                .WithPatientHla(phenotype)
                .Build();

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            var expectedRepresentation = parameters.isRepresented;
            matchDetails.IsDonorPhenotypeUnrepresented.Should().Be(expectedRepresentation);
            matchDetails.IsPatientPhenotypeUnrepresented.Should().Be(expectedRepresentation);
        }

        [TestCaseSource(nameof(PhenotypeBuilders))]
        public async Task CalculateMatchProbability_MatchingVersionGGroupValidInHfSetVersion_AndFoundInHfSet_SubjectsAreRepresented(
            PhenotypeInfoBuilder<string> phenotypeBuilder)
        {
            // This allele was introduced in the matching HLA version v3400 (test HF set is on v3330, and test matching on v3400).
            // It maps to a G/P group valid in the HF set HLA version (01:01:01G/01:01P) and the group will be included in the test HF set.
            const string hlaName = "01:01:115";
            const string gGroupName = "01:01:01G";

            var haplotypes = new List<HaplotypeFrequency>
            {
                DefaultHaplotypeFrequency1
                    .With(h => h.A, gGroupName)
                    .With(h => h.Frequency, DefaultHaplotypeFrequency)
                    .Build(),

                DefaultHaplotypeFrequency2
                    .With(h => h.Frequency, DefaultHaplotypeFrequency)
                    .Build(),
            };

            await ImportFrequencies(haplotypes);

            var phenotype = phenotypeBuilder.WithDataAt(LocusUnderTest, PositionUnderTest, hlaName).Build();

            var matchProbabilityInput = DefaultInputBuilder
                .WithDonorHla(phenotype)
                .WithPatientHla(phenotype)
                .Build();

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.IsDonorPhenotypeUnrepresented.Should().BeFalse();
            matchDetails.IsPatientPhenotypeUnrepresented.Should().BeFalse();
        }

        private async Task ImportDefaultHfSet()
        {
            var haplotypes = new List<HaplotypeFrequency>
            {
                DefaultHaplotypeFrequency1.With(h => h.Frequency, DefaultHaplotypeFrequency).Build(),
                DefaultHaplotypeFrequency2.With(h => h.Frequency, DefaultHaplotypeFrequency).Build(),
            };

            await ImportFrequencies(haplotypes);
        }
    }
}