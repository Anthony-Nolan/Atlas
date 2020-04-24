using FluentAssertions;
using Atlas.Utils.Core.Helpers;
using Atlas.Utils.Core.Models;
using NUnit.Framework;

namespace Atlas.Utils.Test.Core.Helpers
{
    [TestFixture]
    public class AntibodyTypeHelpersTests
    {
#pragma warning disable 618
        [TestCase("POSITIVE", CmvAntibodyType.Positive)]
        [TestCase("positive", CmvAntibodyType.Positive)]
        [TestCase("posITIve", CmvAntibodyType.Positive)]
        [TestCase("NEGATIVE", CmvAntibodyType.Negative)]
        [TestCase("EQUIVOCAL", CmvAntibodyType.Equivocal)]
        [TestCase("UNKNOWN", CmvAntibodyType.Unknown)]
        [TestCase("not even recognised", CmvAntibodyType.Unknown)]
        [TestCase(null, CmvAntibodyType.Unknown)]
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
#pragma warning restore 618

        [TestCase("POSITIVE", VirologyStatus.Positive)]
        [TestCase("positive", VirologyStatus.Positive)]
        [TestCase("posITIve", VirologyStatus.Positive)]
        [TestCase("NEGATIVE", VirologyStatus.Negative)]
        [TestCase("EQUIVOCAL", VirologyStatus.Equivocal)]
        [TestCase("UNKNOWN", VirologyStatus.Unknown)]
        [TestCase("not even recognised", VirologyStatus.Unknown)]
        public void GetCmvAntibodyType_ConvertsStringToCorrectVirologyStatus(string name, VirologyStatus expectedVirologyStatus)
        {
            var actualStatus = AntibodyTypeHelper.GetVirologyStatus(name);

            actualStatus.Should().Be(expectedVirologyStatus);
        }
        
        [TestCase("POSITIVE", VirologyStatus.Positive)]
        [TestCase("positive", VirologyStatus.Positive)]
        [TestCase("posITIve", VirologyStatus.Positive)]
        [TestCase("NEGATIVE", VirologyStatus.Negative)]
        [TestCase("EQUIVOCAL", VirologyStatus.Equivocal)]
        [TestCase("UNKNOWN", VirologyStatus.Unknown)]
        [TestCase("not even recognised", VirologyStatus.Unknown)]
        [TestCase(null, VirologyStatus.Unknown)]
        public void GetVirologyStatus_ConvertsStringToCorrectVirologyStatus(string name, VirologyStatus expectedStatus)
        {
            var actualAntibodyType = AntibodyTypeHelper.GetVirologyStatus(name);

            actualAntibodyType.Should().Be(expectedStatus);
        }
    }
}