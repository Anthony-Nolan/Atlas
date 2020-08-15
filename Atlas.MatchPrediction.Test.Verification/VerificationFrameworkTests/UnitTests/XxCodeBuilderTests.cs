using Atlas.Common.GeneticData;
using Atlas.MatchPrediction.Test.Verification.Services.HlaMaskers;
using Atlas.MatchPrediction.Test.Verification.VerificationFrameworkTests.TestHelpers;
using FluentAssertions;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Test.Verification.VerificationFrameworkTests.UnitTests
{
    [TestFixture]
    public class XxBuilderTests
    {
        private IXxCodeBuilder xxCodeBuilder;

        [SetUp]
        public void SetUp()
        {
            xxCodeBuilder = new XxCodeBuilder();
        }

        [TestCase(":01")]
        [TestCase(":01:01")]
        [TestCase(":01:01G")]
        public async Task ConvertRandomLocusHlaToXxCodes_ConvertsHlaToXxCodes(string hlaSuffix)
        {
            const string firstField = "01";
            const string expectedXxCode = firstField + ":XX";
            const int simulantCount = 1;

            // have to use value that looks like a real allele/ G group to pass test
            var hla = firstField + hlaSuffix;
            var locusInfo = new LocusInfo<string>(hla);

            var typings = SimulantLocusHlaBuilder.New
                .With(x => x.HlaTyping, locusInfo)
                .Build(simulantCount)
                .ToList();

            var request = new TransformationRequest
            {
                ProportionToTransform = 100,
                TotalSimulantCount = simulantCount,
                Typings = typings
            };

            var results = await xxCodeBuilder.ConvertRandomLocusHlaToXxCodes(request);
            var result = results.SelectedTypings.Single();

            result.HlaTyping.Position1.Should().Be(expectedXxCode);
            result.HlaTyping.Position2.Should().Be(expectedXxCode);
        }

        [Test]
        public async Task ConvertRandomLocusHlaToXxCodes_ConvertsCorrectProportion([Values(0,50,100)] int proportion)
        {
            const int simulantCount = 100;

            var typings = SimulantLocusHlaBuilder.New
                .With(x => x.HlaTyping, new LocusInfo<string>("01:01"))
                .Build(simulantCount)
                .ToList();

            var request = new TransformationRequest
            {
                ProportionToTransform = proportion,
                TotalSimulantCount = simulantCount,
                Typings = typings
            };

            var results = await xxCodeBuilder.ConvertRandomLocusHlaToXxCodes(request);

            // `proportion` value can be used directly as long as `simulantCount` is 100
            results.SelectedTypings.Count.Should().Be(proportion);
            results.RemainingTypings.Count.Should().Be(simulantCount - proportion);
        }

        [Test]
        public async Task ConvertRandomLocusHlaToXxCodes_NoOverlapBetweenSelectedAndRemainingTypings()
        {
            const int simulantCount = 10;

            var typings = SimulantLocusHlaBuilder.New
                .With(x => x.HlaTyping, new LocusInfo<string>("01:01"))
                .WithIncrementingIds()
                .Build(simulantCount)
                .ToList();

            var request = new TransformationRequest
            {
                ProportionToTransform = 50,
                TotalSimulantCount = simulantCount,
                Typings = typings
            };

            var results = await xxCodeBuilder.ConvertRandomLocusHlaToXxCodes(request);
            var selected = results.SelectedTypings.Select(x => x.GenotypeSimulantId);
            var remaining = results.RemainingTypings.Select(x => x.GenotypeSimulantId);

            selected.Should().NotBeEquivalentTo(remaining);
        }

        [Test]
        public async Task DeleteRandomLocusHla_DoesNotModifyMetadata()
        {
            const Locus locus = Locus.B;
            const int simulantCount = 1;

            var typings = SimulantLocusHlaBuilder.New
                .With(x => x.HlaTyping, new LocusInfo<string>("01:01"))
                .With(x => x.Locus, locus)
                .Build(simulantCount)
                .ToList();
            var typing = typings.First();

            var request = new TransformationRequest
            {
                ProportionToTransform = 100,
                TotalSimulantCount = simulantCount,
                Typings = typings
            };

            var results = await xxCodeBuilder.ConvertRandomLocusHlaToXxCodes(request);
            var result = results.SelectedTypings.Single();

            result.Locus.Should().Be(locus);
            result.GenotypeSimulantId.Should().Be(typing.GenotypeSimulantId);
        }
    }
}
