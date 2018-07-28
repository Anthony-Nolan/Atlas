using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
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

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Associated);
        }

        [Test]
        public void CalculateGrade_BothTypingsAreSerology_WithSameName_AndSplitSubtype_ReturnsSplit()
        {
            const string sharedSerologyName = "shared-serology";

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

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_BothTypingsAreSerology_WithSameName_AndNotSplitSubtype_ReturnsSplit()
        {
            const string sharedSerologyName = "shared-serology";

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
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(patientSerologySubtype)
                    .WithMatchingSerologies(new[] { new SerologyEntry(donorSerologyName, donorSerologySubtype, false) })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(donorSerologyName)
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
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(patientSerologySubtype)
                    .WithMatchingSerologies(new[] { new SerologyEntry(donorSerologyName, donorSerologySubtype, false) })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(donorSerologyName)
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
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
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
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyDirectlyMappedToPatientAllele.Name)
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
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyDirectlyMappedToPatientAllele.Name)
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
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyDirectlyMappedToPatientAllele.Name)
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
                .WithLookupName(serologyDirectlyMappedToDonorAllele.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToDonorAllele.SerologySubtype)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
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
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToDonorAllele.SerologySubtype)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
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
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToDonorAllele.SerologySubtype)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
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
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToDonorAllele.SerologySubtype)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
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
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().Build())
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
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().Build())
                .Build();

            const string donorAlleleName = "999:999N";
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(donorAlleleName)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Associated);
        }

        #endregion

        #region Tests: Multiple Alleles vs Serology

        [Test]
        public void CalculateGrade_MultipleAlleleVsSerology_WhereMultipleAlleleDirectlyMapsToAssociatedSerology_ReturnsAssociated()
        {
            var serologyDirectlyMappedToPatientMultipleAllele = 
                new SerologyEntry("shared-serology", SerologySubtype.Associated, true);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientMultipleAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyDirectlyMappedToPatientMultipleAllele.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToPatientMultipleAllele.SerologySubtype)
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Associated);
        }

        [Test]
        public void CalculateGrade_MultipleAlleleVsSerology_WhereMultipleAlleleDirectlyMapsToSplitSerology_ReturnsSplit()
        {
            var serologyDirectlyMappedToPatientMultipleAllele = new SerologyEntry("shared-serology", SerologySubtype.Split, true);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientMultipleAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyDirectlyMappedToPatientMultipleAllele.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToPatientMultipleAllele.SerologySubtype)
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_MultipleAlleleVsSerology_WhereMultipleAlleleDirectlyMapsToNotSplitSerology_ReturnsSplit()
        {
            var serologyDirectlyMappedToPatientMultipleAllele = new SerologyEntry("shared-serology", SerologySubtype.NotSplit, true);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientMultipleAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyDirectlyMappedToPatientMultipleAllele.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToPatientMultipleAllele.SerologySubtype)
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
        public void CalculateGrade_MultipleAlleleVsSerology_WithIndirectAssociatedRelationship_ReturnsSplit(
            SerologySubtype directSerologySubtype,
            SerologySubtype indirectSerologySubtype
           )
        {
            var serologyDirectlyMappedToMultipleAllele = new SerologyEntry("patient-serology", directSerologySubtype, true);
            var serologyIndirectlyMatchedToMultipleAllele = new SerologyEntry("shared-serology", indirectSerologySubtype, false);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[]
                    {
                        serologyDirectlyMappedToMultipleAllele,
                        serologyIndirectlyMatchedToMultipleAllele
                    })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyIndirectlyMatchedToMultipleAllele.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyIndirectlyMatchedToMultipleAllele.SerologySubtype)
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_MultipleAlleleVsSerology_WhereMultipleAlleleDirectlyMapsToBroadSerology_ReturnsBroad()
        {
            var serologyDirectlyMappedToPatientMultipleAllele = new SerologyEntry("shared-serology", SerologySubtype.Broad, true);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientMultipleAllele })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyDirectlyMappedToPatientMultipleAllele.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToPatientMultipleAllele.SerologySubtype)
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
        public void CalculateGrade_MultipleAlleleVsSerology_WithIndirectBroadRelationship_ReturnsBroad(
            SerologySubtype directSerologySubtype,
            SerologySubtype indirectSerologySubtype
        )
        {
            var serologyDirectlyMappedToMultipleAllele = new SerologyEntry("patient-serology", directSerologySubtype, true);
            var serologyIndirectlyMatchedToMultipleAllele = new SerologyEntry("shared-serology", indirectSerologySubtype, false);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[]
                    {
                        serologyDirectlyMappedToMultipleAllele,
                        serologyIndirectlyMatchedToMultipleAllele
                    })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyIndirectlyMatchedToMultipleAllele.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyIndirectlyMatchedToMultipleAllele.SerologySubtype)
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Broad);
        }

        #endregion

        #region Tests: Serology vs Multiple Alleles

        [Test]
        public void CalculateGrade_SerologyVsMultipleAllele_WhereMultipleAlleleDirectlyMapsToAssociatedSerology_ReturnsAssociated()
        {
            var serologyDirectlyMappedToDonorMultipleAllele = new SerologyEntry("shared-serology", SerologySubtype.Associated, true);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyDirectlyMappedToDonorMultipleAllele.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToDonorMultipleAllele.SerologySubtype)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorMultipleAllele })
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Associated);
        }

        [Test]
        public void CalculateGrade_SerologyVsMultipleAllele_WhereMultipleAlleleDirectlyMapsToSplitSerology_ReturnsSplit()
        {
            var serologyDirectlyMappedToDonorMultipleAllele = new SerologyEntry("shared-serology", SerologySubtype.Split, true);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyDirectlyMappedToDonorMultipleAllele.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToDonorMultipleAllele.SerologySubtype)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorMultipleAllele })
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_SerologyVsMultipleAllele_WhereMultipleAlleleDirectlyMapsToNotSplitSerology_ReturnsSplit()
        {
            var serologyDirectlyMappedToDonorMultipleAllele = new SerologyEntry("shared-serology", SerologySubtype.NotSplit, true);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyDirectlyMappedToDonorMultipleAllele.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToDonorMultipleAllele.SerologySubtype)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorMultipleAllele })
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
        public void CalculateGrade_SerologyVsMultipleAllele_WithIndirectAssociatedRelationship_ReturnsSplit(
            SerologySubtype directSerologySubtype,
            SerologySubtype indirectSerologySubtype
           )
        {
            var serologyDirectlyMappedToMultipleAllele = new SerologyEntry("donor-serology", directSerologySubtype, true);
            var serologyIndirectlyMatchedToMultipleAllele = new SerologyEntry("shared-serology", indirectSerologySubtype, false);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyIndirectlyMatchedToMultipleAllele.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyIndirectlyMatchedToMultipleAllele.SerologySubtype)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[]
                    {
                        serologyDirectlyMappedToMultipleAllele,
                        serologyIndirectlyMatchedToMultipleAllele
                    })
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_SerologyVsMultipleAllele_WhereMultipleAlleleDirectlyMapsToBroadSerology_ReturnsBroad()
        {
            var serologyDirectlyMappedToDonorMultipleAllele = new SerologyEntry("shared-serology", SerologySubtype.Broad, true);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyDirectlyMappedToDonorMultipleAllele.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToDonorMultipleAllele.SerologySubtype)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorMultipleAllele })
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
        public void CalculateGrade_SerologyVsMultipleAllele_WithIndirectBroadRelationship_ReturnsBroad(
            SerologySubtype directSerologySubtype,
            SerologySubtype indirectSerologySubtype
        )
        {
            var serologyDirectlyMappedToMultipleAllele = new SerologyEntry("donor-serology", directSerologySubtype, true);
            var serologyIndirectlyMatchedToMultipleAllele = new SerologyEntry("shared-serology", indirectSerologySubtype, false);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyIndirectlyMatchedToMultipleAllele.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyIndirectlyMatchedToMultipleAllele.SerologySubtype)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithMatchingSerologies(new[]
                    {
                        serologyDirectlyMappedToMultipleAllele,
                        serologyIndirectlyMatchedToMultipleAllele
                    })
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Broad);
        }

        #endregion

        #region Tests: Consolidated Molecular vs Serology

        [Test]
        public void CalculateGrade_ConsolidatedMolecularVsSerology_WhereConsolidatedMolecularDirectlyMapsToAssociatedSerology_ReturnsAssociated()
        {
            var serologyDirectlyMappedToPatientConsolidatedMolecular = new SerologyEntry("shared-serology", SerologySubtype.Associated, true);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientConsolidatedMolecular })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyDirectlyMappedToPatientConsolidatedMolecular.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToPatientConsolidatedMolecular.SerologySubtype)
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Associated);
        }

        [Test]
        public void CalculateGrade_ConsolidatedMolecularVsSerology_WhereConsolidatedMolecularDirectlyMapsToSplitSerology_ReturnsSplit()
        {
            var serologyDirectlyMappedToPatientConsolidatedMolecular = new SerologyEntry("shared-serology", SerologySubtype.Split, true);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientConsolidatedMolecular })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyDirectlyMappedToPatientConsolidatedMolecular.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToPatientConsolidatedMolecular.SerologySubtype)
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_ConsolidatedMolecularVsSerology_WhereConsolidatedMolecularDirectlyMapsToNotSplitSerology_ReturnsSplit()
        {
            var serologyDirectlyMappedToPatientConsolidatedMolecular = new SerologyEntry("shared-serology", SerologySubtype.NotSplit, true);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientConsolidatedMolecular })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyDirectlyMappedToPatientConsolidatedMolecular.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToPatientConsolidatedMolecular.SerologySubtype)
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
        public void CalculateGrade_ConsolidatedMolecularVsSerology_WithIndirectAssociatedRelationship_ReturnsSplit(
            SerologySubtype directSerologySubtype,
            SerologySubtype indirectSerologySubtype
           )
        {
            var serologyDirectlyMappedToConsolidatedMolecular = new SerologyEntry("patient-serology", directSerologySubtype, true);
            var serologyIndirectlyMatchedToConsolidatedMolecular = new SerologyEntry("shared-serology", indirectSerologySubtype, false);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingSerologies(new[]
                    {
                        serologyDirectlyMappedToConsolidatedMolecular,
                        serologyIndirectlyMatchedToConsolidatedMolecular
                    })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyIndirectlyMatchedToConsolidatedMolecular.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyIndirectlyMatchedToConsolidatedMolecular.SerologySubtype)
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_ConsolidatedMolecularVsSerology_WhereConsolidatedMolecularDirectlyMapsToBroadSerology_ReturnsBroad()
        {
            var serologyDirectlyMappedToPatientConsolidatedMolecular = new SerologyEntry("shared-serology", SerologySubtype.Broad, true);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToPatientConsolidatedMolecular })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyDirectlyMappedToPatientConsolidatedMolecular.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToPatientConsolidatedMolecular.SerologySubtype)
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
        public void CalculateGrade_ConsolidatedMolecularVsSerology_WithIndirectBroadRelationship_ReturnsBroad(
            SerologySubtype directSerologySubtype,
            SerologySubtype indirectSerologySubtype
        )
        {
            var serologyDirectlyMappedToConsolidatedMolecular = new SerologyEntry("patient-serology", directSerologySubtype, true);
            var serologyIndirectlyMatchedToConsolidatedMolecular = new SerologyEntry("shared-serology", indirectSerologySubtype, false);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingSerologies(new[]
                    {
                        serologyDirectlyMappedToConsolidatedMolecular,
                        serologyIndirectlyMatchedToConsolidatedMolecular
                    })
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyIndirectlyMatchedToConsolidatedMolecular.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyIndirectlyMatchedToConsolidatedMolecular.SerologySubtype)
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Broad);
        }

        #endregion

        #region Tests: Serology vs Consolidated Molecular

        [Test]
        public void CalculateGrade_SerologyVsConsolidatedMolecular_WhereConsolidatedMolecularDirectlyMapsToAssociatedSerology_ReturnsAssociated()
        {
            var serologyDirectlyMappedToDonorConsolidatedMolecular = new SerologyEntry("shared-serology", SerologySubtype.Associated, true);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyDirectlyMappedToDonorConsolidatedMolecular.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToDonorConsolidatedMolecular.SerologySubtype)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorConsolidatedMolecular })
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Associated);
        }

        [Test]
        public void CalculateGrade_SerologyVsConsolidatedMolecular_WhereConsolidatedMolecularDirectlyMapsToSplitSerology_ReturnsSplit()
        {
            var serologyDirectlyMappedToDonorConsolidatedMolecular = new SerologyEntry("shared-serology", SerologySubtype.Split, true);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyDirectlyMappedToDonorConsolidatedMolecular.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToDonorConsolidatedMolecular.SerologySubtype)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorConsolidatedMolecular })
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_SerologyVsConsolidatedMolecular_WhereConsolidatedMolecularDirectlyMapsToNotSplitSerology_ReturnsSplit()
        {
            var serologyDirectlyMappedToDonorConsolidatedMolecular = new SerologyEntry("shared-serology", SerologySubtype.NotSplit, true);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyDirectlyMappedToDonorConsolidatedMolecular.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToDonorConsolidatedMolecular.SerologySubtype)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorConsolidatedMolecular })
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
        public void CalculateGrade_SerologyVsConsolidatedMolecular_WithIndirectAssociatedRelationship_ReturnsSplit(
            SerologySubtype directSerologySubtype,
            SerologySubtype indirectSerologySubtype
           )
        {
            var serologyDirectlyMappedToConsolidatedMolecular = new SerologyEntry("donor-serology", directSerologySubtype, true);
            var serologyIndirectlyMatchedToConsolidatedMolecular = new SerologyEntry("shared-serology", indirectSerologySubtype, false);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyIndirectlyMatchedToConsolidatedMolecular.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyIndirectlyMatchedToConsolidatedMolecular.SerologySubtype)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingSerologies(new[]
                    {
                        serologyDirectlyMappedToConsolidatedMolecular,
                        serologyIndirectlyMatchedToConsolidatedMolecular
                    })
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Split);
        }

        [Test]
        public void CalculateGrade_SerologyVsConsolidatedMolecular_WhereConsolidatedMolecularDirectlyMapsToBroadSerology_ReturnsBroad()
        {
            var serologyDirectlyMappedToDonorConsolidatedMolecular = new SerologyEntry("shared-serology", SerologySubtype.Broad, true);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyDirectlyMappedToDonorConsolidatedMolecular.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyDirectlyMappedToDonorConsolidatedMolecular.SerologySubtype)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { serologyDirectlyMappedToDonorConsolidatedMolecular })
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
        public void CalculateGrade_SerologyVsConsolidatedMolecular_WithIndirectBroadRelationship_ReturnsBroad(
            SerologySubtype directSerologySubtype,
            SerologySubtype indirectSerologySubtype
        )
        {
            var serologyDirectlyMappedToConsolidatedMolecular = new SerologyEntry("donor-serology", directSerologySubtype, true);
            var serologyIndirectlyMatchedToConsolidatedMolecular = new SerologyEntry("shared-serology", indirectSerologySubtype, false);

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupName(serologyIndirectlyMatchedToConsolidatedMolecular.Name)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithSerologySubtype(serologyIndirectlyMatchedToConsolidatedMolecular.SerologySubtype)
                    .Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingSerologies(new[]
                    {
                        serologyDirectlyMappedToConsolidatedMolecular,
                        serologyIndirectlyMatchedToConsolidatedMolecular
                    })
                    .Build())
                .Build();

            var grade = serologyGradingCalculator.CalculateGrade(patientLookupResult, donorLookupResult);

            grade.Should().Be(MatchGrade.Broad);
        }

        #endregion
    }
}