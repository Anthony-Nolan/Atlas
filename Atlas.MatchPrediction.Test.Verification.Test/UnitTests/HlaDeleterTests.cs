using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.MatchPrediction.Test.Verification.Services.HlaMaskers;
using Atlas.MatchPrediction.Test.Verification.Test.TestHelpers;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Verification.Test.UnitTests
{
    [TestFixture]
    public class HlaDeleterTests
    {
        private IHlaDeleter deleter;

        [SetUp]
        public void SetUp()
        {
            deleter = new HlaDeleter();
        }

        [Test]
        public async Task DeleteRandomLocusHla_DeletesHla()
        {
            const int simulantCount = 1;
            
            var typings = SimulantLocusHlaBuilder.New.WithTypingFromLocusName(Locus.A).Build(simulantCount).ToList();

            var request = new TransformationRequest
            {
                ProportionToTransform = 100,
                TotalSimulantCount = simulantCount,
                Typings = typings
            };

            var results = await deleter.DeleteRandomLocusHla(request);
            var result = results.SelectedTypings.Single();

            result.HlaTyping.Position1.Should().BeNull();
            result.HlaTyping.Position2.Should().BeNull();
        }

        [Test]
        public async Task DeleteRandomLocusHla_DeletesCorrectProportion([Values(0, 50, 100)] int proportion)
        {
            const int simulantCount = 100;

            var typings = SimulantLocusHlaBuilder.New.WithTypingFromLocusName(Locus.A).Build(simulantCount).ToList();

            var request = new TransformationRequest
            {
                ProportionToTransform = proportion,
                TotalSimulantCount = simulantCount,
                Typings = typings
            };

            var results = await deleter.DeleteRandomLocusHla(request);

            // `proportion` value can be used directly as long as `simulantCount` is 100
            results.SelectedTypings.Count.Should().Be(proportion);
            results.RemainingTypings.Count.Should().Be(simulantCount - proportion);
        }

        [Test]
        public async Task DeleteRandomLocusHla_NoOverlapBetweenSelectedAndRemainingTypings()
        {
            const int simulantCount = 10;

            var typings = SimulantLocusHlaBuilder.New
                .WithTypingFromLocusName(Locus.A)
                .WithIncrementingIds()
                .Build(simulantCount)
                .ToList();

            var request = new TransformationRequest
            {
                ProportionToTransform = 50,
                TotalSimulantCount = simulantCount,
                Typings = typings
            };

            var results = await deleter.DeleteRandomLocusHla(request);
            var selected = results.SelectedTypings.Select(x => x.GenotypeSimulantId);
            var remaining = results.RemainingTypings.Select(x => x.GenotypeSimulantId);

            selected.Should().NotBeEquivalentTo(remaining);
        }

        [Test]
        public async Task DeleteRandomLocusHla_DoesNotModifyMetadata()
        {
            const Locus locus = Locus.B;
            const int simulantCount = 1;

            var typings = SimulantLocusHlaBuilder.New.WithTypingFromLocusName(locus).Build(simulantCount).ToList();
            var typing = typings.First();

            var request = new TransformationRequest
            {
                ProportionToTransform = 100,
                TotalSimulantCount = simulantCount,
                Typings = typings
            };

            var results = await deleter.DeleteRandomLocusHla(request);
            var result = results.SelectedTypings.Single();

            result.Locus.Should().Be(locus);
            result.GenotypeSimulantId.Should().Be(typing.GenotypeSimulantId);
        }
    }
}
