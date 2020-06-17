using Atlas.MultipleAlleleCodeDictionary.Test.TestHelpers.Builders;
using NUnit.Framework;
using System.Linq;
using Atlas.MultipleAlleleCodeDictionary.Services;
using FluentAssertions;

namespace Atlas.MultipleAlleleCodeDictionary.Test.UnitTests
{
    [TestFixture]
    public class MacExpanderTests
    {
        private IMacExpander macExpander;

        [SetUp]
        public void SetUp()
        {
            macExpander = new MacExpander();
        }
        
        [Test]
        public void ExpandMac_ForASpecificMacWithASingleAllele_ExpandsToSingleAllele()
        {
            var singleSpecificHla = "01:02";
            var singleSpecificMac = MacBuilder.New.With(m => m.Hla, singleSpecificHla).Build();

            var result = macExpander.ExpandMac(singleSpecificMac);

            var singleResult = result.Single();
            singleResult.Should().Be("01:02");
        }

        [Test]
        public void ExpandMac_ForASpecificMacWithMultipleAlleles_ExpandsToMultipleCorrectAlleles()
        {
            var multipleSpecificHla = "01:02/01:03/02:03";
            var multipleSpecificMac = MacBuilder.New.With(m => m.Hla, multipleSpecificHla).Build();

            var result = macExpander.ExpandMac(multipleSpecificMac);

            var molecularAlleleDetails = result.ToArray();
            molecularAlleleDetails.Should().Contain("01:02");
            molecularAlleleDetails.Should().Contain("01:03");
            molecularAlleleDetails.Should().Contain("02:03");
        }

        [Test]
        public void ExpandMac_ForAGenericMac_ExpandsToCorrectAlleles()
        {
            var genericHla = "01/02/03";
            var firstField = "10";
            var genericMac = MacBuilder.New.With(m => m.Hla, genericHla).With(m => m.IsGeneric, true);

            var result = macExpander.ExpandMac(genericMac, firstField);
            
            var molecularAlleleDetails = result.ToArray();
            molecularAlleleDetails.Should().Contain("10:01");
            molecularAlleleDetails.Should().Contain("10:02");
            molecularAlleleDetails.Should().Contain("10:03");
        }
    }
}