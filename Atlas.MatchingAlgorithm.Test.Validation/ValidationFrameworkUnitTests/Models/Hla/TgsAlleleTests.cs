using System.Collections.Generic;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Exceptions;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Validation.ValidationFrameworkUnitTests.Models.Hla
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

            tgsAllele.GetHlaForResolution(HlaTypingResolution.Tgs).Should().Be(fourFieldName);
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

            tgsAllele.GetHlaForResolution(HlaTypingResolution.ThreeFieldTruncatedAllele).Should().Be(expectedThreeFieldTruncation);
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

            tgsAllele.GetHlaForResolution(HlaTypingResolution.TwoFieldTruncatedAllele).Should().Be(expectedTwoFieldTruncation);
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

            tgsAllele.GetHlaForResolution(HlaTypingResolution.Tgs).Should().Be(threeFieldName);
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

            tgsAllele.GetHlaForResolution(HlaTypingResolution.TwoFieldTruncatedAllele).Should().Be(expectedTwoFieldTruncation);
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

            tgsAllele.GetHlaForResolution(HlaTypingResolution.ThreeFieldTruncatedAllele).Should().BeNull();
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

            tgsAllele.GetHlaForResolution(HlaTypingResolution.Tgs).Should().Be(twoFieldName);
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

            tgsAllele.GetHlaForResolution(HlaTypingResolution.ThreeFieldTruncatedAllele).Should().BeNull();
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

            tgsAllele.GetHlaForResolution(HlaTypingResolution.TwoFieldTruncatedAllele).Should().BeNull();
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

            tgsAllele.GetHlaForResolution(HlaTypingResolution.NmdpCode).Should().Be(nmdpCode);
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

            tgsAllele.GetHlaForResolution(HlaTypingResolution.Serology).Should().Be(serology);
        }

        [Test]
        public void FromTestDataAllele_WithProvidedPGroup_SetsPGroup()
        {
            const string pGroup = "01:01P";
            var testData = new AlleleTestData
            {
                AlleleName = "01:01",
                PGroup = pGroup
            };

            var tgsAllele = TgsAllele.FromTestDataAllele(testData);

            tgsAllele.GetHlaForResolution(HlaTypingResolution.PGroup).Should().Be(pGroup);
        }

        [Test]
        public void FromTestDataAllele_WithProvidedGGroup_SetsGGroup()
        {
            const string gGroup = "01:01:01G";
            var testData = new AlleleTestData
            {
                AlleleName = "01:01",
                GGroup = gGroup
            };

            var tgsAllele = TgsAllele.FromTestDataAllele(testData);

            tgsAllele.GetHlaForResolution(HlaTypingResolution.GGroup).Should().Be(gGroup);
        }

        [Test]
        public void FromTestDataAllele_SetsXxCodeFromFirstField()
        {
            const string alleleString = "01:01:01:01";
            const string expectedXxCode = "01:XX";
            var testData = new AlleleTestData {AlleleName = alleleString};

            var tgsAllele = TgsAllele.FromTestDataAllele(testData);

            tgsAllele.GetHlaForResolution(HlaTypingResolution.XxCode).Should().Be(expectedXxCode);
        }

        [Test]
        public void FromTestDataAllele_WhenOtherAllelesProvided_ForFourFieldAllele_CreatesAlleleStringOfNames()
        {
            const string alleleName = "01:01:01:01";
            const string otherAlleleName1 = "02:02:02:02";
            const string otherAlleleName2 = "03:03:03";
            const string expectedAlleleString = "01:01:01:01/02:02:02:02/03:03:03";

            var alleleTestData = new AlleleTestData {AlleleName = alleleName};
            var otherAlleles = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = otherAlleleName1},
                new AlleleTestData {AlleleName = otherAlleleName2}
            };

            var tgsAllele = TgsAllele.FromTestDataAllele(alleleTestData, new AlleleStringOptions {NameString = otherAlleles});

            tgsAllele.GetHlaForResolution(HlaTypingResolution.AlleleStringOfNames).Should().Be(expectedAlleleString);
        }

        [Test]
        public void FromTestDataAllele_WhenOtherAllelesProvided_ForThreeFieldAllele_CreatesAlleleStringOfNames()
        {
            const string alleleName = "01:01:01";
            const string otherAlleleName1 = "02:02";
            const string otherAlleleName2 = "03:03:03:03";
            const string expectedAlleleString = "01:01:01/02:02/03:03:03:03";

            var alleleTestData = new AlleleTestData {AlleleName = alleleName};
            var otherAlleles = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = otherAlleleName1},
                new AlleleTestData {AlleleName = otherAlleleName2}
            };

            var tgsAllele = TgsAllele.FromTestDataAllele(alleleTestData, new AlleleStringOptions {NameString = otherAlleles});

            tgsAllele.GetHlaForResolution(HlaTypingResolution.AlleleStringOfNames).Should().Be(expectedAlleleString);
        }

        [Test]
        public void FromTestDataAllele_WhenOtherAllelesProvided_ForTwoFieldAllele_CreatesAlleleStringOfNames()
        {
            const string alleleName = "01:01";
            const string otherAlleleName1 = "02:02:02";
            const string otherAlleleName2 = "03:03:03:03";
            const string expectedAlleleString = "01:01/02:02:02/03:03:03:03";

            var alleleTestData = new AlleleTestData {AlleleName = alleleName};
            var otherAlleles = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = otherAlleleName1},
                new AlleleTestData {AlleleName = otherAlleleName2}
            };

            var tgsAllele = TgsAllele.FromTestDataAllele(alleleTestData, new AlleleStringOptions {NameString = otherAlleles});

            tgsAllele.GetHlaForResolution(HlaTypingResolution.AlleleStringOfNames).Should().Be(expectedAlleleString);
        }

        [Test]
        public void FromTestDataAllele_WhenOtherAllelesProvided_TrimsAsterisksInStringOfNames()
        {
            const string alleleName = "*01:01";
            const string otherAlleleName1 = "*02:02:02";
            const string otherAlleleName2 = "*03:03:03:03";
            const string expectedAlleleString = "*01:01/02:02:02/03:03:03:03";

            var alleleTestData = new AlleleTestData {AlleleName = alleleName};
            var otherAlleles = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = otherAlleleName1},
                new AlleleTestData {AlleleName = otherAlleleName2}
            };

            var tgsAllele = TgsAllele.FromTestDataAllele(alleleTestData, new AlleleStringOptions {NameString = otherAlleles});

            tgsAllele.GetHlaForResolution(HlaTypingResolution.AlleleStringOfNames).Should().Be(expectedAlleleString);
        }

        [Test]
        public void FromTestDataAllele_WhenOtherAllelesProvided_CreatesAlleleStringOfSubtypes()
        {
            const string alleleName = "01:01:01";
            const string otherAlleleName1 = "01:02:02";
            const string otherAlleleName2 = "01:03:03:03";
            const string expectedAlleleString = "01:01/02/03";

            var alleleTestData = new AlleleTestData {AlleleName = alleleName};
            var otherAlleles = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = otherAlleleName1},
                new AlleleTestData {AlleleName = otherAlleleName2}
            };

            var tgsAllele = TgsAllele.FromTestDataAllele(alleleTestData, new AlleleStringOptions {SubtypeString = otherAlleles});

            tgsAllele.GetHlaForResolution(HlaTypingResolution.AlleleStringOfSubtypes).Should().Be(expectedAlleleString);
        }

        [Test]
        public void FromTestDataAllele_WhenNoOtherAllelesProvided_DoesNotCreateAlleleStringOfNames()
        {
            const string alleleName = "01:01:01";
            var alleleTestData = new AlleleTestData {AlleleName = alleleName};

            var tgsAllele = TgsAllele.FromTestDataAllele(alleleTestData);

            tgsAllele.GetHlaForResolution(HlaTypingResolution.AlleleStringOfNames).Should().BeNull();
        }

        [Test]
        public void FromTestDataAllele_WhenNoOtherAllelesProvided_DoesNotCreateAlleleStringOfSubtypes()
        {
            const string alleleName = "01:01:01";
            var alleleTestData = new AlleleTestData {AlleleName = alleleName};

            var tgsAllele = TgsAllele.FromTestDataAllele(alleleTestData);

            tgsAllele.GetHlaForResolution(HlaTypingResolution.AlleleStringOfSubtypes).Should().BeNull();
        }

        [Test]
        public void FromTestDataAllele_WhenOtherAllelesProvided_DoesNotCreateAlleleStringOfSubtypes()
        {
            const string alleleName = "01:01:01";
            var alleleTestData = new AlleleTestData {AlleleName = alleleName};

            var tgsAllele = TgsAllele.FromTestDataAllele(alleleTestData);

            tgsAllele.GetHlaForResolution(HlaTypingResolution.AlleleStringOfSubtypes).Should().BeNull();
        }

        [Test]
        public void FromTestDataAllele_WhenOtherAllelesProvidedDoNotShareAFirstFieldWithPrimaryAllele_ThrowsException()
        {
            const string alleleName = "01:01:01";
            const string otherAlleleName1 = "02:02:02";
            const string otherAlleleName2 = "02:03:03:03";

            var alleleTestData = new AlleleTestData {AlleleName = alleleName};
            var otherAlleles = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = otherAlleleName1},
                new AlleleTestData {AlleleName = otherAlleleName2}
            };

            Assert.Throws<InvalidTestDataException>(() => TgsAllele.FromTestDataAllele(
                alleleTestData,
                new AlleleStringOptions {SubtypeString = otherAlleles}
            ));
        }

        [Test]
        public void FromTestDataAllele_WhenOtherAllelesProvidedDoNotShareAFirstField_ThrowsException()
        {
            const string alleleName = "01:01:01";
            const string otherAlleleName1 = "01:02:02";
            const string otherAlleleName2 = "02:03:03:03";

            var alleleTestData = new AlleleTestData {AlleleName = alleleName};
            var otherAlleles = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = otherAlleleName1},
                new AlleleTestData {AlleleName = otherAlleleName2}
            };

            Assert.Throws<InvalidTestDataException>(() =>
                TgsAllele.FromTestDataAllele(
                    alleleTestData,
                    new AlleleStringOptions {SubtypeString = otherAlleles}
                ));
        }
        
        [Test]
        public void FromTestDataAllele_WhenOtherAllelesProvided_ForStringWithSinglePGroup_CreatesAlleleString()
        {
            const string alleleName = "01:01:01:01";
            const string otherAlleleName1 = "02:02:02:02";
            const string otherAlleleName2 = "03:03:03";
            const string expectedAlleleString = "01:01:01:01/02:02:02:02/03:03:03";

            var alleleTestData = new AlleleTestData {AlleleName = alleleName};
            var otherAlleles = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = otherAlleleName1},
                new AlleleTestData {AlleleName = otherAlleleName2}
            };

            var tgsAllele = TgsAllele.FromTestDataAllele(alleleTestData, new AlleleStringOptions {NameStringWithSinglePGroup = otherAlleles});

            tgsAllele.GetHlaForResolution(HlaTypingResolution.AlleleStringOfNamesWithSinglePGroup).Should().Be(expectedAlleleString);
        }
        
        [Test]
        public void FromTestDataAllele_WhenOtherAllelesProvided_ForStringWithMultiplePGroups_CreatesAlleleString()
        {
            const string alleleName = "01:01:01:01";
            const string otherAlleleName1 = "02:02:02:02";
            const string otherAlleleName2 = "03:03:03";
            const string expectedAlleleString = "01:01:01:01/02:02:02:02/03:03:03";

            var alleleTestData = new AlleleTestData {AlleleName = alleleName};
            var otherAlleles = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = otherAlleleName1},
                new AlleleTestData {AlleleName = otherAlleleName2}
            };

            var tgsAllele = TgsAllele.FromTestDataAllele(alleleTestData, new AlleleStringOptions {NameStringWithMultiplePGroups = otherAlleles});

            tgsAllele.GetHlaForResolution(HlaTypingResolution.AlleleStringOfNamesWithMultiplePGroups).Should().Be(expectedAlleleString);
        }
        
        [Test]
        public void FromTestDataAllele_WhenNoOtherAllelesProvided_ForStringWithSinglePGroup_DoesNotCreateAlleleString()
        {
            const string alleleName = "01:01:01:01";

            var alleleTestData = new AlleleTestData {AlleleName = alleleName};

            var tgsAllele = TgsAllele.FromTestDataAllele(alleleTestData);

            tgsAllele.GetHlaForResolution(HlaTypingResolution.AlleleStringOfNamesWithSinglePGroup).Should().BeNullOrEmpty();
        }
        
        [Test]
        public void FromTestDataAllele_WhenNoOtherAllelesProvided_ForStringWithMultiplePGroups_DoesNotCreateAlleleString()
        {
            const string alleleName = "01:01:01:01";

            var alleleTestData = new AlleleTestData {AlleleName = alleleName};

            var tgsAllele = TgsAllele.FromTestDataAllele(alleleTestData);

            tgsAllele.GetHlaForResolution(HlaTypingResolution.AlleleStringOfNamesWithMultiplePGroups).Should().BeNullOrEmpty();
        }

        [Test]
        public void FromTestDataAllele_WithFourFieldTestData_AndUnambiguousResolution_GetsFourFieldName()
        {
            const string fourFieldName = "01:02:03:04";
            var testData = new AlleleTestData
            {
                AlleleName = fourFieldName
            };

            var tgsAllele = TgsAllele.FromTestDataAllele(testData);

            tgsAllele.GetHlaForResolution(HlaTypingResolution.Unambiguous).Should().Be(fourFieldName);
        }

        [Test]
        public void FromTestDataAllele_WithThreeFieldTestData_AndUnambiguousResolution_ReturnsNull()
        {
            const string threeFieldName = "01:02:03";
            var testData = new AlleleTestData
            {
                AlleleName = threeFieldName
            };

            var tgsAllele = TgsAllele.FromTestDataAllele(testData);

            tgsAllele.GetHlaForResolution(HlaTypingResolution.Unambiguous).Should().BeNullOrEmpty();
        }

        [Test]
        public void FromTestDataAllele_WithTwoFieldTestData_AndUnambiguousResolution_ReturnsNull()
        {
            const string twoFieldName = "01:02";
            var testData = new AlleleTestData
            {
                AlleleName = twoFieldName
            };

            var tgsAllele = TgsAllele.FromTestDataAllele(testData);

            tgsAllele.GetHlaForResolution(HlaTypingResolution.Unambiguous).Should().BeNullOrEmpty();
        }
    }
}