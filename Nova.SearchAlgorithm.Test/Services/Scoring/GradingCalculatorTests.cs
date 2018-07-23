using FluentAssertions;
using Nova.SearchAlgorithm.Common.Models.Scoring;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.Services.Scoring.Grading;
using Nova.SearchAlgorithm.Test.Builders;
using Nova.SearchAlgorithm.Test.Builders.ScoringInfo;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Services.Scoring
{
    [TestFixture]
    public class GradingCalculatorTests
    {
        private IGradingCalculator gradingCalculator;

        [SetUp]
        public void SetUp()
        {
            gradingCalculator = new GradingCalculator();
        }

        [Test]
        public void CalculateGrade_BothTypingsAreExpressingAlleles_WithSameName_AndFullGDnaSequences_ReturnsGDna()
        {
            const string sharedAlleleName = "999:999";

            var patientAlleleStatus = new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.GDna);
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedAlleleName)
                    .WithAlleleTypingStatus(patientAlleleStatus).Build())
                .Build();

            var donorAlleleStatus = new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.GDna);
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedAlleleName)
                    .WithAlleleTypingStatus(donorAlleleStatus).Build())
                .Build();

            var grade = gradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.GDna);
        }

        [Test]
        public void CalculateGrade_BothTypingsAreExpressingAlleles_WithSameName_AndFullCDnaSequences_ReturnsCDna()
        {
            const string sharedAlleleName = "999:999";

            var patientAlleleStatus = new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.CDna);
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedAlleleName)
                    .WithAlleleTypingStatus(patientAlleleStatus).Build())
                .Build();

            var donorAlleleStatus = new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.CDna);
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedAlleleName)
                    .WithAlleleTypingStatus(donorAlleleStatus).Build())
                .Build();

            var grade = gradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.CDna);
        }

        [TestCase(DnaCategory.GDna, DnaCategory.GDna)]
        [TestCase(DnaCategory.CDna, DnaCategory.CDna)]
        [TestCase(DnaCategory.GDna, DnaCategory.CDna)]
        [TestCase(DnaCategory.CDna, DnaCategory.GDna)]
        public void CalculateGrade_BothTypingsAreExpressingAlleles_WithSameFirstThreeFields_AndFullSequences_ReturnsCDna(
            DnaCategory patientDnaCategory,
            DnaCategory donorDnaCategory)
        {
            const string sharedFirstThreeFields = "999:999:999";

            const string patientAlleleName = sharedFirstThreeFields + ":01";
            var patientAlleleStatus = new AlleleTypingStatus(SequenceStatus.Full, patientDnaCategory);
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(patientAlleleName)
                    .WithAlleleTypingStatus(patientAlleleStatus)
                    .Build())
                .Build();

            const string donorAlleleName = sharedFirstThreeFields + ":999";
            var donorAlleleStatus = new AlleleTypingStatus(SequenceStatus.Full, donorDnaCategory);
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(donorAlleleName)
                    .WithAlleleTypingStatus(donorAlleleStatus)
                    .Build())
                .Build();

            var grade = gradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.CDna);
        }

        [TestCase(DnaCategory.GDna, DnaCategory.GDna)]
        [TestCase(DnaCategory.CDna, DnaCategory.CDna)]
        [TestCase(DnaCategory.GDna, DnaCategory.CDna)]
        [TestCase(DnaCategory.CDna, DnaCategory.GDna)]
        public void CalculateGrade_BothTypingsAreExpressingAlleles_WithSameFirstTwoFields_AndFullSequences_ReturnsProtein(
            DnaCategory patientDnaCategory,
            DnaCategory donorDnaCategory)
        {
            const string sharedFirstTwoFields = "999:999";

            const string patientAlleleName = sharedFirstTwoFields + ":11";
            var patientAlleleStatus = new AlleleTypingStatus(SequenceStatus.Full, patientDnaCategory);
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(patientAlleleName)
                    .WithAlleleTypingStatus(patientAlleleStatus)
                    .Build())
                .Build();

            const string donorAlleleName = sharedFirstTwoFields + ":22";
            var donorAlleleStatus = new AlleleTypingStatus(SequenceStatus.Full, donorDnaCategory);
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(donorAlleleName)
                    .WithAlleleTypingStatus(donorAlleleStatus)
                    .Build())
                .Build();

            var grade = gradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.CDna);
        }

        [Test]
        public void CalculateGrade_BothTypingsAreExpressingAlleles_WithDifferentNames_ButSameGGroup_ReturnsGGroup()
        {
            const string sharedGGroup = "shared-g-group";

            const string patientAlleleName = "111:111";
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(patientAlleleName)
                    .WithMatchingGGroup(sharedGGroup)
                    .Build())
                .Build();

            const string donorAlleleName = "999:999";
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(donorAlleleName)
                    .WithMatchingGGroup(sharedGGroup)
                    .Build())
                .Build();

            var grade = gradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.GGroup);
        }

        [Test]
        public void CalculateGrade_BothTypingsAreExpressingAlleles_WithDifferentNamesAndGGroups_ButSamePGroup_ReturnsGGroup()
        {
            const string sharedPGroup = "shared-p-group";

            const string patientAlleleName = "111:111";
            const string patientGGroup = "patient-g-group";
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(patientAlleleName)
                    .WithMatchingGGroup(patientGGroup)
                    .WithMatchingPGroup(sharedPGroup)
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

            var grade = gradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.PGroup);
        }

        [Test]
        public void CalculateGrade_BothTypingsAreExpressingAlleles_WithDifferentNamesAndGGroupsAndPGroups_ReturnsMismatch()
        {
            const string patientAlleleName = "111:111";
            const string patientGGroup = "patient-g-group";
            const string patientPGroup = "patient-p-group";
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(patientAlleleName)
                    .WithMatchingGGroup(patientGGroup)
                    .WithMatchingPGroup(patientPGroup)
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

            var grade = gradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Mismatch);
        }

        [TestCase("999:999", "999:999N")]
        [TestCase("999:999N", "999:999")]
        public void CalculateGrade_OneAlleleIsExpressingAndOtherIsNullExpresser_ReturnsMismatch(
            string patientAlleleName,
            string donorAlleleName)
        {
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(patientAlleleName)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(donorAlleleName)
                    .Build())
                .Build();

            var grade = gradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Mismatch);
        }

        // TODO: NOVA-1479 - Add tests for scoring null vs. null alleles

        [Test]
        public void CalculateGrade_BothTypingsAreSerology_WithSameName_AndAssociatedSubtype_ReturnsAssociated()
        {
            const string sharedSerologyName = "9999";

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(sharedSerologyName)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(SerologySubtype.Associated)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(sharedSerologyName)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(SerologySubtype.Associated)
                    .Build())
                .Build();

            var grade = gradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Associated);
        }

        [Test]
        public void CalculateGrade_BothTypingsAreSerology_WithSameName_AndSplitSubtype_ReturnsSplit()
        {
            const string sharedSerologyName = "9999";

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(sharedSerologyName)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(SerologySubtype.Split)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(sharedSerologyName)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(SerologySubtype.Split)
                    .Build())
                .Build();

            var grade = gradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_BothTypingsAreSerology_WithSameName_AndNotSplitSubtype_ReturnsSplit()
        {
            const string sharedSerologyName = "9999";

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(sharedSerologyName)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(SerologySubtype.NotSplit)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(sharedSerologyName)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(SerologySubtype.NotSplit)
                    .Build())
                .Build();

            var grade = gradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        // This covers the case from WMDA matching framework:
        // s1 does not have any splits & s2 is associated to s1, or vice versa.
        [TestCase(SerologySubtype.Split, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.Split)]
        [TestCase(SerologySubtype.NotSplit, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.NotSplit)]
        public void CalculateGrade_BothTypingsAreSerology_WithDifferentNames_ButHaveAssociatedRelationship_ReturnsSplit(
            SerologySubtype patientSerologySubtype,
            SerologySubtype donorSerologySubtype
           )
        {
            var patientSerology = new SerologyEntry("patient-serology", patientSerologySubtype);
            var donorSerology = new SerologyEntry("donor-serology", donorSerologySubtype);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(patientSerology.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(patientSerology.SerologySubtype)
                    .WithMatchingSerologies(new[] { donorSerology })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(donorSerology.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(donorSerology.SerologySubtype)
                    .WithMatchingSerologies(new[] { patientSerology })
                    .Build())
                .Build();

            var grade = gradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_BothTypingsAreSerology_WithSameName_AndBroadSubtype_ReturnsBroad()
        {
            const string sharedSerologyName = "9999";

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(sharedSerologyName)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(SerologySubtype.Broad)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(sharedSerologyName)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(SerologySubtype.Broad)
                    .Build())
                .Build();

            var grade = gradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Broad);
        }

        // This covers the case from WMDA matching framework:
        // s1 is a broad & s2 is a split of s1, or vice versa.
        // s1 is a broad & s2 is associated to a split of s1, or vice versa.
        [TestCase(SerologySubtype.Broad, SerologySubtype.Split)]
        [TestCase(SerologySubtype.Split, SerologySubtype.Broad)]
        [TestCase(SerologySubtype.Broad, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.Broad)]
        public void CalculateGrade_BothTypingsAreSerology_WithDifferentNames_ButHaveBroadRelationship_ReturnsBroad(
            SerologySubtype patientSerologySubtype,
            SerologySubtype donorSerologySubtype
        )
        {
            var patientSerology = new SerologyEntry("patient-serology", patientSerologySubtype);
            var donorSerology = new SerologyEntry("donor-serology", donorSerologySubtype);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(patientSerology.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(patientSerology.SerologySubtype)
                    .WithMatchingSerologies(new[] { donorSerology })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(donorSerology.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(donorSerology.SerologySubtype)
                    .WithMatchingSerologies(new[] { patientSerology })
                    .Build())
                .Build();

            var grade = gradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Broad);
        }
    }
}