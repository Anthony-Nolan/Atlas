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
    public class SerologyGradingCalculatorTests
    {
        private ISerologyGradingCalculator serologyGradingCalculator;

        [SetUp]
        public void SetUp()
        {
            serologyGradingCalculator = new SerologyGradingCalculator();
        }

        #region Tests: Both Typings are Serology

        [Test]
        public void CalculateGrade_BothTypingsAreSerology_WithSameName_AndAssociatedSubtype_ReturnsAssociated()
        {
            const string sharedSerologyName = "shared-serology";

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(sharedSerologyName)
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(SerologySubtype.Associated)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(sharedSerologyName)
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(SerologySubtype.Associated)
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Associated);
        }

        [Test]
        public void CalculateGrade_BothTypingsAreSerology_WithSameName_AndSplitSubtype_ReturnsSplit()
        {
            const string sharedSerologyName = "shared-serology";

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(sharedSerologyName)
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(SerologySubtype.Split)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(sharedSerologyName)
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(SerologySubtype.Split)
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_BothTypingsAreSerology_WithSameName_AndNotSplitSubtype_ReturnsSplit()
        {
            const string sharedSerologyName = "shared-serology";

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(sharedSerologyName)
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(SerologySubtype.NotSplit)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(sharedSerologyName)
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(SerologySubtype.NotSplit)
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

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
            const string patientSerologyName = "patient-serology";
            const string donorSerologyName = "donor-serology";

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(patientSerologyName)
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(patientSerologySubtype)
                    .WithMatchingSerologies(new[] { new SerologyEntry(donorSerologyName, donorSerologySubtype, false) })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(donorSerologyName)
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(donorSerologySubtype)
                    .WithMatchingSerologies(new[] { new SerologyEntry(patientSerologyName, patientSerologySubtype, false) })
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_BothTypingsAreSerology_WithSameName_AndBroadSubtype_ReturnsBroad()
        {
            const string sharedSerologyName = "shared-serology";

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(sharedSerologyName)
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(SerologySubtype.Broad)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(sharedSerologyName)
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(SerologySubtype.Broad)
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

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
            const string patientSerologyName = "patient-serology";
            const string donorSerologyName = "donor-serology";

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(patientSerologyName)
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(patientSerologySubtype)
                    .WithMatchingSerologies(new[] { new SerologyEntry(donorSerologyName, donorSerologySubtype, false) })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(donorSerologyName)
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(donorSerologySubtype)
                    .WithMatchingSerologies(new[] { new SerologyEntry(patientSerologyName, patientSerologySubtype, false) })
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Broad);
        }

        #endregion       

        #region Tests: Expressing Allele vs Serology

        [Test]
        public void CalculateGrade_ExpressingAlleleVsSerology_WhereAlleleDirectlyMapsToAssociatedSerology_ReturnsAssociated()
        {
            var serologyDirectlyMappedToPatientAllele = new SerologyEntry("shared-serology", SerologySubtype.Associated, true);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.OriginalAllele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithLookupName(serologyDirectlyMappedToPatientAllele.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToPatientAllele.SerologySubtype)
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Associated);
        }

        [Test]
        public void CalculateGrade_ExpressingAlleleVsSerology_WhereAlleleDirectlyMapsToSplitSerology_ReturnsSplit()
        {
            var serologyDirectlyMappedToPatientAllele = new SerologyEntry("shared-serology", SerologySubtype.Split, true);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.OriginalAllele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyDirectlyMappedToPatientAllele.Name)
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToPatientAllele.SerologySubtype)
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_ExpressingAlleleVsSerology_WhereAlleleDirectlyMapsToNotSplitSerology_ReturnsSplit()
        {
            var serologyDirectlyMappedToPatientAllele = new SerologyEntry("shared-serology", SerologySubtype.NotSplit, true);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.OriginalAllele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyDirectlyMappedToPatientAllele.Name)
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToPatientAllele.SerologySubtype)
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        // This covers the case from WMDA matching framework:
        // s1 does not have any splits & s2 is associated to s1, or vice versa.
        [TestCase(SerologySubtype.Split, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.Split)]
        [TestCase(SerologySubtype.NotSplit, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.NotSplit)]
        public void CalculateGrade_ExpressingAlleleVsSerology_WithIndirectAssociatedRelationship_ReturnsSplit(
            SerologySubtype directSerologySubtype,
            SerologySubtype indirectSerologySubtype
           )
        {
            var serologyDirectlyMappedToAllele = new SerologyEntry("patient-serology", directSerologySubtype, true);
            var serologyIndirectlyMatchedToAllele = new SerologyEntry("shared-serology", indirectSerologySubtype, false);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.OriginalAllele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[]
                    {
                        serologyDirectlyMappedToAllele,
                        serologyIndirectlyMatchedToAllele
                    })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyIndirectlyMatchedToAllele.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyIndirectlyMatchedToAllele.SerologySubtype)
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_ExpressingAlleleVsSerology_WhereAlleleDirectlyMapsToBroadSerology_ReturnsBroad()
        {
            var serologyDirectlyMappedToPatientAllele = new SerologyEntry("shared-serology", SerologySubtype.Broad, true);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.OriginalAllele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyDirectlyMappedToPatientAllele.Name)
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToPatientAllele.SerologySubtype)
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

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
            SerologySubtype directSerologySubtype,
            SerologySubtype indirectSerologySubtype
        )
        {
            var serologyDirectlyMappedToAllele = new SerologyEntry("patient-serology", directSerologySubtype, true);
            var serologyIndirectlyMatchedToAllele = new SerologyEntry("shared-serology", indirectSerologySubtype, false);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.OriginalAllele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[]
                    {
                        serologyDirectlyMappedToAllele,
                        serologyIndirectlyMatchedToAllele
                    })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyIndirectlyMatchedToAllele.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyIndirectlyMatchedToAllele.SerologySubtype)
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Broad);
        }

        #endregion

        #region Tests: Serology vs Expressing Allele

        [Test]
        public void CalculateGrade_SerologyVsExpressingAllele_WhereAlleleDirectlyMapsToAssociatedSerology_ReturnsAssociated()
        {
            var serologyDirectlyMappedToDonorAllele = new SerologyEntry("shared-serology", SerologySubtype.Associated, true);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithLookupName(serologyDirectlyMappedToDonorAllele.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToDonorAllele.SerologySubtype)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.OriginalAllele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorAllele })
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Associated);
        }

        [Test]
        public void CalculateGrade_SerologyVsExpressingAllele_WhereAlleleDirectlyMapsToSplitSerology_ReturnsSplit()
        {
            var serologyDirectlyMappedToDonorAllele = new SerologyEntry("shared-serology", SerologySubtype.Split, true);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyDirectlyMappedToDonorAllele.Name)
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToDonorAllele.SerologySubtype)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.OriginalAllele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorAllele })
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_SerologyVsExpressingAllele_WhereAlleleDirectlyMapsToNotSplitSerology_ReturnsSplit()
        {
            var serologyDirectlyMappedToDonorAllele = new SerologyEntry("shared-serology", SerologySubtype.NotSplit, true);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyDirectlyMappedToDonorAllele.Name)
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToDonorAllele.SerologySubtype)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.OriginalAllele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorAllele })
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        // This covers the case from WMDA matching framework:
        // s1 does not have any splits & s2 is associated to s1, or vice versa.
        [TestCase(SerologySubtype.Split, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.Split)]
        [TestCase(SerologySubtype.NotSplit, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.NotSplit)]
        public void CalculateGrade_SerologyVsExpressingAllele_WithIndirectAssociatedRelationship_ReturnsSplit(
            SerologySubtype directSerologySubtype,
            SerologySubtype indirectSerologySubtype
           )
        {
            var serologyDirectlyMappedToAllele = new SerologyEntry("donor-serology", directSerologySubtype, true);
            var serologyIndirectlyMatchedToAllele = new SerologyEntry("shared-serology", indirectSerologySubtype, false);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyIndirectlyMatchedToAllele.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyIndirectlyMatchedToAllele.SerologySubtype)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.OriginalAllele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[]
                    {
                        serologyDirectlyMappedToAllele,
                        serologyIndirectlyMatchedToAllele
                    })
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_SerologyVsExpressingAllele_WhereAlleleDirectlyMapsToBroadSerology_ReturnsBroad()
        {
            var serologyDirectlyMappedToDonorAllele = new SerologyEntry("shared-serology", SerologySubtype.Broad, true);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyDirectlyMappedToDonorAllele.Name)
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToDonorAllele.SerologySubtype)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.OriginalAllele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorAllele })
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

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
            SerologySubtype directSerologySubtype,
            SerologySubtype indirectSerologySubtype
        )
        {
            var serologyDirectlyMappedToAllele = new SerologyEntry("donor-serology", directSerologySubtype, true);
            var serologyIndirectlyMatchedToAllele = new SerologyEntry("shared-serology", indirectSerologySubtype, false);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyIndirectlyMatchedToAllele.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyIndirectlyMatchedToAllele.SerologySubtype)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.OriginalAllele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[]
                    {
                        serologyDirectlyMappedToAllele,
                        serologyIndirectlyMatchedToAllele
                    })
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Broad);
        }

        #endregion

        #region Tests: Null Allele vs Serology

        [Test]
        public void CalculateGrade_NullAlleleVsSerology_ReturnsMismatch()
        {
            const string patientAlleleName = "999:999N";
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(patientAlleleName)
                .WithLookupResultCategory(LookupResultCategory.OriginalAllele)
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Associated);
        }

        #endregion

        #region Tests: Serology vs Null Allele

        [Test]
        public void CalculateGrade_SerologyVsNullAllele_ReturnsMismatch()
        {
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .Build();

            const string donorAlleleName = "999:999N";
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(donorAlleleName)
                .WithLookupResultCategory(LookupResultCategory.OriginalAllele)
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Associated);
        }

        #endregion

        #region Tests: XX Code vs Serology

        [Test]
        public void CalculateGrade_XxCodeVsSerology_WhereXxCodeDirectlyMapsToAssociatedSerology_ReturnsAssociated()
        {
            var serologyDirectlyMappedToPatientXxCode = new SerologyEntry("shared-serology", SerologySubtype.Associated, true);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.XxCode)
                .WithHlaScoringInfo(new XxCodeScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientXxCode })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithLookupName(serologyDirectlyMappedToPatientXxCode.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToPatientXxCode.SerologySubtype)
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Associated);
        }

        [Test]
        public void CalculateGrade_XxCodeVsSerology_WhereXxCodeDirectlyMapsToSplitSerology_ReturnsSplit()
        {
            var serologyDirectlyMappedToPatientXxCode = new SerologyEntry("shared-serology", SerologySubtype.Split, true);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.XxCode)
                .WithHlaScoringInfo(new XxCodeScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientXxCode })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyDirectlyMappedToPatientXxCode.Name)
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToPatientXxCode.SerologySubtype)
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_XxCodeVsSerology_WhereXxCodeDirectlyMapsToNotSplitSerology_ReturnsSplit()
        {
            var serologyDirectlyMappedToPatientXxCode = new SerologyEntry("shared-serology", SerologySubtype.NotSplit, true);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.XxCode)
                .WithHlaScoringInfo(new XxCodeScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientXxCode })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyDirectlyMappedToPatientXxCode.Name)
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToPatientXxCode.SerologySubtype)
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        // This covers the case from WMDA matching framework:
        // s1 does not have any splits & s2 is associated to s1, or vice versa.
        [TestCase(SerologySubtype.Split, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.Split)]
        [TestCase(SerologySubtype.NotSplit, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.NotSplit)]
        public void CalculateGrade_XxCodeVsSerology_WithIndirectAssociatedRelationship_ReturnsSplit(
            SerologySubtype directSerologySubtype,
            SerologySubtype indirectSerologySubtype
           )
        {
            var serologyDirectlyMappedToXxCode = new SerologyEntry("patient-serology", directSerologySubtype, true);
            var serologyIndirectlyMatchedToXxCode = new SerologyEntry("shared-serology", indirectSerologySubtype, false);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.XxCode)
                .WithHlaScoringInfo(new XxCodeScoringInfoBuilder()
                    .WithMatchingSerologies(new[]
                    {
                        serologyDirectlyMappedToXxCode,
                        serologyIndirectlyMatchedToXxCode
                    })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyIndirectlyMatchedToXxCode.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyIndirectlyMatchedToXxCode.SerologySubtype)
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_XxCodeVsSerology_WhereXxCodeDirectlyMapsToBroadSerology_ReturnsBroad()
        {
            var serologyDirectlyMappedToPatientXxCode = new SerologyEntry("shared-serology", SerologySubtype.Broad, true);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.XxCode)
                .WithHlaScoringInfo(new XxCodeScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientXxCode })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyDirectlyMappedToPatientXxCode.Name)
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToPatientXxCode.SerologySubtype)
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Broad);
        }

        // This covers the case from WMDA matching framework:
        // s1 is a broad & s2 is a split of s1, or vice versa.
        // s1 is a broad & s2 is associated to a split of s1, or vice versa.
        [TestCase(SerologySubtype.Broad, SerologySubtype.Split)]
        [TestCase(SerologySubtype.Split, SerologySubtype.Broad)]
        [TestCase(SerologySubtype.Broad, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.Broad)]
        public void CalculateGrade_XxCodeVsSerology_WithIndirectBroadRelationship_ReturnsBroad(
            SerologySubtype directSerologySubtype,
            SerologySubtype indirectSerologySubtype
        )
        {
            var serologyDirectlyMappedToXxCode = new SerologyEntry("patient-serology", directSerologySubtype, true);
            var serologyIndirectlyMatchedToXxCode = new SerologyEntry("shared-serology", indirectSerologySubtype, false);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.XxCode)
                .WithHlaScoringInfo(new XxCodeScoringInfoBuilder()
                    .WithMatchingSerologies(new[]
                    {
                        serologyDirectlyMappedToXxCode,
                        serologyIndirectlyMatchedToXxCode
                    })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyIndirectlyMatchedToXxCode.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyIndirectlyMatchedToXxCode.SerologySubtype)
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Broad);
        }

        #endregion

        #region Tests: Serology vs XX Code

        [Test]
        public void CalculateGrade_SerologyVsXxCode_WhereXxCodeDirectlyMapsToAssociatedSerology_ReturnsAssociated()
        {
            var serologyDirectlyMappedToDonorXxCode = new SerologyEntry("shared-serology", SerologySubtype.Associated, true);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithLookupName(serologyDirectlyMappedToDonorXxCode.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToDonorXxCode.SerologySubtype)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.XxCode)
                .WithHlaScoringInfo(new XxCodeScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorXxCode })
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Associated);
        }

        [Test]
        public void CalculateGrade_SerologyVsXxCode_WhereXxCodeDirectlyMapsToSplitSerology_ReturnsSplit()
        {
            var serologyDirectlyMappedToDonorXxCode = new SerologyEntry("shared-serology", SerologySubtype.Split, true);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyDirectlyMappedToDonorXxCode.Name)
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToDonorXxCode.SerologySubtype)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.XxCode)
                .WithHlaScoringInfo(new XxCodeScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorXxCode })
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_SerologyVsXxCode_WhereXxCodeDirectlyMapsToNotSplitSerology_ReturnsSplit()
        {
            var serologyDirectlyMappedToDonorXxCode = new SerologyEntry("shared-serology", SerologySubtype.NotSplit, true);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyDirectlyMappedToDonorXxCode.Name)
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToDonorXxCode.SerologySubtype)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.XxCode)
                .WithHlaScoringInfo(new XxCodeScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorXxCode })
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        // This covers the case from WMDA matching framework:
        // s1 does not have any splits & s2 is associated to s1, or vice versa.
        [TestCase(SerologySubtype.Split, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.Split)]
        [TestCase(SerologySubtype.NotSplit, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.NotSplit)]
        public void CalculateGrade_SerologyVsXxCode_WithIndirectAssociatedRelationship_ReturnsSplit(
            SerologySubtype directSerologySubtype,
            SerologySubtype indirectSerologySubtype
           )
        {
            var serologyDirectlyMappedToXxCode = new SerologyEntry("donor-serology", directSerologySubtype, true);
            var serologyIndirectlyMatchedToXxCode = new SerologyEntry("shared-serology", indirectSerologySubtype, false);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyIndirectlyMatchedToXxCode.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyIndirectlyMatchedToXxCode.SerologySubtype)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.XxCode)
                .WithHlaScoringInfo(new XxCodeScoringInfoBuilder()
                    .WithMatchingSerologies(new[]
                    {
                        serologyDirectlyMappedToXxCode,
                        serologyIndirectlyMatchedToXxCode
                    })
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_SerologyVsXxCode_WhereXxCodeDirectlyMapsToBroadSerology_ReturnsBroad()
        {
            var serologyDirectlyMappedToDonorXxCode = new SerologyEntry("shared-serology", SerologySubtype.Broad, true);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyDirectlyMappedToDonorXxCode.Name)
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToDonorXxCode.SerologySubtype)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.XxCode)
                .WithHlaScoringInfo(new XxCodeScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorXxCode })
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Broad);
        }

        // This covers the case from WMDA matching framework:
        // s1 is a broad & s2 is a split of s1, or vice versa.
        // s1 is a broad & s2 is associated to a split of s1, or vice versa.
        [TestCase(SerologySubtype.Broad, SerologySubtype.Split)]
        [TestCase(SerologySubtype.Split, SerologySubtype.Broad)]
        [TestCase(SerologySubtype.Broad, SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Associated, SerologySubtype.Broad)]
        public void CalculateGrade_SerologyVsXxCode_WithIndirectBroadRelationship_ReturnsBroad(
            SerologySubtype directSerologySubtype,
            SerologySubtype indirectSerologySubtype
        )
        {
            var serologyDirectlyMappedToXxCode = new SerologyEntry("donor-serology", directSerologySubtype, true);
            var serologyIndirectlyMatchedToXxCode = new SerologyEntry("shared-serology", indirectSerologySubtype, false);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyIndirectlyMatchedToXxCode.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyIndirectlyMatchedToXxCode.SerologySubtype)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.XxCode)
                .WithHlaScoringInfo(new XxCodeScoringInfoBuilder()
                    .WithMatchingSerologies(new[]
                    {
                        serologyDirectlyMappedToXxCode,
                        serologyIndirectlyMatchedToXxCode
                    })
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Broad);
        }

        #endregion
    }
}