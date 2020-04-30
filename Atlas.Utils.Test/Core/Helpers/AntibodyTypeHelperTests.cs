using FluentAssertions;
using Atlas.Utils.Core.Helpers;
using Atlas.Utils.Core.Models;
using NUnit.Framework;

namespace Atlas.Utils.Test.Core.Helpers
{
    [TestFixture]
    public class AntibodyTypeHelpersTests
    {
        [TestCase("POSITIVE", CmvAntibodyType.Positive)]
        [TestCase("positive", CmvAntibodyType.Positive)]
        [TestCase("posITIve", CmvAntibodyType.Positive)]
        [TestCase("NEGATIVE", CmvAntibodyType.Negative)]
        [TestCase("EQUIVOCAL", CmvAntibodyType.Equivocal)]
        [TestCase("UNKNOWN", CmvAntibodyType.Unknown)]
        [TestCase("not even recognised", CmvAntibodyType.Unknown)]
        public void GetCmvAntibodyType_ConvertsStringToCorrectAntibodyType(string name, CmvAntibodyType expectedAntibodyType)
        {
            var actualAntibodyType = AntibodyTypeHelper.GetCmvAntibodyType(name);

            actualAntibodyType.Should().Be(expectedAntibodyType);
        }

        [Test]
        public void GetCmvAntibodyType_ForComplexObject_ReturnsUnknown()
        {
            var antibodyType = AntibodyTypeHelper.GetCmvAntibodyType(new {Test = "test"});
            antibodyType.Should().Be(CmvAntibodyType.Unknown);
        }
    }
}