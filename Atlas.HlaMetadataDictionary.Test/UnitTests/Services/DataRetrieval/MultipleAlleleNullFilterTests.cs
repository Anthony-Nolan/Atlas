using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders.ScoringInfoBuilders;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.DataRetrieval
{
    [TestFixture]
    public class MultipleAlleleNullFilterTests
    {
        private const string ExpressingAlleleName = "01:01";
        private const string NullAlleleName = "01:01N";

        private IHlaMetadataSource<AlleleTyping> nullSource;
        private IHlaMetadataSource<AlleleTyping> expressingSource;
        private SingleAlleleScoringInfo nullScoringInfo;
        private SingleAlleleScoringInfo expressingScoringInfo;

        [SetUp]
        public void SetUp()
        {
            nullSource = Substitute.For<IHlaMetadataSource<AlleleTyping>>();
            expressingSource = Substitute.For<IHlaMetadataSource<AlleleTyping>>();

            nullScoringInfo = new SingleAlleleScoringInfoBuilder().WithAlleleName(NullAlleleName).Build();
            expressingScoringInfo = new SingleAlleleScoringInfoBuilder().WithAlleleName(ExpressingAlleleName).Build();

            nullSource.TypingForHlaMetadata.Returns(new AlleleTyping(Locus.A, NullAlleleName));
            expressingSource.TypingForHlaMetadata.Returns(new AlleleTyping(Locus.A, ExpressingAlleleName));
        }

        [Test]
        public void Filter_RemovesNullAllelesFromAlleleSource()
        {
            var sources = new[] {nullSource, expressingSource};

            var filteredSources = MultipleAlleleNullFilter.Filter(sources).ToList();

            filteredSources.Should().BeEquivalentTo(new[] {expressingSource});
        }

        [Test]
        public void Filter_DoesNotRemoveExpressingAllelesFromAlleleSource()
        {
            var sources = new[] {expressingSource};

            var filteredSources = MultipleAlleleNullFilter.Filter(sources).ToList();

            filteredSources.Should().BeEquivalentTo(new[] {expressingSource});
        }

        [Test]
        public void Filter_RemovesNullSingleAlleleScoringInfos()
        {
            var sources = new[] {nullScoringInfo, expressingScoringInfo};

            var filteredSources = MultipleAlleleNullFilter.Filter(sources).ToList();

            filteredSources.Should().BeEquivalentTo(new[] {expressingScoringInfo});
        }

        [Test]
        public void Filter_DoesNotRemoveExpressingSingleAlleleScoringInfo()
        {
            var sources = new[] {expressingScoringInfo};

            var filteredSources = MultipleAlleleNullFilter.Filter(sources).ToList();

            filteredSources.Should().BeEquivalentTo(new[] {expressingScoringInfo});
        }
    }
}