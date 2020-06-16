using Atlas.MultipleAlleleCodeDictionary.Test.TestHelpers.Builders;
using Atlas.MultipleAlleleCodeDictionary.utils;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Models;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;
using Atlas.MultipleAlleleCodeDictionary.MacImportServices;
using Atlas.MultipleAlleleCodeDictionary.MacImportServices.SourceData;
using Atlas.MultipleAlleleCodeDictionary.Test.TestHelpers.Builders;
using Atlas.MultipleAlleleCodeDictionary.utils;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData.Hla.Models.MolecularHlaTyping;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

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
        public void MacExpander_WillExpandSingleSpecificMac()
        {
            var singleSpecificHla = "01:02";
            var singleSpecificMac = MacBuilder.New.With(m => m.Hla, singleSpecificHla).Build();

            var result = macExpander.ExpandMac(singleSpecificMac);

            var singleResult = result.Single();
            singleResult.FamilyField.Should().Be("01");
            singleResult.SubtypeField.Should().Be("02");
        }

        [Test]
        public void MacExpander_willExpandMultipleSpecificMac()
        {
            var multipleSpecificHla = "01:02/01:03/02:03";
            var multipleSpecificMac = MacBuilder.New.With(m => m.Hla, multipleSpecificHla).Build();

            var result = macExpander.ExpandMac(multipleSpecificMac);

            var molecularAlleleDetails = result.ToArray();
            molecularAlleleDetails.Should().Contain(x => x.FamilyField == "01" && x.SubtypeField == "02");
            molecularAlleleDetails.Should().Contain(x => x.FamilyField == "01" && x.SubtypeField == "03");
            molecularAlleleDetails.Should().Contain(x => x.FamilyField == "02" && x.SubtypeField == "03");
        }

        [Test]
        public void MacExpander_Will_ExpandGenericMacs()
        {
            var genericHla = "01/02/03";
            var firstField = "10";
            var genericMac = MacBuilder.New.With(m => m.Hla, genericHla).With(m => m.IsGeneric, true);

            var result = macExpander.ExpandMac(genericMac, firstField);
            
            var molecularAlleleDetails = result.ToArray();
            molecularAlleleDetails.Should().Contain(x => x.FamilyField == "10" && x.SubtypeField == "01");
            molecularAlleleDetails.Should().Contain(x => x.FamilyField == "10" && x.SubtypeField == "02");
            molecularAlleleDetails.Should().Contain(x => x.FamilyField == "10" && x.SubtypeField == "03");
        }
    }
}