using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.InternalModels.HLATypings;
using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.DataGeneration.HlaMatchPreCalculation.AlleleToSerology
{
    internal class AlleleToSerologyMatchingTest : MatchedOnTestBase<MatchedAllele>
    {
        [TestCaseSource(
            typeof(AlleleToSerologyMatchingTestCaseSources),
            nameof(AlleleToSerologyMatchingTestCaseSources.ExpressingAllelesMatchingSerologies))]
        public void AlleleToSerologyMatching_ExpressedAlleles_HaveCorrectMatchingSerology(
            Locus locus,
            string alleleName,
            object[] matchingSerologies)
        {
            var actualMatchingSerologies = GetSingleMatchingTyping(locus, alleleName).MatchingSerologies;
            var expectedMatchingSerologies = matchingSerologies
                .Select(m => (object[])m)
                .Select(BuildMatchingSerology);

            actualMatchingSerologies.Should().BeEquivalentTo(expectedMatchingSerologies);
        }

        [Test]
        public void AlleleToSerologyMatching_NonExpressedAlleles_HaveNoMatchingSerology()
        {
            var serologyCounts = MatchedHla
                .Where(m => !m.HlaTyping.IsDeleted && m.HlaTyping is AlleleTyping)
                .Select(m => new
                {
                    Allele = m.HlaTyping as AlleleTyping,
                    SerologyCount = m.MatchingSerologies.Count()
                });

            serologyCounts
                .Where(s => s.Allele.IsNullExpresser && s.SerologyCount != 0)
                .Should()
                .BeEmpty();
        }

        [TestCaseSource(
            typeof(AlleleToSerologyMatchingTestCaseSources),
            nameof(AlleleToSerologyMatchingTestCaseSources.DeletedAllelesMatchingSerologies))]
        public void AlleleToSerologyMatching_DeletedAlleles_IdenticalHlaUsedToFindMatchingSerology(
            Locus locus,
            string alleleName,
            object[] matchingSerologies)
        {
            var actualMatchingSerologies = GetSingleMatchingTyping(locus, alleleName).MatchingSerologies;
            var expectedMatchingSerologies = matchingSerologies
                .Select(m => (object[])m)
                .Select(BuildMatchingSerology);

            actualMatchingSerologies.Should().BeEquivalentTo(expectedMatchingSerologies);
        }

        [Test]
        public void AlleleToSerologyMatching_DeletedAlleleWithNoIdenticalHla_HasNoMatchingSerology()
        {
            var deletedNoIdentical = GetSingleMatchingTyping(Locus.A, "02:100");
            deletedNoIdentical.MatchingSerologies.Should().BeEmpty();
        }

        [TestCaseSource(
            typeof(AlleleToSerologyMatchingTestCaseSources),
            nameof(AlleleToSerologyMatchingTestCaseSources.AllelesMappedToSpecificSubtypeMatchingSerologies))]
        public void AlleleToSerologyMatching_AlleleMappedToSpecificSubtype_HasCorrectMatchingSerologies(Locus locus,
            string alleleName,
            object[] matchingSerologies)
        {
            var actualMatchingSerologies = GetSingleMatchingTyping(locus, alleleName).MatchingSerologies;
            var expectedMatchingSerologies = matchingSerologies
                .Select(m => (object[])m)
                .Select(BuildMatchingSerology);

            actualMatchingSerologies.Should().BeEquivalentTo(expectedMatchingSerologies);
        }

        [TestCaseSource(
            typeof(AlleleToSerologyMatchingTestCaseSources),
            nameof(AlleleToSerologyMatchingTestCaseSources.B15AllelesMatchingSerologies))]
        public void AlleleToSerologyMatching_B15Alleles_HaveCorrectMatchingSerologies(
            AlleleTestCase alleleTestCaseDetails,
            object[] matchingSerologies)
        {
            var actualMatchingSerologies = 
                GetSingleMatchingTyping(alleleTestCaseDetails.Locus, alleleTestCaseDetails.Name)
                .MatchingSerologies;

            var expectedMatchingSerologies = matchingSerologies
                .Select(m => (object[])m)
                .Select(BuildMatchingSerology);

            actualMatchingSerologies.Should().BeEquivalentTo(expectedMatchingSerologies);
        }

        [TestCaseSource(
            typeof(AlleleToSerologyMatchingTestCaseSources),
            nameof(AlleleToSerologyMatchingTestCaseSources.AllelesOfUnknownSerology))]
        public void AlleleToSerologyMatching_AllelesOfUnknownSerology_HaveCorrectMatchingSerologies(
            Locus locus,
            string alleleName,
            object[] matchingSerologies)
        {
            var actualMatchingSerologies = GetSingleMatchingTyping(locus, alleleName).MatchingSerologies;
            var expectedMatchingSerologies = matchingSerologies
                .Select(m => (object[])m)
                .Select(BuildMatchingSerology);

            actualMatchingSerologies.Should().BeEquivalentTo(expectedMatchingSerologies);
        }

        private static MatchingSerology BuildMatchingSerology(object[] dataSource)
        {
            var serology = new SerologyTyping(
                dataSource[0].ToString(),
                dataSource[1].ToString(),
                (SerologySubtype)dataSource[2]);

            var isDeletedMapping = (bool)dataSource[3];

            return new MatchingSerology(serology, isDeletedMapping);
        }
    }
}
