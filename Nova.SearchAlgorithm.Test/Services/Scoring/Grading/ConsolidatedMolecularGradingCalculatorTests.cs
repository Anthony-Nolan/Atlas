using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Services.Scoring.Grading;
using Nova.SearchAlgorithm.Test.Builders;
using Nova.SearchAlgorithm.Test.Builders.ScoringInfo;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Services.Scoring.Grading
{
    [TestFixture]
    public class ConsolidatedMolecularGradingCalculatorTests
    {
        private IConsolidatedMolecularGradingCalculator consolidatedMolecularGradingCalculator;

        [SetUp]
        public void SetUp()
        {
            consolidatedMolecularGradingCalculator = new ConsolidatedMolecularGradingCalculator();
        }

        #region Tests: Both Typings Consolidated Molecular

        [Test]
        public void CalculateGrade_BothTypingsAreConsolidatedMolecular_WithSameName_ReturnsGGroup()
        {
            const string sharedConsolidatedMolecularName = "shared-hla-name";

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(sharedConsolidatedMolecularName)
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder().Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(sharedConsolidatedMolecularName)
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder().Build())
                .Build();

            var grade = consolidatedMolecularGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.GGroup);
        }

        [Test]
        public void CalculateGrade_BothTypingsAreConsolidatedMolecular_WithDifferentNames_ButIntersectingGGroups_ReturnsGGroup()
        {
            const string sharedGGroup = "shared-g-group";

            const string patientConsolidatedMolecularName = "patient-hla-name";
            const string patientGGroup = "patient-g-group";
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(patientConsolidatedMolecularName)
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { sharedGGroup, patientGGroup })
                    .Build())
                .Build();

            const string donorConsolidatedMolecularName = "donor-hla-name";
            const string donorGGroup = "donor-g-group";
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(donorConsolidatedMolecularName)
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { sharedGGroup, donorGGroup })
                    .Build())
                .Build();

            var grade = consolidatedMolecularGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.GGroup);
        }

        [Test]
        public void CalculateGrade_BothTypingsAreConsolidatedMolecular_WithDifferentNamesAndGGroups_ButIntersectingPGroups_ReturnsGGroup()
        {
            const string sharedPGroup = "shared-p-group";

            const string patientConsolidatedMolecularName = "patient-hla-name";
            const string patientGGroup = "patient-g-group";
            const string patientPGroup = "patient-p-group";
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(patientConsolidatedMolecularName)
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new []{ patientGGroup })
                    .WithMatchingPGroups(new[] { sharedPGroup, patientPGroup })
                    .Build())
                .Build();

            const string donorConsolidatedMolecularName = "donor-hla-name";
            const string donorGGroup = "donor-g-group";
            const string donorPGroup = "donor-p-group";
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(donorConsolidatedMolecularName)
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { donorGGroup })
                    .WithMatchingPGroups(new[] { sharedPGroup, donorPGroup })
                    .Build())
                .Build();

            var grade = consolidatedMolecularGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.PGroup);
        }

        [Test]
        public void CalculateGrade_BothTypingsAreConsolidatedMolecular_WithDifferentNamesAndGGroupsAndPGroups_ReturnsMismatch()
        {
            const string patientConsolidatedMolecularName = "patient-hla-name";
            const string patientGGroup = "patient-g-group";
            const string patientPGroup = "patient-p-group";
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(patientConsolidatedMolecularName)
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { patientGGroup })
                    .WithMatchingPGroups(new[] { patientPGroup })
                    .Build())
                .Build();

            const string donorConsolidatedMolecularName = "donor-hla-name";
            const string donorGGroup = "donor-g-group";
            const string donorPGroup = "donor-p-group";
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(donorConsolidatedMolecularName)
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { donorGGroup })
                    .WithMatchingPGroups(new[] { donorPGroup })
                    .Build())
                .Build();

            var grade = consolidatedMolecularGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Mismatch);
        }

        #endregion

        #region Tests: Consolidated Molecular vs. Expressing Allele

        [Test]
        public void CalculateGrade_ConsolidatedMolecularVsExpressingAllele_WithIntersectingGGroups_ReturnsGGroup()
        {
            const string sharedGGroup = "shared-g-group";

            const string patientGGroup = "patient-g-group";
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { sharedGGroup, patientGGroup })
                    .Build())
                .Build();

            const string donorAlleleName = "999:999";
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(donorAlleleName)
                    .WithMatchingGGroup(sharedGGroup)
                    .Build())
                .Build();

            var grade = consolidatedMolecularGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.GGroup);
        }

        [Test]
        public void CalculateGrade_ConsolidatedMolecularVsExpressingAllele_WithDifferentGGroups_ButIntersectingPGroups_ReturnsGGroup()
        {
            const string sharedPGroup = "shared-p-group";

            const string patientGGroup = "patient-g-group";
            const string patientPGroup = "patient-p-group";
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { patientGGroup })
                    .WithMatchingPGroups(new[] { sharedPGroup, patientPGroup })
                    .Build())
                .Build();

            const string donorAlleleName = "999:999";
            const string donorGGroup = "donor-g-group";
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(donorAlleleName)
                    .WithMatchingGGroup(donorGGroup)
                    .WithMatchingPGroup(sharedPGroup)
                    .Build())
                .Build();

            var grade = consolidatedMolecularGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.PGroup);
        }

        [Test]
        public void CalculateGrade_ConsolidatedMolecularVsExpressingAllele_WithDifferentGGroupsAndPGroups_ReturnsMismatch()
        {
            const string patientGGroup = "patient-g-group";
            const string patientPGroup = "patient-p-group";
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { patientGGroup })
                    .WithMatchingPGroups(new[] { patientPGroup })
                    .Build())
                .Build();

            const string donorAlleleName = "999:999";
            const string donorGGroup = "donor-g-group";
            const string donorPGroup = "donor-p-group";
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(donorAlleleName)
                    .WithMatchingGGroup(donorGGroup)
                    .WithMatchingPGroup(donorPGroup)
                    .Build())
                .Build();

            var grade = consolidatedMolecularGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Mismatch);
        }

        #endregion

        #region Tests: Expressing Allele vs. Consolidated Molecular

        [Test]
        public void CalculateGrade_ExpressingAlleleVsConsolidatedMolecular_WithIntersectingGGroups_ReturnsGGroup()
        {
            const string sharedGGroup = "shared-g-group";

            const string patientAlleleName = "999:999";
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(patientAlleleName)
                    .WithMatchingGGroup(sharedGGroup)
                    .Build())
                .Build();

            const string donorGGroup = "donor-g-group";
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { sharedGGroup, donorGGroup })
                    .Build())
                .Build();

            var grade = consolidatedMolecularGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.GGroup);
        }

        [Test]
        public void CalculateGrade_ExpressingAlleleVsConsolidatedMolecular_WithDifferentGGroups_ButIntersectingPGroups_ReturnsGGroup()
        {
            const string sharedPGroup = "shared-p-group";

            const string patientAlleleName = "999:999";
            const string patientGGroup = "patient-g-group";
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(patientAlleleName)
                    .WithMatchingGGroup(patientGGroup)
                    .WithMatchingPGroup(sharedPGroup)
                    .Build())
                .Build();

            const string donorGGroup = "donor-g-group";
            const string donorPGroup = "donor-p-group";
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { donorGGroup })
                    .WithMatchingPGroups(new[] { sharedPGroup, donorPGroup })
                    .Build())
                .Build();

            var grade = consolidatedMolecularGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.PGroup);
        }

        [Test]
        public void CalculateGrade_ExpressingAlleleVsConsolidatedMolecular_WithDifferentGGroupsAndPGroups_ReturnsMismatch()
        {
            const string patientAlleleName = "999:999";
            const string patientGGroup = "patient-g-group";
            const string patientPGroup = "patient-p-group";
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(patientAlleleName)
                    .WithMatchingGGroup(patientGGroup)
                    .WithMatchingPGroup(patientPGroup)
                    .Build())
                .Build();

            const string donorGGroup = "donor-g-group";
            const string donorPGroup = "donor-p-group";
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { donorGGroup })
                    .WithMatchingPGroups(new[] { donorPGroup })
                    .Build())
                .Build();   

            var grade = consolidatedMolecularGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Mismatch);
        }

        #endregion

        // TODO: NOVA-1479 - Add tests for scoring Consolidated Molecular vs. null allele & vice versa

        #region Tests: Consolidated Molecular vs. Multiple Alleles

        [Test]
        public void CalculateGrade_ConsolidatedMolecularVsMultipleAllele_WithIntersectingGGroups_ReturnsGGroup()
        {
            const string sharedGGroup = "shared-g-group";

            const string patientGGroup = "patient-g-group";
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { sharedGGroup, patientGGroup })
                    .Build())
                .Build();

            const string donorGGroup = "donor-g-group";
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new []{
                        new SingleAlleleScoringInfoBuilder()
                        .WithMatchingGGroup(sharedGGroup)
                        .Build(),
                        new SingleAlleleScoringInfoBuilder()
                        .WithMatchingGGroup(donorGGroup)
                        .Build()
                        })
                    .Build())
                .Build();

            var grade = consolidatedMolecularGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.GGroup);
        }

        [Test]
        public void CalculateGrade_ConsolidatedMolecularVsMultipleAllele_WithDifferentGGroups_ButIntersectingPGroups_ReturnsGGroup()
        {
            const string sharedPGroup = "shared-p-group";

            const string patientGGroup = "patient-g-group";
            const string patientPGroup = "patient-p-group";
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { patientGGroup })
                    .WithMatchingPGroups(new[] { sharedPGroup, patientPGroup })
                    .Build())
                .Build();

            const string donorGGroup = "donor-g-group";
            const string donorPGroup = "donor-p-group";
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[]{
                        new SingleAlleleScoringInfoBuilder()
                            .WithMatchingGGroup(donorGGroup)
                            .WithMatchingPGroup(sharedPGroup)
                            .Build(),
                        new SingleAlleleScoringInfoBuilder()
                            .WithMatchingGGroup(donorGGroup)
                            .WithMatchingPGroup(donorPGroup)
                            .Build()
                    })
                    .Build())
                .Build();

            var grade = consolidatedMolecularGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.PGroup);
        }

        [Test]
        public void CalculateGrade_ConsolidatedMolecularVsMultipleAllele_WithDifferentGGroupsAndPGroups_ReturnsMismatch()
        {
            const string patientGGroup = "patient-g-group";
            const string patientPGroup = "patient-p-group";
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { patientGGroup })
                    .WithMatchingPGroups(new[] { patientPGroup })
                    .Build())
                .Build();

            const string donorGGroup = "donor-g-group";
            const string donorPGroup = "donor-p-group";
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[]{
                        new SingleAlleleScoringInfoBuilder()
                            .WithMatchingGGroup(donorGGroup)
                            .WithMatchingPGroup(donorPGroup)
                            .Build()
                    })
                    .Build())
                .Build();

            var grade = consolidatedMolecularGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Mismatch);
        }

        #endregion

        #region Tests: Multiple Alleles vs. Consolidated Molecular

        [Test]
        public void CalculateGrade_MultipleAlleleVsConsolidatedMolecular_WithIntersectingGGroups_ReturnsGGroup()
        {
            const string sharedGGroup = "shared-g-group";

            const string patientGGroup = "patient-g-group";
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[]{
                        new SingleAlleleScoringInfoBuilder()
                            .WithMatchingGGroup(sharedGGroup)
                            .Build(),
                        new SingleAlleleScoringInfoBuilder()
                            .WithMatchingGGroup(patientGGroup)
                            .Build()
                    })
                    .Build())
                .Build();

            const string donorGGroup = "donor-g-group";
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { sharedGGroup, donorGGroup })
                    .Build())
                .Build();

            var grade = consolidatedMolecularGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.GGroup);
        }

        [Test]
        public void CalculateGrade_MultipleAlleleVsConsolidatedMolecular_WithDifferentGGroups_ButIntersectingPGroups_ReturnsGGroup()
        {
            const string sharedPGroup = "shared-p-group";

            const string patientGGroup = "patient-g-group";
            const string patientPGroup = "patient-p-group";
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[]{
                        new SingleAlleleScoringInfoBuilder()
                            .WithMatchingGGroup(patientGGroup)
                            .WithMatchingPGroup(sharedPGroup)
                            .Build(),
                        new SingleAlleleScoringInfoBuilder()
                            .WithMatchingGGroup(patientGGroup)
                            .WithMatchingPGroup(patientPGroup)
                            .Build()
                    })
                    .Build())
                .Build();

            const string donorGGroup = "donor-g-group";
            const string donorPGroup = "donor-p-group";
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { donorGGroup })
                    .WithMatchingPGroups(new[] { sharedPGroup, donorPGroup })
                    .Build())
                .Build();

            var grade = consolidatedMolecularGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.PGroup);
        }

        [Test]
        public void CalculateGrade_MultipleAlleleVsConsolidatedMolecular_WithDifferentGGroupsAndPGroups_ReturnsMismatch()
        {
            const string patientGGroup = "patient-g-group";
            const string patientPGroup = "patient-p-group";
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[]{
                        new SingleAlleleScoringInfoBuilder()
                            .WithMatchingGGroup(patientGGroup)
                            .WithMatchingPGroup(patientPGroup)
                            .Build()
                    })
                    .Build())
                .Build();

            const string donorGGroup = "donor-g-group";
            const string donorPGroup = "donor-p-group";
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { donorGGroup })
                    .WithMatchingPGroups(new[] { donorPGroup })
                    .Build())
                .Build();

            var grade = consolidatedMolecularGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Mismatch);
        }

        #endregion
    }
}