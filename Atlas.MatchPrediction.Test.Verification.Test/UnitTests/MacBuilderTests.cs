using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchPrediction.Test.Verification.Services.HlaMaskers;
using Atlas.MatchPrediction.Test.Verification.Test.TestHelpers;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Verification.Test.UnitTests
{
    [TestFixture]
    public class MacBuilderTests
    {
        private IXxCodeBuilder xxCodeBuilder;
        private IHlaMetadataDictionary hmd;
        private IExpandedMacCache cache;

        private IMacBuilder macBuilder;

        [SetUp]
        public void SetUp()
        {
            xxCodeBuilder = Substitute.For<IXxCodeBuilder>();
            cache = Substitute.For<IExpandedMacCache>();

            hmd = Substitute.For<IHlaMetadataDictionary>();
            var hmdFactory = Substitute.For<IHlaMetadataDictionaryFactory>();
            hmdFactory.BuildDictionary(Arg.Any<string>()).ReturnsForAnyArgs(hmd);

            macBuilder = new MacBuilder(xxCodeBuilder, hmdFactory, cache);
        }

        [TestCase("99:01")]
        [TestCase("99:01:99")]
        [TestCase("99:01:99G")]
        public async Task ConvertRandomLocusHla_SelectsPotentialMacsBySecondFieldOfHla(string hlaName)
        {
            const int simulantCount = 1;

            // have to use value that looks like a real allele/ G group to pass test
            var locusInfo = new LocusInfo<string>(hlaName);

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

            await macBuilder.ConvertRandomHlaToMacs(request, "version");

            await cache.Received().GetCodesBySecondField("01");
        }

        [Test]
        public async Task ConvertRandomLocusHla_NoPotentialMacs_ReturnsOriginalHla()
        {
            // have to use values that looks like real alleles to pass test
            const string hla = "01:01";

            cache.GetCodesBySecondField(default).ReturnsForAnyArgs(new List<string>());

            const int simulantCount = 1;
            var request = new TransformationRequest
            {
                ProportionToTransform = 100,
                TotalSimulantCount = simulantCount,
                Typings = SimulantLocusHlaBuilder.New
                    .With(x => x.HlaTyping, new LocusInfo<string>(hla))
                    .Build(simulantCount)
                    .ToList()
            };

            var results = await macBuilder.ConvertRandomHlaToMacs(request, "version");
            var result = results.SelectedTypings.Single();

            result.HlaTyping.Position1.Should().Be(hla);
            result.HlaTyping.Position2.Should().Be(hla);
        }

        [Test]
        public async Task ConvertRandomLocusHla_OnlyPotentialMacExpandsToSubsetOfRelatedAlleles_ReturnsMac()
        {
            // have to use values that looks like real alleles to pass test
            const string firstField = "01";
            const string hla = "01:01";
            var relatedAlleles = new[] { "01:01", "01:33", "01:55" };
            var macSecondFields = new List<string> { "01", "33" };

            hmd.ConvertHla(default, default, default).ReturnsForAnyArgs(relatedAlleles);

            const string potentialMac = "MAC";
            cache.GetCodesBySecondField(default).ReturnsForAnyArgs(new[] { potentialMac });
            cache.GetSecondFieldsByCode(default).ReturnsForAnyArgs(macSecondFields);

            const int simulantCount = 1;
            var request = new TransformationRequest
            {
                ProportionToTransform = 100,
                TotalSimulantCount = simulantCount,
                Typings = SimulantLocusHlaBuilder.New
                    .With(x => x.HlaTyping, new LocusInfo<string>(hla))
                    .Build(simulantCount)
                    .ToList()
            };

            var results = await macBuilder.ConvertRandomHlaToMacs(request, "version");
            var result = results.SelectedTypings.Single();

            const string expected = firstField + ":" + potentialMac;
            result.HlaTyping.Position1.Should().Be(expected);
            result.HlaTyping.Position2.Should().Be(expected);
        }

        [Test]
        public async Task ConvertRandomLocusHla_OnlyPotentialMacDoesNotExpandToSubsetOfRelatedAlleles_ReturnsOriginalHla()
        {
            // have to use values that looks like real alleles to pass test
            const string hla = "01:01";
            var relatedAlleles = new[] { "01:01", "01:33", "01:55" };
            var macSecondFields = new List<string> { "01", "999" };

            hmd.ConvertHla(default, default, default).ReturnsForAnyArgs(relatedAlleles);

            const string potentialMac = "MAC";
            cache.GetCodesBySecondField(default).ReturnsForAnyArgs(new[] { potentialMac });
            cache.GetSecondFieldsByCode(default).ReturnsForAnyArgs(macSecondFields);

            const int simulantCount = 1;
            var request = new TransformationRequest
            {
                ProportionToTransform = 100,
                TotalSimulantCount = simulantCount,
                Typings = SimulantLocusHlaBuilder.New
                    .With(x => x.HlaTyping, new LocusInfo<string>(hla))
                    .Build(simulantCount)
                    .ToList()
            };

            var results = await macBuilder.ConvertRandomHlaToMacs(request, "version");
            var result = results.SelectedTypings.Single();

            result.HlaTyping.Position1.Should().Be(hla);
            result.HlaTyping.Position2.Should().Be(hla);
        }

        [Test]
        public async Task ConvertRandomLocusHla_MultiplePotentialMacs_ReturnsMacThatExpandsToSubsetOfRelatedAlleles()
        {
            // have to use values that looks like real alleles to pass test
            const string firstField = "01";
            const string hla = "01:01";
            var relatedAlleles = new[] { "01:01", "01:33", "01:55" };
            var macOneFields = new List<string> { "01", "678" };
            var macTwoFields = new List<string> { "01", "999" };
            var macThreeFields = new List<string> { "01", "55" };

            hmd.ConvertHla(default, default, default).ReturnsForAnyArgs(relatedAlleles);

            const string macOne = "ONE";
            const string macTwo = "TWO";
            const string macThree = "THREE";
            cache.GetCodesBySecondField(default).ReturnsForAnyArgs(new[] { macOne, macTwo, macThree });
            
            cache.GetSecondFieldsByCode(macOne).Returns(macOneFields);
            cache.GetSecondFieldsByCode(macTwo).Returns(macTwoFields);
            cache.GetSecondFieldsByCode(macThree).Returns(macThreeFields);

            const int simulantCount = 1;
            var request = new TransformationRequest
            {
                ProportionToTransform = 100,
                TotalSimulantCount = simulantCount,
                Typings = SimulantLocusHlaBuilder.New
                    .With(x => x.HlaTyping, new LocusInfo<string>(hla))
                    .Build(simulantCount)
                    .ToList()
            };

            var results = await macBuilder.ConvertRandomHlaToMacs(request, "version");
            var result = results.SelectedTypings.Single();

            const string expected = firstField + ":" + macThree;
            result.HlaTyping.Position1.Should().Be(expected);
            result.HlaTyping.Position2.Should().Be(expected);
        }
    }
}
