using System.Collections.Generic;
using Atlas.Utils.Hla.Services;
using FluentAssertions;
using Atlas.Utils.Core.Http.Exceptions;
using NUnit.Framework;

namespace Atlas.Utils.Test.Hla.Services
{
    [TestFixture]
    public class AlleleStringSplitterServiceTests
    {
        private IAlleleStringSplitterService splitterService;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var categorisationService = new HlaCategorisationService();
            splitterService = new AlleleStringSplitterService(categorisationService);
        }

        [TestCase("*01:01/01:02", new[] { "01:01", "01:02" })]
        [TestCase("01:01/01:02/01:03", new[] { "01:01", "01:02", "01:03" })]
        [TestCase("*01:01/*01:02/*01:03", new[] { "01:01", "01:02", "01:03" })]
        [TestCase("99:99/99:99:99/99:99:99:99/123:123N", new[] { "99:99", "99:99:99", "99:99:99:99", "123:123N" })]
        [TestCase("*01:01/02", new[] { "01:01", "01:02" })]
        [TestCase("*99:99N/100", new[] { "99:99N", "99:100" })]
        [TestCase("90:91/92L/93/94N/95", new[] { "90:91", "90:92L", "90:93", "90:94N", "90:95" })]
        public void GetAlleleNamesFromAlleleString_WhenAlleleString_ReturnsAlleles(string alleleString, string[] expectedAlleles)
        {
            var actualAlleles = splitterService.GetAlleleNamesFromAlleleString(alleleString);
            actualAlleles.Should().BeEquivalentTo((IEnumerable<string>)expectedAlleles);
        }

        [TestCase("*01:01:01:01")]
        [TestCase("*01:XX")]
        [TestCase("*01:NMDP")]
        [TestCase("*01:01:01G")]
        [TestCase("*01:01P")]
        [TestCase("1")]
        [TestCase("NOT-VALID-HLA")]
        public void GetAlleleNamesFromAlleleString_WhenHlaNameIsNotAlleleString_RaiseError(string hlaName)
        {
            Assert.Throws<NovaHttpException>(() => splitterService.GetAlleleNamesFromAlleleString(hlaName));
        }
    }
}
