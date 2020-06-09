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

        [Test]
        // Four Values: Dna 1, Dna 2, Serology 1, Serology 2 
        // Each value can be in 4 states: present, empty, null, or parent-property-undefined (PPU)
        // That would give Dna 16 states (4 * 4) ... but if Dna 1 is PPU, then so is Dna 2, ruling out 6 states.
        // So Dna combined has 10 states.
        // Serology also has 10 states.
        // Dna and Serology are independent, so we have 100 states to test the interactions for ...
        //Serology Is fully Specced
        [TestCase(true, "*1", "*2",  true, "3", "4",    "*1", "*2")]
        [TestCase(true, "*1", "",    true, "3", "4",    "*1", "*1")]
        [TestCase(true, "*1", null,  true, "3", "4",    "*1", "*1")]
        [TestCase(true, "", "*2",    true, "3", "4",    null, "*2")] // These ones are a particularly notable edge case!
        [TestCase(true, null, "*2",  true, "3", "4",    null, "*2")] // These ones are a particularly notable edge case!
        [TestCase(true, "", "",      true, "3", "4",    "3", "4")]
        [TestCase(true, null, "",    true, "3", "4",    "3", "4")]
        [TestCase(true, "", null,    true, "3", "4",    "3", "4")]
        [TestCase(true, null, null,  true, "3", "4",    "3", "4")]
        [TestCase(false, null, null, true, "3", "4",    "3", "4")]
        //Serology Is Partial (2="")
        [TestCase(true, "*1", "*2",  true, "3", "",     "*1", "*2")]
        [TestCase(true, "*1", "",    true, "3", "",     "*1", "*1")]
        [TestCase(true, "*1", null,  true, "3", "",     "*1", "*1")]
        [TestCase(true, "", "*2",    true, "3", "",     null, "*2")]
        [TestCase(true, null, "*2",  true, "3", "",     null, "*2")]
        [TestCase(true, "", "",      true, "3", "",     "3", "3")]
        [TestCase(true, null, "",    true, "3", "",     "3", "3")]
        [TestCase(true, "", null,    true, "3", "",     "3", "3")]
        [TestCase(true, null, null,  true, "3", "",     "3", "3")]
        [TestCase(false, null, null, true, "3", "",     "3", "3")]
        //Serology Is Partial (2=null)
        [TestCase(true, "*1", "*2",  true, "3", null,    "*1", "*2")]
        [TestCase(true, "*1", "",    true, "3", null,    "*1", "*1")]
        [TestCase(true, "*1", null,  true, "3", null,    "*1", "*1")]
        [TestCase(true, "", "*2",    true, "3", null,    null, "*2")]
        [TestCase(true, null, "*2",  true, "3", null,    null, "*2")]
        [TestCase(true, "", "",      true, "3", null,    "3", "3")]
        [TestCase(true, null, "",    true, "3", null,    "3", "3")]
        [TestCase(true, "", null,    true, "3", null,    "3", "3")]
        [TestCase(true, null, null,  true, "3", null,    "3", "3")]
        [TestCase(false, null, null, true, "3", null,    "3", "3")]
        //Serology Is Partial (1="")
        [TestCase(true, "*1", "*2",  true, "", "4",      "*1", "*2")]
        [TestCase(true, "*1", "",    true, "", "4",      "*1", "*1")]
        [TestCase(true, "*1", null,  true, "", "4",      "*1", "*1")]
        [TestCase(true, "", "*2",    true, "", "4",      null, "*2")]
        [TestCase(true, null, "*2",  true, "", "4",      null, "*2")]
        [TestCase(true, "", "",      true, "", "4",      null, "4")]
        [TestCase(true, null, "",    true, "", "4",      null, "4")]
        [TestCase(true, "", null,    true, "", "4",      null, "4")]
        [TestCase(true, null, null,  true, "", "4",      null, "4")]
        [TestCase(false, null, null, true, "", "4",      null, "4")]
        //Serology Is Partial (1=null)
        [TestCase(true, "*1", "*2",  true, null, "4",    "*1", "*2")]
        [TestCase(true, "*1", "",    true, null, "4",    "*1", "*1")]
        [TestCase(true, "*1", null,  true, null, "4",    "*1", "*1")]
        [TestCase(true, "", "*2",    true, null, "4",    null, "*2")]
        [TestCase(true, null, "*2",  true, null, "4",    null, "*2")]
        [TestCase(true, "", "",      true, null, "4",    null, "4")]
        [TestCase(true, null, "",    true, null, "4",    null, "4")]
        [TestCase(true, "", null,    true, null, "4",    null, "4")]
        [TestCase(true, null, null,  true, null, "4",    null, "4")]
        [TestCase(false, null, null, true, null, "4",    null, "4")]
        //Serology Is unspecified (both="")
        [TestCase(true, "*1", "*2",  true, "", "",       "*1", "*2")]
        [TestCase(true, "*1", "",    true, "", "",       "*1", "*1")]
        [TestCase(true, "*1", null,  true, "", "",       "*1", "*1")]
        [TestCase(true, "", "*2",    true, "", "",       null, "*2")]
        [TestCase(true, null, "*2",  true, "", "",       null, "*2")]
        [TestCase(true, "", "",      true, "", "",       null, null)]
        [TestCase(true, null, "",    true, "", "",       null, null)]
        [TestCase(true, "", null,    true, "", "",       null, null)]
        [TestCase(true, null, null,  true, "", "",       null, null)]
        [TestCase(false, null, null, true, "", "",       null, null)]
        //Serology Is unspecified (both=null)
        [TestCase(true, "*1", "*2",  true, null, null,   "*1", "*2")]
        [TestCase(true, "*1", "",    true, null, null,   "*1", "*1")]
        [TestCase(true, "*1", null,  true, null, null,   "*1", "*1")]
        [TestCase(true, "", "*2",    true, null, null,   null, "*2")]
        [TestCase(true, null, "*2",  true, null, null,   null, "*2")]
        [TestCase(true, "", "",      true, null, null,   null, null)]
        [TestCase(true, null, "",    true, null, null,   null, null)]
        [TestCase(true, "", null,    true, null, null,   null, null)]
        [TestCase(true, null, null,  true, null, null,   null, null)]
        [TestCase(false, null, null, true, null, null,   null, null)]
        //Serology Is unspecified ("", null)
        [TestCase(true, "*1", "*2",  true, "", null,     "*1", "*2")]
        [TestCase(true, "*1", "",    true, "", null,     "*1", "*1")]
        [TestCase(true, "*1", null,  true, "", null,     "*1", "*1")]
        [TestCase(true, "", "*2",    true, "", null,     null, "*2")]
        [TestCase(true, null, "*2",  true, "", null,     null, "*2")]
        [TestCase(true, "", "",      true, "", null,     null, null)]
        [TestCase(true, null, "",    true, "", null,     null, null)]
        [TestCase(true, "", null,    true, "", null,     null, null)]
        [TestCase(true, null, null,  true, "", null,     null, null)]
        [TestCase(false, null, null, true, "", null,     null, null)]
        //Serology Is unspecified (null, "")
        [TestCase(true, "*1", "*2",  true, null, "",     "*1", "*2")]
        [TestCase(true, "*1", "",    true, null, "",     "*1", "*1")]
        [TestCase(true, "*1", null,  true, null, "",     "*1", "*1")]
        [TestCase(true, "", "*2",    true, null, "",     null, "*2")]
        [TestCase(true, null, "*2",  true, null, "",     null, "*2")]
        [TestCase(true, "", "",      true, null, "",     null, null)]
        [TestCase(true, null, "",    true, null, "",     null, null)]
        [TestCase(true, "", null,    true, null, "",     null, null)]
        [TestCase(true, null, null,  true, null, "",     null, null)]
        [TestCase(false, null, null, true, null, "",     null, null)]
        //Serology Is absent (not set)
        [TestCase(true, "*1", "*2",  false, null, null,  "*1", "*2")]
        [TestCase(true, "*1", "",    false, null, null,  "*1", "*1")]
        [TestCase(true, "*1", null,  false, null, null,  "*1", "*1")]
        [TestCase(true, "", "*2",    false, null, null,  null, "*2")]
        [TestCase(true, null, "*2",  false, null, null,  null, "*2")]
        [TestCase(true, "", "",      false, null, null,  null, null)]
        [TestCase(true, null, "",    false, null, null,  null, null)]
        [TestCase(true, "", null,    false, null, null,  null, null)]
        [TestCase(true, null, null,  false, null, null,  null, null)]
        [TestCase(false, null, null, false, null, null,  null, null)]
        public void HlaLocusData_GivenThatStringsPresentAreValidHlas_DefaultingBetweenFieldsAndSerologiesAreCorrect(bool molecularIsDefined, string molecularField1, string molecularField2, bool serologyIsDefined, string serologyField1, string serologyField2, string expectedField1, string expectedField2)
        {
            PerformLocusReadingTest(molecularIsDefined, molecularField1, molecularField2, serologyIsDefined, serologyField1, serologyField2, expectedField1, expectedField2, permissiveCategoriser);
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