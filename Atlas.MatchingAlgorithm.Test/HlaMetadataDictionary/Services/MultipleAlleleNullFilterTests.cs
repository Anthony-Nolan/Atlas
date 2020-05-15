using System.Linq;
using Atlas.Common.GeneticData;
using FluentAssertions;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.HlaMetadataDictionary.Models.HLATypings;
using Atlas.HlaMetadataDictionary.Models.Lookups;
using Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup;
using Atlas.HlaMetadataDictionary.Services;
using Atlas.MatchingAlgorithm.Test.Builders.ScoringInfo;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.HlaMetadataDictionary.Services
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

            nullSource.TypingForHlaLookupResult.Returns(new AlleleTyping(Locus.A, NullAlleleName));
            expressingSource.TypingForHlaLookupResult.Returns(new AlleleTyping(Locus.A, ExpressingAlleleName));
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