using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
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
        public void MatchedAlleles_WhereAlleleTypingIsValid_ExpectedNumberOfPGroupsPerAllele()
        {
            var pGroupCounts = MatchingTypings
                .Where(m => !m.HlaTyping.IsDeleted)
                .Select(m => new { Allele = m.HlaTyping as AlleleTyping, PGroupCount = m.MatchingPGroups.Count() })
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
        public void MatchedAlleles_WhereAlleleTypingIsValid_OnlyOneGGroupPerAllele()
        {
            var gGroupCounts = MatchingTypings
                .Where(m => !m.HlaTyping.IsDeleted)
                .Select(m => new { Allele = m.HlaTyping as AlleleTyping, GGroupCount = m.MatchingGGroups.Count() })
                .ToList();

            var allelesWithIncorrectCounts = gGroupCounts.Where(g => g.GGroupCount != 1);
            Assert.IsFalse(allelesWithIncorrectCounts.Any());
        }

        [Test]
        public void MatchedAlleles_ForAllAlleles_AlleleStatusOnlyUnknownForDeletedTypings()
        {
            var typingStatus = MatchingTypings
                .Select(m => (AlleleTyping)m.HlaTyping)
                .Select(m => new
                {
                    IsAlleleDeleted = m.IsDeleted,
                    IsSequenceStatusUnknown = m.Status.SequenceStatus == SequenceStatus.Unknown,
                    IsDnaCategoryUnknown = m.Status.DnaCategory == DnaCategory.Unknown
                })
                .ToList();

            var validTypingsWhereStatusIsUnknown = typingStatus.Where(allele =>
                !allele.IsAlleleDeleted && (allele.IsSequenceStatusUnknown || allele.IsDnaCategoryUnknown));

            var deletedTypingsWhereStatusIsKnown = typingStatus.Where(allele =>
                allele.IsAlleleDeleted && (!allele.IsSequenceStatusUnknown || !allele.IsDnaCategoryUnknown));

            Assert.Multiple(() =>
            {
                Assert.IsFalse(validTypingsWhereStatusIsUnknown.Any());
                Assert.IsFalse(deletedTypingsWhereStatusIsKnown.Any());
            });
        }

        [TestCase("A*", MatchLocus.A, "01:01:01:01", "01:01P", "01:01:01G", SequenceStatus.Full, DnaCategory.GDna, Description = "Normal Allele")]
        [TestCase("B*", MatchLocus.B, "39:01:01:02L", "39:01P", "39:01:01G", SequenceStatus.Full, DnaCategory.GDna, Description = "L-Allele")]
        [TestCase("C*", MatchLocus.C, "07:01:01:14Q", "07:01P", "07:01:01G", SequenceStatus.Full, DnaCategory.GDna, Description = "Q-Allele")]
        [TestCase("B*", MatchLocus.B, "44:02:01:02S", "44:02P", "44:02:01G", SequenceStatus.Full, DnaCategory.GDna, Description = "S-Allele")]
        [TestCase("A*", MatchLocus.A, "29:01:01:02N", null, "29:01:01G", SequenceStatus.Full, DnaCategory.GDna, Description = "Null Allele")]
        public void MatchedAlleles_ForVaryingExpressionStatuses_CorrectMatchingInfoAssigned(
            string locus,
            MatchLocus matchLocus,
            string alleleName,
            string pGroup,
            string gGroup,
            SequenceStatus sequenceStatus,
            DnaCategory dnaCategory)
        {
            var actual = GetSingleMatchingTyping(matchLocus, alleleName);

            var status = new AlleleTypingStatus(sequenceStatus, dnaCategory);
            var expected = new AlleleInfoForMatching(
                new AlleleTyping(locus, alleleName, status),
                new AlleleTyping(locus, alleleName, status),
                pGroup == null ? new List<string>() : new List<string> { pGroup },
                new List<string> { gGroup });

            Assert.AreEqual(actual, expected);
        }

        [TestCase("A*", MatchLocus.A, "11:53", "11:02:01", "11:02P", "11:02:01G", SequenceStatus.Full, DnaCategory.GDna, Description = "Deleted Allele & Identical Hla are expressing")]
        [TestCase("A*", MatchLocus.A, "01:34N", "01:01:38L", "01:01P", "01:01:01G", SequenceStatus.Full, DnaCategory.GDna, Description = "Deleted Allele is null; Identical Hla is expressing")]
        [TestCase("A*", MatchLocus.A, "03:260", "03:284N", null, "03:284N", SequenceStatus.Full, DnaCategory.GDna, Description = "Deleted Allele is expressing; Identical Hla is null")]
        [TestCase("A*", MatchLocus.A, "02:100", "02:100", null, null, SequenceStatus.Unknown, DnaCategory.Unknown, Description = "Deleted Allele has no Identical Hla")]
        public void MatchedAlleles_WhenDeletedAllele_IdenticalHlaUsedToAssignMatchingInfo(
            string locus,
            MatchLocus matchLocus,
            string alleleName,
            string alleleNameUsedInMatching,
            string pGroup,
            string gGroup,
            SequenceStatus sequenceStatus,
            DnaCategory dnaCategory)
        {
            var actual = GetSingleMatchingTyping(matchLocus, alleleName);

            var alleleTypingStatus = new AlleleTypingStatus(SequenceStatus.Unknown, DnaCategory.Unknown);
            var alleleTyping = new AlleleTyping(locus, alleleName, alleleTypingStatus, true);

            var usedInMatchingStatus = new AlleleTypingStatus(sequenceStatus, dnaCategory);
            var usedInMatching = new AlleleTyping(locus, alleleNameUsedInMatching, usedInMatchingStatus, alleleNameUsedInMatching.Equals(alleleName));

            var expected = new AlleleInfoForMatching(
                alleleTyping,
                usedInMatching,
                pGroup == null ? new List<string>() : new List<string> { pGroup },
                gGroup == null ? new List<string>() : new List<string> { gGroup });

            Assert.AreEqual(actual, expected);
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

            Assert.IsEmpty(MatchingTypings.Where(m => confidentialAlleles.Contains(m.HlaTyping)));
        }
    }
}
