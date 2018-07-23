using FluentAssertions;
using Nova.SearchAlgorithm.Common.Models.Scoring;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
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
            const string sharedGGroup = "888:888:888G";

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
            const string sharedPGroup = "888:888P";

            const string patientAlleleName = "111:111";
            const string patientGGroup = "111:111:111G";
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(patientAlleleName)
                    .WithMatchingGGroup(patientGGroup)
                    .WithMatchingPGroup(sharedPGroup)
                    .Build())
                .Build();

            const string donorAlleleName = "999:999";
            const string donorGGroup = "999:999:999G";
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
            const string patientGGroup = "111:111:111G";
            const string patientPGroup = "111:111P";
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(patientAlleleName)
                    .WithMatchingGGroup(patientGGroup)
                    .WithMatchingPGroup(patientPGroup)
                    .Build())
                .Build();

            const string donorAlleleName = "999:999";
            const string donorGGroup = "999:999:999G";
            const string donorPGroup = "999:999P";
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
    }
}