using FluentAssertions;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationFrameworkUnitTests.Models.Hla
{
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

    }
}