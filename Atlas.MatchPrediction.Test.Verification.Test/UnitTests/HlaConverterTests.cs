using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.MatchPrediction.Models.FileSchema;
using Atlas.MatchPrediction.Test.Verification.Services.HlaMaskers;
using Atlas.MatchPrediction.Test.Verification.Test.TestHelpers;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Verification.Test.UnitTests
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

        #region LargeGGroup

        [Test]
        public async Task ConvertRandomLocusHla_LargeGGroup_ConvertsCorrectProportionsOfHla(
            [Values(HlaConversionCategory.PGroup, HlaConversionCategory.Serology)] HlaConversionCategory category,
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

            await converter.ConvertRandomLocusHla(request, "version", ImportTypingCategory.LargeGGroup, category);

            // number of requests is 2 * `simulantCount` * `proportion`/100
            await hmd.Received(2 * proportion).ConvertHla(locus, Arg.Any<string>(), Arg.Any<TargetHlaCategory>());
        }

        [TestCase(HlaConversionCategory.PGroup)]
        [TestCase(HlaConversionCategory.Serology)]
        public async Task ConvertRandomLocusHla_LargeGGroup_ReturnsConvertedHla(HlaConversionCategory category)
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

            var results = await converter.ConvertRandomLocusHla(
                request, "version", ImportTypingCategory.LargeGGroup, category);
            var result = results.SelectedTypings.Single();

            result.HlaTyping.Position1.Should().Be(convertedHla);
            result.HlaTyping.Position2.Should().Be(convertedHla);
        }

        [TestCase(HlaConversionCategory.PGroup)]
        [TestCase(HlaConversionCategory.Serology)]
        public async Task ConvertRandomLocusHla_LargeGGroup_BothTypingsCannotBeConverted_LeavesBothTypingsUnmodified(HlaConversionCategory category)
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

            var results = await converter.ConvertRandomLocusHla(
                request, "version", ImportTypingCategory.LargeGGroup, category);
            var result = results.SelectedTypings.Single();

            result.HlaTyping.Position1.Should().Be(typing.HlaTyping.Position1);
            result.HlaTyping.Position2.Should().Be(typing.HlaTyping.Position2);
        }

        [TestCase(HlaConversionCategory.PGroup)]
        [TestCase(HlaConversionCategory.Serology)]
        public async Task ConvertRandomLocusHla_LargeGGroup_OneTypingCanBeConverted_LeavesCorrectTypingUnmodified(HlaConversionCategory category)
        {
            const Locus locus = Locus.A;
            const int simulantCount = 1;
            const string convertedHla = "converted-hla";

            var typings = SimulantLocusHlaBuilder.New.WithTypingFromLocusName(locus).Build(simulantCount).ToList();
            var typing = typings.Single();

            hmd.ConvertHla(locus, typing.HlaTyping.Position1, category.ToTargetHlaCategory()).Returns(new List<string>());
            hmd.ConvertHla(locus, typing.HlaTyping.Position2, category.ToTargetHlaCategory()).Returns(new[] { convertedHla });

            var request = new TransformationRequest
            {
                ProportionToTransform = 100,
                TotalSimulantCount = simulantCount,
                Typings = typings
            };

            var results = await converter.ConvertRandomLocusHla(
                request, "version", ImportTypingCategory.LargeGGroup, category);
            var result = results.SelectedTypings.Single();

            result.HlaTyping.Position1.Should().Be(typing.HlaTyping.Position1);
            result.HlaTyping.Position2.Should().Be(convertedHla);
        }
        #endregion

        #region SmallGGroup

        [Test]
        public async Task ConvertRandomLocusHla_SmallGGroup_ConvertsCorrectProportionsOfHla(
            [Values(HlaConversionCategory.PGroup, HlaConversionCategory.Serology)] HlaConversionCategory category,
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

            await converter.ConvertRandomLocusHla(request, "version", ImportTypingCategory.SmallGGroup, category);

            // number of requests is 2 * `simulantCount` * `proportion`/100
            await hmd.Received(2 * proportion).ConvertSmallGGroupToPGroup(locus, Arg.Any<string>());
        }

        [Test]
        public async Task ConvertRandomLocusHla_SmallGGroupToPGroup_ReturnsPGroup()
        {
            const Locus locus = Locus.A;
            const int simulantCount = 1;
            const string pGroup = "p-group";

            hmd.ConvertSmallGGroupToPGroup(default, default).ReturnsForAnyArgs(pGroup);

            var typings = SimulantLocusHlaBuilder.New.WithTypingFromLocusName(locus).Build(simulantCount).ToList();

            var request = new TransformationRequest
            {
                ProportionToTransform = 100,
                TotalSimulantCount = simulantCount,
                Typings = typings
            };

            var results = await converter.ConvertRandomLocusHla(
                request, "version", ImportTypingCategory.SmallGGroup, HlaConversionCategory.PGroup);
            var result = results.SelectedTypings.Single();

            result.HlaTyping.Position1.Should().Be(pGroup);
            result.HlaTyping.Position2.Should().Be(pGroup);
        }

        [Test]
        public async Task ConvertRandomLocusHla_SmallGGroupToSerology_ReturnsSerology()
        {
            const Locus locus = Locus.A;
            const int simulantCount = 1;
            const string pGroup = "p-group";
            const string serology = "serology";

            hmd.ConvertSmallGGroupToPGroup(default, default).ReturnsForAnyArgs(pGroup);
            hmd.ConvertHla(default, default, default).ReturnsForAnyArgs(new[] { serology });

            var typings = SimulantLocusHlaBuilder.New.WithTypingFromLocusName(locus).Build(simulantCount).ToList();

            var request = new TransformationRequest
            {
                ProportionToTransform = 100,
                TotalSimulantCount = simulantCount,
                Typings = typings
            };

            var results = await converter.ConvertRandomLocusHla(
                request, "version", ImportTypingCategory.SmallGGroup, HlaConversionCategory.Serology);
            var result = results.SelectedTypings.Single();

            result.HlaTyping.Position1.Should().Be(serology);
            result.HlaTyping.Position2.Should().Be(serology);
        }

        [TestCase(HlaConversionCategory.PGroup)]
        [TestCase(HlaConversionCategory.Serology)]
        public async Task ConvertRandomLocusHla_SmallGGroup_BothTypingsCannotBeConverted_LeavesBothTypingsUnmodified(HlaConversionCategory category)
        {
            hmd.ConvertSmallGGroupToPGroup(default, default).ReturnsNullForAnyArgs();

            const int simulantCount = 1;

            var typings = SimulantLocusHlaBuilder.New.WithTypingFromLocusName(Locus.A).Build(simulantCount).ToList();
            var typing = typings.Single();

            var request = new TransformationRequest
            {
                ProportionToTransform = 100,
                TotalSimulantCount = simulantCount,
                Typings = typings
            };

            var results = await converter.ConvertRandomLocusHla(
                request, "version", ImportTypingCategory.SmallGGroup, category);
            var result = results.SelectedTypings.Single();

            result.HlaTyping.Position1.Should().Be(typing.HlaTyping.Position1);
            result.HlaTyping.Position2.Should().Be(typing.HlaTyping.Position2);
        }

        [Test]
        public async Task ConvertRandomLocusHla_SmallGGroupToPGroup_OneTypingCanBeConverted_LeavesCorrectTypingUnmodified()
        {
            const Locus locus = Locus.A;
            const int simulantCount = 1;
            const string pGroup = "p-group";

            var typings = SimulantLocusHlaBuilder.New.WithTypingFromLocusName(locus).Build(simulantCount).ToList();
            var typing = typings.Single();

            hmd.ConvertSmallGGroupToPGroup(locus, typing.HlaTyping.Position1).ReturnsNull();
            hmd.ConvertSmallGGroupToPGroup(locus, typing.HlaTyping.Position2).Returns(pGroup);

            var request = new TransformationRequest
            {
                ProportionToTransform = 100,
                TotalSimulantCount = simulantCount,
                Typings = typings
            };

            var results = await converter.ConvertRandomLocusHla(
                request, "version", ImportTypingCategory.SmallGGroup, HlaConversionCategory.PGroup);
            var result = results.SelectedTypings.Single();

            result.HlaTyping.Position1.Should().Be(typing.HlaTyping.Position1);
            result.HlaTyping.Position2.Should().Be(pGroup);
        }

        [Test]
        public async Task ConvertRandomLocusHla_SmallGGroupToSerology_OneTypingCanBeConverted_LeavesCorrectTypingUnmodified()
        {
            const Locus locus = Locus.A;
            const int simulantCount = 1;
            const string pGroup = "p-group";
            const string serology = "serology";

            var typings = SimulantLocusHlaBuilder.New.WithTypingFromLocusName(locus).Build(simulantCount).ToList();
            var typing = typings.Single();

            // position 1 is not expressing
            hmd.ConvertSmallGGroupToPGroup(locus, typing.HlaTyping.Position1).ReturnsNull();
            // position 2 has P group and serology
            hmd.ConvertSmallGGroupToPGroup(locus, typing.HlaTyping.Position2).Returns(pGroup);
            hmd.ConvertHla(locus, pGroup, TargetHlaCategory.Serology).Returns(new[] { serology });

            var request = new TransformationRequest
            {
                ProportionToTransform = 100,
                TotalSimulantCount = simulantCount,
                Typings = typings
            };

            var results = await converter.ConvertRandomLocusHla(
                request, "version", ImportTypingCategory.SmallGGroup, HlaConversionCategory.Serology);
            var result = results.SelectedTypings.Single();

            result.HlaTyping.Position1.Should().Be(typing.HlaTyping.Position1);
            result.HlaTyping.Position2.Should().Be(serology);
        }
        #endregion
    }
}
