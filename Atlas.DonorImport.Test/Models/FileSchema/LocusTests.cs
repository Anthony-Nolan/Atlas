using Atlas.DonorImport.Models.FileSchema;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Models.FileSchema
{
    [TestFixture]
    public class LocusTests
    {
        private const string DefaultMolecularHlaValue = "hla-molecular";
        private const string DefaultSerologyHlaValue = "hla-serology";

        [Test]
        public void Field1_WhenOnlyMolecularTypingPresent_ReturnsMolecularField1()
        {
            var locus = new Locus {Dna = new DnaLocus {Field1 = DefaultMolecularHlaValue}};

            locus.Field1.Should().Be(DefaultMolecularHlaValue);
        }
        
        [Test]
        public void Field2_WhenOnlyMolecularTypingPresent_ReturnsMolecularField2()
        {
            var locus = new Locus {Dna = new DnaLocus {Field2 = DefaultMolecularHlaValue}};

            locus.Field2.Should().Be(DefaultMolecularHlaValue);
        }
        
        [Test]
        public void Field1_WhenOnlySerologyTypingPresent_ReturnsSerologyField1()
        {
            var locus = new Locus {Serology = new SerologyLocus() {Field1 = DefaultSerologyHlaValue}};

            locus.Field1.Should().Be(DefaultSerologyHlaValue);
        }
        
        [Test]
        public void Field2_WhenOnlySerologyTypingPresent_ReturnsSerologyField2()
        {
            var locus = new Locus {Serology = new SerologyLocus() {Field2 = DefaultSerologyHlaValue}};

            locus.Field2.Should().Be(DefaultSerologyHlaValue);
        }
        
        [Test]
        public void Field1_WhenMolecularAndSerologyTypingPresent_ReturnsMolecularField1()
        {
            var locus = new Locus
            {
                Dna = new DnaLocus() { Field1 = DefaultMolecularHlaValue},
                Serology = new SerologyLocus() {Field1 = DefaultSerologyHlaValue}
            };

            locus.Field1.Should().Be(DefaultMolecularHlaValue);
        }
        
        [Test]
        public void Field2_WhenMolecularAndSerologyTypingPresent_ReturnsMolecularField2()
        {
            var locus = new Locus
            {
                Dna = new DnaLocus() { Field2 = DefaultMolecularHlaValue},
                Serology = new SerologyLocus() {Field2 = DefaultSerologyHlaValue}
            };

            locus.Field2.Should().Be(DefaultMolecularHlaValue);
        }
        
        [Test]
        public void Field1_WhenNoTypingPresent_ReturnsNull()
        {
            var locus = new Locus();

            locus.Field1.Should().BeNull();
        }
        
        [Test]
        public void Field2_WhenNoTypingPresent_ReturnsNull()
        {
            var locus = new Locus();

            locus.Field2.Should().BeNull();
        }
    }
}