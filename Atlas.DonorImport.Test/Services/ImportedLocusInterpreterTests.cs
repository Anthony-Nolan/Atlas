using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.Models.Mapping;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Services
{
    /// <summary>
    /// All of these tests are considering the following object:
    /// {
    ///     Dna: {
    ///         Field1: `molecularField1`,
    ///         Field2: `molecularField2`,
    ///     },
    ///     Serology: {
    ///         Field1: `molecularField1`,
    ///         Field2: `molecularField2`,
    ///     }
    /// }
    ///
    /// Where occasionally the Dna or Serology sub-objects are entirely absent.
    /// 
    /// And then answering the question:
    ///    Given that data, what are "The Field1 Value" and "The Field2 Value"         
    /// </summary>
    [TestFixture]
    internal class ImportedLocusInterpreterTests
    {
        private const string MolecularHlaValue = "*hla-molecular";
        private const string SerologyHlaValue = "hla-serology";
        private const string NewAllele = "NEW";
        private IHlaCategorisationService permissiveCategoriser;
        private IHlaCategorisationService dismissiveCategoriser;

        [OneTimeSetUp]
        public void SetUp()
        {
            permissiveCategoriser = Substitute.For<IHlaCategorisationService>();
            permissiveCategoriser.ConformsToValidHlaFormat(default).ReturnsForAnyArgs(true);
            dismissiveCategoriser = Substitute.For<IHlaCategorisationService>();
            dismissiveCategoriser.ConformsToValidHlaFormat(default).ReturnsForAnyArgs(false);
        }

        [Test]
        public void Interpret_WhenLocusNull_ReturnsNullInfo()
        {
            var result = new ImportedLocusInterpreter(null, null).Interpret(null, default);
            result.Position1.Should().BeNull();
            result.Position2.Should().BeNull();
        }

        [Test]
        public void Interpret_WhenNoTypingPresent_ReturnsNullInfo()
        {
            LocusInterpretationTestPerformer.NewTestCase
                .WithCategoriser(permissiveCategoriser)
                .ShouldHaveHomozygousFields(null);
        }

        [Test]
        public void Interpret_WhenOnlyHomozygousMolecularTypingsArePresent_ReturnsMolecularFields()
        {
            LocusInterpretationTestPerformer.NewTestCase
                .WithCategoriser(permissiveCategoriser)
                .WithHomozygousMolecular(MolecularHlaValue)
                .ShouldHaveHomozygousFields(MolecularHlaValue);
        }

        [Test]
        public void Interpret_WhenOnlyHomozygousSerologyTypingsArePresent_ReturnsSerologyFields()
        {
            LocusInterpretationTestPerformer.NewTestCase
                .WithCategoriser(permissiveCategoriser)
                .WithHomozygousSerology(SerologyHlaValue)
                .ShouldHaveHomozygousFields(SerologyHlaValue);
        }

        [Test]
        public void Interpret_WhenNewAllelePresent_ReturnsNewAllele()
        {
            LocusInterpretationTestPerformer.NewTestCase
                .WithCategoriser(permissiveCategoriser)
                .WithNewAllele(NewAllele, MolecularHlaValue)
                .ShouldHaveFields(NewAllele, MolecularHlaValue);
        }

        [TestCase(MolecularHlaValue, null, MolecularHlaValue)]
        [TestCase(MolecularHlaValue, "", MolecularHlaValue)]
        [TestCase(null, SerologyHlaValue, SerologyHlaValue)]
        [TestCase("", SerologyHlaValue, SerologyHlaValue)]
        [TestCase(MolecularHlaValue, SerologyHlaValue, MolecularHlaValue)]
        [TestCase(null, null, null)]
        [TestCase("", "", null)]
        public void Interpret_WhenHomozygousDataFromFromAVarietyOfMolecularAndOrSerologyTypingsArePresent_ReturnsCorrectHomozygousFields(
            string molecularTyping,
            string serologyTyping,
            string expectedField)
        {
            LocusInterpretationTestPerformer.NewTestCase
                .WithCategoriser(permissiveCategoriser)
                .WithHomozygousMolecular(molecularTyping)
                .WithHomozygousSerology(serologyTyping)
                .ShouldHaveHomozygousFields(expectedField);
        }

        [Test]
        // Regarding the number of cases here:
        // We have Four Values: Dna 1, Dna 2, Serology 1, Serology 2 
        // Each value can be in 3 states, independently: present, emptyString, null
        // So we have a total of 3^4 = 81 cases.
        //Serology Is fully Specced
        [TestCase("*1", "*2",  "3", "4",    "*1", "*2")]
        [TestCase("*1", "",    "3", "4",    "*1", "*1")]
        [TestCase("*1", null,  "3", "4",    "*1", "*1")]
        [TestCase("", "*2",    "3", "4",    null, "*2")] // These ones are a particularly notable edge case!
        [TestCase(null, "*2",  "3", "4",    null, "*2")] // These ones are a particularly notable edge case!
        [TestCase("", "",      "3", "4",    "3", "4")]
        [TestCase(null, "",    "3", "4",    "3", "4")]
        [TestCase("", null,    "3", "4",    "3", "4")]
        [TestCase(null, null,  "3", "4",    "3", "4")]
        //Serology Is Partial (2="")
        [TestCase("*1", "*2",  "3", "",     "*1", "*2")]
        [TestCase("*1", "",    "3", "",     "*1", "*1")]
        [TestCase("*1", null,  "3", "",     "*1", "*1")]
        [TestCase("", "*2",    "3", "",     null, "*2")]
        [TestCase(null, "*2",  "3", "",     null, "*2")]
        [TestCase("", "",      "3", "",     "3", "3")]
        [TestCase(null, "",    "3", "",     "3", "3")]
        [TestCase("", null,    "3", "",     "3", "3")]
        [TestCase(null, null,  "3", "",     "3", "3")]
        //Serology Is Partial (2=null)
        [TestCase("*1", "*2",  "3", null,    "*1", "*2")]
        [TestCase("*1", "",    "3", null,    "*1", "*1")]
        [TestCase("*1", null,  "3", null,    "*1", "*1")]
        [TestCase("", "*2",    "3", null,    null, "*2")]
        [TestCase(null, "*2",  "3", null,    null, "*2")]
        [TestCase("", "",      "3", null,    "3", "3")]
        [TestCase(null, "",    "3", null,    "3", "3")]
        [TestCase("", null,    "3", null,    "3", "3")]
        [TestCase(null, null,  "3", null,    "3", "3")]
        //Serology Is Partial (1="")
        [TestCase("*1", "*2",  "", "4",      "*1", "*2")]
        [TestCase("*1", "",    "", "4",      "*1", "*1")]
        [TestCase("*1", null,  "", "4",      "*1", "*1")]
        [TestCase("", "*2",    "", "4",      null, "*2")]
        [TestCase(null, "*2",  "", "4",      null, "*2")]
        [TestCase("", "",      "", "4",      null, "4")]
        [TestCase(null, "",    "", "4",      null, "4")]
        [TestCase("", null,    "", "4",      null, "4")]
        [TestCase(null, null,  "", "4",      null, "4")]
        //Serology Is Partial (1=null)
        [TestCase("*1", "*2",  null, "4",    "*1", "*2")]
        [TestCase("*1", "",    null, "4",    "*1", "*1")]
        [TestCase("*1", null,  null, "4",    "*1", "*1")]
        [TestCase("", "*2",    null, "4",    null, "*2")]
        [TestCase(null, "*2",  null, "4",    null, "*2")]
        [TestCase("", "",      null, "4",    null, "4")]
        [TestCase(null, "",    null, "4",    null, "4")]
        [TestCase("", null,    null, "4",    null, "4")]
        [TestCase(null, null,  null, "4",    null, "4")]
        //Serology Is unspecified (both="")
        [TestCase("*1", "*2",  "", "",       "*1", "*2")]
        [TestCase("*1", "",    "", "",       "*1", "*1")]
        [TestCase("*1", null,  "", "",       "*1", "*1")]
        [TestCase("", "*2",    "", "",       null, "*2")]
        [TestCase(null, "*2",  "", "",       null, "*2")]
        [TestCase("", "",      "", "",       null, null)]
        [TestCase(null, "",    "", "",       null, null)]
        [TestCase("", null,    "", "",       null, null)]
        [TestCase(null, null,  "", "",       null, null)]
        //Serology Is unspecified (both=null)
        [TestCase("*1", "*2",  null, null,   "*1", "*2")]
        [TestCase("*1", "",    null, null,   "*1", "*1")]
        [TestCase("*1", null,  null, null,   "*1", "*1")]
        [TestCase("", "*2",    null, null,   null, "*2")]
        [TestCase(null, "*2",  null, null,   null, "*2")]
        [TestCase("", "",      null, null,   null, null)]
        [TestCase(null, "",    null, null,   null, null)]
        [TestCase("", null,    null, null,   null, null)]
        [TestCase(null, null,  null, null,   null, null)]
        //Serology Is unspecified ("", null)
        [TestCase("*1", "*2",  "", null,     "*1", "*2")]
        [TestCase("*1", "",    "", null,     "*1", "*1")]
        [TestCase("*1", null,  "", null,     "*1", "*1")]
        [TestCase("", "*2",    "", null,     null, "*2")]
        [TestCase(null, "*2",  "", null,     null, "*2")]
        [TestCase("", "",      "", null,     null, null)]
        [TestCase(null, "",    "", null,     null, null)]
        [TestCase("", null,    "", null,     null, null)]
        [TestCase(null, null,  "", null,     null, null)]
        //Serology Is unspecified (null, "")
        [TestCase("*1", "*2",  null, "",     "*1", "*2")]
        [TestCase("*1", "",    null, "",     "*1", "*1")]
        [TestCase("*1", null,  null, "",     "*1", "*1")]
        [TestCase("", "*2",    null, "",     null, "*2")]
        [TestCase(null, "*2",  null, "",     null, "*2")]
        [TestCase("", "",      null, "",     null, null)]
        [TestCase(null, "",    null, "",     null, null)]
        [TestCase("", null,    null, "",     null, null)]
        [TestCase(null, null,  null, "",     null, null)]
        public void Interpret_WhenMolecularAndSerologyObjectsAreDefinedButSomeDataIsMissing_DefaultingBetweenFieldsAndSerologiesIsCorrect(
            string molecularField1,
            string molecularField2,
            string serologyField1,
            string serologyField2,
            string expectedField1,
            string expectedField2)
        {
            LocusInterpretationTestPerformer.NewTestCase
                .WithCategoriser(permissiveCategoriser)
                .WithMolecular(molecularField1, molecularField2)
                .WithSerology(serologyField1, serologyField2)
                .ShouldHaveFields(expectedField1, expectedField2);
        }

        [TestCase("*1", "*2",  "*1", "*2")]
        [TestCase("*1", "",    "*1", "*1")]
        [TestCase("*1", null,  "*1", "*1")]
        [TestCase("", "*2",    null, "*2")]
        [TestCase(null, "*2",  null, "*2")]
        [TestCase("", "",      null, null)]
        [TestCase(null, "",    null, null)]
        [TestCase("", null,    null, null)]
        [TestCase(null, null,  null, null)]
        public void Interpret_WhenSerologyObjectIsAbsentButSomeMolecularDataIsMissing_DefaultingBetweenFieldsAndSerologiesIsCorrect(
            string serologyField1,
            string serologyField2,
            string expectedField1,
            string expectedField2)
        {
            LocusInterpretationTestPerformer.NewTestCase
                .WithCategoriser(permissiveCategoriser)
                .WithMolecular(serologyField1, serologyField2)
                .ShouldHaveFields(expectedField1, expectedField2);
        }

        [TestCase("3", "4",    "3", "4")]
        [TestCase("3", "",     "3", "3")]
        [TestCase("3", null,   "3", "3")]
        [TestCase("", "4",     null, "4")]
        [TestCase(null, "4",   null, "4")]
        [TestCase("", "",      null, null)]
        [TestCase(null, null,  null, null)]
        [TestCase("", null,    null, null)]
        [TestCase(null, "",    null, null)]
        public void Interpret_WhenMolecularObjectIsAbsentButSomeSerologyDataIsMissing_DefaultingBetweenFieldsAndSerologiesIsCorrect(
            string serologyField1,
            string serologyField2,
            string expectedField1,
            string expectedField2)
        {
            LocusInterpretationTestPerformer.NewTestCase
                .WithCategoriser(permissiveCategoriser)
                .WithSerology(serologyField1, serologyField2)
                .ShouldHaveFields(expectedField1, expectedField2);
        }

        private class LocusInterpretationTestPerformer
        {
            public static LocusInterpretationTestPerformer NewTestCase => new LocusInterpretationTestPerformer();
            private ImportedLocus locus = new ImportedLocus();
            private IHlaCategorisationService categoriser = null;
            private ILogger logger = Substitute.For<ILogger>();

            public LocusInterpretationTestPerformer WithCategoriser(IHlaCategorisationService categoriser)
            {
                this.categoriser = categoriser;
                return this;
            }

            public LocusInterpretationTestPerformer WithMolecular(string field1, string field2)
            {
                locus.Dna = new TwoFieldStringData { Field1 = field1, Field2 = field2 };
                return this;
            }

            public LocusInterpretationTestPerformer WithSerology(string field1, string field2)
            {
                locus.Serology = new TwoFieldStringData { Field1 = field1, Field2 = field2 };
                return this;
            }

            public LocusInterpretationTestPerformer WithNewAllele(string field1, string field2)
            {
                locus.Dna = new TwoFieldStringData { Field1 = field1, Field2 = field2 };
                return this;
            }

            public void ShouldHaveFields(string expectedField1, string expectedField2)
            {
                var interpretedLocus = new ImportedLocusInterpreter(categoriser, logger).Interpret(locus, default);
                interpretedLocus.Position1.Should().Be(expectedField1);
                interpretedLocus.Position2.Should().Be(expectedField2);
            }

            public LocusInterpretationTestPerformer WithHomozygousMolecular(string bothFields) => WithMolecular(bothFields, bothFields);
            public LocusInterpretationTestPerformer WithHomozygousSerology(string bothFields) => WithSerology(bothFields, bothFields);
            public void ShouldHaveHomozygousFields(string bothFields) => ShouldHaveFields(bothFields, bothFields);
        }
    }
}