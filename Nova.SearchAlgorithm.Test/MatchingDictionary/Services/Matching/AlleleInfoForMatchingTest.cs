using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.Matching
{
    [TestFixtureSource(typeof(MatchedHlaTestFixtureArgs), nameof(MatchedHlaTestFixtureArgs.MatchedAlleles))]
    public class AlleleInfoForMatchingTest : MatchedOnTestBase<IAlleleInfoForMatching>
    {
        public AlleleInfoForMatchingTest(IEnumerable<IAlleleInfoForMatching> matchingAlleles) : base(matchingAlleles)
        {
        }

        [Test]
        public void ValidAlleleTypesHaveExpectedNumberOfPGroups()
        {
            var pGroupCounts = MatchingTypes
                .Where(m => !m.HlaType.IsDeleted)
                .Select(m => new { Allele = m.HlaType as Allele, PGroupCount = m.MatchingPGroups.Count() })
                .ToList();

            var expressed = pGroupCounts.Where(p =>
                !p.Allele.IsNullExpresser && p.PGroupCount != 1);

            var notExpressed = pGroupCounts.Where(p =>
                p.Allele.IsNullExpresser && p.PGroupCount != 0);

            Assert.Multiple(() =>
            {
                Assert.IsFalse(expressed.Any());
                Assert.IsFalse(notExpressed.Any());
            });
        }

        [Test]
        public void ExpressedAllelesHaveCorrectMatchingPGroup()
        {
            var normalAllele = new AlleleInfoForMatching(
                new Allele("A*", "01:01:01:01"), new Allele("A*", "01:01:01:01"), new List<string> { "01:01P" });
            var lowAllele = new AlleleInfoForMatching(
                new Allele("B*", "39:01:01:02L"), new Allele("B*", "39:01:01:02L"), new List<string> { "39:01P" });
            var questionableAllele = new AlleleInfoForMatching(
                new Allele("C*", "07:01:01:14Q"), new Allele("C*", "07:01:01:14Q"), new List<string> { "07:01P" });
            var secretedAllele = new AlleleInfoForMatching(
                new Allele("B*", "44:02:01:02S"), new Allele("B*", "44:02:01:02S"), new List<string> { "44:02P" });

            Assert.AreEqual(normalAllele, GetSingleMatchingType(MatchLocus.A, "01:01:01:01"));
            Assert.AreEqual(lowAllele, GetSingleMatchingType(MatchLocus.B, "39:01:01:02L"));
            Assert.AreEqual(questionableAllele, GetSingleMatchingType(MatchLocus.C, "07:01:01:14Q"));
            Assert.AreEqual(secretedAllele, GetSingleMatchingType(MatchLocus.B, "44:02:01:02S"));
        }

        [Test]
        public void NonExpressedAlleleHasNoPGroup()
        {
            var nullAllele = new AlleleInfoForMatching(
                new Allele("A*", "29:01:01:02N"), new Allele("A*", "29:01:01:02N"), new List<string>());

            Assert.AreEqual(nullAllele, GetSingleMatchingType(MatchLocus.A, "29:01:01:02N"));
        }

        [Test]
        public void IdenticalHlaUsedToFindMatchingPGroupsForDeletedAlleles()
        {
            var deletedWithIdentical = new AlleleInfoForMatching(
                new Allele("A*", "11:53", true), new Allele("A*", "11:02:01"), new List<string> { "11:02P" });
            var deletedIsNullIdenticalIsExpressing = new AlleleInfoForMatching(
                new Allele("A*", "01:34N", true), new Allele("A*", "01:01:38L"), new List<string> { "01:01P" });
            var deletedIsExpressingIdenticalIsNull = new AlleleInfoForMatching(
                new Allele("A*", "03:260", true), new Allele("A*", "03:284N"), new List<string>());

            Assert.AreEqual(deletedWithIdentical, GetSingleMatchingType(MatchLocus.A, "11:53"));
            Assert.AreEqual(deletedIsNullIdenticalIsExpressing, GetSingleMatchingType(MatchLocus.A, "01:34N"));
            Assert.AreEqual(deletedIsExpressingIdenticalIsNull, GetSingleMatchingType(MatchLocus.A, "03:260"));
        }

        [Test]
        public void DeletedAlleleWithNoIdenticalHlaHasNoMatchingPGroup()
        {
            var deletedNoIdentical = new AlleleInfoForMatching(
                new Allele("A*", "02:100", true), new Allele("A*", "02:100", true), new List<string>());

            Assert.AreEqual(deletedNoIdentical, GetSingleMatchingType(MatchLocus.A, "02:100"));
        }

        [Test]
        public void ConfidentialAllelesAreExcludedFromMatchingAllelesList()
        {
            var confidentialAlleles = new List<HlaType>
            {
                new HlaType("A*", "02:01:01:28"),
                new HlaType("B*", "18:37:02"),
                new HlaType("B*", "48:43"),
                new HlaType("DQB1*", "03:01:01:20"),
                new HlaType("DQB1*", "03:23:03")
            };

            Assert.IsEmpty(MatchingTypes.Where(m => confidentialAlleles.Contains(m.HlaType)));
        }
    }
}
