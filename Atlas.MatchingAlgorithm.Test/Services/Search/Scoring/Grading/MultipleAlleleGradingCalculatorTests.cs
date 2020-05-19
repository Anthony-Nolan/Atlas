﻿using System;
using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.Models.HLATypings;
using Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults.PerLocus;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading.GradingCalculators;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.ScoringInfo;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Search.Scoring.Grading
{
    [TestFixture]
    public class MultipleAlleleGradingCalculatorTests : GradingCalculatorTestsBase
    {
        private const Locus Dpb1Locus = Locus.Dpb1;
        private const Locus NonDpb1Locus = Locus.A;

        private IPermissiveMismatchCalculator permissiveMismatchCalculator;

        [SetUp]
        public override void SetUpGradingCalculator()
        {
            permissiveMismatchCalculator = Substitute.For<IPermissiveMismatchCalculator>();
            GradingCalculator = new MultipleAlleleGradingCalculator(permissiveMismatchCalculator);
        }

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
        public void CalculateGrade_BothTypingsAreMultipleAllele_AtDpb1_WithDifferentExpressingAlleles_FromDifferentGGroupsAndPGroups_ButPermissivelyMismatched_ReturnsPermissiveMismatch()
        {
            const string patientAlleleName = "111:111";
            var patientAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName(patientAlleleName)
                .WithMatchingGGroup("patient-g-group")
                .WithMatchingPGroup("patient-p-group")
                .Build();
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .AtLocus(Dpb1Locus)
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { patientAllele })
                    .Build())
                .Build();

            const string donorAlleleName = "999:999";
            var donorAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName(donorAlleleName)
                .WithMatchingGGroup("donor-g-group")
                .WithMatchingPGroup("donor-p-group")
                .Build();            
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .AtLocus(Dpb1Locus)
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { donorAllele })
                    .Build())
                .Build();

            permissiveMismatchCalculator
                .IsPermissiveMismatch(Dpb1Locus, patientAlleleName, donorAlleleName)
                .Returns(true);

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.PermissiveMismatch);
        }

        [Test]
        public void CalculateGrade_BothTypingsAreMultipleAllele_AtDpb1_WithDifferentExpressingAlleles_FromDifferentGGroupsAndPGroups_AndNotPermissivelyMismatched_ReturnsMismatch()
        {
            const string patientAlleleName = "111:111";
            var patientAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName(patientAlleleName)
                .WithMatchingGGroup("patient-g-group")
                .WithMatchingPGroup("patient-p-group")
                .Build();
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .AtLocus(Dpb1Locus)
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { patientAllele })
                    .Build())
                .Build();

            const string donorAlleleName = "donor-hla-name";
            var donorAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName(donorAlleleName)
                .WithMatchingGGroup("donor-g-group")
                .WithMatchingPGroup("donor-p-group")
                .Build();
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .AtLocus(Dpb1Locus)
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { donorAllele })
                    .Build())
                .Build();

            permissiveMismatchCalculator
                .IsPermissiveMismatch(Dpb1Locus, patientAlleleName, donorAlleleName)
                .Returns(false);

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Mismatch);
        }

        [Test]
        public void CalculateGrade_BothTypingsAreMultipleAllele_AtNonDpb1Locus_WithDifferentExpressingAlleles_FromDifferentGGroupsAndPGroups_ReturnsMismatch()
        {
            var patientAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("111:111")
                .WithMatchingGGroup("patient-g-group")
                .WithMatchingPGroup("patient-p-group")
                .Build();
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .AtLocus(NonDpb1Locus)
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
                .AtLocus(NonDpb1Locus)
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
        public void CalculateGrade_MultipleAlleleVsExpressingAllele_AtDpb1_WithDifferentExpressingAlleles_FromDifferentGGroupsAndPGroups_ButPermissivelyMismatched_ReturnsPermissiveMismatch()
        {
            const string patientAlleleName = "111:111";
            var patientAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName(patientAlleleName)
                .WithMatchingGGroup("patient-g-group")
                .WithMatchingPGroup("patient-p-group")
                .Build();
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .AtLocus(Dpb1Locus)
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { patientAllele })
                    .Build())
                .Build();

            const string donorAlleleName = "999:999";
            var donorAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName(donorAlleleName)
                .WithMatchingGGroup("donor-g-group")
                .WithMatchingPGroup("donor-p-group")
                .Build();
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .AtLocus(Dpb1Locus)
                .WithHlaScoringInfo(donorAllele)
                .Build();

            permissiveMismatchCalculator
                .IsPermissiveMismatch(Dpb1Locus, patientAlleleName, donorAlleleName)
                .Returns(true);

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.PermissiveMismatch);
        }

        [Test]
        public void CalculateGrade_MultipleAlleleVsExpressingAllele_AtDpb1_WithDifferentExpressingAlleles_FromDifferentGGroupsAndPGroups_AndNotPermissivelyMismatched_ReturnsMismatch()
        {
            const string patientAlleleName = "111:111";
            var patientAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName(patientAlleleName)
                .WithMatchingGGroup("patient-g-group")
                .WithMatchingPGroup("patient-p-group")
                .Build();
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .AtLocus(Dpb1Locus)
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { patientAllele })
                    .Build())
                .Build();

            const string donorAlleleName = "999:999";
            var donorAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName(donorAlleleName)
                .WithMatchingGGroup("donor-g-group")
                .WithMatchingPGroup("donor-p-group")
                .Build();
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .AtLocus(Dpb1Locus)
                .WithHlaScoringInfo(donorAllele)
                .Build();

            permissiveMismatchCalculator
                .IsPermissiveMismatch(Dpb1Locus, patientAlleleName, donorAlleleName)
                .Returns(false);

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Mismatch);
        }

        [Test]
        public void CalculateGrade_MultipleAlleleVsExpressingAllele_AtNonDpb1Locus_WithDifferentExpressingAlleles_FromDifferentGGroupsAndPGroups_ReturnsMismatch()
        {
            var patientAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("111:111")
                .WithMatchingGGroup("patient-g-group")
                .WithMatchingPGroup("patient-p-group")
                .Build();
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .AtLocus(NonDpb1Locus)
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
                .AtLocus(NonDpb1Locus)
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
        public void CalculateGrade_ExpressingAlleleVsMultipleAllele_AtDpb1_WithDifferentExpressingAlleles_FromDifferentGGroupsAndPGroups_ButPermissivelyMismatched_ReturnsPermissiveMismatch()
        {
            const string patientAlleleName = "111:111";
            var patientAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName(patientAlleleName)
                .WithMatchingGGroup("patient-g-group")
                .WithMatchingPGroup("patient-p-group")
                .Build();
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .AtLocus(Dpb1Locus)
                .WithHlaScoringInfo(patientAllele)
                .Build();

            const string donorAlleleName = "999:999";
            var donorAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName(donorAlleleName)
                .WithMatchingGGroup("donor-g-group")
                .WithMatchingPGroup("donor-p-group")
                .Build();
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .AtLocus(Dpb1Locus)
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { donorAllele })
                    .Build())
                .Build();

            permissiveMismatchCalculator
                .IsPermissiveMismatch(Dpb1Locus, patientAlleleName, donorAlleleName)
                .Returns(true);

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.PermissiveMismatch);
        }

        [Test]
        public void CalculateGrade_ExpressingAlleleVsMultipleAllele_AtDpb1_WithDifferentExpressingAlleles_FromDifferentGGroupsAndPGroups_AndNotPermissivelyMismatched_ReturnsMismatch()
        {
            const string patientAlleleName = "111:111";
            var patientAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName(patientAlleleName)
                .WithMatchingGGroup("patient-g-group")
                .WithMatchingPGroup("patient-p-group")
                .Build();
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .AtLocus(Dpb1Locus)
                .WithHlaScoringInfo(patientAllele)
                .Build();

            const string donorAlleleName = "999:999";
            var donorAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName(donorAlleleName)
                .WithMatchingGGroup("donor-g-group")
                .WithMatchingPGroup("donor-p-group")
                .Build();
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .AtLocus(Dpb1Locus)
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] { donorAllele })
                    .Build())
                .Build();

            permissiveMismatchCalculator
                .IsPermissiveMismatch(Dpb1Locus, patientAlleleName, donorAlleleName)
                .Returns(false);

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Mismatch);
        }

        [Test]
        public void CalculateGrade_ExpressingAlleleVsMultipleAllele_AtNonDpb1Locus_WithDifferentExpressingAlleles_FromDifferentGGroupsAndPGroups_ReturnsMismatch()
        {
            var patientAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("111:111")
                .WithMatchingGGroup("patient-g-group")
                .WithMatchingPGroup("patient-p-group")
                .Build();
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .AtLocus(NonDpb1Locus)
                .WithHlaScoringInfo(patientAllele)
                .Build();

            var donorAllele = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("999:999")
                .WithMatchingGGroup("donor-g-group")
                .WithMatchingPGroup("donor-p-group")
                .Build();
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .AtLocus(NonDpb1Locus)
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