using System.Collections.Generic;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.GeneticData.Hla.Services.AlleleNameUtils;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.Common.Test.Hla.Services
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

        [TestCase("Field1:Field2:Field3:Field4N", new[]{"Field1","Field2"})]
        [TestCase("Field1:Field2:Field3N", new[]{"Field1","Field2"})]
        [TestCase("Field1:Field2P", new[]{"Field1","Field2P"})]
        [TestCase("Field1:Field2N", new[]{"Field1","Field2N"})]
        [TestCase("Field1:Field2", new[]{"Field1","Field2"})]
        public void FirstTwoFields_ReturnExpectedTwoFields(string hla, string[] expected)
        {
            var actual = AlleleSplitter.FirstTwoFields(hla);

            actual.Should().BeEquivalentTo(expected);
        }

        [TestCase("Field1:Field2:Field3:Field4N", "Field1:Field2")]
        [TestCase("Field1:Field2:Field3N", "Field1:Field2")]
        [TestCase("Field1:Field2P", "Field1:Field2P")]
        [TestCase("Field1:Field2N", "Field1:Field2N")]
        [TestCase("Field1:Field2", "Field1:Field2")]
        public void FirstTwoFieldsAsString_ReturnExpectedTwoFields(string hla, string expected)
        {
            var actual = AlleleSplitter.FirstTwoFieldsAsString(hla);

            actual.Should().Be(expected);
        }

        [TestCase("Field1:Field2:Field3:Field4N", "Field1:Field2N")]
        [TestCase("Field1:Field2:Field3N", "Field1:Field2N")]
        [TestCase("Field1:Field2N", "Field1:Field2N")]
        [TestCase("Field1:Field2", "Field1:Field2")]
        public void FirstTwoFieldsWithExpressionSuffixAsString_ReturnsFirstTwoFieldsWithExpressionSuffix(string allele, string expectedAllele)
        {
            var actualAllele = AlleleSplitter.FirstTwoFieldsWithExpressionSuffixAsString(allele);

            actualAllele.Should().Be(expectedAllele);
        }

        [TestCase("Field1:Field2:Field3:Field4N", "Field1:Field2")]
        [TestCase("Field1:Field2:Field3N", "Field1:Field2")]
        [TestCase("Field1:Field2g", "Field1:Field2")]
        [TestCase("Field1:Field2N", "Field1:Field2")]
        [TestCase("Field1:Field2P", "Field1:Field2")]
        [TestCase("Field1:Field2", "Field1:Field2")]
        public void FirstTwoFieldsAsStringWithSuffixRemoved_ReturnsFirstTwoFieldsWithNoSuffix(string allele, string expected)
        {
            var actual = AlleleSplitter.FirstTwoFieldsAsStringWithSuffixRemoved(allele);

            actual.Should().Be(expected);
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

        [TestCase("Field1:Field2", "Field2")]
        [TestCase("Field1:Field2g", "Field2g")]
        [TestCase("Field1:Field2N", "Field2N")]
        [TestCase("Field1:Field2:Field3", "Field2")]
        [TestCase("Field1:Field2:Field3:Field4", "Field2")]
        public void SecondField_ReturnsSecondField(string hla, string expected)
        {
            var actual = AlleleSplitter.SecondField(hla);
            actual.Should().Be(expected);
        }

        [TestCase("Field1:Field2", "Field2")]
        [TestCase("Field1:Field2g", "Field2")]
        [TestCase("Field1:Field2N", "Field2")]
        [TestCase("Field1:Field2:Field3", "Field2")]
        [TestCase("Field1:Field2:Field3:Field4", "Field2")]
        public void SecondFieldWithSuffixRemoved_ReturnsSecondFieldWithSuffixRemoved(string hla, string expected)
        {
            var actual = AlleleSplitter.SecondFieldWithSuffixRemoved(hla);
            actual.Should().Be(expected);
        }
    }
}
