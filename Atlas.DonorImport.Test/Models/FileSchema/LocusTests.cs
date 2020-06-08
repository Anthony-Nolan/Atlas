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
        private IHlaCategorisationService permissiveCategoriser = Substitute.For<IHlaCategorisationService>();
        private IHlaCategorisationService dismissiveCategoriser = Substitute.For<IHlaCategorisationService>();

        [OneTimeSetUp]
        public void SetUp()
        {
            permissiveCategoriser = Substitute.For<IHlaCategorisationService>();
            permissiveCategoriser.IsRecognisableHla(default).ReturnsForAnyArgs(true);
            dismissiveCategoriser = Substitute.For<IHlaCategorisationService>();
            dismissiveCategoriser.IsRecognisableHla(default).ReturnsForAnyArgs(false);
        }

        [Test]
        public void Field1_WhenOnlyMolecularTypingPresent_ReturnsMolecularField1()
        {
            var locus = new Locus {Dna = new DnaLocus {Field1 = DefaultMolecularHlaValue}};

            locus.ReadField1(permissiveCategoriser, logger).Should().Be(DefaultMolecularHlaValue);
        }

        [Test]
        public void Field2_WhenOnlyMolecularTypingPresent_ReturnsMolecularField2()
        {
            var locus = new Locus {Dna = new DnaLocus {Field2 = DefaultMolecularHlaValue}};

            locus.ReadField2(permissiveCategoriser, logger).Should().Be(DefaultMolecularHlaValue);
        }

        [Test]
        public void Field1_WhenOnlySerologyTypingPresent_ReturnsSerologyField1()
        {
            var locus = new Locus {Serology = new SerologyLocus() {Field1 = DefaultSerologyHlaValue}};

            locus.ReadField1(permissiveCategoriser, logger).Should().Be(DefaultSerologyHlaValue);
        }

        [Test]
        public void Field2_WhenOnlySerologyTypingPresent_ReturnsSerologyField2()
        {
            var locus = new Locus {Serology = new SerologyLocus() {Field2 = DefaultSerologyHlaValue}};

            locus.ReadField2(permissiveCategoriser, logger).Should().Be(DefaultSerologyHlaValue);
        }

        [TestCase(DefaultMolecularHlaValue, null, DefaultMolecularHlaValue)]
        [TestCase(DefaultMolecularHlaValue, "", DefaultMolecularHlaValue)]
        [TestCase(null, DefaultSerologyHlaValue, DefaultSerologyHlaValue)]
        [TestCase("", DefaultSerologyHlaValue, DefaultSerologyHlaValue)]
        [TestCase(DefaultMolecularHlaValue, DefaultSerologyHlaValue, DefaultMolecularHlaValue)]
        [TestCase(null, null, null)]
        [TestCase("", "", null)]
        public void Field1_WhenMolecularAndSerologyTypingPresent_ReturnsCorrectField(
            string molecularTyping,
            string serologyTyping,
            string expectedField)
        {
            var locus = new Locus
            {
                Dna = new DnaLocus() {Field1 = molecularTyping},
                Serology = new SerologyLocus() {Field1 = serologyTyping}
            };

            locus.ReadField1(permissiveCategoriser, logger).Should().Be(expectedField);
        }
        
        [TestCase(DefaultMolecularHlaValue, null, DefaultMolecularHlaValue)]
        [TestCase(DefaultMolecularHlaValue, "", DefaultMolecularHlaValue)]
        [TestCase(null, DefaultSerologyHlaValue, DefaultSerologyHlaValue)]
        [TestCase("", DefaultSerologyHlaValue, DefaultSerologyHlaValue)]
        [TestCase(DefaultMolecularHlaValue, DefaultSerologyHlaValue, DefaultMolecularHlaValue)]
        [TestCase(null, null, null)]
        [TestCase("", "", null)]
        public void Field2_WhenMolecularAndSerologyTypingPresent_ReturnsCorrectField(
            string molecularTyping,
            string serologyTyping,
            string expectedField)
        {
            var locus = new Locus
            {
                Dna = new DnaLocus() {Field2 = molecularTyping},
                Serology = new SerologyLocus() {Field2 = serologyTyping}
            };

            locus.ReadField2(permissiveCategoriser, logger).Should().Be(expectedField);
        }

        [Test]
        public void Field1_WhenNoTypingPresent_ReturnsNull()
        {
            var locus = new Locus();

            locus.ReadField1(permissiveCategoriser, logger).Should().BeNull();
        }

        [Test]
        public void Field2_WhenNoTypingPresent_ReturnsNull()
        {
            var locus = new Locus();

            locus.ReadField2(permissiveCategoriser, logger).Should().BeNull();
        }
    }
}