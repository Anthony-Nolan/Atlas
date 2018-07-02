using System.Linq;
using FluentAssertions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Models.HlaTypings
{
    [TestFixture]
    public class AlleleModelTest
    {
        private static object[] _alleles =
        {
            new object[]
            {
                new AlleleTyping(MatchLocus.Dqb1, "03:01:01:01"),
                new AlleleTyping(MatchLocus.A, "24:02:01:02L"),
                new AlleleTyping(MatchLocus.B, "27:05:02:04Q"),
                new AlleleTyping(MatchLocus.B, "44:02:01:02S"),
                // Note: made-up alleles, as no "A" or "C" alleles have yet been found
                new AlleleTyping(MatchLocus.Drb1, "01:01:01:01A"),
                new AlleleTyping(MatchLocus.Drb1, "01:01:01:01C"),
                new AlleleTyping(MatchLocus.A, "29:01:01:02N")
            }
        };

        [TestCaseSource(nameof(_alleles))]
        public void AlleleExpressionCorrectlyIdentified(
            AlleleTyping normalAllele,
            AlleleTyping lowAllele,
            AlleleTyping questionableAllele,
            AlleleTyping secretedAllele,
            AlleleTyping aberrantAllele,
            AlleleTyping cytoplasmAllele,
            AlleleTyping nullAllele)
        {
            Assert.AreEqual(normalAllele.ExpressionSuffix, "");
            Assert.AreEqual(lowAllele.ExpressionSuffix, "L");
            Assert.AreEqual(questionableAllele.ExpressionSuffix, "Q");
            Assert.AreEqual(secretedAllele.ExpressionSuffix, "S");
            Assert.AreEqual(aberrantAllele.ExpressionSuffix, "A");
            Assert.AreEqual(cytoplasmAllele.ExpressionSuffix, "C");
            Assert.AreEqual(nullAllele.ExpressionSuffix, "N");
        }

        [TestCaseSource(nameof(_alleles))]
        public void NullExpresserCorrectlyIdentified(
            AlleleTyping normalAllele,
            AlleleTyping lowAllele,
            AlleleTyping questionableAllele,
            AlleleTyping secretedAllele,
            AlleleTyping aberrantAllele,
            AlleleTyping cytoplasmAllele,
            AlleleTyping nullAllele)
        {
            Assert.IsFalse(normalAllele.IsNullExpresser);
            Assert.IsFalse(lowAllele.IsNullExpresser);
            Assert.IsFalse(questionableAllele.IsNullExpresser);
            Assert.IsFalse(secretedAllele.IsNullExpresser);
            Assert.IsFalse(aberrantAllele.IsNullExpresser);
            Assert.IsFalse(cytoplasmAllele.IsNullExpresser);
            Assert.IsTrue(nullAllele.IsNullExpresser);
        }

        [TestCaseSource(nameof(_alleles))]
        public void AlleleFieldsCorrectlyExtracted(
            AlleleTyping normalAllele,
            AlleleTyping lowAllele,
            AlleleTyping questionableAllele,
            AlleleTyping secretedAllele,
            AlleleTyping aberrantAllele,
            AlleleTyping cytoplasmAllele,
            AlleleTyping nullAllele)
        {
            Assert.IsTrue(normalAllele.Fields.SequenceEqual(new[] { "03", "01", "01", "01" }));
            Assert.IsTrue(lowAllele.Fields.SequenceEqual(new[] { "24", "02", "01", "02" }));
            Assert.IsTrue(questionableAllele.Fields.SequenceEqual(new[] { "27", "05", "02", "04" }));
            Assert.IsTrue(secretedAllele.Fields.SequenceEqual(new[] { "44", "02", "01", "02" }));
            Assert.IsTrue(aberrantAllele.Fields.SequenceEqual(new[] { "01", "01", "01", "01" }));
            Assert.IsTrue(cytoplasmAllele.Fields.SequenceEqual(new[] { "01", "01", "01", "01" }));
            Assert.IsTrue(nullAllele.Fields.SequenceEqual(new[] { "29", "01", "01", "02" }));
        }

        [TestCaseSource(nameof(_alleles))]
        public void TwoFieldNameCorrectlyConstructed(
            AlleleTyping normalAllele,
            AlleleTyping lowAllele,
            AlleleTyping questionableAllele,
            AlleleTyping secretedAllele,
            AlleleTyping aberrantAllele,
            AlleleTyping cytoplasmAllele,
            AlleleTyping nullAllele)
        {
            Assert.AreEqual(normalAllele.TwoFieldName, "03:01");
            Assert.AreEqual(lowAllele.TwoFieldName, "24:02L");
            Assert.AreEqual(questionableAllele.TwoFieldName, "27:05Q");
            Assert.AreEqual(secretedAllele.TwoFieldName, "44:02S");
            Assert.AreEqual(aberrantAllele.TwoFieldName, "01:01A");
            Assert.AreEqual(cytoplasmAllele.TwoFieldName, "01:01C");
            Assert.AreEqual(nullAllele.TwoFieldName, "29:01N");
        }

        [TestCaseSource(nameof(_alleles))]
        public void TruncatedNameVariantsCorrectlyConstructed(
            AlleleTyping normalAllele,
            AlleleTyping lowAllele,
            AlleleTyping questionableAllele,
            AlleleTyping secretedAllele,
            AlleleTyping aberrantAllele,
            AlleleTyping cytoplasmAllele,
            AlleleTyping nullAllele)
        {
            normalAllele.NameVariantsTruncatedByFieldAndOrExpressionSuffix.ShouldBeEquivalentTo(new[] { "03:01", "03:01:01" });
            lowAllele.NameVariantsTruncatedByFieldAndOrExpressionSuffix.ShouldBeEquivalentTo(new[] { "24:02", "24:02:01", "24:02L", "24:02:01L" });
            questionableAllele.NameVariantsTruncatedByFieldAndOrExpressionSuffix.ShouldBeEquivalentTo(new[] { "27:05", "27:05:02", "27:05Q", "27:05:02Q" });
            secretedAllele.NameVariantsTruncatedByFieldAndOrExpressionSuffix.ShouldBeEquivalentTo(new[] { "44:02", "44:02:01", "44:02S", "44:02:01S" });
            aberrantAllele.NameVariantsTruncatedByFieldAndOrExpressionSuffix.ShouldBeEquivalentTo(new[] { "01:01", "01:01:01", "01:01A", "01:01:01A" });
            cytoplasmAllele.NameVariantsTruncatedByFieldAndOrExpressionSuffix.ShouldBeEquivalentTo(new[] { "01:01", "01:01:01", "01:01C", "01:01:01C" });
            nullAllele.NameVariantsTruncatedByFieldAndOrExpressionSuffix.ShouldBeEquivalentTo(new[] { "29:01", "29:01:01", "29:01N", "29:01:01N" });
        }
    }
}
