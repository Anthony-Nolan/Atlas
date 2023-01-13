using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.DataGeneration.HlaMatchPreCalculation
{
    internal class AlleleInfoForMatchingTest : MatchedOnTestBase<IAlleleInfoForMatching>
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
                .Select(m => new MatchedAlleleTestData((AlleleTyping) m.HlaTyping, m.MatchingPGroups.Count(), m.MatchingGGroups.Count()));

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

        [TestCase("A*", Locus.A, "01:01:01:01", "01:01P", "01:01:01G", SequenceStatus.Full, DnaCategory.GDna, Description = "Normal Allele")]
        [TestCase("B*", Locus.B, "39:01:01:02L", "39:01P", "39:01:01G", SequenceStatus.Full, DnaCategory.GDna, Description = "L-Allele")]
        [TestCase("C*", Locus.C, "07:01:01:14Q", "07:01P", "07:01:01G", SequenceStatus.Full, DnaCategory.GDna, Description = "Q-Allele")]
        [TestCase("B*", Locus.B, "44:02:01:02S", "44:02P", "44:02:01G", SequenceStatus.Full, DnaCategory.GDna, Description = "S-Allele")]
        [TestCase("A*", Locus.A, "29:01:01:02N", null, "29:01:01G", SequenceStatus.Full, DnaCategory.GDna, Description = "Null Allele")]
        public void MatchedAlleles_ForVaryingExpressionStatuses_CorrectMatchingInfoAssigned(
            string molecularLocus,
            Locus locus,
            string alleleName,
            string pGroup,
            string gGroup,
            SequenceStatus sequenceStatus,
            DnaCategory dnaCategory)
        {
            var status = new AlleleTypingStatus(sequenceStatus, dnaCategory);
            var expected = BuildAlleleInfoForMatching(
                new AlleleTyping(molecularLocus, alleleName, status),
                new AlleleTyping(molecularLocus, alleleName, status),
                pGroup,
                gGroup);

            var actual = GetSingleMatchingTyping(locus, alleleName);

            Assert.AreEqual(expected, actual);
        }

        [TestCase("A*", Locus.A, "11:53", "11:02:01", "11:02P", "11:02:01G", SequenceStatus.Full, DnaCategory.GDna, Description = "Deleted Allele & Identical Hla are expressing")]
        [TestCase("A*", Locus.A, "01:34N", "01:01:38L", "01:01P", "01:01:01G", SequenceStatus.Full, DnaCategory.GDna, Description = "Deleted Allele is null; Identical Hla is expressing")]
        [TestCase("A*", Locus.A, "03:260", "03:284N", null, "03:284N", SequenceStatus.Full, DnaCategory.GDna, Description = "Deleted Allele is expressing; Identical Hla is null")]
        [TestCase("A*", Locus.A, "02:100", "02:100", null, null, SequenceStatus.Unknown, DnaCategory.Unknown, Description = "Deleted Allele has no Identical Hla")]
        public void MatchedAlleles_WhenDeletedAllele_IdenticalHlaUsedToAssignMatchingInfo(
            string molecularLocus,
            Locus locus,
            string alleleName,
            string alleleNameUsedInMatching,
            string pGroup,
            string gGroup,
            SequenceStatus sequenceStatus,
            DnaCategory dnaCategory)
        {
            var alleleTypingStatus = new AlleleTypingStatus(SequenceStatus.Unknown, DnaCategory.Unknown);
            var deletedAlleleTyping = new AlleleTyping(
                molecularLocus, alleleName, alleleTypingStatus, true);

            var usedInMatchingStatus = new AlleleTypingStatus(sequenceStatus, dnaCategory);
            var usedInMatching = new AlleleTyping(
                molecularLocus, alleleNameUsedInMatching, usedInMatchingStatus, alleleNameUsedInMatching.Equals(alleleName));

            var expected = BuildAlleleInfoForMatching(
                deletedAlleleTyping,
                usedInMatching,
                pGroup,
                gGroup);

            var actual = GetSingleMatchingTyping(locus, alleleName);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void MatchedAlleles_WhenAlleleIsConfidential_TypingIsExcluded()
        {
            var confidentialAlleles = new List<HlaTyping>
            {
                new HlaTyping(TypingMethod.Molecular, "A*", "02:01:01:28"),
                new HlaTyping(TypingMethod.Molecular, "B*", "18:37:02"),
                new HlaTyping(TypingMethod.Molecular, "B*", "48:43"),
                new HlaTyping(TypingMethod.Molecular, "C*", "06:211N"),
                new HlaTyping(TypingMethod.Molecular, "DQB1*", "03:01:01:20"),
                new HlaTyping(TypingMethod.Molecular, "DQB1*", "03:23:03")
            };

            Assert.IsEmpty(matchedAlleleTestData.Where(m => confidentialAlleles.Contains(m.AlleleTyping)));
        }

        private static IAlleleInfoForMatching BuildAlleleInfoForMatching(
            AlleleTyping alleleTyping, AlleleTyping typingUsedInMatching, string pGroup, string gGroup)
        {
            var alleleInfo = new AlleleInfoForMatching(
                alleleTyping,
                typingUsedInMatching, 
                pGroup == null ? new List<string>() : new List<string> { pGroup },
                gGroup == null ? new List<string>() : new List<string> { gGroup }
                );

            return alleleInfo;
        }
    }
}