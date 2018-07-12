using System.Collections.Generic;
using System.Linq;
using ApprovalTests;
using ApprovalTests.Reporters;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.HlaMatchPreCalculation.SerologyToSerology
{
    [UseReporter(typeof(NUnitReporter))]
    [ApprovalTests.Namers.UseApprovalSubdirectory("../../../../Resources/MDPreCalc")]
    public class SerologyToSerologyMatchingTest : MatchedOnTestBase<ISerologyInfoForMatching>
    {
        [TestCase("A", MatchLocus.A, "9", SerologySubtype.Broad,
            new[] { "9", "23", "24", "2403" },
            new[] { SerologySubtype.Broad, SerologySubtype.Split, SerologySubtype.Split, SerologySubtype.Associated },
            Description = "Broad serology has at least one Split with an Associated antigen")]

        [TestCase("DQ", MatchLocus.Dqb1, "1", SerologySubtype.Broad,
            new[] { "1", "5", "6" },
            new[] { SerologySubtype.Broad, SerologySubtype.Split, SerologySubtype.Split },
            Description = "Broad serology has Splits with no Associated antigens")]

        [TestCase("B", MatchLocus.B, "21", SerologySubtype.Broad,
        new[] { "21", "4005", "49", "50" },
        new[] { SerologySubtype.Broad, SerologySubtype.Associated, SerologySubtype.Split, SerologySubtype.Split },
        Description = "Broad serology has its own Associated antigen")]

        [TestCase("B", MatchLocus.B, "51", SerologySubtype.Split,
        new[] { "51", "5", "5102", "5103" },
        new[] { SerologySubtype.Split, SerologySubtype.Broad, SerologySubtype.Associated, SerologySubtype.Associated },
        Description = "Split serology has at least one Associated antigen")]

        [TestCase("Cw", MatchLocus.C, "10", SerologySubtype.Split,
        new[] { "10","3"},
        new[] {SerologySubtype.Split, SerologySubtype.Broad },
        Description = "Split serology has no Associated antigens")]

        [TestCase("B", MatchLocus.B, "3902", SerologySubtype.Associated,
        new[] { "3902","39", "16"},
        new[] { SerologySubtype.Associated, SerologySubtype.Split, SerologySubtype.Broad },
        Description = "Associated serology is direct child of a Split antigen")]

        [TestCase("B", MatchLocus.B, "4005", SerologySubtype.Associated,
        new[] { "4005","21"},
        new[] { SerologySubtype.Associated, SerologySubtype.Broad},
        Description = "Associated serology is direct child of a Broad antigen")]

        [TestCase("DR", MatchLocus.Drb1, "103", SerologySubtype.Associated,
        new[] { "103","1"},
        new[] { SerologySubtype.Associated, SerologySubtype.NotSplit},
        Description = "Associated serology is direct child of a Not-Split antigen")]

        [TestCase("A", MatchLocus.A, "2", SerologySubtype.NotSplit,
        new[] { "2","203", "210"},
        new[] { SerologySubtype.NotSplit,SerologySubtype.Associated,SerologySubtype.Associated},
        Description = "Not-Split serology has at least one Associated antigen")]

        [TestCase("DR", MatchLocus.Drb1, "9", SerologySubtype.NotSplit,
        new[] { "9" },
        new[] { SerologySubtype.NotSplit },
        Description = "Not-Split serology with no Associated antigens")]
        public void MatchedSerologies_WhenValidSerology_MatchingSerologiesCorrectlyAssigned(
            string locus,
            MatchLocus matchLocus,
            string serologyName,
            SerologySubtype serologySubtype,
            string[] expectedMatchingSerologyNames,
            SerologySubtype[] expectedMatchingSerologySubtypes)
        {
            var expectedSerologyTyping = new SerologyTyping(locus, serologyName, serologySubtype);

            var expectedMatchingSerologies = new List<SerologyTyping>();
            for (var i = 0; i < expectedMatchingSerologyNames.Count(); i++)
            {
                expectedMatchingSerologies.Add(
                    new SerologyTyping(locus, expectedMatchingSerologyNames[i], expectedMatchingSerologySubtypes[i]));
            }

            var expectedSerologyInfo = new SerologyInfoForMatching
            (
                expectedSerologyTyping,
                expectedSerologyTyping,
                expectedMatchingSerologies
            );

            var actualSerologyInfo = GetSingleMatchingTyping(matchLocus, serologyName);

            Assert.AreEqual(expectedSerologyInfo, actualSerologyInfo);
        }
        
        [Test]
        public void MatchedSerologies_WhenDeletedSerology_MatchingSerologiesCorrectlyAssigned()
        {
            const string locus = "Cw";
            const MatchLocus matchLocus = MatchLocus.C;
            const string deletedSerologyName = "11";
            const string serologyUsedInMatchingName = "1";

            var expectedDeletedSerology =
                new SerologyTyping(locus, deletedSerologyName, SerologySubtype.NotSplit, true);
            var expectedTypingUsedInMatching =
                new SerologyTyping(locus, serologyUsedInMatchingName, SerologySubtype.NotSplit);
            var expectedMatchingSerologies = new List<SerologyTyping>
            {
                expectedDeletedSerology,
                expectedTypingUsedInMatching
            };

            var expectedSerologyInfo = new SerologyInfoForMatching
            (
                expectedDeletedSerology,
                expectedTypingUsedInMatching,
                expectedMatchingSerologies
            );

            var actualSerologyInfo = GetSingleMatchingTyping(matchLocus, deletedSerologyName);

            Assert.AreEqual(expectedSerologyInfo, actualSerologyInfo);
        }

        [Test]
        public void MatchedSerologies_CollectionContainsAllExpectedSerology()
        {
            var str = string.Join("\r\n", MatchedHla
                .OrderBy(s => s.HlaTyping.MatchLocus)
                .ThenBy(s => int.Parse(s.HlaTyping.Name))
                .Select(s => $"{s.HlaTyping.MatchLocus.ToString().ToUpper()}\t{s.HlaTyping.Name}")
                .ToList());
            Approvals.Verify(str);
        }

        [Test]
        public void MatchedSerologies_WhereSerologyIsValid_CollectionOnlyContainsValidRelationships()
        {
            var groupBySubtype = MatchedHla
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
            Assert.IsEmpty(
                groupBySubtype.Where(s => s.SubtypeCounts.Any(sc => sc.Subtype == s.MatchedType.SerologySubtype)));

            // Broads cannot be matched to NotSplit, and must have at least two Splits
            Assert.IsEmpty(
                broads.Where(b => b.SubtypeCounts.Any(sc => sc.Subtype == SerologySubtype.NotSplit)));
            Assert.IsEmpty(
                broads.Where(b => b.SubtypeCounts.Single(sc => sc.Subtype == SerologySubtype.Split).Count < 2));

            // Splits cannot be matched to NotSplit, and must have one Broad
            Assert.IsEmpty(
                splits.Where(s => s.SubtypeCounts.Any(sc => sc.Subtype == SerologySubtype.NotSplit)));
            Assert.IsEmpty(
                splits.Where(s => s.SubtypeCounts.Single(sc => sc.Subtype == SerologySubtype.Broad).Count != 1));

            // Associated can only have:
            //      * 1 x NotSplit, or;
            //      * 1 x Broad, or;
            //      * 1 x Split and 1 x Broad
            Assert.IsEmpty(
                associated.Where(a => a.SubtypeCounts.Any(sc => sc.Count != 1)));
            Assert.IsEmpty(
                associated.Where(a =>
                    a.SubtypeCounts.Any(sc => sc.Subtype == SerologySubtype.NotSplit)
                    && a.SubtypeCounts.Any(sc => sc.Subtype != SerologySubtype.NotSplit)));
            Assert.IsEmpty(
                associated
                .Where(a => a.SubtypeCounts.Any(sc => sc.Subtype == SerologySubtype.Split))
                .Where(a => a.SubtypeCounts.All(sc => sc.Subtype != SerologySubtype.Broad)));

            // NotSplits can only be matched to Associated
            Assert.IsEmpty(
                notSplits.Where(n => n.SubtypeCounts.Any(sc => sc.Subtype != SerologySubtype.Associated)));
        }
    }
}
