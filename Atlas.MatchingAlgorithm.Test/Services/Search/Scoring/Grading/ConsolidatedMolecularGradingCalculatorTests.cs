using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders.ScoringInfoBuilders;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading.GradingCalculators;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;

namespace Atlas.MatchingAlgorithm.Test.Services.Search.Scoring.Grading
{
    [TestFixture]
    public class ConsolidatedMolecularGradingCalculatorTests : GradingCalculatorTestsBase
    {
        private const Locus Dpb1Locus = Locus.Dpb1;
        private const Locus NonDpb1Locus = Locus.A;

        [SetUp]
        public override void SetUpGradingCalculator()
        {
            GradingCalculator = new ConsolidatedMolecularGradingCalculator();
        }

        #region Tests: Exception Cases

        [TestCase(typeof(ConsolidatedMolecularScoringInfo), typeof(SerologyScoringInfo))]
        [TestCase(typeof(SerologyScoringInfo), typeof(ConsolidatedMolecularScoringInfo))]
        [TestCase(typeof(SerologyScoringInfo), typeof(SerologyScoringInfo))]
        [TestCase(typeof(SingleAlleleScoringInfo), typeof(SingleAlleleScoringInfo))]
        [TestCase(typeof(SingleAlleleScoringInfo), typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(MultipleAlleleScoringInfo), typeof(SingleAlleleScoringInfo))]
        [TestCase(typeof(MultipleAlleleScoringInfo), typeof(MultipleAlleleScoringInfo))]
        public override void CalculateGrade_OneOrBothScoringInfosAreNotOfPermittedTypes_ThrowsException(
            Type patientScoringInfoType,
            Type donorScoringInfoType
        )
        {
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(ScoringInfoBuilderFactory.GetDefaultScoringInfoFromBuilder(patientScoringInfoType))
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(ScoringInfoBuilderFactory.GetDefaultScoringInfoFromBuilder(donorScoringInfoType))
                .Build();

            Assert.Throws<ArgumentException>(() =>
                GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult));
        }

        #endregion

        #region Tests: Both Typings Consolidated Molecular

        [Test]
        public void CalculateGrade_BothTypingsAreConsolidatedMolecular_WithSameName_ReturnsGGroup()
        {
            const string sharedConsolidatedMolecularName = "shared-hla-name";

            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithLookupName(sharedConsolidatedMolecularName)
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder().Build())
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithLookupName(sharedConsolidatedMolecularName)
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder().Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.GGroup);
        }

        [Test]
        public void CalculateGrade_BothTypingsAreConsolidatedMolecular_WithDifferentNames_ButIntersectingGGroups_ReturnsGGroup()
        {
            const string sharedGGroup = "shared-g-group";

            const string patientConsolidatedMolecularName = "patient-hla-name";
            const string patientGGroup = "patient-g-group";
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithLookupName(patientConsolidatedMolecularName)
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { sharedGGroup, patientGGroup })
                    .Build())
                .Build();

            const string donorConsolidatedMolecularName = "donor-hla-name";
            const string donorGGroup = "donor-g-group";
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithLookupName(donorConsolidatedMolecularName)
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { sharedGGroup, donorGGroup })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.GGroup);
        }

        [Test]
        public void CalculateGrade_BothTypingsAreConsolidatedMolecular_WithDifferentNamesAndGGroups_ButIntersectingPGroups_ReturnsPGroup()
        {
            const string sharedPGroup = "shared-p-group";

            const string patientConsolidatedMolecularName = "patient-hla-name";
            const string patientGGroup = "patient-g-group";
            const string patientPGroup = "patient-p-group";
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithLookupName(patientConsolidatedMolecularName)
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new []{ patientGGroup })
                    .WithMatchingPGroups(new[] { sharedPGroup, patientPGroup })
                    .Build())
                .Build();

            const string donorConsolidatedMolecularName = "donor-hla-name";
            const string donorGGroup = "donor-g-group";
            const string donorPGroup = "donor-p-group";
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithLookupName(donorConsolidatedMolecularName)
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { donorGGroup })
                    .WithMatchingPGroups(new[] { sharedPGroup, donorPGroup })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.PGroup);
        }

        [Test]
        public void CalculateGrade_BothTypingsAreConsolidatedMolecular_AtNonDpb1Locus_WithDifferentNamesAndGGroupsAndPGroups_ReturnsMismatch()
        {
            const string patientConsolidatedMolecularName = "patient-hla-name";
            const string patientGGroup = "patient-g-group";
            const string patientPGroup = "patient-p-group";
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .AtLocus(NonDpb1Locus)
                .WithLookupName(patientConsolidatedMolecularName)
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { patientGGroup })
                    .WithMatchingPGroups(new[] { patientPGroup })
                    .Build())
                .Build();

            const string donorConsolidatedMolecularName = "donor-hla-name";
            const string donorGGroup = "donor-g-group";
            const string donorPGroup = "donor-p-group";
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .AtLocus(NonDpb1Locus)
                .WithLookupName(donorConsolidatedMolecularName)
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { donorGGroup })
                    .WithMatchingPGroups(new[] { donorPGroup })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Mismatch);
        }

        #endregion

        #region Tests: Consolidated Molecular vs. Expressing Allele

        [Test]
        public void CalculateGrade_ConsolidatedMolecularVsExpressingAllele_WithIntersectingGGroups_ReturnsGGroup()
        {
            const string sharedGGroup = "shared-g-group";

            const string patientGGroup = "patient-g-group";
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { sharedGGroup, patientGGroup })
                    .Build())
                .Build();

            const string donorAlleleName = "999:999";
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(donorAlleleName)
                    .WithMatchingGGroup(sharedGGroup)
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.GGroup);
        }

        [Test]
        public void CalculateGrade_ConsolidatedMolecularVsExpressingAllele_WithDifferentGGroups_ButIntersectingPGroups_ReturnsGGroup()
        {
            const string sharedPGroup = "shared-p-group";

            const string patientGGroup = "patient-g-group";
            const string patientPGroup = "patient-p-group";
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { patientGGroup })
                    .WithMatchingPGroups(new[] { sharedPGroup, patientPGroup })
                    .Build())
                .Build();

            const string donorAlleleName = "999:999";
            const string donorGGroup = "donor-g-group";
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(donorAlleleName)
                    .WithMatchingGGroup(donorGGroup)
                    .WithMatchingPGroup(sharedPGroup)
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.PGroup);
        }

        [Test]
        public void CalculateGrade_ConsolidatedMolecularVsExpressingAllele_AtNonDpb1Locus_WithDifferentGGroupsAndPGroups_ReturnsMismatch()
        {
            const string patientGGroup = "patient-g-group";
            const string patientPGroup = "patient-p-group";
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .AtLocus(NonDpb1Locus)
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { patientGGroup })
                    .WithMatchingPGroups(new[] { patientPGroup })
                    .Build())
                .Build();

            const string donorAlleleName = "999:999";
            const string donorGGroup = "donor-g-group";
            const string donorPGroup = "donor-p-group";
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .AtLocus(NonDpb1Locus)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(donorAlleleName)
                    .WithMatchingGGroup(donorGGroup)
                    .WithMatchingPGroup(donorPGroup)
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Mismatch);
        }

        #endregion

        #region Tests: Expressing Allele vs. Consolidated Molecular

        [Test]
        public void CalculateGrade_ExpressingAlleleVsConsolidatedMolecular_WithIntersectingGGroups_ReturnsGGroup()
        {
            const string sharedGGroup = "shared-g-group";

            const string patientAlleleName = "999:999";
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(patientAlleleName)
                    .WithMatchingGGroup(sharedGGroup)
                    .Build())
                .Build();

            const string donorGGroup = "donor-g-group";
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { sharedGGroup, donorGGroup })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.GGroup);
        }

        [Test]
        public void CalculateGrade_ExpressingAlleleVsConsolidatedMolecular_WithDifferentGGroups_ButIntersectingPGroups_ReturnsGGroup()
        {
            const string sharedPGroup = "shared-p-group";

            const string patientAlleleName = "999:999";
            const string patientGGroup = "patient-g-group";
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(patientAlleleName)
                    .WithMatchingGGroup(patientGGroup)
                    .WithMatchingPGroup(sharedPGroup)
                    .Build())
                .Build();

            const string donorGGroup = "donor-g-group";
            const string donorPGroup = "donor-p-group";
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { donorGGroup })
                    .WithMatchingPGroups(new[] { sharedPGroup, donorPGroup })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.PGroup);
        }

        [Test]
        public void CalculateGrade_ExpressingAlleleVsConsolidatedMolecular_AtNonDpb1Locus_WithDifferentGGroupsAndPGroups_ReturnsMismatch()
        {
            const string patientAlleleName = "999:999";
            const string patientGGroup = "patient-g-group";
            const string patientPGroup = "patient-p-group";
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .AtLocus(NonDpb1Locus)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(patientAlleleName)
                    .WithMatchingGGroup(patientGGroup)
                    .WithMatchingPGroup(patientPGroup)
                    .Build())
                .Build();

            const string donorGGroup = "donor-g-group";
            const string donorPGroup = "donor-p-group";
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .AtLocus(NonDpb1Locus)
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { donorGGroup })
                    .WithMatchingPGroups(new[] { donorPGroup })
                    .Build())
                .Build();   

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Mismatch);
        }

        #endregion

        #region Tests: Consolidated Molecular vs. Null Allele & Vice Versa

        [Test]
        public void CalculateGrade_ConsolidatedMolecularVsNullAllele_AtNonDpb1Locus_ReturnsMismatch()
        {
            const string patientGGroup = "patient-g-group";
            const string patientPGroup = "patient-p-group";
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .AtLocus(NonDpb1Locus)
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { patientGGroup })
                    .WithMatchingPGroups(new[] { patientPGroup })
                    .Build())
                .Build();

            const string donorAlleleName = "999:999N";
            const string donorGGroup = null;
            const string donorPGroup = null;
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .AtLocus(NonDpb1Locus)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(donorAlleleName)
                    .WithMatchingGGroup(donorGGroup)
                    .WithMatchingPGroup(donorPGroup)
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Mismatch);
        }

        [Test]
        public void CalculateGrade_NullAlleleVsConsolidatedMolecular_AtNonDpb1Locus_ReturnsMismatch()
        {
            const string patientAlleleName = "999:999N";
            const string patientGGroup = null;
            const string patientPGroup = null;
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .AtLocus(NonDpb1Locus)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(patientAlleleName)
                    .WithMatchingGGroup(patientGGroup)
                    .WithMatchingPGroup(patientPGroup)
                    .Build())
                .Build();

            const string donorGGroup = "donor-g-group";
            const string donorPGroup = "donor-p-group";
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .AtLocus(NonDpb1Locus)
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { donorGGroup })
                    .WithMatchingPGroups(new[] { donorPGroup })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Mismatch);
        }

        #endregion

        #region Tests: Consolidated Molecular vs. Multiple Alleles

        [Test]
        public void CalculateGrade_ConsolidatedMolecularVsMultipleAllele_WithIntersectingGGroups_ReturnsGGroup()
        {
            const string sharedGGroup = "shared-g-group";

            const string patientGGroup = "patient-g-group";
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { sharedGGroup, patientGGroup })
                    .Build())
                .Build();

            const string donorGGroup = "donor-g-group";
            var donorLookupResult = new HlaScoringMetadataBuilder()
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

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.GGroup);
        }

        [Test]
        public void CalculateGrade_ConsolidatedMolecularVsMultipleAllele_WithDifferentGGroups_ButIntersectingPGroups_ReturnsGGroup()
        {
            const string sharedPGroup = "shared-p-group";

            const string patientGGroup = "patient-g-group";
            const string patientPGroup = "patient-p-group";
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { patientGGroup })
                    .WithMatchingPGroups(new[] { sharedPGroup, patientPGroup })
                    .Build())
                .Build();

            const string donorGGroup = "donor-g-group";
            const string donorPGroup = "donor-p-group";
            var donorLookupResult = new HlaScoringMetadataBuilder()
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

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.PGroup);
        }

        [Test]
        public void CalculateGrade_ConsolidatedMolecularVsMultipleAllele_AtNonDpb1Locus_WithDifferentGGroupsAndPGroups_ReturnsMismatch()
        {
            const string patientGGroup = "patient-g-group";
            const string patientPGroup = "patient-p-group";
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .AtLocus(NonDpb1Locus)
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { patientGGroup })
                    .WithMatchingPGroups(new[] { patientPGroup })
                    .Build())
                .Build();

            const string donorGGroup = "donor-g-group";
            const string donorPGroup = "donor-p-group";
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .AtLocus(NonDpb1Locus)
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[]{
                        new SingleAlleleScoringInfoBuilder()
                            .WithMatchingGGroup(donorGGroup)
                            .WithMatchingPGroup(donorPGroup)
                            .Build()
                    })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Mismatch);
        }

        #endregion

        #region Tests: Multiple Alleles vs. Consolidated Molecular

        [Test]
        public void CalculateGrade_MultipleAlleleVsConsolidatedMolecular_WithIntersectingGGroups_ReturnsGGroup()
        {
            const string sharedGGroup = "shared-g-group";

            const string patientGGroup = "patient-g-group";
            var patientLookupResult = new HlaScoringMetadataBuilder()
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
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { sharedGGroup, donorGGroup })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.GGroup);
        }

        [Test]
        public void CalculateGrade_MultipleAlleleVsConsolidatedMolecular_WithDifferentGGroups_ButIntersectingPGroups_ReturnsGGroup()
        {
            const string sharedPGroup = "shared-p-group";

            const string patientGGroup = "patient-g-group";
            const string patientPGroup = "patient-p-group";
            var patientLookupResult = new HlaScoringMetadataBuilder()
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
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { donorGGroup })
                    .WithMatchingPGroups(new[] { sharedPGroup, donorPGroup })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.PGroup);
        }

        [Test]
        public void CalculateGrade_MultipleAlleleVsConsolidatedMolecular_AtNonDpb1Locus_WithDifferentGGroupsAndPGroups_ReturnsMismatch()
        {
            const string patientGGroup = "patient-g-group";
            const string patientPGroup = "patient-p-group";
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .AtLocus(NonDpb1Locus)
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
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .AtLocus(NonDpb1Locus)
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] { donorGGroup })
                    .WithMatchingPGroups(new[] { donorPGroup })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Mismatch);
        }

        #endregion
    }
}