using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Test.SharedTestHelpers;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Atlas.Common.Test.Hla.Services
{
    [TestFixture]
    public class AlleleNamesExtractorTests
    {
        private IAlleleNamesExtractor extractor;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                var categorisationService = new HlaCategorisationService();
                extractor = new AlleleNamesExtractor(categorisationService);
            });
        }

        [TestCase("*01:01/01:02", new[] { "01:01", "01:02" })]
        [TestCase("01:01/01:02/01:03", new[] { "01:01", "01:02", "01:03" })]
        [TestCase("*01:01/*01:02/*01:03", new[] { "01:01", "01:02", "01:03" })]
        [TestCase("99:99/99:99:99/99:99:99:99/123:123N", new[] { "99:99", "99:99:99", "99:99:99:99", "123:123N" })]
        [TestCase("*01:01/02", new[] { "01:01", "01:02" })]
        [TestCase("*99:99N/100", new[] { "99:99N", "99:100" })]
        [TestCase("90:91/92L/93/94N/95", new[] { "90:91", "90:92L", "90:93", "90:94N", "90:95" })]
        public void GetAlleleNamesFromAlleleString_ReturnsAlleles(string alleleString, string[] expectedAlleles)
        {
            var actualAlleles = extractor.GetAlleleNamesFromAlleleString(alleleString);
            actualAlleles.Should().BeEquivalentTo((IEnumerable<string>)expectedAlleles);
        }

        [TestCase("*01:01:01:01")]
        [TestCase("*01:XX")]
        [TestCase("*01:NMDP")]
        [TestCase("*01:01:01G")]
        [TestCase("*01:01P")]
        [TestCase("1")]
        public void GetAlleleNamesFromAlleleString_HlaIsNotAlleleString_RaiseError(string hlaName)
        {
            extractor.Invoking(service => service.GetAlleleNamesFromAlleleString(hlaName)).Should().Throw<Exception>();
        }

        [Test]
        public void GetAlleleNamesFromAlleleString_InvalidHla_RaiseError()
        {
            extractor.Invoking(service => service.GetAlleleNamesFromAlleleString("NOT-VALID-HLA")).Should().Throw<Exception>();
        }
    }
}
