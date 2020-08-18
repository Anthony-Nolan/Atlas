using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders.ScoringInfoBuilders;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading.GradingCalculators;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;

namespace Atlas.MatchingAlgorithm.Test.Services.Search.Scoring.Grading
{
    [TestFixture]
    public class SerologyGradingCalculatorTests : GradingCalculatorTestsBase
    {
        [SetUp]
        public override void SetUpGradingCalculator()
        {
            GradingCalculator = new SerologyGradingCalculator();
        }

        #region Tests: Exception Cases

        [TestCase(typeof(SingleAlleleScoringInfo), typeof(SingleAlleleScoringInfo))]
        [TestCase(typeof(SingleAlleleScoringInfo), typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(SingleAlleleScoringInfo), typeof(ConsolidatedMolecularScoringInfo))]
        [TestCase(typeof(MultipleAlleleScoringInfo), typeof(SingleAlleleScoringInfo))]
        [TestCase(typeof(MultipleAlleleScoringInfo), typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(MultipleAlleleScoringInfo), typeof(ConsolidatedMolecularScoringInfo))]
        [TestCase(typeof(ConsolidatedMolecularScoringInfo), typeof(SingleAlleleScoringInfo))]
        [TestCase(typeof(ConsolidatedMolecularScoringInfo), typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(ConsolidatedMolecularScoringInfo), typeof(ConsolidatedMolecularScoringInfo))]
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

        #region Tests: Both Typings are Serology

        [Test]
        public void CalculateGrade_BothTypingsAreSerology_WithDirectAssociatedRelationship_ReturnsAssociated()
        {
            var sharedSerology = new SerologyEntry("shared-serology", SerologySubtype.Associated, true);

            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { sharedSerology })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { sharedSerology })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Associated);
        }

        [Test]
        public void CalculateGrade_BothTypingsAreSerology_WithDirectSplitRelationship_ReturnsSplit()
        {
            var sharedSerology = new SerologyEntry("shared-serology", SerologySubtype.Split, true);

            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { sharedSerology })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { sharedSerology })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_BothTypingsAreSerology_WithDirectNotSplitRelationship_ReturnsSplit()
        {
            var sharedSerology = new SerologyEntry("shared-serology", SerologySubtype.NotSplit, true);

            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { sharedSerology })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { sharedSerology })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        // This covers the case from WMDA matching framework:
        // s1 does not have any splits & s2 is associated to s1, or vice versa.
        [TestCase(SerologySubtype.Split, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.Split)]
        [TestCase(SerologySubtype.NotSplit, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.NotSplit)]
        public void CalculateGrade_BothTypingsAreSerology_WithIndirectSplitToAssociatedRelationship_ReturnsSplit(
            SerologySubtype patientSerologySubtype,
            SerologySubtype donorSerologySubtype
           )
        {
            const string patientSerologyName = "patient-serology";
            const string donorSerologyName = "donor-serology";

            var patientSerologies = new[]
            {
                new SerologyEntry(patientSerologyName, patientSerologySubtype, true),
                new SerologyEntry(donorSerologyName, donorSerologySubtype, false)
            };
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(patientSerologies)
                    .Build())
                .Build();

            var donorSerologies = new[]
            {
                new SerologyEntry(donorSerologyName, donorSerologySubtype, true),
                new SerologyEntry(patientSerologyName, patientSerologySubtype, false)
            };
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(donorSerologies)
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_BothTypingsAreSerology_WithDirectBroadRelationship_ReturnsBroad()
        {
            var sharedSerology = new SerologyEntry("shared-serology", SerologySubtype.Broad, true);

            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { sharedSerology })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { sharedSerology })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Broad);
        }

        // This covers the case from WMDA matching framework:
        // s1 is a broad & s2 is a split of s1, or vice versa.
        // s1 is a broad & s2 is associated to a split of s1, or vice versa.
        [TestCase(SerologySubtype.Broad, SerologySubtype.Split)]
        [TestCase(SerologySubtype.Split, SerologySubtype.Broad)]
        [TestCase(SerologySubtype.Broad, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.Broad)]
        public void CalculateGrade_BothTypingsAreSerology_WithIndirectBroadRelationship_ReturnsBroad(
            SerologySubtype patientSerologySubtype,
            SerologySubtype donorSerologySubtype
        )
        {
            const string patientSerologyName = "patient-serology";
            const string donorSerologyName = "donor-serology";

            var patientSerologies = new[]
            {
                new SerologyEntry(patientSerologyName, patientSerologySubtype, true),
                new SerologyEntry(donorSerologyName, donorSerologySubtype, false)
            };
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(patientSerologies)
                    .Build())
                .Build();

            var donorSerologies = new[]
            {
                new SerologyEntry(donorSerologyName, donorSerologySubtype, true),
                new SerologyEntry(patientSerologyName, patientSerologySubtype, false)

            };
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(donorSerologies)
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Broad);
        }

        #endregion       

        #region Tests: Expressing Allele vs Serology

        [Test]
        public void CalculateGrade_ExpressingAlleleVsSerology_WhereAlleleDirectlyMapsToAssociatedSerology_ReturnsAssociated()
        {
            var serologyDirectlyMappedToPatientAllele = new SerologyEntry("shared-serology", SerologySubtype.Associated, true);

            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientAllele })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Associated);
        }

        [Test]
        public void CalculateGrade_ExpressingAlleleVsSerology_WhereAlleleDirectlyMapsToSplitSerology_ReturnsSplit()
        {
            var serologyDirectlyMappedToPatientAllele = new SerologyEntry("shared-serology", SerologySubtype.Split, true);

            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientAllele })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_ExpressingAlleleVsSerology_WhereAlleleDirectlyMapsToNotSplitSerology_ReturnsSplit()
        {
            var serologyDirectlyMappedToPatientAllele = new SerologyEntry("shared-serology", SerologySubtype.NotSplit, true);

            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientAllele })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        // This covers the case from WMDA matching framework:
        // s1 does not have any splits & s2 is associated to s1, or vice versa.
        [TestCase(SerologySubtype.Split, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.Split)]
        [TestCase(SerologySubtype.NotSplit, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.NotSplit)]
        public void CalculateGrade_ExpressingAlleleVsSerology_WithIndirectSplitToAssociatedRelationship_ReturnsSplit(
            SerologySubtype directToAlleleSubtype,
            SerologySubtype indirectToAlleleSubtype
           )
        {
            const string sharedSerologyName = "shared-serology";

            var patientSerologies = new[]
            {
                new SerologyEntry("patient-serology", directToAlleleSubtype, true),
                new SerologyEntry(sharedSerologyName, indirectToAlleleSubtype, false)
            };
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(patientSerologies)
                    .Build())
                .Build();

            var donorSerologies = new[]
            {
                new SerologyEntry(sharedSerologyName, indirectToAlleleSubtype, true)
            };
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(donorSerologies)
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_ExpressingAlleleVsSerology_WhereAlleleDirectlyMapsToBroadSerology_ReturnsBroad()
        {
            var serologyDirectlyMappedToPatientAllele = new SerologyEntry("shared-serology", SerologySubtype.Broad, true);

            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientAllele })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Broad);
        }

        // This covers the case from WMDA matching framework:
        // s1 is a broad & s2 is a split of s1, or vice versa.
        // s1 is a broad & s2 is associated to a split of s1, or vice versa.
        [TestCase(SerologySubtype.Broad, SerologySubtype.Split)]
        [TestCase(SerologySubtype.Split, SerologySubtype.Broad)]
        [TestCase(SerologySubtype.Broad, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.Broad)]
        public void CalculateGrade_ExpressingAlleleVsSerology_WithIndirectBroadRelationship_ReturnsBroad(
            SerologySubtype directToAlleleSubtype,
            SerologySubtype indirectToAlleleSubtype
        )
        {
            const string sharedSerologyName = "shared-serology";

            var patientSerologies = new[]
            {
                new SerologyEntry("patient-serology", directToAlleleSubtype, true),
                new SerologyEntry(sharedSerologyName, indirectToAlleleSubtype, false)
            };
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(patientSerologies)
                    .Build())
                .Build();

            var donorSerologies = new[]
            {
                new SerologyEntry(sharedSerologyName, indirectToAlleleSubtype, true)
            };
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(donorSerologies)
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Broad);
        }

        #endregion

        #region Tests: Serology vs Expressing Allele

        [Test]
        public void CalculateGrade_SerologyVsExpressingAllele_WhereAlleleDirectlyMapsToAssociatedSerology_ReturnsAssociated()
        {
            var serologyDirectlyMappedToDonorAllele = new SerologyEntry("shared-serology", SerologySubtype.Associated, true);

            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorAllele })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Associated);
        }

        [Test]
        public void CalculateGrade_SerologyVsExpressingAllele_WhereAlleleDirectlyMapsToSplitSerology_ReturnsSplit()
        {
            var serologyDirectlyMappedToDonorAllele = new SerologyEntry("shared-serology", SerologySubtype.Split, true);

            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorAllele })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_SerologyVsExpressingAllele_WhereAlleleDirectlyMapsToNotSplitSerology_ReturnsSplit()
        {
            var serologyDirectlyMappedToDonorAllele = new SerologyEntry("shared-serology", SerologySubtype.NotSplit, true);

            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorAllele })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        // This covers the case from WMDA matching framework:
        // s1 does not have any splits & s2 is associated to s1, or vice versa.
        [TestCase(SerologySubtype.Split, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.Split)]
        [TestCase(SerologySubtype.NotSplit, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.NotSplit)]
        public void CalculateGrade_SerologyVsExpressingAllele_WithIndirectSplitToAssociatedRelationship_ReturnsSplit(
            SerologySubtype directToAlleleSubtype,
            SerologySubtype indirectToAlleleSubtype
           )
        {
            const string sharedSerologyName = "shared-serology";

            var patientSerologies = new[]
            {
                new SerologyEntry(sharedSerologyName, indirectToAlleleSubtype, true)
            };
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(patientSerologies)
                    .Build())
                .Build();

            var donorSerologies = new[]
            {
                new SerologyEntry("donor-serology", directToAlleleSubtype, true),
                new SerologyEntry(sharedSerologyName, indirectToAlleleSubtype, false)
            };
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(donorSerologies)
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_SerologyVsExpressingAllele_WhereAlleleDirectlyMapsToBroadSerology_ReturnsBroad()
        {
            var serologyDirectlyMappedToDonorAllele = new SerologyEntry("shared-serology", SerologySubtype.Broad, true);

            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorAllele })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Broad);
        }

        // This covers the case from WMDA matching framework:
        // s1 is a broad & s2 is a split of s1, or vice versa.
        // s1 is a broad & s2 is associated to a split of s1, or vice versa.
        [TestCase(SerologySubtype.Broad, SerologySubtype.Split)]
        [TestCase(SerologySubtype.Split, SerologySubtype.Broad)]
        [TestCase(SerologySubtype.Broad, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.Broad)]
        public void CalculateGrade_SerologyVsExpressingAllele_WithIndirectBroadRelationship_ReturnsBroad(
            SerologySubtype directToAlleleSubtype,
            SerologySubtype indirectToAlleleSubtype
        )
        {
            const string sharedSerologyName = "shared-serology";

            var patientSerologies = new[]
            {
                new SerologyEntry(sharedSerologyName, indirectToAlleleSubtype, true)
            };
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(patientSerologies)
                    .Build())
                .Build();

            var donorSerologies = new[]
            {
                new SerologyEntry("donor-serology", directToAlleleSubtype, true),
                new SerologyEntry(sharedSerologyName, indirectToAlleleSubtype, false)
            };
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(donorSerologies)
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Broad);
        }

        #endregion

        #region Tests: Null Allele vs Serology

        [Test]
        public void CalculateGrade_NullAlleleVsSerology_ReturnsMismatch()
        {
            // null alleles do not have any matching serologies
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new List<SerologyEntry>())
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Mismatch);
        }

        #endregion

        #region Tests: Serology vs Null Allele

        [Test]
        public void CalculateGrade_SerologyVsNullAllele_ReturnsMismatch()
        {
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().Build())
                .Build();

            // null alleles do not have any matching serologies
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new List<SerologyEntry>())
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Mismatch);
        }

        #endregion

        #region Tests: Multiple Alleles vs Serology

        [Test]
        public void CalculateGrade_MultipleAlleleVsSerology_WhereMultipleAlleleDirectlyMapsToAssociatedSerology_ReturnsAssociated()
        {
            var serologyDirectlyMappedToPatientMultipleAllele =
                new SerologyEntry("shared-serology", SerologySubtype.Associated, true);

            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientMultipleAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientMultipleAllele })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Associated);
        }

        [Test]
        public void CalculateGrade_MultipleAlleleVsSerology_WhereMultipleAlleleDirectlyMapsToSplitSerology_ReturnsSplit()
        {
            var serologyDirectlyMappedToPatientMultipleAllele = new SerologyEntry("shared-serology", SerologySubtype.Split, true);

            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientMultipleAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientMultipleAllele })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_MultipleAlleleVsSerology_WhereMultipleAlleleDirectlyMapsToNotSplitSerology_ReturnsSplit()
        {
            var serologyDirectlyMappedToPatientMultipleAllele = new SerologyEntry("shared-serology", SerologySubtype.NotSplit, true);

            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientMultipleAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientMultipleAllele })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        // This covers the case from WMDA matching framework:
        // s1 does not have any splits & s2 is associated to s1, or vice versa.
        [TestCase(SerologySubtype.Split, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.Split)]
        [TestCase(SerologySubtype.NotSplit, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.NotSplit)]
        public void CalculateGrade_MultipleAlleleVsSerology_WithIndirectSplitToAssociatedRelationship_ReturnsSplit(
            SerologySubtype directToMultipleAlleleSubtype,
            SerologySubtype indirectToMultipleAlleleSubtype
           )
        {
            const string sharedSerologyName = "shared-serology";

            var patientSerologies = new[]
            {
                new SerologyEntry("patient-serology", directToMultipleAlleleSubtype, true),
                new SerologyEntry(sharedSerologyName, indirectToMultipleAlleleSubtype, false)
            };
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(patientSerologies)
                    .Build())
                .Build();

            var donorSerologies = new[]
            {
                new SerologyEntry(sharedSerologyName, indirectToMultipleAlleleSubtype, true)
            };
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(donorSerologies)
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_MultipleAlleleVsSerology_WhereMultipleAlleleDirectlyMapsToBroadSerology_ReturnsBroad()
        {
            var serologyDirectlyMappedToPatientMultipleAllele = new SerologyEntry("shared-serology", SerologySubtype.Broad, true);

            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientMultipleAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientMultipleAllele })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Broad);
        }

        // This covers the case from WMDA matching framework:
        // s1 is a broad & s2 is a split of s1, or vice versa.
        // s1 is a broad & s2 is associated to a split of s1, or vice versa.
        [TestCase(SerologySubtype.Broad, SerologySubtype.Split)]
        [TestCase(SerologySubtype.Split, SerologySubtype.Broad)]
        [TestCase(SerologySubtype.Broad, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.Broad)]
        public void CalculateGrade_MultipleAlleleVsSerology_WithIndirectBroadRelationship_ReturnsBroad(
            SerologySubtype directToMultipleAlleleSubtype,
            SerologySubtype indirectToMultipleAlleleSubtype
        )
        {
            const string sharedSerologyName = "shared-serology";

            var patientSerologies = new[]
            {
                new SerologyEntry("patient-serology", directToMultipleAlleleSubtype, true),
                new SerologyEntry(sharedSerologyName, indirectToMultipleAlleleSubtype, false)
            };
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(patientSerologies)
                    .Build())
                .Build();

            var donorSerologies = new[]
            {
                new SerologyEntry(sharedSerologyName, indirectToMultipleAlleleSubtype, true)
            };
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(donorSerologies)
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Broad);
        }

        #endregion

        #region Tests: Serology vs Multiple Alleles

        [Test]
        public void CalculateGrade_SerologyVsMultipleAllele_WhereMultipleAlleleDirectlyMapsToAssociatedSerology_ReturnsAssociated()
        {
            var serologyDirectlyMappedToDonorMultipleAllele = new SerologyEntry("shared-serology", SerologySubtype.Associated, true);

            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorMultipleAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorMultipleAllele })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Associated);
        }

        [Test]
        public void CalculateGrade_SerologyVsMultipleAllele_WhereMultipleAlleleDirectlyMapsToSplitSerology_ReturnsSplit()
        {
            var serologyDirectlyMappedToDonorMultipleAllele = new SerologyEntry("shared-serology", SerologySubtype.Split, true);

            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorMultipleAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorMultipleAllele })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_SerologyVsMultipleAllele_WhereMultipleAlleleDirectlyMapsToNotSplitSerology_ReturnsSplit()
        {
            var serologyDirectlyMappedToDonorMultipleAllele = new SerologyEntry("shared-serology", SerologySubtype.NotSplit, true);

            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorMultipleAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorMultipleAllele })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        // This covers the case from WMDA matching framework:
        // s1 does not have any splits & s2 is associated to s1, or vice versa.
        [TestCase(SerologySubtype.Split, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.Split)]
        [TestCase(SerologySubtype.NotSplit, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.NotSplit)]
        public void CalculateGrade_SerologyVsMultipleAllele_WithIndirectSplitToAssociatedRelationship_ReturnsSplit(
            SerologySubtype directToMultipleAlleleSubtype,
            SerologySubtype indirectToMultipleAlleleSubtype
           )
        {
            const string sharedSerologyName = "shared-serology";

            var patientSerologies = new[]
            {
                new SerologyEntry(sharedSerologyName, indirectToMultipleAlleleSubtype, true)
            };
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(patientSerologies)
                    .Build())
                .Build();

            var donorSerologies = new[]
            {
                new SerologyEntry("donor-serology", directToMultipleAlleleSubtype, true),
                new SerologyEntry(sharedSerologyName, indirectToMultipleAlleleSubtype, false)
            };
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(donorSerologies)
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_SerologyVsMultipleAllele_WhereMultipleAlleleDirectlyMapsToBroadSerology_ReturnsBroad()
        {
            var serologyDirectlyMappedToDonorMultipleAllele = new SerologyEntry("shared-serology", SerologySubtype.Broad, true);

            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorMultipleAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorMultipleAllele })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Broad);
        }

        // This covers the case from WMDA matching framework:
        // s1 is a broad & s2 is a split of s1, or vice versa.
        // s1 is a broad & s2 is associated to a split of s1, or vice versa.
        [TestCase(SerologySubtype.Broad, SerologySubtype.Split)]
        [TestCase(SerologySubtype.Split, SerologySubtype.Broad)]
        [TestCase(SerologySubtype.Broad, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.Broad)]
        public void CalculateGrade_SerologyVsMultipleAllele_WithIndirectBroadRelationship_ReturnsBroad(
            SerologySubtype directToMultipleAlleleSubtype,
            SerologySubtype indirectToMultipleAlleleSubtype
        )
        {
            const string sharedSerologyName = "shared-serology";

            var patientSerologies = new[]
            {
                new SerologyEntry(sharedSerologyName, indirectToMultipleAlleleSubtype, true)
            };
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(patientSerologies)
                    .Build())
                .Build();

            var donorSerologies = new[]
            {
                new SerologyEntry("donor-serology", directToMultipleAlleleSubtype, true),
                new SerologyEntry(sharedSerologyName, indirectToMultipleAlleleSubtype, false)
            };
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(donorSerologies)
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Broad);
        }

        #endregion

        #region Tests: Consolidated Molecular vs Serology

        [Test]
        public void CalculateGrade_ConsolidatedMolecularVsSerology_WhereConsolidatedMolecularDirectlyMapsToAssociatedSerology_ReturnsAssociated()
        {
            var serologyDirectlyMappedToPatientConsolidatedMolecular = new SerologyEntry("shared-serology", SerologySubtype.Associated, true);

            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientConsolidatedMolecular })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientConsolidatedMolecular })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Associated);
        }

        [Test]
        public void CalculateGrade_ConsolidatedMolecularVsSerology_WhereConsolidatedMolecularDirectlyMapsToSplitSerology_ReturnsSplit()
        {
            var serologyDirectlyMappedToPatientConsolidatedMolecular = new SerologyEntry("shared-serology", SerologySubtype.Split, true);

            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientConsolidatedMolecular })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientConsolidatedMolecular })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_ConsolidatedMolecularVsSerology_WhereConsolidatedMolecularDirectlyMapsToNotSplitSerology_ReturnsSplit()
        {
            var serologyDirectlyMappedToPatientConsolidatedMolecular = new SerologyEntry("shared-serology", SerologySubtype.NotSplit, true);

            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientConsolidatedMolecular })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientConsolidatedMolecular })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        // This covers the case from WMDA matching framework:
        // s1 does not have any splits & s2 is associated to s1, or vice versa.
        [TestCase(SerologySubtype.Split, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.Split)]
        [TestCase(SerologySubtype.NotSplit, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.NotSplit)]
        public void CalculateGrade_ConsolidatedMolecularVsSerology_WithIndirectSplitToAssociatedRelationship_ReturnsSplit(
            SerologySubtype directToConsolidatedMolecularSubtype,
            SerologySubtype indirectToConsolidatedMolecularSubtype
           )
        {
            const string sharedSerologyName = "shared-serology";

            var patientSerologies = new[]
            {
                new SerologyEntry("patient-serology", directToConsolidatedMolecularSubtype, true),
                new SerologyEntry(sharedSerologyName, indirectToConsolidatedMolecularSubtype, false)
            };
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingSerologies(patientSerologies)
                    .Build())
                .Build();

            var donorSerologies = new[]
            {
                new SerologyEntry(sharedSerologyName, indirectToConsolidatedMolecularSubtype, true)
            };
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(donorSerologies)
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_ConsolidatedMolecularVsSerology_WhereConsolidatedMolecularDirectlyMapsToBroadSerology_ReturnsBroad()
        {
            var serologyDirectlyMappedToPatientConsolidatedMolecular = new SerologyEntry("shared-serology", SerologySubtype.Broad, true);

            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientConsolidatedMolecular })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientConsolidatedMolecular })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Broad);
        }

        // This covers the case from WMDA matching framework:
        // s1 is a broad & s2 is a split of s1, or vice versa.
        // s1 is a broad & s2 is associated to a split of s1, or vice versa.
        [TestCase(SerologySubtype.Broad, SerologySubtype.Split)]
        [TestCase(SerologySubtype.Split, SerologySubtype.Broad)]
        [TestCase(SerologySubtype.Broad, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.Broad)]
        public void CalculateGrade_ConsolidatedMolecularVsSerology_WithIndirectBroadRelationship_ReturnsBroad(
            SerologySubtype directToConsolidatedMolecularSubtype,
            SerologySubtype indirectToConsolidatedMolecularSubtype
        )
        {
            const string sharedSerologyName = "shared-serology";

            var patientSerologies = new[]
            {
                new SerologyEntry("patient-serology", directToConsolidatedMolecularSubtype, true),
                new SerologyEntry(sharedSerologyName, indirectToConsolidatedMolecularSubtype, false)
            };
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingSerologies(patientSerologies)
                    .Build())
                .Build();

            var donorSerologies = new[]
            {
                new SerologyEntry(sharedSerologyName, indirectToConsolidatedMolecularSubtype, true)
            };
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(donorSerologies)
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Broad);
        }

        #endregion

        #region Tests: Serology vs Consolidated Molecular

        [Test]
        public void CalculateGrade_SerologyVsConsolidatedMolecular_WhereConsolidatedMolecularDirectlyMapsToAssociatedSerology_ReturnsAssociated()
        {
            var serologyDirectlyMappedToDonorConsolidatedMolecular = new SerologyEntry("shared-serology", SerologySubtype.Associated, true);

            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorConsolidatedMolecular })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorConsolidatedMolecular })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Associated);
        }

        [Test]
        public void CalculateGrade_SerologyVsConsolidatedMolecular_WhereConsolidatedMolecularDirectlyMapsToSplitSerology_ReturnsSplit()
        {
            var serologyDirectlyMappedToDonorConsolidatedMolecular = new SerologyEntry("shared-serology", SerologySubtype.Split, true);

            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorConsolidatedMolecular })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorConsolidatedMolecular })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_SerologyVsConsolidatedMolecular_WhereConsolidatedMolecularDirectlyMapsToNotSplitSerology_ReturnsSplit()
        {
            var serologyDirectlyMappedToDonorConsolidatedMolecular = new SerologyEntry("shared-serology", SerologySubtype.NotSplit, true);

            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorConsolidatedMolecular })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorConsolidatedMolecular })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        // This covers the case from WMDA matching framework:
        // s1 does not have any splits & s2 is associated to s1, or vice versa.
        [TestCase(SerologySubtype.Split, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.Split)]
        [TestCase(SerologySubtype.NotSplit, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.NotSplit)]
        public void CalculateGrade_SerologyVsConsolidatedMolecular_WithIndirectSplitToAssociatedRelationship_ReturnsSplit(
            SerologySubtype directToConsolidateMolecularSubtype,
            SerologySubtype indirectToConsolidatedMolecularSubtype
           )
        {
            const string sharedSerologyName = "shared-serology";

            var patientSerologies = new[]
            {
                new SerologyEntry(sharedSerologyName, indirectToConsolidatedMolecularSubtype, true)
            };
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(patientSerologies)
                    .Build())
                .Build();

            var donorSerologies = new[]
            {
                new SerologyEntry("donor-serology", directToConsolidateMolecularSubtype, true),
                new SerologyEntry(sharedSerologyName, indirectToConsolidatedMolecularSubtype, false)
            };
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingSerologies(donorSerologies)
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_SerologyVsConsolidatedMolecular_WhereConsolidatedMolecularDirectlyMapsToBroadSerology_ReturnsBroad()
        {
            var serologyDirectlyMappedToDonorConsolidatedMolecular = new SerologyEntry("shared-serology", SerologySubtype.Broad, true);

            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorConsolidatedMolecular })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorConsolidatedMolecular })
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Broad);
        }

        // This covers the case from WMDA matching framework:
        // s1 is a broad & s2 is a split of s1, or vice versa.
        // s1 is a broad & s2 is associated to a split of s1, or vice versa.
        [TestCase(SerologySubtype.Broad, SerologySubtype.Split)]
        [TestCase(SerologySubtype.Split, SerologySubtype.Broad)]
        [TestCase(SerologySubtype.Broad, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.Broad)]
        public void CalculateGrade_SerologyVsConsolidatedMolecular_WithIndirectBroadRelationship_ReturnsBroad(
            SerologySubtype directToConsolidateMolecularSubtype,
            SerologySubtype indirectToConsolidatedMolecularSubtype
        )
        {
            const string sharedSerologyName = "shared-serology";

            var patientSerologies = new[]
            {
                new SerologyEntry(sharedSerologyName, indirectToConsolidatedMolecularSubtype, true)
            };
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(patientSerologies)
                    .Build())
                .Build();

            var donorSerologies = new[]
            {
                new SerologyEntry("donor-serology", directToConsolidateMolecularSubtype, true),
                new SerologyEntry(sharedSerologyName, indirectToConsolidatedMolecularSubtype, false)
            };
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingSerologies(donorSerologies)
                    .Build())
                .Build();

            var grade = GradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Broad);
        }

        #endregion
    }
}