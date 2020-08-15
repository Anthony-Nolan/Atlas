using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.MatchPrediction.Test.Verification.Services.HlaMaskers;
using Atlas.MatchPrediction.Test.Verification.VerificationFrameworkTests.TestHelpers;
using NSubstitute;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;

namespace Atlas.MatchPrediction.Test.Verification.VerificationFrameworkTests.UnitTests
{
    [TestFixture]
    public class HlaConverterTests
    {
        private IHlaMetadataDictionary hmd;
        private IHlaConverter converter;

        [SetUp]
        public void SetUp()
        {
            hmd = Substitute.For<IHlaMetadataDictionary>();
            var hmdFactory = Substitute.For<IHlaMetadataDictionaryFactory>();
            hmdFactory.BuildDictionary(default).ReturnsForAnyArgs(hmd);

            converter = new HlaConverter(hmdFactory);
        }

        [Test]
        public async Task ConvertRandomLocusHla_ConvertsCorrectProportionsOfHla(
            [Values(TargetHlaCategory.PGroup, TargetHlaCategory.Serology)] TargetHlaCategory target,
            [Values(0, 50, 100)] int proportion)
        {
            const Locus locus = Locus.A;
            const int simulantCount = 100;

            var typings = SimulantLocusHlaBuilder.New.WithTypingFromLocusName(locus).Build(simulantCount).ToList();

            var request = new TransformationRequest
            {
                ProportionToTransform = proportion,
                TotalSimulantCount = simulantCount,
                Typings = typings
            };

            await converter.ConvertRandomLocusHla(request, "version", target);

            // number of requests is 2 * `simulantCount` * `proportion`/100
            await hmd.Received(2 * proportion).ConvertHla(locus, Arg.Any<string>(), target);
        }

        [TestCase(TargetHlaCategory.PGroup)]
        [TestCase(TargetHlaCategory.Serology)]
        public async Task ConvertRandomLocusHla_ReturnsConvertedHla(TargetHlaCategory target)
        {
            const Locus locus = Locus.A;
            const int simulantCount = 1;
            const string convertedHla = "converted-hla";

            hmd.ConvertHla(default, default, default).ReturnsForAnyArgs(new[] { convertedHla });

            var typings = SimulantLocusHlaBuilder.New.WithTypingFromLocusName(locus).Build(simulantCount).ToList();

            var request = new TransformationRequest
            {
                ProportionToTransform = 100,
                TotalSimulantCount = simulantCount,
                Typings = typings
            };

            var results = await converter.ConvertRandomLocusHla(request, "version", target);
            var result = results.SelectedTypings.Single();

            result.HlaTyping.Position1.Should().Be(convertedHla);
            result.HlaTyping.Position2.Should().Be(convertedHla);
        }

        [TestCase(TargetHlaCategory.PGroup)]
        [TestCase(TargetHlaCategory.Serology)]
        public async Task ConvertRandomLocusHla_BothTypingsCannotBeConverted_LeavesBothTypingsUnmodified(TargetHlaCategory target)
        {
            hmd.ConvertHla(default, default, default).ReturnsForAnyArgs(new List<string>());

            const int simulantCount = 1;

            var typings = SimulantLocusHlaBuilder.New.WithTypingFromLocusName(Locus.A).Build(simulantCount).ToList();
            var typing = typings.Single();

            var request = new TransformationRequest
            {
                ProportionToTransform = 100,
                TotalSimulantCount = simulantCount,
                Typings = typings
            };

            var results = await converter.ConvertRandomLocusHla(request, "version", target);
            var result = results.SelectedTypings.Single();

            result.HlaTyping.Position1.Should().Be(typing.HlaTyping.Position1);
            result.HlaTyping.Position2.Should().Be(typing.HlaTyping.Position2);
        }

        [TestCase(TargetHlaCategory.PGroup)]
        [TestCase(TargetHlaCategory.Serology)]
        public async Task ConvertRandomLocusHla_OneTypingCanBeConverted_LeavesCorrectTypingUnmodified(TargetHlaCategory target)
        {
            const Locus locus = Locus.A;
            const int simulantCount = 1;
            const string convertedHla = "converted-hla";

            var typings = SimulantLocusHlaBuilder.New.WithTypingFromLocusName(locus).Build(simulantCount).ToList();
            var typing = typings.Single();

            hmd.ConvertHla(locus, typing.HlaTyping.Position1, target).Returns(new List<string>());
            hmd.ConvertHla(locus, typing.HlaTyping.Position2, target).Returns(new[] { convertedHla });

            var request = new TransformationRequest
            {
                ProportionToTransform = 100,
                TotalSimulantCount = simulantCount,
                Typings = typings
            };

            var results = await converter.ConvertRandomLocusHla(request, "version", target);
            var result = results.SelectedTypings.Single();

            result.HlaTyping.Position1.Should().Be(typing.HlaTyping.Position1);
            result.HlaTyping.Position2.Should().Be(convertedHla);
        }
    }
}
