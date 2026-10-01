using Atlas.Common.ApplicationInsights;
using AwesomeAssertions;
using Microsoft.ApplicationInsights.DataContracts;
using NUnit.Framework;

namespace Atlas.Common.Test.ApplicationInsights
{
    [TestFixture]
    public class CloudRoleNameTelemetryInitializerTests
    {
        private const string RoleName = "dev-atlas-match-prediction-ca";

        [Test]
        public void Initialize_WhenRoleNameConfigured_SetsCloudRoleName()
        {
            var telemetry = new TraceTelemetry("message");

            new CloudRoleNameTelemetryInitializer(RoleName).Initialize(telemetry);

            telemetry.Context.Cloud.RoleName.Should().Be(RoleName);
        }

        [Test]
        public void Initialize_WhenTelemetryAlreadyHasRoleName_DoesNotOverwriteIt()
        {
            const string existingRoleName = "existing-role";
            var telemetry = new DependencyTelemetry { Context = { Cloud = { RoleName = existingRoleName } } };

            new CloudRoleNameTelemetryInitializer(RoleName).Initialize(telemetry);

            telemetry.Context.Cloud.RoleName.Should().Be(existingRoleName);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Initialize_WhenRoleNameNotConfigured_LeavesRoleNameEmpty(string configuredRoleName)
        {
            var telemetry = new TraceTelemetry("message");

            new CloudRoleNameTelemetryInitializer(configuredRoleName).Initialize(telemetry);

            telemetry.Context.Cloud.RoleName.Should().BeNullOrEmpty();
        }
    }
}
