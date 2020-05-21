using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.Models.Lookups.MatchingLookup;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.HlaMetadataDictionary.Models.Lookups
{
    [TestFixture]
    public class HlaMatchingLookupResultTest
    {
        [TestCase(Locus.A)]
        [TestCase(Locus.B)]
        [TestCase(Locus.C)]
        [TestCase(Locus.Dqb1)]
        [TestCase(Locus.Drb1)]
        public void HlaMatchingLookupResult_WhenTypingIsMolecularAndHasNoMatchingPGroups_IsNullExpressingTypingIsTrue(
            Locus locus)
        {
            const string lookupName = "lookup-name";
            const TypingMethod typingMethod = TypingMethod.Molecular;
            var pGroups = new string[] { };

            var matchingLookupResult = new HlaMatchingLookupResult(
                locus,
                lookupName,
                typingMethod,
                pGroups);

            matchingLookupResult.IsNullExpressingTyping.Should().BeTrue();
        }

        [TestCase(Locus.A)]
        [TestCase(Locus.B)]
        [TestCase(Locus.C)]
        [TestCase(Locus.Dqb1)]
        [TestCase(Locus.Drb1)]
        public void HlaMatchingLookupResult_WhenTypingIsMolecularAndHasAtLeastOneMatchingPGroup_IsNullExpressingTypingIsFalse(
            Locus locus)
        {
            const string lookupName = "lookup-name";
            const TypingMethod typingMethod = TypingMethod.Molecular;
            var pGroups = new[] { "p-group" };

            var matchingLookupResult = new HlaMatchingLookupResult(
                locus,
                lookupName,
                typingMethod,
                pGroups);

            matchingLookupResult.IsNullExpressingTyping.Should().BeFalse();
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
        public void HlaMatchingLookupResult_WhenTypingIsSerology_IsNullExpressingTypingIsFalse(
            Locus locus,
            string[] pGroups)
        {
            const string lookupName = "lookup-name";
            const TypingMethod typingMethod = TypingMethod.Serology;

            var matchingLookupResult = new HlaMatchingLookupResult(
                locus,
                lookupName,
                typingMethod,
                pGroups);

            matchingLookupResult.IsNullExpressingTyping.Should().BeFalse();
        }
    }
}
