using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.Services.HlaValidation;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.Tests
{
    internal class HlaValidationTests
    {
        private IHlaMetadataDictionary hlaMetadataDictionary;

        [SetUp]
        public void SetUp()
        {
            var hmdFactory = DependencyInjection.DependencyInjection.Provider.GetService<IHlaMetadataDictionaryFactory>();
            hlaMetadataDictionary = hmdFactory.BuildDictionary(FileBackedHlaMetadataRepositoryBaseReader.NewerTestsHlaVersion);
        }

        [TestCase("01:01:01G", Locus.A, true)]       // G group with "G" character
        [TestCase("01:01:02", Locus.A, true)]        // implicit G group from un-grouped single allele
        [TestCase("01:18N", Locus.A, true)]          // null allele 
        [TestCase("07:37:01G", Locus.B, true)]       // different locus 
        [TestCase("01:01g", Locus.A, false)]         // small g group
        [TestCase("01:01P", Locus.A, false)]         // P group
        [TestCase("not-hla", Locus.A, false)]        // invalid hla full stop 
        [TestCase("01:XX", Locus.A, false)]          // non-grouped typing 
        [TestCase("01:01", Locus.A, false)]          // allele that should be in the "01:01:01G" group 
        [TestCase("01:01:01:01", Locus.A, false)]    // mult-field-allele that should be in the "01:01:01G" group 
        public async Task ValidateHla_ReturnsTrueOnlyForValidGGroups(string hlaName, Locus locus, bool expectedValidity)
        {
            var validity = await hlaMetadataDictionary.ValidateHla(locus, hlaName, HlaValidationCategory.GGroup);

            validity.Should().Be(expectedValidity);
        }

        [TestCase("01:01g", Locus.A, true)]          // small g group with "g" character
        [TestCase("01:02", Locus.A, true)]           // implicit small g group from un-grouped single allele
        [TestCase("01:123N", Locus.A, true)]         // null allele 
        [TestCase("15:10g", Locus.B, true)]          // different locus 
        [TestCase("01:01:01G", Locus.A, false)]      // big G group
        [TestCase("01:01P", Locus.A, false)]         // P group
        [TestCase("not-hla", Locus.A, false)]        // invalid hla full stop 
        [TestCase("01:XX", Locus.A, false)]          // non-grouped typing 
        [TestCase("01:01", Locus.A, false)]          // allele that should be in the "01:01g" group 
        [TestCase("01:01:01:01", Locus.A, false)]    // mult-field-allele that should be in the "01:01g" group 
        public async Task ValidateHla_ReturnsTrueOnlyForValidSmallGGroups(string hlaName, Locus locus, bool expectedValidity)
        {
            var validity = await hlaMetadataDictionary.ValidateHla(locus, hlaName, HlaValidationCategory.SmallGGroup);

            validity.Should().Be(expectedValidity);
        }
    }
}