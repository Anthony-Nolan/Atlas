using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.DataGeneration.HlaMatchPreCalculation
{
    // ReSharper disable once InconsistentNaming
    internal class MatchedAllele_AlleleGroupsTests : MatchedOnTestBase<MatchedAllele>
    {
        private class MatchedAlleleTestData
        {
            public AlleleTyping AlleleTyping { get; }
            public int PGroupCount { get; }
            public int GGroupCount { get; }

            public MatchedAlleleTestData(AlleleTyping alleleTyping, int pGroupCount, int gGroupCount)
            {
                AlleleTyping = alleleTyping;
                PGroupCount = pGroupCount;
                GGroupCount = gGroupCount;
            }
        }

        private IQueryable<MatchedAlleleTestData> matchedAlleleTestData;
        private IQueryable<MatchedAlleleTestData> validAllelesFromTestDataset;

        [SetUp]
        public void SetUp()
        {
            var matchedHla = SharedTestDataCache.GetMatchedHla();
            matchedAlleleTestData = matchedHla
                .Where(m => m.GetType() == typeof(AlleleTyping))
                .AsQueryable()
                .Select(m => new MatchedAlleleTestData((AlleleTyping)m.HlaTyping, m.MatchingPGroups.Count, m.MatchingGGroups.Count));

            validAllelesFromTestDataset = matchedAlleleTestData.Where(m => !m.AlleleTyping.IsDeleted);
        }

        [Test]
        public void MatchedAlleles_WhereAlleleTypingIsValid_ExpectedNumberOfPGroupsPerAllele()
        {
            var expressedAllelesThatDoNotHavePGroupCountOfOne = validAllelesFromTestDataset
                .Where(x => !x.AlleleTyping.IsNullExpresser && x.PGroupCount != 1);

            var nullAllelesThatDoNotHavePGroupCountOfZero = validAllelesFromTestDataset
                .Where(x => x.AlleleTyping.IsNullExpresser && x.PGroupCount != 0);

            Assert.IsEmpty(expressedAllelesThatDoNotHavePGroupCountOfOne);
            Assert.IsEmpty(nullAllelesThatDoNotHavePGroupCountOfZero);
        }

        [Test]
        public void MatchedAlleles_WhereAlleleTypingIsValid_OnlyOneGGroupPerAllele()
        {
            var allelesThatDoNotHaveGGroupCountOfOne = validAllelesFromTestDataset.Where(g => g.GGroupCount != 1);
            Assert.IsEmpty(allelesThatDoNotHaveGGroupCountOfOne);
        }

        [Test]
        public void MatchedAlleles_ForAllAlleles_AlleleStatusOnlyUnknownForDeletedTypings()
        {
            var typingStatuses = matchedAlleleTestData
                .Select(x => new
                {
                    IsAlleleDeleted = x.AlleleTyping.IsDeleted,
                    IsSequenceStatusUnknown = x.AlleleTyping.Status.SequenceStatus == SequenceStatus.Unknown,
                    IsDnaCategoryUnknown = x.AlleleTyping.Status.DnaCategory == DnaCategory.Unknown
                });

            var validTypingsWhereStatusIsUnknown = typingStatuses.Where(allele =>
                !allele.IsAlleleDeleted && (allele.IsSequenceStatusUnknown || allele.IsDnaCategoryUnknown));

            var deletedTypingsWhereStatusIsKnown = typingStatuses.Where(allele =>
                allele.IsAlleleDeleted && (!allele.IsSequenceStatusUnknown || !allele.IsDnaCategoryUnknown));

            Assert.Multiple(() =>
            {
                Assert.IsEmpty(validTypingsWhereStatusIsUnknown);
                Assert.IsEmpty(deletedTypingsWhereStatusIsKnown);
            });
        }

        [TestCase(Locus.A, "01:01:01:01", "01:01P", "01:01:01G", Description = "Normal Allele")]
        [TestCase(Locus.B, "39:01:01:02L", "39:01P", "39:01:01G", Description = "L-Allele")]
        [TestCase(Locus.C, "07:01:01:14Q", "07:01P", "07:01:01G", Description = "Q-Allele")]
        [TestCase(Locus.B, "44:02:01:02S", "44:02P", "44:02:01G", Description = "S-Allele")]
        [TestCase(Locus.A, "29:01:01:02N", null, "29:01:01G", Description = "Null Allele")]
        public void MatchedAlleles_ForVaryingExpressionStatuses_CorrectMatchingInfoAssigned(
            Locus locus,
            string alleleName,
            string pGroup,
            string gGroup)
        {
            var actual = GetSingleMatchingTyping(locus, alleleName);

            AssertAlleleGroupsAreEqual(actual.MatchingPGroups, pGroup);
            AssertAlleleGroupsAreEqual(actual.MatchingGGroups, gGroup);
        }

        [TestCase(Locus.A, "11:53", "11:02P", "11:02:01G", Description = "Deleted Allele & Identical Hla are expressing")]
        [TestCase(Locus.A, "01:34N", "01:01P", "01:01:01G", Description = "Deleted Allele is null; Identical Hla is expressing")]
        [TestCase(Locus.A, "03:260", null, "03:284N", Description = "Deleted Allele is expressing; Identical Hla is null")]
        [TestCase(Locus.A, "02:100", null, null, Description = "Deleted Allele has no Identical Hla")]
        public void MatchedAlleles_WhenDeletedAllele_IdenticalHlaUsedToAssignMatchingInfo(
            Locus locus,
            string alleleName,
            string pGroup,
            string gGroup)
        {
            var actual = GetSingleMatchingTyping(locus, alleleName);

            AssertAlleleGroupsAreEqual(actual.MatchingPGroups, pGroup);
            AssertAlleleGroupsAreEqual(actual.MatchingGGroups, gGroup);
        }

        [Test]
        public void MatchedAlleles_WhenAlleleIsConfidential_TypingIsExcluded()
        {
            var confidentialAlleles = new List<HlaTyping>
            {
                new(TypingMethod.Molecular, "A*", "02:01:01:28"),
                new(TypingMethod.Molecular, "B*", "18:37:02"),
                new(TypingMethod.Molecular, "B*", "48:43"),
                new(TypingMethod.Molecular, "C*", "06:211N"),
                new(TypingMethod.Molecular, "DQB1*", "03:01:01:20"),
                new(TypingMethod.Molecular, "DQB1*", "03:23:03")
            };

            Assert.IsEmpty(matchedAlleleTestData.Where(m => confidentialAlleles.Contains(m.AlleleTyping)));
        }

        private static void AssertAlleleGroupsAreEqual(IEnumerable<string> actual, string expected)
        {
            if (expected == null)
            {
                actual.Should().BeEmpty();
                return;
            }

            actual.Should().Equal(expected);
        }
    }
}