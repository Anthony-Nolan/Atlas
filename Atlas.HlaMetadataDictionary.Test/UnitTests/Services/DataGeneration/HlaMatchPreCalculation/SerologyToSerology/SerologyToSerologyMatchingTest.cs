using System.Collections.Generic;
using System.Linq;
using ApprovalTests;
using ApprovalTests.Namers;
using ApprovalTests.Reporters;
using ApprovalTests.Reporters.TestFrameworks;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.InternalModels.HLATypings;
using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.DataGeneration.HlaMatchPreCalculation.SerologyToSerology
{
    [UseReporter(typeof(NUnitReporter))]
    [UseApprovalSubdirectory("Approvals")]
    internal class SerologyToSerologyMatchingTest : MatchedOnTestBase<ISerologyInfoForMatching>
    {
        [TestCaseSource(
            typeof(SerologyToSerologyMatchingTestCaseSources),
            nameof(SerologyToSerologyMatchingTestCaseSources.ExpectedSerologyInfos)
            )]
        public void MatchedSerologies_WhenValidSerology_SerologyInfoCorrectlyAssigned(
            string serologyLocus,
            Locus locus,
            string serologyName,
            SerologySubtype serologySubtype,
            object[][] matchingSerologies)
        {
            var actualSerologyInfo = GetSingleMatchingTyping(locus, serologyName);

            var expectedSerologyTyping = new SerologyTyping(serologyLocus, serologyName, serologySubtype);

            var expectedMatchingSerologies = matchingSerologies
                .Select(m =>
                    new MatchingSerology(
                        new SerologyTyping(serologyLocus, m[0].ToString(), (SerologySubtype)m[1]),
                            (bool)m[2]));

            var expectedSerologyInfo = new SerologyInfoForMatching
            (
                expectedSerologyTyping,
                expectedSerologyTyping,
                expectedMatchingSerologies
            );

            actualSerologyInfo.Should().BeEquivalentTo((ISerologyInfoForMatching)expectedSerologyInfo);
        }

        [Test]
        public void MatchedSerologies_WhenDeletedSerology_SerologyInfoCorrectlyAssigned()
        {
            const Locus locus = Locus.C;
            const string deletedSerologyName = "11";
            const string serologyUsedInMatchingName = "1";

            var actualSerologyInfo = GetSingleMatchingTyping(locus, deletedSerologyName);

            const string typingLocus = "Cw";
            var expectedDeletedSerology =
                new SerologyTyping(typingLocus, deletedSerologyName, SerologySubtype.NotSplit, true);
            var expectedTypingUsedInMatching =
                new SerologyTyping(typingLocus, serologyUsedInMatchingName, SerologySubtype.NotSplit);
            var expectedMatchingSerologies = new List<MatchingSerology>
            {
                new MatchingSerology(expectedDeletedSerology, true),
                new MatchingSerology(expectedTypingUsedInMatching, true)
            };

            var expectedSerologyInfo = new SerologyInfoForMatching
            (
                expectedDeletedSerology,
                expectedTypingUsedInMatching,
                expectedMatchingSerologies
            );

            actualSerologyInfo.Should().BeEquivalentTo((ISerologyInfoForMatching)expectedSerologyInfo);
        }

        [Test]
        public void MatchedSerologies_CollectionContainsAllExpectedSerology()
        {
            var str = SharedTestDataCache
                .GetMatchedHla()
                .OfType<MatchedSerology>()
                .OrderBy(s => s.HlaTyping.Locus)
                .ThenBy(s => int.Parse(s.HlaTyping.Name))
                .Select(s => $"{s.HlaTyping.Locus.ToString().ToUpper()}\t{s.HlaTyping.Name}")
                .StringJoinWithNewline();

            Approvals.Verify(str);
        }

        [Test]
        public void MatchedSerologies_WhereSerologyIsValid_CollectionOnlyContainsValidRelationships()
        {
            var groupBySubtype = SharedTestDataCache
                .GetMatchedHla()
                .OfType<MatchedSerology>()
                .Where(m => !m.HlaTyping.IsDeleted)
                .Select(m => new
                {
                    MatchedType = (SerologyTyping)m.HlaTyping,
                    SubtypeCounts = m.MatchingSerologies
                        .Select(s => s.SerologyTyping)
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
