using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.MatchingDictionary.Tests.Services.AlleleMatching
{
    [TestFixtureSource(typeof(AlleleMatchingTestFixtureArgs))]
    public class AlleleMatchingTest : MatchedHlaTestBase<IMatchingPGroups>
    {
        public AlleleMatchingTest(IEnumerable<IMatchingPGroups> matchingAlleles) : base(matchingAlleles)
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
            var normalAllele = new AlleleToPGroup(
                new Allele("A*", "01:01:01:01"), new Allele("A*", "01:01:01:01"), new List<string> { "01:01P" });
            var lowAllele = new AlleleToPGroup(
                new Allele("B*", "39:01:01:02L"), new Allele("B*", "39:01:01:02L"), new List<string> { "39:01P" });
            var questionableAllele = new AlleleToPGroup(
                new Allele("C*", "07:01:01:14Q"), new Allele("C*", "07:01:01:14Q"), new List<string> { "07:01P" });
            var secretedAllele = new AlleleToPGroup(
                new Allele("B*", "44:02:01:02S"), new Allele("B*", "44:02:01:02S"), new List<string> { "44:02P" });

            Assert.AreEqual(normalAllele, GetSingleMatchingType("A", "01:01:01:01"));
            Assert.AreEqual(lowAllele, GetSingleMatchingType("B", "39:01:01:02L"));
            Assert.AreEqual(questionableAllele, GetSingleMatchingType("C", "07:01:01:14Q"));
            Assert.AreEqual(secretedAllele, GetSingleMatchingType("B", "44:02:01:02S"));
        }

        [Test]
        public void NonExpressedAlleleHasNoPGroup()
        {
            var nullAllele = new AlleleToPGroup(
                new Allele("A*", "29:01:01:02N"), new Allele("A*", "29:01:01:02N"), new List<string>());

            Assert.AreEqual(nullAllele, GetSingleMatchingType("A", "29:01:01:02N"));
        }

        [Test]
        public void IdenticalHlaUsedToFindMatchingPGroupsForDeletedAlleles()
        {
            var deletedWithIdentical = new AlleleToPGroup(
                new Allele("A*", "11:53", true), new Allele("A*", "11:02:01"), new List<string> { "11:02P" });
            var deletedIsNullIdenticalIsExpressing = new AlleleToPGroup(
                new Allele("A*", "01:34N", true), new Allele("A*", "01:01:38L"), new List<string> { "01:01P" });
            var deletedIsExpressingIdenticalIsNull = new AlleleToPGroup(
                new Allele("A*", "03:260", true), new Allele("A*", "03:284N"), new List<string>());

            Assert.AreEqual(deletedWithIdentical, GetSingleMatchingType("A", "11:53"));
            Assert.AreEqual(deletedIsNullIdenticalIsExpressing, GetSingleMatchingType("A", "01:34N"));
            Assert.AreEqual(deletedIsExpressingIdenticalIsNull, GetSingleMatchingType("A", "03:260"));
        }

        [Test]
        public void DeletedAlleleWithNoIdenticalHlaHasNoMatchingPGroup()
        {
            var deletedNoIdentical = new AlleleToPGroup(
                new Allele("A*", "02:100", true), new Allele("A*", "02:100", true), new List<string>());

            Assert.AreEqual(deletedNoIdentical, GetSingleMatchingType("A", "02:100"));
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
