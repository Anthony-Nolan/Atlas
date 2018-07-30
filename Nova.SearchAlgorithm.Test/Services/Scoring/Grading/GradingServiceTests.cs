using Nova.SearchAlgorithm.Services.Scoring.Grading;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Services.Scoring.Grading
{
    public class GradingServiceTests
    {
        private IGradingService gradingService;

        [SetUp]
        public void SetUpBeforeEachTest()
        {
            gradingService = new GradingService();
        }

        #region Tests: Best Orientation(s) returned

        [Test]
        public void CalculateGrades_TwoMatchesInDirect_TwoMismatchesInCross_ReturnsDirectOrientation()
        {
            
        }

        [Test]
        public void CalculateGrades_TwoBetterMatchesInDirect_TwoWorseMatchesInCross_ReturnsDirectOrientation()
        {

        }

        [Test]
        public void CalculateGrades_BetterMatchAndMismatchInDirect_WorseMatchAndMismatchInCross_ReturnsDirectOrientation()
        {

        }

        [Test]
        public void CalculateGrades_TwoMismatchesInDirect_TwoMatchesInCross_ReturnsCrossOrientation()
        {

        }

        [Test]
        public void CalculateGrades_TwoWorseMatchesInDirect_TwoBetterMatchesInCross_ReturnsCrossOrientation()
        {

        }

        [Test]
        public void CalculateGrades_WorseMatchAndMismatchInDirect_BetterMatchAndMismatchInCross_ReturnsCrossOrientation()
        {

        }

        [Test]
        public void CalculateGrades_TwoSameMatchesInDirect_TwoSameMatchesInCross_ReturnsBothOrientations()
        {

        }

        [Test]
        public void CalculateGrades_SameMatchAndMismatchInDirect_SameMatchAndMismatchInCross_ReturnsBothOrientations()
        {

        }

        [Test]
        public void CalculateGrades_TwoMismatchesInDirect_TwoMismatchesInCross_ReturnsBothOrientations()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsMissingTheLocusTyping_ReturnsBothOrientations()
        {

        }

        [Test]
        public void CalculateGrades_DonorIsMissingTheLocusTyping_ReturnsBothOrientations()
        {

        }

        [Test]
        public void CalculateGrades_PatientAndDonorAreBothMissingTheLocusTyping_ReturnsBothOrientations()
        {

        }

        #endregion

        #region Tests: Best Grades returned

        [Test]
        public void CalculateGrades_TwoMatchesInDirect_TwoMismatchesInCross_ReturnsTwoMatches()
        {

        }

        [Test]
        public void CalculateGrades_TwoBetterMatchesInDirect_TwoWorseMatchesInCross_ReturnsTwoBetterMatches()
        {

        }

        [Test]
        public void CalculateGrades_BetterMatchAndMismatchInDirect_WorseMatchAndMismatchInCross_ReturnsBetterMatchAndMismatch()
        {

        }

        [Test]
        public void CalculateGrades_TwoMismatchesInDirect_TwoMatchesInCross_ReturnsTwoMatches()
        {

        }

        [Test]
        public void CalculateGrades_TwoWorseMatchesInDirect_TwoBetterMatchesInCross_ReturnsTwoBetterMatches()
        {

        }

        [Test]
        public void CalculateGrades_WorseMatchAndMismatchInDirect_BetterMatchAndMismatchInCross_ReturnsBetterMatchAndMismatch()
        {

        }

        [Test]
        public void CalculateGrades_TwoSameMatchesInDirect_TwoSameMatchesInCross_ReturnsTwoSameMatches()
        {

        }

        [Test]
        public void CalculateGrades_SameMatchAndMismatchInDirect_SameMatchAndMismatchInCross_ReturnsSameMatchAndMismatch()
        {

        }

        [Test]
        public void CalculateGrades_TwoMismatchesInDirect_TwoMismatchesInCross_ReturnsTwoMismatches()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsMissingTheLocusTyping_ReturnsTwoPGroupGrades()
        {

        }

        [Test]
        public void CalculateGrades_DonorIsMissingTheLocusTyping_ReturnsTwoPGroupGrades()
        {

        }

        [Test]
        public void CalculateGrades_PatientAndDonorAreBothMissingTheLocusTyping_ReturnsTwoPGroupGrades()
        {

        }

        #endregion

        #region Tests: Molecular or Serology Grade assigned

        [Test]
        public void CalculateGrades_PatientIsMolecular_DonorIsMolecular_ReturnsMolecularGrade()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsMolecular_DonorIsSerology_ReturnsSerologyGrade()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsSerology_DonorIsMolecular_ReturnsSerologyGrade()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsSerology_DonorIsSerology_ReturnsSerologyGrade()
        {

        }

        #endregion

        #region Tests: Typing affects the maximum possible grade assigned

        [Test]
        public void CalculateGrades_PatientIsSingleAllele_DonorIsSingleAllele_ReturnsMaxGradeOfGDna()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsSingleAllele_DonorIsMultipleAllele_ReturnsMaxGradeOfGDna()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsSingleAllele_DonorIsConsolidatedMolecular_ReturnsMaxGradeOfGGroup()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsSingleAllele_DonorIsSerology_ReturnsMaxGradeOfAssociated()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsMultipleAllele_DonorIsSingleAllele_ReturnsMaxGradeOfGDna()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsMultipleAllele_DonorIsMultipleAllele_ReturnsMaxGradeOfGDna()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsMultipleAllele_DonorIsConsolidatedMolecular_ReturnsMaxGradeOfGGroup()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsMultipleAllele_DonorIsSerology_ReturnsMaxGradeOfAssociated()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsConsolidatedMolecular_DonorIsSingleAllele_ReturnsMaxGradeOfGGroup()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsConsolidatedMolecular_DonorIsMultipleAllele_ReturnsMaxGradeOfGGroup()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsConsolidatedMolecular_DonorIsConsolidatedMolecular_ReturnsMaxGradeOfGGroup()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsConsolidatedMolecular_DonorIsSerology_ReturnsMaxGradeOfAssociated()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsSerology_DonorIsSingleAllele_ReturnsMaxGradeOfAssociated()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsSerology_DonorIsMultipleAllele_ReturnsMaxGradeOfAssociated()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsSerology_DonorIsConsolidatedMolecular_ReturnsMaxGradeOfAssociated()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsSerology_DonorIsSerology_ReturnsMaxGradeOfAssociated()
        {

        }

        #endregion
    }
}
