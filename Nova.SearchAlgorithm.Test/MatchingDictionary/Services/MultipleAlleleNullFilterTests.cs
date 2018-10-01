using System.Linq;
using FluentAssertions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.Test.Builders.ScoringInfo;
using NSubstitute;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services
{
    [TestFixture]
    public class MultipleAlleleNullFilterTests
    {
        private const string ExpressingAlleleName = "01:01";
        private const string NullAlleleName = "01:01N";

        private IHlaLookupResultSource<AlleleTyping> nullSource;
        private IHlaLookupResultSource<AlleleTyping> expressingSource;
        private SingleAlleleScoringInfo nullScoringInfo;
        private SingleAlleleScoringInfo expressingScoringInfo;

        [SetUp]
        public void SetUp()
        {
            nullSource = Substitute.For<IHlaLookupResultSource<AlleleTyping>>();
            expressingSource = Substitute.For<IHlaLookupResultSource<AlleleTyping>>();

            nullScoringInfo = new SingleAlleleScoringInfoBuilder().WithAlleleName(NullAlleleName).Build();
            expressingScoringInfo = new SingleAlleleScoringInfoBuilder().WithAlleleName(ExpressingAlleleName).Build();

            nullSource.TypingForHlaLookupResult.Returns(new AlleleTyping(MatchLocus.A, NullAlleleName));
            expressingSource.TypingForHlaLookupResult.Returns(new AlleleTyping(MatchLocus.A, ExpressingAlleleName));
        }

        [Test]
        public void Filter_RemovesNullAllelesFromAlleleSource()
        {
            var sources = new[] {nullSource, expressingSource};

            var filteredSources = MultipleAlleleNullFilter.Filter(sources).ToList();

            filteredSources.ShouldBeEquivalentTo(new[] {expressingSource});
        }

        [Test]
        public void Filter_DoesNotRemoveExpressingAllelesFromAlleleSource()
        {
            var sources = new[] {expressingSource};

            var filteredSources = MultipleAlleleNullFilter.Filter(sources).ToList();

            filteredSources.ShouldBeEquivalentTo(new[] {expressingSource});
        }

        [Test]
        public void Filter_RemovesNullSingleAlleleScoringInfos()
        {
            var sources = new[] {nullScoringInfo, expressingScoringInfo};

            var filteredSources = MultipleAlleleNullFilter.Filter(sources).ToList();

            filteredSources.ShouldBeEquivalentTo(new[] {expressingScoringInfo});
        }

        [Test]
        public void Filter_DoesNotRemoveExpressingSingleAlleleScoringInfo()
        {
            var sources = new[] {expressingScoringInfo};

            var filteredSources = MultipleAlleleNullFilter.Filter(sources).ToList();

            filteredSources.ShouldBeEquivalentTo(new[] {expressingScoringInfo});
        }
    }
}