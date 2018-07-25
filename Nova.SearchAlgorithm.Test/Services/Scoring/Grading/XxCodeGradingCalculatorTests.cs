using FluentAssertions;
using Nova.SearchAlgorithm.Common.Models.Scoring;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.Services.Scoring.Grading;
using Nova.SearchAlgorithm.Test.Builders;
using Nova.SearchAlgorithm.Test.Builders.ScoringInfo;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Services.Scoring.Grading
{
    [TestFixture]
    public class XxCodeGradingCalculatorTests
    {
        private IXxCodeGradingCalculator xxCodeGradingCalculator;

        [SetUp]
        public void SetUp()
        {
            xxCodeGradingCalculator = new XxCodeGradingCalculator();
        }

        #region Tests: Both Typings XX Codes

        [Test]
        public void CalculateGrade_BothTypingsAreXxCodes_WithSameName_ReturnsGGroup()
        {
            const string sharedXxCodeName = "shared-xx-code";

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(sharedXxCodeName)
                .WithLookupResultCategory(LookupResultCategory.XxCode)
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(sharedXxCodeName)
                .WithLookupResultCategory(LookupResultCategory.XxCode)
                .Build();

            var grade = xxCodeGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.GGroup);
        }

        [Test]
        public void CalculateGrade_BothTypingsAreXxCodes_WithDifferentNames_ButIntersectingGGroups_ReturnsGGroup()
        {
            const string sharedGGroup = "shared-g-group";

            const string patientXxCodeName = "patient-xx-code";
            const string patientGGroup = "patient-g-group";
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(patientXxCodeName)
                .WithLookupResultCategory(LookupResultCategory.XxCode)
                .WithHlaScoringInfo(new XxCodeScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { sharedGGroup, patientGGroup })
                    .Build())
                .Build();

            const string donorXxCodeName = "donor-xx-code";
            const string donorGGroup = "donor-g-group";
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(donorXxCodeName)
                .WithLookupResultCategory(LookupResultCategory.XxCode)
                .WithHlaScoringInfo(new XxCodeScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { sharedGGroup, donorGGroup })
                    .Build())
                .Build();

            var grade = xxCodeGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.GGroup);
        }

        [Test]
        public void CalculateGrade_BothTypingsAreXxCodes_WithDifferentNamesAndGGroups_ButIntersectingPGroups_ReturnsGGroup()
        {
            const string sharedPGroup = "shared-p-group";

            const string patientXxCodeName = "patient-xx-code";
            const string patientGGroup = "patient-g-group";
            const string patientPGroup = "patient-p-group";
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(patientXxCodeName)
                .WithLookupResultCategory(LookupResultCategory.XxCode)
                .WithHlaScoringInfo(new XxCodeScoringInfoBuilder()
                    .WithMatchingGGroups(new []{ patientGGroup })
                    .WithMatchingPGroups(new[] { sharedPGroup, patientPGroup })
                    .Build())
                .Build();

            const string donorXxCodeName = "donor-xx-code";
            const string donorGGroup = "donor-g-group";
            const string donorPGroup = "donor-p-group";
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(donorXxCodeName)
                .WithLookupResultCategory(LookupResultCategory.XxCode)
                .WithHlaScoringInfo(new XxCodeScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { donorGGroup })
                    .WithMatchingPGroups(new[] { sharedPGroup, donorPGroup })
                    .Build())
                .Build();

            var grade = xxCodeGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.PGroup);
        }

        [Test]
        public void CalculateGrade_BothTypingsAreXxCodes_WithDifferentNamesAndGGroupsAndPGroups_ReturnsMismatch()
        {
            const string patientXxCodeName = "patient-xx-code";
            const string patientGGroup = "patient-g-group";
            const string patientPGroup = "patient-p-group";
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(patientXxCodeName)
                .WithLookupResultCategory(LookupResultCategory.XxCode)
                .WithHlaScoringInfo(new XxCodeScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { patientGGroup })
                    .WithMatchingPGroups(new[] { patientPGroup })
                    .Build())
                .Build();

            const string donorXxCodeName = "donor-xx-code";
            const string donorGGroup = "donor-g-group";
            const string donorPGroup = "donor-p-group";
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(donorXxCodeName)
                .WithLookupResultCategory(LookupResultCategory.XxCode)
                .WithHlaScoringInfo(new XxCodeScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { donorGGroup })
                    .WithMatchingPGroups(new[] { donorPGroup })
                    .Build())
                .Build();

            var grade = xxCodeGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Mismatch);
        }

        #endregion

        #region Tests: XX Code vs. Expressing Allele

        [Test]
        public void CalculateGrade_XxCodeVsExpressingAllele_WithIntersectingGGroups_ReturnsGGroup()
        {
            const string sharedGGroup = "shared-g-group";

            const string patientGGroup = "patient-g-group";
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.XxCode)
                .WithHlaScoringInfo(new XxCodeScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { sharedGGroup, patientGGroup })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.OriginalAllele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingGGroup(sharedGGroup)
                    .Build())
                .Build();

            var grade = xxCodeGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.GGroup);
        }

        [Test]
        public void CalculateGrade_XxCodeVsExpressingAllele_WithDifferentGGroups_ButIntersectingPGroups_ReturnsGGroup()
        {
            const string sharedPGroup = "shared-p-group";

            const string patientGGroup = "patient-g-group";
            const string patientPGroup = "patient-p-group";
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.XxCode)
                .WithHlaScoringInfo(new XxCodeScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { patientGGroup })
                    .WithMatchingPGroups(new[] { sharedPGroup, patientPGroup })
                    .Build())
                .Build();

            const string donorGGroup = "donor-g-group";
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.OriginalAllele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingGGroup(donorGGroup)
                    .WithMatchingPGroup(sharedPGroup)
                    .Build())
                .Build();

            var grade = xxCodeGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.PGroup);
        }

        [Test]
        public void CalculateGrade_XxCodeVsExpressingAllele_WithDifferentGGroupsAndPGroups_ReturnsMismatch()
        {
            const string patientGGroup = "patient-g-group";
            const string patientPGroup = "patient-p-group";
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.XxCode)
                .WithHlaScoringInfo(new XxCodeScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { patientGGroup })
                    .WithMatchingPGroups(new[] { patientPGroup })
                    .Build())
                .Build();

            const string donorGGroup = "donor-g-group";
            const string donorPGroup = "donor-p-group";
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.OriginalAllele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingGGroup(donorGGroup)
                    .WithMatchingPGroup(donorPGroup)
                    .Build())
                .Build();

            var grade = xxCodeGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Mismatch);
        }

        #endregion

        #region Tests: Expressing Allele vs. XX Code

        [Test]
        public void CalculateGrade_ExpressingAlleleVsXxCode_WithIntersectingGGroups_ReturnsGGroup()
        {
            const string sharedGGroup = "shared-g-group";

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.OriginalAllele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingGGroup(sharedGGroup)
                    .Build())
                .Build();

            const string donorGGroup = "donor-g-group";
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.XxCode)
                .WithHlaScoringInfo(new XxCodeScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { sharedGGroup, donorGGroup })
                    .Build())
                .Build();

            var grade = xxCodeGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.GGroup);
        }

        [Test]
        public void CalculateGrade_ExpressingAlleleVsXxCode_WithDifferentGGroups_ButIntersectingPGroups_ReturnsGGroup()
        {
            const string sharedPGroup = "shared-p-group";

            const string patientGGroup = "patient-g-group";
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.OriginalAllele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingGGroup(patientGGroup)
                    .WithMatchingPGroup(sharedPGroup)
                    .Build())
                .Build();

            const string donorGGroup = "donor-g-group";
            const string donorPGroup = "donor-p-group";
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.XxCode)
                .WithHlaScoringInfo(new XxCodeScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { donorGGroup })
                    .WithMatchingPGroups(new[] { sharedPGroup, donorPGroup })
                    .Build())
                .Build();

            var grade = xxCodeGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.PGroup);
        }

        [Test]
        public void CalculateGrade_ExpressingVsAlleleXxCode_WithDifferentGGroupsAndPGroups_ReturnsMismatch()
        {
            const string patientGGroup = "patient-g-group";
            const string patientPGroup = "patient-p-group";
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.OriginalAllele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingGGroup(patientGGroup)
                    .WithMatchingPGroup(patientPGroup)
                    .Build())
                .Build();

            const string donorGGroup = "donor-g-group";
            const string donorPGroup = "donor-p-group";
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.XxCode)
                .WithHlaScoringInfo(new XxCodeScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { donorGGroup })
                    .WithMatchingPGroups(new[] { donorPGroup })
                    .Build())
                .Build();   

            var grade = xxCodeGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Mismatch);
        }

        #endregion

        // TODO: NOVA-1479 - Add tests for scoring XX code vs. null allele & vice versa
    }
}