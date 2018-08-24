using System.Collections.Generic;
using System.Web.SessionState;
using System.Web.UI;
using FluentAssertions;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationFrameworkUnitTests.Models.Hla
{
    /// <summary>
    /// Note for all these tests that the TgsAllele class assumes the test data will be in the correct format.
    /// AlleleName in the test data must contain at least one ':'
    /// </summary>
    [TestFixture]
    public class TgsAlleleTests
    {
        [Test]
        public void FromTestDataAllele_WithFourFieldTestData_SetsTgsAlleleToFourField()
        {
            const string fourFieldName = "01:02:03:04";
            var testData = new AlleleTestData
            {
                AlleleName = fourFieldName,
            };

            var tgsAllele = TgsAllele.FromTestDataAllele(testData);

            tgsAllele.GetHlaForCategory(HlaTypingResolution.Tgs).Should().Be(fourFieldName);
        }

        [Test]
        public void FromTestDataAllele_WithFourFieldTestData_TruncatesToThreeFieldAllele()
        {
            const string fourFieldName = "01:02:03:04";
            const string expectedThreeFieldTruncation = "01:02:03";
            var testData = new AlleleTestData
            {
                AlleleName = fourFieldName,
            };

            var tgsAllele = TgsAllele.FromTestDataAllele(testData);

            tgsAllele.GetHlaForCategory(HlaTypingResolution.ThreeFieldTruncatedAllele).Should().Be(expectedThreeFieldTruncation);
        }

        [Test]
        public void FromTestDataAllele_WithFourFieldTestData_TruncatesToTwoFieldAllele()
        {
            const string fourFieldName = "01:02:03:04";
            const string expectedTwoFieldTruncation = "01:02";
            var testData = new AlleleTestData
            {
                AlleleName = fourFieldName,
            };

            var tgsAllele = TgsAllele.FromTestDataAllele(testData);

            tgsAllele.GetHlaForCategory(HlaTypingResolution.TwoFieldTruncatedAllele).Should().Be(expectedTwoFieldTruncation);
        }

        [Test]
        public void FromTestDataAllele_WithThreeFieldTestData_SetsTgsAlleleToThreeField()
        {
            const string threeFieldName = "01:02:03";
            var testData = new AlleleTestData
            {
                AlleleName = threeFieldName,
            };

            var tgsAllele = TgsAllele.FromTestDataAllele(testData);

            tgsAllele.GetHlaForCategory(HlaTypingResolution.Tgs).Should().Be(threeFieldName);
        }

        [Test]
        public void FromTestDataAllele_WithThreeFieldTestData_TruncatesToTwoFieldAllele()
        {
            const string threeFieldName = "01:02:03";
            const string expectedTwoFieldTruncation = "01:02";
            var testData = new AlleleTestData
            {
                AlleleName = threeFieldName,
            };

            var tgsAllele = TgsAllele.FromTestDataAllele(testData);

            tgsAllele.GetHlaForCategory(HlaTypingResolution.TwoFieldTruncatedAllele).Should().Be(expectedTwoFieldTruncation);
        }

        [Test]
        public void FromTestDataAllele_WithThreeFieldTestData_DoesNotReturnThreeFieldTruncatedData()
        {
            const string threeFieldName = "01:02:03";
            var testData = new AlleleTestData
            {
                AlleleName = threeFieldName,
            };

            var tgsAllele = TgsAllele.FromTestDataAllele(testData);

            tgsAllele.GetHlaForCategory(HlaTypingResolution.ThreeFieldTruncatedAllele).Should().BeNull();
        }

        [Test]
        public void FromTestDataAllele_WithTwoFieldTestData_SetsTgsAlleleToTwoField()
        {
            const string twoFieldName = "01:02";
            var testData = new AlleleTestData
            {
                AlleleName = twoFieldName,
            };

            var tgsAllele = TgsAllele.FromTestDataAllele(testData);

            tgsAllele.GetHlaForCategory(HlaTypingResolution.Tgs).Should().Be(twoFieldName);
        }

        [Test]
        public void FromTestDataAllele_WithTwoFieldTestData_DoesNotSetThreeFieldTruncatedData()
        {
            const string twoFieldName = "01:02";
            var testData = new AlleleTestData
            {
                AlleleName = twoFieldName,
            };

            var tgsAllele = TgsAllele.FromTestDataAllele(testData);

            tgsAllele.GetHlaForCategory(HlaTypingResolution.ThreeFieldTruncatedAllele).Should().BeNull();
        }

        [Test]
        public void FromTestDataAllele_WithTwoFieldTestData_DoesNotReturnTwoFieldTruncatedData()
        {
            const string twoFieldName = "01:02";
            var testData = new AlleleTestData
            {
                AlleleName = twoFieldName,
            };

            var tgsAllele = TgsAllele.FromTestDataAllele(testData);

            tgsAllele.GetHlaForCategory(HlaTypingResolution.TwoFieldTruncatedAllele).Should().BeNull();
        }

        [Test]
        public void FromTestDataAllele_WithProvidedNmdpCode_SetsNmdpCode()
        {
            const string nmdpCode = "*01:NMDP";
            var testData = new AlleleTestData
            {
                AlleleName = "01:01",
                NmdpCode = nmdpCode,
            };

            var tgsAllele = TgsAllele.FromTestDataAllele(testData);

            tgsAllele.GetHlaForCategory(HlaTypingResolution.NmdpCode).Should().Be(nmdpCode);
        }

        [Test]
        public void FromTestDataAllele_WithProvidedSerology_SetsSerology()
        {
            const string serology = "1";
            var testData = new AlleleTestData
            {
                AlleleName = "01:01",
                Serology = serology,
            };

            var tgsAllele = TgsAllele.FromTestDataAllele(testData);

            tgsAllele.GetHlaForCategory(HlaTypingResolution.Serology).Should().Be(serology);
        }

        [Test]
        public void FromTestDataAllele_SetsXxCodeFromFirstField()
        {
            const string alleleString = "01:01:01:01";
            const string expectedXxCode = "01:XX";
            var testData = new AlleleTestData {AlleleName = alleleString};

            var tgsAllele = TgsAllele.FromTestDataAllele(testData);

            tgsAllele.GetHlaForCategory(HlaTypingResolution.XxCode).Should().Be(expectedXxCode);
        }

        [Test]
        public void FromTestDataAllele_WhenOtherAllelesProvided_ForFourFieldAllele_CreatesAlleleStringOfNames()
        {
            const string alleleName = "01:01:01:01";
            const string otherAlleleName1 = "02:02:02:02";
            const string otherAlleleName2 = "03:03:03";
            const string expectedAlleleString = "01:01:01:01/02:02:02:02/03:03:03";
            
            var alleleTestData = new AlleleTestData{ AlleleName = alleleName };
            var otherAlleles = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = otherAlleleName1},
                new AlleleTestData {AlleleName = otherAlleleName2}
            };

            var tgsAllele = TgsAllele.FromTestDataAllele(alleleTestData, otherAlleles);

            tgsAllele.GetHlaForCategory(HlaTypingResolution.AlleleStringOfNames).Should().Be(expectedAlleleString);
        }
        
        [Test]
        public void FromTestDataAllele_WhenOtherAllelesProvided_ForThreeFieldAllele_CreatesAlleleStringOfNames()
        {
            const string alleleName = "01:01:01";
            const string otherAlleleName1 = "02:02";
            const string otherAlleleName2 = "03:03:03:03";
            const string expectedAlleleString = "01:01:01/02:02/03:03:03:03";
            
            var alleleTestData = new AlleleTestData{ AlleleName = alleleName };
            var otherAlleles = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = otherAlleleName1},
                new AlleleTestData {AlleleName = otherAlleleName2}
            };

            var tgsAllele = TgsAllele.FromTestDataAllele(alleleTestData, otherAlleles);

            tgsAllele.GetHlaForCategory(HlaTypingResolution.AlleleStringOfNames).Should().Be(expectedAlleleString);
        }
             
        [Test]
        public void FromTestDataAllele_WhenOtherAllelesProvided_ForTwoFieldAllele_CreatesAlleleStringOfNames()
        {
            const string alleleName = "01:01";
            const string otherAlleleName1 = "02:02:02";
            const string otherAlleleName2 = "03:03:03:03";
            const string expectedAlleleString = "01:01/02:02:02/03:03:03:03";
            
            var alleleTestData = new AlleleTestData{ AlleleName = alleleName };
            var otherAlleles = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = otherAlleleName1},
                new AlleleTestData {AlleleName = otherAlleleName2}
            };

            var tgsAllele = TgsAllele.FromTestDataAllele(alleleTestData, otherAlleles);

            tgsAllele.GetHlaForCategory(HlaTypingResolution.AlleleStringOfNames).Should().Be(expectedAlleleString);
        }
        
        [Test]
        public void FromTestDataAllele_WhenOtherAllelesProvided_CreatesAlleleStringOfSubtypes()
        {
            const string alleleName = "01:01:01";
            const string otherAlleleName1 = "01:02:02";
            const string otherAlleleName2 = "01:03:03:03";
            const string expectedAlleleString = "01:01/02/03";
            
            var alleleTestData = new AlleleTestData{ AlleleName = alleleName };
            var otherAlleles = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = otherAlleleName1},
                new AlleleTestData {AlleleName = otherAlleleName2}
            };

            var tgsAllele = TgsAllele.FromTestDataAllele(alleleTestData, otherAlleles);

            tgsAllele.GetHlaForCategory(HlaTypingResolution.AlleleStringOfSubtypes).Should().Be(expectedAlleleString);
        }
        
        [Test]
        public void FromTestDataAllele_WhenNoOtherAllelesProvided_DoesNotCreateAlleleStringOfNames()
        {
            const string alleleName = "01:01:01";
            var alleleTestData = new AlleleTestData{ AlleleName = alleleName };

            var tgsAllele = TgsAllele.FromTestDataAllele(alleleTestData);

            tgsAllele.GetHlaForCategory(HlaTypingResolution.AlleleStringOfNames).Should().BeNull();
        }
        
        [Test]
        public void FromTestDataAllele_WhenNoOtherAllelesProvided_DoesNotCreateAlleleStringOfSubtypes()
        {
            const string alleleName = "01:01:01";
            var alleleTestData = new AlleleTestData{ AlleleName = alleleName };

            var tgsAllele = TgsAllele.FromTestDataAllele(alleleTestData);

            tgsAllele.GetHlaForCategory(HlaTypingResolution.AlleleStringOfSubtypes).Should().BeNull();
        }
        
        [Test]
        public void FromTestDataAllele_WhenOtherAllelesProvidedDoNotShareAFirstFieldWithPrimaryAllele_DoesNotCreateAlleleStringOfSubtypes()
        {
            const string alleleName = "01:01:01";
            const string otherAlleleName1 = "02:02:02";
            const string otherAlleleName2 = "02:03:03:03";
            
            var alleleTestData = new AlleleTestData{ AlleleName = alleleName };
            var otherAlleles = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = otherAlleleName1},
                new AlleleTestData {AlleleName = otherAlleleName2}
            };

            var tgsAllele = TgsAllele.FromTestDataAllele(alleleTestData, otherAlleles);

            tgsAllele.GetHlaForCategory(HlaTypingResolution.AlleleStringOfSubtypes).Should().BeNull();
        }
        
        [Test]
        public void FromTestDataAllele_WhenOtherAllelesProvidedDoNotShareAFirstField_OnlyCreatesAlleleStringWithAllelesMatchingFirstFieldOfPrimaryAllele()
        {
            const string alleleName = "01:01:01";
            const string otherAlleleName1 = "01:02:02";
            const string otherAlleleName2 = "02:03:03:03";
            const string expectedAlleleString = "01:01/02";
            
            var alleleTestData = new AlleleTestData{ AlleleName = alleleName };
            var otherAlleles = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = otherAlleleName1},
                new AlleleTestData {AlleleName = otherAlleleName2}
            };

            var tgsAllele = TgsAllele.FromTestDataAllele(alleleTestData, otherAlleles);

            tgsAllele.GetHlaForCategory(HlaTypingResolution.AlleleStringOfSubtypes).Should().Be(expectedAlleleString);
        }
    }
}