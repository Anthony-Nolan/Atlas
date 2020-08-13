using Atlas.MatchPrediction.Test.Verification.Services.HlaMaskers;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Verification.VerificationFrameworkTests.UnitTests
{
    [TestFixture]
    public class MacBuilderTests
    {
        private IMacBuilder macBuilder;

        [SetUp]
        public void SetUp()
        {
            macBuilder = new MacBuilder();
        }

        // TODO ATLAS-478: Implement mac builder
    }
}
