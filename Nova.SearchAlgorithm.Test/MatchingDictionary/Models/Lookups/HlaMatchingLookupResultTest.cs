using FluentAssertions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Models.Lookups
{
    [TestFixture]
    public class HlaMatchingLookupResultTest
    {
        [TestCase(MatchLocus.A)]
        [TestCase(MatchLocus.B)]
        [TestCase(MatchLocus.C)]
        [TestCase(MatchLocus.Dqb1)]
        [TestCase(MatchLocus.Drb1)]
        public void HlaMatchingLookupResult_WhenTypingIsMolecularAndHasNoMatchingPGroups_IsNullExpressingTypingIsTrue(
            MatchLocus matchLocus)
        {
            const string lookupName = "lookup-name";
            const TypingMethod typingMethod = TypingMethod.Molecular;
            var pGroups = new string[] { };

            var matchingLookupResult = new HlaMatchingLookupResult(
                matchLocus,
                lookupName,
                typingMethod,
                pGroups);

            matchingLookupResult.IsNullExpressingTyping.Should().BeTrue();
        }

        [TestCase(MatchLocus.A)]
        [TestCase(MatchLocus.B)]
        [TestCase(MatchLocus.C)]
        [TestCase(MatchLocus.Dqb1)]
        [TestCase(MatchLocus.Drb1)]
        public void HlaMatchingLookupResult_WhenTypingIsMolecularAndHasAtLeastOneMatchingPGroup_IsNullExpressingTypingIsFalse(
            MatchLocus matchLocus)
        {
            const string lookupName = "lookup-name";
            const TypingMethod typingMethod = TypingMethod.Molecular;
            var pGroups = new[] { "p-group" };

            var matchingLookupResult = new HlaMatchingLookupResult(
                matchLocus,
                lookupName,
                typingMethod,
                pGroups);

            matchingLookupResult.IsNullExpressingTyping.Should().BeFalse();
        }

        [TestCase(MatchLocus.A, new string[] { })]
        [TestCase(MatchLocus.B, new string[] { })]
        [TestCase(MatchLocus.C, new string[] { })]
        [TestCase(MatchLocus.Dqb1, new string[] { })]
        [TestCase(MatchLocus.Drb1, new string[] { })]
        [TestCase(MatchLocus.A, new[] { "p-group" })]
        [TestCase(MatchLocus.B, new[] { "p-group" })]
        [TestCase(MatchLocus.C, new[] { "p-group" })]
        [TestCase(MatchLocus.Dqb1, new[] { "p-group" })]
        [TestCase(MatchLocus.Drb1, new[] { "p-group" })]
        public void HlaMatchingLookupResult_WhenTypingIsSerology_IsNullExpressingTypingIsFalse(
            MatchLocus matchLocus,
            string[] pGroups)
        {
            const string lookupName = "lookup-name";
            const TypingMethod typingMethod = TypingMethod.Serology;

            var matchingLookupResult = new HlaMatchingLookupResult(
                matchLocus,
                lookupName,
                typingMethod,
                pGroups);

            matchingLookupResult.IsNullExpressingTyping.Should().BeFalse();
        }
    }
}
