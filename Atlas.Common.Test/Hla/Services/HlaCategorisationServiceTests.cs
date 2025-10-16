using System.Collections.Generic;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.Common.Utils.Http;
using NUnit.Framework;

namespace Atlas.Common.Test.Hla.Services
{
    [TestFixture]
    public class HlaCategorisationServiceTests
    {
        private IHlaCategorisationService hlaCategorisationService;

        [SetUp]
        public void SetUp()
        {
            hlaCategorisationService = new HlaCategorisationService();
        }

        [TestCase("01:XX1")]
        [TestCase("ABC:01")]
        [TestCase("01:ABC:DEF")]
        [TestCase("01:G")]
        [TestCase("01:01G")]
        [TestCase("01:01:01:01G")]
        [TestCase("01:P")]
        [TestCase("01:01:01P")]
        [TestCase("01:01:01:01P")]
        [TestCase("01")]
        [TestCase("*1")]
        [TestCase("01:01/")]
        [TestCase("01:01/02:XX")]
        [TestCase("01:01:01:01:01/01:02")]
        [TestCase("01:01:01/02")]
        [TestCase("01:01:01:01/02")]
        [TestCase(":")]
        [TestCase("01:")]
        [TestCase(":01")]
        [TestCase("01::01")]
        [TestCase("01:01:01:01:01")]
        [TestCase("01:01:01:01:")]
        [TestCase("01:01X")]
        [TestCase("")]
        public void GetHlaTypingCategory_WhenHlaNameDoesNotFitKnownPattern_ThrowsBadRequestException(string hlaName)
        {
            Assert.Throws<AtlasHttpException>(() => hlaCategorisationService.GetHlaTypingCategory(hlaName));
        }

        [TestCase("*01:XX")]
        [TestCase("01:XX")]
        [TestCase("9999:XX")]
        public void GetHlaTypingCategory_WhenHlaNameFitsXxCodePattern_ReturnsXxCode(string hlaName)
        {
            Assert.AreEqual(hlaCategorisationService.GetHlaTypingCategory(hlaName), HlaTypingCategory.XxCode);
        }

        [TestCase("*01:AB")]
        [TestCase("*01:ABC")]
        [TestCase("*01:ABCD")]
        [TestCase("01:ABCDE")]
        [TestCase("01:ABCDEF")]
        [TestCase("01:AAXX")]
        [TestCase("01:XXNMDP")]
        public void GetHlaTypingCategory_WhenHlaNameFitsNmdpCodePattern_ReturnsNmdpCode(string hlaName)
        {
            Assert.AreEqual(hlaCategorisationService.GetHlaTypingCategory(hlaName), HlaTypingCategory.NmdpCode);
        }

        [TestCase("*01:01:01G")]
        [TestCase("01:01:01G")]
        [TestCase("999:999:999G")]
        public void GetHlaTypingCategory_WhenHlaNameFitsGGroupPattern_ReturnsGGroup(string hlaName)
        {
            Assert.AreEqual(hlaCategorisationService.GetHlaTypingCategory(hlaName), HlaTypingCategory.GGroup);
        }

        [TestCase("*01:01P")]
        [TestCase("01:01P")]
        [TestCase("999:999P")]
        public void GetHlaTypingCategory_WhenHlaNameFitsPGroupPattern_ReturnsPGroup(string hlaName)
        {
            Assert.AreEqual(hlaCategorisationService.GetHlaTypingCategory(hlaName), HlaTypingCategory.PGroup);
        }

        [TestCase("*01:01g")]
        [TestCase("01:01g")]
        [TestCase("999:999g")]
        public void GetHlaTypingCategory_WhenHlaNameFitsSmallGGroupPattern_ReturnsSmallGGroup(string hlaName)
        {
            Assert.AreEqual(hlaCategorisationService.GetHlaTypingCategory(hlaName), HlaTypingCategory.SmallGGroup);
        }

        [TestCase("1")]
        [TestCase("123")]
        [TestCase("9999")]
        public void GetHlaTypingCategory_WhenHlaNameFitsSerologyPattern_ReturnsSerology(string hlaName)
        {
            Assert.AreEqual(hlaCategorisationService.GetHlaTypingCategory(hlaName), HlaTypingCategory.Serology);
        }

        [TestCase("*01:01")]
        [TestCase("*01:01:01")]
        [TestCase("*01:01:01:01")]
        [TestCase("*01:01N")]
        [TestCase("*01:01:01C")]
        [TestCase("*01:01:01:01L")]
        [TestCase("01:01Q")]
        [TestCase("01:01:01A")]
        [TestCase("01:01:01:01S")]
        [TestCase("999:999:999:999")]
        public void GetHlaTypingCategory_WhenHlaNameFitsAllelePattern_ReturnsAllele(string hlaName)
        {
            Assert.AreEqual(hlaCategorisationService.GetHlaTypingCategory(hlaName), HlaTypingCategory.Allele);
        }

        [TestCase("*01:01/01:02")]
        [TestCase("01:01/01:02/01:03")]
        [TestCase("*01:01/*01:02/*01:03")]
        [TestCase("99:99/99:99:99/99:99:99:99/123:123N")]
        public void GetHlaTypingCategory_WhenHlaNameFitsAlleleStringOfNamesPattern_ReturnsAlleleStringOfNames(string hlaName)
        {
            Assert.AreEqual(hlaCategorisationService.GetHlaTypingCategory(hlaName), HlaTypingCategory.AlleleStringOfNames);
        }

        [TestCase("*01:01/02")]
        [TestCase("*99:99N/100")]
        [TestCase("90:91/92L/93/94N/95")]
        public void GetHlaTypingCategory_WhenHlaNameFitsAlleleStringOfSubtypesPattern_ReturnsAlleleStringOfSubtypes(string hlaName)
        {
            Assert.AreEqual(hlaCategorisationService.GetHlaTypingCategory(hlaName), HlaTypingCategory.AlleleStringOfSubtypes);
        }

        [TestCase("NEW")]
        public void GetHlaTypingCategory_WhenHlaNameIsNewAllele_ReturnsNEW(string hlaName)
        {
            Assert.AreEqual(hlaCategorisationService.GetHlaTypingCategory(hlaName), HlaTypingCategory.NEW);
        }

        [Test, Repeat(100000), IgnoreExceptOnCiPerfTest("Ran in ~2.9s")]
        public void PerformanceTest()
        {
            const string nmdpCode = "*01:NMDP";
            const string xxCode = "*01:XX";
            const string gGroup = "01:01:901G";
            const string pGroup = "01:901P";
            const string serology = "7";
            const string allele = "*01:01";
            const string alleleStringOfNames = "*01:01/02:01";
            const string alleleStringOfSubtypes = "*01:01/02";
            foreach (var hla in new List<string> {nmdpCode, xxCode, gGroup, pGroup, serology, allele, alleleStringOfNames, alleleStringOfSubtypes})
            {
                hlaCategorisationService.GetHlaTypingCategory(hla);
            }
        }
    }
}