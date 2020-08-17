using Atlas.Common.Helpers;
using FluentAssertions;
using NUnit.Framework;
using System.Collections.Generic;

namespace Atlas.Common.Test.Helpers
{
    public class AlleleSplitterTests
    {
        [TestCase("Field", 1)]
        [TestCase("Field:Field", 2)]
        [TestCase("Field:Field:Field", 3)]
        [TestCase("Field:Field:Field:Field", 4)]
        public void NumberOfFields_ReturnsExpectedNumberOfFields(string allele, int expectedNumberOfFields)
        {
            var actualNumberOfFields = AlleleSplitter.NumberOfFields(allele);
            actualNumberOfFields.Should().Be(expectedNumberOfFields);
        }

        [Test]
        public void FirstThreeFields_ReturnExpectedThreeFields()
        {
            const string allele = "Field1:Field2:Field3:Field4";
            var expectedListOfAlleles = new List<string> { "Field1", "Field2", "Field3" };

            var actualListOfAlleles = AlleleSplitter.FirstThreeFields(allele);

            actualListOfAlleles.Should().BeEquivalentTo(expectedListOfAlleles);
        }

        [Test]
        public void FirstThreeFieldsAsString_ReturnExpectedThreeFields()
        {
            const string allele = "Field1:Field2:Field3:Field4";
            const string expectedAllele = "Field1:Field2:Field3";

            var actualAllele = AlleleSplitter.FirstThreeFieldsAsString(allele);

            actualAllele.Should().Be(expectedAllele);
        }

        [Test]
        public void FirstTwoFields_ReturnExpectedTwoFields()
        {
            const string allele = "Field1:Field2:Field3:Field4";
            var expectedListOfAlleles = new List<string> { "Field1", "Field2" };

            var actualListOfAlleles = AlleleSplitter.FirstTwoFields(allele);

            actualListOfAlleles.Should().BeEquivalentTo(expectedListOfAlleles);
        }

        [Test]
        public void FirstTwoFieldsAsString_ReturnExpectedTwoFields()
        {
            const string allele = "Field1:Field2:Field3:Field4";
            const string expectedAllele = "Field1:Field2";

            var actualAllele = AlleleSplitter.FirstTwoFieldsAsString(allele);

            actualAllele.Should().Be(expectedAllele);
        }

        [TestCase("Field:Field", "Field")]
        [TestCase("Field:Field:Field", "Field:Field")]
        [TestCase("Field:Field:Field:Field", "Field:Field:Field")]
        public void RemoveLastField_ReturnsExpectedAllele(string allele, string expectedAllele)
        {
            var actualAllele = AlleleSplitter.RemoveLastField(allele);
            actualAllele.Should().Be(expectedAllele);
        }

        [TestCase("Field1")]
        [TestCase("Field1:Field2")]
        [TestCase("Field1:Field2:Field3")]
        [TestCase("Field1:Field2:Field3:Field4")]
        public void FirstField_ReturnsFirstField(string allele)
        {
            const string expectedAllele = "Field1";
            var actualAllele = AlleleSplitter.FirstField(allele);
            actualAllele.Should().Be(expectedAllele);
        }

        [TestCase("Field1:Field2")]
        [TestCase("Field1:Field2:Field3")]
        [TestCase("Field1:Field2:Field3:Field4")]
        public void SecondField_ReturnsSecondField(string allele)
        {
            const string expectedAllele = "Field2";
            var actualAllele = AlleleSplitter.SecondField(allele);
            actualAllele.Should().Be(expectedAllele);
        }
    }
}
