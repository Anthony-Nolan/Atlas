using System.Collections.Generic;
using System.Linq;
using ApprovalTests;
using ApprovalTests.Namers;
using ApprovalTests.Reporters;
using ApprovalTests.Reporters.TestFrameworks;
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
    // ReSharper disable once InconsistentNaming
    internal class MatchedSerology_SerologyToSerologyTests : MatchedOnTestBase<MatchedSerology>
    {
        [TestCaseSource(
            typeof(SerologyToSerologyTestCaseSources),
            nameof(SerologyToSerologyTestCaseSources.ExpectedSerologyInfos)
            )]
        public void MatchedSerology_WhenValidSerology_SerologyInfoCorrectlyAssigned(
            string serologyLocus,
            Locus locus,
            string serologyName,
            SerologySubtype serologySubtype,
            object[][] matchingSerologies)
        {
            var expectedSerologyTyping = new SerologyTyping(serologyLocus, serologyName, serologySubtype);
            var expectedMatchingSerologies = matchingSerologies.Select(m =>
                new MatchingSerology(new SerologyTyping(serologyLocus, m[0].ToString(), (SerologySubtype)m[1]), (bool)m[2]));

            var actualMatchedSerology = GetSingleMatchingTyping(locus, serologyName);

            actualMatchedSerology.HlaTyping.Should().Be(expectedSerologyTyping);
            actualMatchedSerology.TypingForHlaMetadata.Should().Be(expectedSerologyTyping);
            actualMatchedSerology.MatchingSerologies.Should().BeEquivalentTo(expectedMatchingSerologies);
        }

        [Test]
        public void MatchedSerology_WhenDeletedSerology_SerologyInfoCorrectlyAssigned()
        {
            const string typingLocus = "Cw";
            const Locus locus = Locus.C;
            const string deletedSerologyName = "11";
            const string serologyUsedInMatchingName = "1";

            var expectedDeletedSerology =
                new SerologyTyping(typingLocus, deletedSerologyName, SerologySubtype.NotSplit, true);

            var expectedTypingUsedInMatching =
                new SerologyTyping(typingLocus, serologyUsedInMatchingName, SerologySubtype.NotSplit);

            var expectedMatchingSerologies = new List<MatchingSerology>
            {
                new(expectedDeletedSerology, true),
                new(expectedTypingUsedInMatching, true)
            };

            var actualMatchedSerology = GetSingleMatchingTyping(locus, deletedSerologyName);

            actualMatchedSerology.HlaTyping.Should().Be(expectedDeletedSerology);
            actualMatchedSerology.TypingUsedInMatching.Should().Be(expectedTypingUsedInMatching);
            actualMatchedSerology.TypingForHlaMetadata.Should().Be(expectedDeletedSerology);
            actualMatchedSerology.MatchingSerologies.Should().BeEquivalentTo(expectedMatchingSerologies);
        }

        [Test]
        public void MatchedSerology_CollectionContainsAllExpectedSerology()
        {
            var str = MatchedHla
                .OrderBy(s => s.HlaTyping.Locus)
                .ThenBy(s => int.Parse(s.HlaTyping.Name))
                .Select(s => $"{s.HlaTyping.Locus.ToString().ToUpper()}\t{s.HlaTyping.Name}")
                .StringJoinWithNewline();

            Approvals.Verify(str);
        }

        [Test]
        public void MatchedSerology_WhereSerologyIsValid_CollectionOnlyContainsValidRelationships()
        {
            var serologiesBySubtype = MatchedHla
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

            var broads = serologiesBySubtype.Where(s => s.MatchedType.SerologySubtype == SerologySubtype.Broad).ToList();
            var splits = serologiesBySubtype.Where(s => s.MatchedType.SerologySubtype == SerologySubtype.Split).ToList();
            var associated = serologiesBySubtype.Where(s => s.MatchedType.SerologySubtype == SerologySubtype.Associated).ToList();
            var notSplits = serologiesBySubtype.Where(s => s.MatchedType.SerologySubtype == SerologySubtype.NotSplit).ToList();

            // Matching list should not contain the subtype of the Matched type
            Assert.IsEmpty(
                serologiesBySubtype.Where(s => s.SubtypeCounts.Any(sc => sc.Subtype == s.MatchedType.SerologySubtype)));

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
