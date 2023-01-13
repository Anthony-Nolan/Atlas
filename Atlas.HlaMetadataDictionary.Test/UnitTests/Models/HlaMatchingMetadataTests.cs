using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Models
{
    [TestFixture]
    public class HlaMatchingMetadataTests
    {
        [TestCase(Locus.A)]
        [TestCase(Locus.B)]
        [TestCase(Locus.C)]
        [TestCase(Locus.Dqb1)]
        [TestCase(Locus.Drb1)]
        public void HlaMatchingMetadata_WhenTypingIsMolecularAndHasNoMatchingPGroups_IsNullExpressingTypingIsTrue(
            Locus locus)
        {
            const string lookupName = "lookup-name";
            const TypingMethod typingMethod = TypingMethod.Molecular;
            var pGroups = new string[] { };

            var matchingMetadata = new HlaMatchingMetadata(
                locus,
                lookupName,
                typingMethod,
                pGroups);

            matchingMetadata.IsNullExpressingTyping.Should().BeTrue();
        }

        [TestCase(Locus.A)]
        [TestCase(Locus.B)]
        [TestCase(Locus.C)]
        [TestCase(Locus.Dqb1)]
        [TestCase(Locus.Drb1)]
        public void HlaMatchingMetadata_WhenTypingIsMolecularAndHasAtLeastOneMatchingPGroup_IsNullExpressingTypingIsFalse(
            Locus locus)
        {
            const string lookupName = "lookup-name";
            const TypingMethod typingMethod = TypingMethod.Molecular;
            var pGroups = new[] { "p-group" };

            var matchingMetadata = new HlaMatchingMetadata(
                locus,
                lookupName,
                typingMethod,
                pGroups);

            matchingMetadata.IsNullExpressingTyping.Should().BeFalse();
        }

        [TestCase(Locus.A, new string[] { })]
        [TestCase(Locus.B, new string[] { })]
        [TestCase(Locus.C, new string[] { })]
        [TestCase(Locus.Dqb1, new string[] { })]
        [TestCase(Locus.Drb1, new string[] { })]
        [TestCase(Locus.A, new[] { "p-group" })]
        [TestCase(Locus.B, new[] { "p-group" })]
        [TestCase(Locus.C, new[] { "p-group" })]
        [TestCase(Locus.Dqb1, new[] { "p-group" })]
        [TestCase(Locus.Drb1, new[] { "p-group" })]
        public void HlaMatchingMetadata_WhenTypingIsSerology_IsNullExpressingTypingIsFalse(
            Locus locus,
            string[] pGroups)
        {
            const string lookupName = "lookup-name";
            const TypingMethod typingMethod = TypingMethod.Serology;

            var matchingMetadata = new HlaMatchingMetadata(
                locus,
                lookupName,
                typingMethod,
                pGroups);

            matchingMetadata.IsNullExpressingTyping.Should().BeFalse();
        }
    }
}
