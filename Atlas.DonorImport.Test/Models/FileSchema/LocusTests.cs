using System;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.DonorImport.Models.FileSchema;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

#pragma warning disable 618

namespace Atlas.DonorImport.Test.Models.FileSchema
{
    [TestFixture]
    internal class LocusTests
    {
        private const string DefaultMolecularHlaValue = "*hla-molecular";
        private const string DefaultSerologyHlaValue = "hla-serology";
        private ILogger logger = Substitute.For<ILogger>();
        private IHlaCategorisationService permissiveCategoriser;
        private IHlaCategorisationService dismissiveCategoriser;

        [OneTimeSetUp]
        public void SetUp()
        {
            permissiveCategoriser = Substitute.For<IHlaCategorisationService>();
            permissiveCategoriser.IsRecognisableHla(default).ReturnsForAnyArgs(true);
            dismissiveCategoriser = Substitute.For<IHlaCategorisationService>();
            dismissiveCategoriser.IsRecognisableHla(default).ReturnsForAnyArgs(false);
        }

        [Test]
        public void Fields1And2_WhenOnlyMolecularTypingPresent_ReturnsMolecularFields()
        {
            PerformLocusReadingTest(true, DefaultMolecularHlaValue, DefaultMolecularHlaValue, false, DefaultMolecularHlaValue, DefaultMolecularHlaValue);
        }

        [Test]
        public void Fields1And2_WhenOnlySerologyTypingPresent_ReturnsSerologyFields()
        {
            PerformLocusReadingTest(false, true, DefaultSerologyHlaValue, DefaultSerologyHlaValue, DefaultSerologyHlaValue, DefaultSerologyHlaValue);
        }

        [TestCase(DefaultMolecularHlaValue, null, DefaultMolecularHlaValue)]
        [TestCase(DefaultMolecularHlaValue, "", DefaultMolecularHlaValue)]
        [TestCase(null, DefaultSerologyHlaValue, DefaultSerologyHlaValue)]
        [TestCase("", DefaultSerologyHlaValue, DefaultSerologyHlaValue)]
        [TestCase(DefaultMolecularHlaValue, DefaultSerologyHlaValue, DefaultMolecularHlaValue)]
        [TestCase(null, null, null)]
        [TestCase("", "", null)]
        public void Field1And2_WhenMolecularAndSerologyTypingPresentAndMatch_ReturnsCorrectField(
            string molecularTyping,
            string serologyTyping,
            string expectedField)
        {
            PerformLocusReadingTest(true, molecularTyping, molecularTyping, true, serologyTyping, serologyTyping, expectedField, expectedField);
        }

        [Test]
        public void Field1And2_WhenNoTypingPresent_ReturnsNull()
        {
            PerformLocusReadingTest(false, false,  null, null);
        }

        #region Overloads
        private void PerformLocusReadingTest(bool molecularIsDefined, bool serologyIsDefined, string expectedField1, string expectedField2)
        {
            if (molecularIsDefined)
            {
                throw new InvalidOperationException("The Test declared Molecular data was defined by didn't actually define it.");
            }

            if (serologyIsDefined)
            {
                throw new InvalidOperationException("The Test declared Serology data was defined by didn't actually define it.");
            }

            PerformLocusReadingTest(false, null, null, false, null, null, expectedField1, expectedField2);
        }

        private void PerformLocusReadingTest(bool molecularIsDefined, bool serologyIsDefined, string serologyField1, string serologyField2, string expectedField1, string expectedField2)
        {
            if (molecularIsDefined)
            {
                throw new InvalidOperationException("The Test declared Molecular data was defined by didn't provide it.");
            }

            PerformLocusReadingTest(false, null, null, serologyIsDefined, serologyField1, serologyField2, expectedField1, expectedField2);
        }

        private void PerformLocusReadingTest(bool molecularIsDefined, string molecularField1, string molecularField2, bool serologyIsDefined, string expectedField1, string expectedField2)
        {
            if (serologyIsDefined)
            {
                throw new InvalidOperationException("The Test declared Serology data was defined by didn't actually define it.");
            }

            PerformLocusReadingTest(molecularIsDefined, molecularField1, molecularField2, false, null, null, expectedField1, expectedField2);
        }
        #endregion

        private void PerformLocusReadingTest(bool molecularIsDefined, string molecularField1, string molecularField2, bool serologyIsDefined, string serologyField1, string serologyField2, string expectedField1, string expectedField2, IHlaCategorisationService categoriser = null)
        {
            var locus = new Locus();
            if (molecularIsDefined)
            {
                locus.Dna = new DnaLocus { Field1 = molecularField1, Field2 = molecularField2 };
            }
            if (serologyIsDefined)
            {
                locus.Serology = new SerologyLocus { Field1 = serologyField1, Field2 = serologyField2 };
            }

            var categoriserToUse = categoriser ?? permissiveCategoriser;
            locus.ReadField1(categoriserToUse, logger).Should().Be(expectedField1);
            locus.ReadField2(categoriserToUse, logger).Should().Be(expectedField2);
        }
    }
}