﻿using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using Nova.SearchAlgorithm.Services.Scoring.Grading;
using Nova.SearchAlgorithm.Test.Builders;
using Nova.SearchAlgorithm.Test.Builders.ScoringInfo;
using NUnit.Framework;
using System;

namespace Nova.SearchAlgorithm.Test.Services.Scoring.Grading
{
    [TestFixture]
    public class MultipleAlleleGradingCalculatorTests :
        GradingCalculatorTestsBase<MultipleAlleleGradingCalculator>
    {
        #region Tests: Exception Cases

        [TestCase(typeof(SingleAlleleScoringInfo), typeof(SingleAlleleScoringInfo))]
        [TestCase(typeof(ConsolidatedMolecularScoringInfo), typeof(ConsolidatedMolecularScoringInfo))]
        [TestCase(typeof(SerologyScoringInfo), typeof(ConsolidatedMolecularScoringInfo))]
        [TestCase(typeof(ConsolidatedMolecularScoringInfo), typeof(SerologyScoringInfo))]
        [TestCase(typeof(SerologyScoringInfo), typeof(SerologyScoringInfo))]
        public override void CalculateGrade_OneOrBothScoringInfosAreNotOfPermittedTypes_ThrowsException(
            Type patientScoringInfoType,
            Type donorScoringInfoType
            )
        {
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(ScoringInfoBuilderFactory.GetDefaultScoringInfoFromBuilder(patientScoringInfoType))
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(ScoringInfoBuilderFactory.GetDefaultScoringInfoFromBuilder(donorScoringInfoType))
                .Build();

            Assert.Throws<ArgumentException>(() =>
                GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult));
        }

        #endregion

        #region Tests: Both Typings Multiple Allele

        [Test]
        public void CalculateGrade_BothTypingsAreMultipleAllele_WithSameExpressingAllele_HavingFullGDnaSequence_ReturnsGDna()
        {
            var sharedAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("999:999")
                .WithAlleleTypingStatus(new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.GDna))
                .Build();

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { sharedAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { sharedAllele })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.GDna);
        }

        [Test]
        public void CalculateGrade_BothTypingsAreMultipleAllele_WithSameExpressingAllele_HavingFullCDnaSequence_ReturnsCDna()
        {
            var sharedAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("999:999")
                .WithAlleleTypingStatus(new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.CDna))
                .Build();

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { sharedAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { sharedAllele })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.CDna);
        }

        [TestCase(DnaCategory.GDna, DnaCategory.GDna)]
        [TestCase(DnaCategory.CDna, DnaCategory.CDna)]
        [TestCase(DnaCategory.GDna, DnaCategory.CDna)]
        [TestCase(DnaCategory.CDna, DnaCategory.GDna)]
        public void CalculateGrade_BothTypingsAreMultipleAllele_WithSameExpressingAllele_HavingSameFirstThreeFields_AndFullSequence_ReturnsCDna(
            DnaCategory patientDnaCategory,
            DnaCategory donorDnaCategory)
        {
            const string sharedFirstThreeFields = "999:999:999";

            var patientAllele = new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedFirstThreeFields + ":01")
                    .WithAlleleTypingStatus(new AlleleTypingStatus(SequenceStatus.Full, patientDnaCategory))
                    .Build();
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { patientAllele })
                    .Build())
                .Build();

            var donorAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName(sharedFirstThreeFields + ":999")
                .WithAlleleTypingStatus(new AlleleTypingStatus(SequenceStatus.Full, donorDnaCategory))
                .Build();
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { donorAllele })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.CDna);
        }

        [TestCase(DnaCategory.GDna, DnaCategory.GDna)]
        [TestCase(DnaCategory.CDna, DnaCategory.CDna)]
        [TestCase(DnaCategory.GDna, DnaCategory.CDna)]
        [TestCase(DnaCategory.CDna, DnaCategory.GDna)]
        public void CalculateGrade_BothTypingsAreMultipleAllele_WithSameExpressingAllele_HavingSameFirstTwoFields_AndFullSequence_ReturnsProtein(
            DnaCategory patientDnaCategory,
            DnaCategory donorDnaCategory)
        {
            const string sharedFirstTwoFields = "999:999";

            var patientAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName(sharedFirstTwoFields + ":11")
                .WithAlleleTypingStatus(new AlleleTypingStatus(SequenceStatus.Full, patientDnaCategory))
                .Build();
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { patientAllele })
                    .Build())
                .Build();

            var donorAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName(sharedFirstTwoFields + ":22")
                .WithAlleleTypingStatus(new AlleleTypingStatus(SequenceStatus.Full, donorDnaCategory))
                .Build();
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { donorAllele })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Protein);
        }

        [Test]
        public void CalculateGrade_BothTypingsAreMultipleAllele_WithDifferentExpressingAlleles_ButFromSameGGroup_ReturnsGGroup()
        {
            const string sharedGGroup = "shared-g-group";

            var patientAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("111:111")
                .WithMatchingGGroup(sharedGGroup)
                .Build();
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { patientAllele })
                    .Build())
                .Build();

            var donorAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("999:999")
                .WithMatchingGGroup(sharedGGroup)
                .Build();
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { donorAllele })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.GGroup);
        }

        [Test]
        public void CalculateGrade_BothTypingsAreMultipleAllele_WithDifferentExpressingAlleles_ButFromSamePGroup_ReturnsPGroup()
        {
            const string sharedPGroup = "shared-p-group";

            var patientAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("111:111")
                .WithMatchingGGroup("patient-g-group")
                .WithMatchingPGroup(sharedPGroup)
                .Build();
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { patientAllele })
                    .Build())
                .Build();

            var donorAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("999:999")
                .WithMatchingGGroup("donor-g-group")
                .WithMatchingPGroup(sharedPGroup)
                .Build();
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { donorAllele })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.PGroup);
        }

        [Test]
        public void CalculateGrade_BothTypingsAreMultipleAllele_WithDifferentExpressingAlleles_FromDifferentGGroupsAndPGroups_ReturnsMismatch()
        {
            var patientAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("111:111")
                .WithMatchingGGroup("patient-g-group")
                .WithMatchingPGroup("patient-p-group")
                .Build();
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { patientAllele })
                    .Build())
                .Build();

            var donorAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("999:999")
                .WithMatchingGGroup("donor-g-group")
                .WithMatchingPGroup("donor-p-group")
                .Build();
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { donorAllele })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Mismatch);
        }

        #endregion

        #region Tests: Multiple vs. Expressing Allele

        [Test]
        public void CalculateGrade_MultipleAlleleVsExpressingAllele_WithSameExpressingAllele_HavingFullGDnaSequence_ReturnsGDna()
        {
            var sharedAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("999:999")
                .WithAlleleTypingStatus(new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.GDna))
                .Build();

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { sharedAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(sharedAllele)
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.GDna);
        }

        [Test]
        public void CalculateGrade_MultipleAlleleVsExpressingAllele_WithSameExpressingAllele_HavingFullCDnaSequence_ReturnsCDna()
        {
            var sharedAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("999:999")
                .WithAlleleTypingStatus(new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.CDna))
                .Build();

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { sharedAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(sharedAllele)
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.CDna);
        }

        [TestCase(DnaCategory.GDna, DnaCategory.GDna)]
        [TestCase(DnaCategory.CDna, DnaCategory.CDna)]
        [TestCase(DnaCategory.GDna, DnaCategory.CDna)]
        [TestCase(DnaCategory.CDna, DnaCategory.GDna)]
        public void CalculateGrade_MultipleAlleleVsExpressingAllele_WithSameExpressingAllele_HavingSameFirstThreeFields_AndFullSequence_ReturnsCDna(
            DnaCategory patientDnaCategory,
            DnaCategory donorDnaCategory)
        {
            const string sharedFirstThreeFields = "999:999:999";

            var patientAllele = new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedFirstThreeFields + ":01")
                    .WithAlleleTypingStatus(new AlleleTypingStatus(SequenceStatus.Full, patientDnaCategory))
                    .Build();
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { patientAllele })
                    .Build())
                .Build();

            var donorAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName(sharedFirstThreeFields + ":999")
                .WithAlleleTypingStatus(new AlleleTypingStatus(SequenceStatus.Full, donorDnaCategory))
                .Build();
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(donorAllele)
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.CDna);
        }

        [TestCase(DnaCategory.GDna, DnaCategory.GDna)]
        [TestCase(DnaCategory.CDna, DnaCategory.CDna)]
        [TestCase(DnaCategory.GDna, DnaCategory.CDna)]
        [TestCase(DnaCategory.CDna, DnaCategory.GDna)]
        public void CalculateGrade_MultipleAlleleVsExpressingAllele_WithSameExpressingAllele_HavingSameFirstTwoFields_AndFullSequence_ReturnsProtein(
            DnaCategory patientDnaCategory,
            DnaCategory donorDnaCategory)
        {
            const string sharedFirstTwoFields = "999:999";

            var patientAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName(sharedFirstTwoFields + ":11")
                .WithAlleleTypingStatus(new AlleleTypingStatus(SequenceStatus.Full, patientDnaCategory))
                .Build();
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { patientAllele })
                    .Build())
                .Build();

            var donorAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName(sharedFirstTwoFields + ":22")
                .WithAlleleTypingStatus(new AlleleTypingStatus(SequenceStatus.Full, donorDnaCategory))
                .Build();
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(donorAllele)
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Protein);
        }

        [Test]
        public void CalculateGrade_MultipleAlleleVsExpressingAllele_WithDifferentExpressingAlleles_ButFromSameGGroup_ReturnsGGroup()
        {
            const string sharedGGroup = "shared-g-group";

            var patientAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("111:111")
                .WithMatchingGGroup(sharedGGroup)
                .Build();
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { patientAllele })
                    .Build())
                .Build();

            var donorAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("999:999")
                .WithMatchingGGroup(sharedGGroup)
                .Build();
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(donorAllele)
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.GGroup);
        }

        [Test]
        public void CalculateGrade_MultipleAlleleVsExpressingAllele_WithDifferentExpressingAlleles_ButFromSamePGroup_ReturnsPGroup()
        {
            const string sharedPGroup = "shared-p-group";

            var patientAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("111:111")
                .WithMatchingGGroup("patient-g-group")
                .WithMatchingPGroup(sharedPGroup)
                .Build();
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { patientAllele })
                    .Build())
                .Build();

            var donorAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("999:999")
                .WithMatchingGGroup("donor-g-group")
                .WithMatchingPGroup(sharedPGroup)
                .Build();
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(donorAllele)
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.PGroup);
        }

        [Test]
        public void CalculateGrade_MultipleAlleleVsExpressingAllele_WithDifferentExpressingAlleles_FromDifferentGGroupsAndPGroups_ReturnsMismatch()
        {
            var patientAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("111:111")
                .WithMatchingGGroup("patient-g-group")
                .WithMatchingPGroup("patient-p-group")
                .Build();
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { patientAllele })
                    .Build())
                .Build();

            var donorAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("999:999")
                .WithMatchingGGroup("donor-g-group")
                .WithMatchingPGroup("donor-p-group")
                .Build();
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(donorAllele)
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Mismatch);
        }

        #endregion

        #region Tests: Expressing Allele vs. Multiple Allele

        [Test]
        public void CalculateGrade_ExpressingAlleleVsMultipleAllele_WithSameExpressingAllele_HavingFullGDnaSequence_ReturnsGDna()
        {
            var sharedAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("999:999")
                .WithAlleleTypingStatus(new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.GDna))
                .Build();

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(sharedAllele)
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { sharedAllele })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.GDna);
        }

        [Test]
        public void CalculateGrade_ExpressingAlleleVsMultipleAllele_WithSameExpressingAllele_HavingFullCDnaSequence_ReturnsCDna()
        {
            var sharedAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("999:999")
                .WithAlleleTypingStatus(new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.CDna))
                .Build();

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(sharedAllele)
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { sharedAllele })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.CDna);
        }

        [TestCase(DnaCategory.GDna, DnaCategory.GDna)]
        [TestCase(DnaCategory.CDna, DnaCategory.CDna)]
        [TestCase(DnaCategory.GDna, DnaCategory.CDna)]
        [TestCase(DnaCategory.CDna, DnaCategory.GDna)]
        public void CalculateGrade_ExpressingAlleleVsMultipleAllele_WithSameExpressingAllele_HavingSameFirstThreeFields_AndFullSequence_ReturnsCDna(
            DnaCategory patientDnaCategory,
            DnaCategory donorDnaCategory)
        {
            const string sharedFirstThreeFields = "999:999:999";

            var patientAllele = new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedFirstThreeFields + ":01")
                    .WithAlleleTypingStatus(new AlleleTypingStatus(SequenceStatus.Full, patientDnaCategory))
                    .Build();
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(patientAllele)
                .Build();

            var donorAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName(sharedFirstThreeFields + ":999")
                .WithAlleleTypingStatus(new AlleleTypingStatus(SequenceStatus.Full, donorDnaCategory))
                .Build();
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { donorAllele })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.CDna);
        }

        [TestCase(DnaCategory.GDna, DnaCategory.GDna)]
        [TestCase(DnaCategory.CDna, DnaCategory.CDna)]
        [TestCase(DnaCategory.GDna, DnaCategory.CDna)]
        [TestCase(DnaCategory.CDna, DnaCategory.GDna)]
        public void CalculateGrade_ExpressingAlleleVsMultipleAllele_WithSameExpressingAllele_HavingSameFirstTwoFields_AndFullSequence_ReturnsProtein(
            DnaCategory patientDnaCategory,
            DnaCategory donorDnaCategory)
        {
            const string sharedFirstTwoFields = "999:999";

            var patientAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName(sharedFirstTwoFields + ":11")
                .WithAlleleTypingStatus(new AlleleTypingStatus(SequenceStatus.Full, patientDnaCategory))
                .Build();
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(patientAllele)
                .Build();

            var donorAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName(sharedFirstTwoFields + ":22")
                .WithAlleleTypingStatus(new AlleleTypingStatus(SequenceStatus.Full, donorDnaCategory))
                .Build();
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { donorAllele })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Protein);
        }

        [Test]
        public void CalculateGrade_ExpressingAlleleVsMultipleAllele_WithDifferentExpressingAlleles_ButFromSameGGroup_ReturnsGGroup()
        {
            const string sharedGGroup = "shared-g-group";

            var patientAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("111:111")
                .WithMatchingGGroup(sharedGGroup)
                .Build();
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(patientAllele)
                .Build();

            var donorAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("999:999")
                .WithMatchingGGroup(sharedGGroup)
                .Build();
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { donorAllele })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.GGroup);
        }

        [Test]
        public void CalculateGrade_ExpressingAlleleVsMultipleAllele_WithDifferentExpressingAlleles_ButFromSamePGroup_ReturnsPGroup()
        {
            const string sharedPGroup = "shared-p-group";

            var patientAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("111:111")
                .WithMatchingGGroup("patient-g-group")
                .WithMatchingPGroup(sharedPGroup)
                .Build();
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(patientAllele)
                .Build();

            var donorAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("999:999")
                .WithMatchingGGroup("donor-g-group")
                .WithMatchingPGroup(sharedPGroup)
                .Build();
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { donorAllele })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.PGroup);
        }

        [Test]
        public void CalculateGrade_ExpressingAlleleVsMultipleAllele_WithDifferentExpressingAlleles_FromDifferentGGroupsAndPGroups_ReturnsMismatch()
        {
            var patientAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("111:111")
                .WithMatchingGGroup("patient-g-group")
                .WithMatchingPGroup("patient-p-group")
                .Build();
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(patientAllele)
                .Build();

            var donorAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("999:999")
                .WithMatchingGGroup("donor-g-group")
                .WithMatchingPGroup("donor-p-group")
                .Build();
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { donorAllele })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Mismatch);
        }

        #endregion

        [Test]
        public void CalculateGrade_MoreThanOnePossibleMatchGrade_BestGradeReturned()
        {
            const string sharedPGroup = "shared-p-group";

            var sharedGDnaAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("111:111")
                .WithAlleleTypingStatus(new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.GDna))
                .WithMatchingPGroup(sharedPGroup)
                .Build();

            const string mismatchedGGroup = "mismatch-g-group";
            var patientPGroupMatchedAllele = new SingleAlleleScoringInfoBuilder()
                .WithMatchingGGroup(mismatchedGGroup)
                .WithMatchingPGroup(sharedPGroup)
                .Build();

            var patientMismatchedAllele = new SingleAlleleScoringInfoBuilder()
                .WithMatchingGGroup(mismatchedGGroup)
                .WithMatchingPGroup("mismatched-p-group")
                .Build();

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { sharedGDnaAllele, patientPGroupMatchedAllele, patientMismatchedAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(sharedGDnaAllele)
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            // Possible grades are GDna, PGroup, or Mismatch; GDna should be returned.
            grade.Should().Be(MatchGrade.GDna);
        }
    }
}