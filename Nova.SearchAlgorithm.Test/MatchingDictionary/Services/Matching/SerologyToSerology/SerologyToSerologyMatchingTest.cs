using System.Collections.Generic;
using System.Linq;
using ApprovalTests;
using ApprovalTests.Reporters;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.Matching.SerologyToSerology
{
    [TestFixtureSource(typeof(MatchedHlaTestFixtureArgs), nameof(MatchedHlaTestFixtureArgs.MatchedSerology))]
    [UseReporter(typeof(NUnitReporter))]
    public class SerologyToSerologyMatchingTest : MatchedOnTestBase<ISerologyInfoForMatching>
    {
        public SerologyToSerologyMatchingTest(IEnumerable<ISerologyInfoForMatching> matchingSerology) : base(matchingSerology)
        {
        }

        [Test]
        public void ServiceReturnsAllExpectedSerology()
        {
            var str = string.Join("\r\n", MatchedHlaTypings
                .OrderBy(s => s.HlaTyping.MatchLocus)
                .ThenBy(s => int.Parse(s.HlaTyping.Name))
                .Select(s => $"{s.HlaTyping.MatchLocus.ToString().ToUpper()}\t{s.HlaTyping.Name}")
                .ToList());
            Approvals.Verify(str);
        }

        [Test]
        public void BroadMatchingSerologiesAreCorrect()
        {
            var broadWhereSplitHasAssociated = new SerologyInfoForMatching
            (
                new SerologyTyping("A", "9", SerologySubtype.Broad),
                new SerologyTyping("A", "9", SerologySubtype.Broad),
                new List<SerologyTyping>
                {
                    new SerologyTyping("A", "9", SerologySubtype.Broad),
                    new SerologyTyping ("A", "23", SerologySubtype.Split),
                    new SerologyTyping ("A", "24", SerologySubtype.Split),
                    new SerologyTyping ("A", "2403", SerologySubtype.Associated)
                }
            );

            var broadWhereSplitHasNoAssociated = new SerologyInfoForMatching
            (
                new SerologyTyping("DQ", "1", SerologySubtype.Broad),
                new SerologyTyping("DQ", "1", SerologySubtype.Broad),
                new List<SerologyTyping>
                {
                    new SerologyTyping("DQ", "1", SerologySubtype.Broad),
                    new SerologyTyping ("DQ", "5", SerologySubtype.Split),
                    new SerologyTyping ("DQ", "6", SerologySubtype.Split)
                }
            );

            var broadHasSplitAndAssociated = new SerologyInfoForMatching
            (
                new SerologyTyping("B", "21", SerologySubtype.Broad),
                new SerologyTyping("B", "21", SerologySubtype.Broad),
                new List<SerologyTyping>
                {
                    new SerologyTyping("B", "21", SerologySubtype.Broad),
                    new SerologyTyping ("B", "4005", SerologySubtype.Associated),
                    new SerologyTyping ("B", "49", SerologySubtype.Split),
                    new SerologyTyping ("B", "50", SerologySubtype.Split)
                }
            );

            Assert.AreEqual(broadWhereSplitHasAssociated, GetSingleMatchingTyping(MatchLocus.A, "9"));
            Assert.AreEqual(broadWhereSplitHasNoAssociated, GetSingleMatchingTyping(MatchLocus.Dqb1, "1"));
            Assert.AreEqual(broadHasSplitAndAssociated, GetSingleMatchingTyping(MatchLocus.B, "21"));
        }

        [Test]
        public void SplitMatchingSerologiesAreCorrect()
        {
            var splitHasAssociated = new SerologyInfoForMatching
            (
                new SerologyTyping("B", "51", SerologySubtype.Split),
                new SerologyTyping("B", "51", SerologySubtype.Split),
                new List<SerologyTyping>
                {
                    new SerologyTyping("B", "51", SerologySubtype.Split),
                    new SerologyTyping ("B", "5", SerologySubtype.Broad),
                    new SerologyTyping ("B", "5102", SerologySubtype.Associated),
                    new SerologyTyping ("B", "5103", SerologySubtype.Associated)
                }
            );

            var splitNoAssociated = new SerologyInfoForMatching
            (
                new SerologyTyping("Cw", "10", SerologySubtype.Split),
                new SerologyTyping("Cw", "10", SerologySubtype.Split),
                new List<SerologyTyping>
                {
                    new SerologyTyping("Cw", "10", SerologySubtype.Split),
                    new SerologyTyping ("Cw", "3", SerologySubtype.Broad)
                }
            );

            Assert.AreEqual(splitHasAssociated, GetSingleMatchingTyping(MatchLocus.B, "51"));
            Assert.AreEqual(splitNoAssociated, GetSingleMatchingTyping(MatchLocus.C, "10"));
        }

        [Test]
        public void AssociatedMatchingSerologiesAreCorrect()
        {
            var associatedWithSplit = new SerologyInfoForMatching
            (
                new SerologyTyping("B", "3902", SerologySubtype.Associated),
                new SerologyTyping("B", "3902", SerologySubtype.Associated),
                new List<SerologyTyping>
                {
                    new SerologyTyping("B", "3902", SerologySubtype.Associated),
                    new SerologyTyping ("B", "39", SerologySubtype.Split),
                    new SerologyTyping ("B", "16", SerologySubtype.Broad)
                }
            );

            var associatedWithBroad = new SerologyInfoForMatching
            (
                new SerologyTyping("B", "4005", SerologySubtype.Associated),
                new SerologyTyping("B", "4005", SerologySubtype.Associated),
                new List<SerologyTyping>
                {
                    new SerologyTyping("B", "4005", SerologySubtype.Associated),
                    new SerologyTyping ("B", "21", SerologySubtype.Broad)
                }
            );

            var associatedWithNotSplit = new SerologyInfoForMatching
            (
                new SerologyTyping("DR", "103", SerologySubtype.Associated),
                new SerologyTyping("DR", "103", SerologySubtype.Associated),
                new List<SerologyTyping>
                {
                    new SerologyTyping("DR", "103", SerologySubtype.Associated),
                    new SerologyTyping ("DR", "1", SerologySubtype.NotSplit)
                }
            );

            Assert.AreEqual(associatedWithSplit, GetSingleMatchingTyping(MatchLocus.B, "3902"));
            Assert.AreEqual(associatedWithBroad, GetSingleMatchingTyping(MatchLocus.B, "4005"));
            Assert.AreEqual(associatedWithNotSplit, GetSingleMatchingTyping(MatchLocus.Drb1, "103"));
        }

        [Test]
        public void NotSplitMatchingSerologiesAreCorrect()
        {
            var notSplitWithAssociated = new SerologyInfoForMatching
            (
                new SerologyTyping("A", "2", SerologySubtype.NotSplit),
                new SerologyTyping("A", "2", SerologySubtype.NotSplit),
                new List<SerologyTyping>
                {
                    new SerologyTyping("A", "2", SerologySubtype.NotSplit),
                    new SerologyTyping ("A", "203", SerologySubtype.Associated),
                    new SerologyTyping ("A", "210", SerologySubtype.Associated)
                }
            );

            var notSplitNoAssociatedA = new SerologyInfoForMatching
            (
                new SerologyTyping("A", "1", SerologySubtype.NotSplit),
                new SerologyTyping("A", "1", SerologySubtype.NotSplit),
                new List<SerologyTyping> { new SerologyTyping("A", "1", SerologySubtype.NotSplit) }
            );

            var notSplitNoAssociatedB = new SerologyInfoForMatching
            (
                new SerologyTyping("B", "13", SerologySubtype.NotSplit),
                new SerologyTyping("B", "13", SerologySubtype.NotSplit),
                new List<SerologyTyping> { new SerologyTyping("B", "13", SerologySubtype.NotSplit) }
            );

            var notSplitNoAssociatedC = new SerologyInfoForMatching
            (
                new SerologyTyping("Cw", "8", SerologySubtype.NotSplit),
                new SerologyTyping("Cw", "8", SerologySubtype.NotSplit),
                new List<SerologyTyping> { new SerologyTyping("Cw", "8", SerologySubtype.NotSplit) }
            );

            var notSplitNoAssociatedDq = new SerologyInfoForMatching
            (
                new SerologyTyping("DQ", "4", SerologySubtype.NotSplit),
                new SerologyTyping("DQ", "4", SerologySubtype.NotSplit),
                new List<SerologyTyping> { new SerologyTyping("DQ", "4", SerologySubtype.NotSplit) }
            );

            var notSplitNoAssociatedDr = new SerologyInfoForMatching
            (
                new SerologyTyping("DR", "9", SerologySubtype.NotSplit),
                new SerologyTyping("DR", "9", SerologySubtype.NotSplit),
                new List<SerologyTyping> { new SerologyTyping("DR", "9", SerologySubtype.NotSplit) }
            );

            Assert.AreEqual(notSplitWithAssociated, GetSingleMatchingTyping(MatchLocus.A, "2"));
            Assert.AreEqual(notSplitNoAssociatedA, GetSingleMatchingTyping(MatchLocus.A, "1"));
            Assert.AreEqual(notSplitNoAssociatedB, GetSingleMatchingTyping(MatchLocus.B, "13"));
            Assert.AreEqual(notSplitNoAssociatedC, GetSingleMatchingTyping(MatchLocus.C, "8"));
            Assert.AreEqual(notSplitNoAssociatedDq, GetSingleMatchingTyping(MatchLocus.Dqb1, "4"));
            Assert.AreEqual(notSplitNoAssociatedDr, GetSingleMatchingTyping(MatchLocus.Drb1, "9"));
        }

        [Test]
        public void DeletedMatchingSerologiesAreCorrect()
        {
            var deletedSerology = new SerologyInfoForMatching
            (
                new SerologyTyping("Cw", "11", SerologySubtype.NotSplit, true),
                new SerologyTyping("Cw", "1", SerologySubtype.NotSplit),
                new List<SerologyTyping>
                {
                    new SerologyTyping("Cw", "11", SerologySubtype.NotSplit, true),
                    new SerologyTyping("Cw", "1", SerologySubtype.NotSplit)
                }
            );

            Assert.AreEqual(deletedSerology, GetSingleMatchingTyping(MatchLocus.C, "11"));
        }

        [Test]
        public void MatchingSerologyOnlyContainsValidRelationships()
        {
            var groupBySubtype = MatchedHlaTypings
                .Where(m => !m.HlaTyping.IsDeleted)
                .Select(m => new
                {
                    MatchedType = (SerologyTyping)m.HlaTyping,
                    SubtypeCounts = m.MatchingSerologies
                        .Where(s => !s.Equals(m.HlaTyping))
                        .GroupBy(s => s.SerologySubtype)
                        .Select(s => new { Subtype = s.Key, Count = s.Count() })
                }).ToList();

            var broads = groupBySubtype.Where(s => s.MatchedType.SerologySubtype == SerologySubtype.Broad).ToList();
            var splits = groupBySubtype.Where(s => s.MatchedType.SerologySubtype == SerologySubtype.Split).ToList();
            var associated = groupBySubtype.Where(s => s.MatchedType.SerologySubtype == SerologySubtype.Associated).ToList();
            var notSplits = groupBySubtype.Where(s => s.MatchedType.SerologySubtype == SerologySubtype.NotSplit).ToList();

            // Matching list should not contain the subtype of the Matched type
            Assert.IsFalse(
                groupBySubtype.Any(s => s.SubtypeCounts.Any(sc => sc.Subtype == s.MatchedType.SerologySubtype)));

            // Broads cannot be matched to NotSplit, and must have at least two Splits
            Assert.IsFalse(
                broads.Any(b => b.SubtypeCounts.Any(sc => sc.Subtype == SerologySubtype.NotSplit)));
            Assert.IsFalse(
                broads.Any(b => b.SubtypeCounts.Single(sc => sc.Subtype == SerologySubtype.Split).Count < 2));

            // Splits cannot be matched to NotSplit, and must have one Broad
            Assert.IsFalse(
                splits.Any(s => s.SubtypeCounts.Any(sc => sc.Subtype == SerologySubtype.NotSplit)));
            Assert.IsFalse(
                splits.Any(s => s.SubtypeCounts.Single(sc => sc.Subtype == SerologySubtype.Broad).Count != 1));

            // Associated can only have:
            //      * 1 x NotSplit, or;
            //      * 1 x Broad, or;
            //      * 1 x Split and 1 x Broad
            Assert.IsFalse(
                associated.Any(a => a.SubtypeCounts.Any(sc => sc.Count != 1)));
            Assert.IsFalse(
                associated.Any(a =>
                    a.SubtypeCounts.Any(sc => sc.Subtype == SerologySubtype.NotSplit)
                    && a.SubtypeCounts.Any(sc => sc.Subtype != SerologySubtype.NotSplit)));
            Assert.IsFalse(
                associated
                .Where(a => a.SubtypeCounts.Any(sc => sc.Subtype == SerologySubtype.Split))
                .Any(a => a.SubtypeCounts.All(sc => sc.Subtype != SerologySubtype.Broad)));

            // NotSplits can only be matched to Associated
            Assert.IsFalse(
                notSplits.Any(n => n.SubtypeCounts.Any(sc => sc.Subtype != SerologySubtype.Associated)));
        }
    }
}
