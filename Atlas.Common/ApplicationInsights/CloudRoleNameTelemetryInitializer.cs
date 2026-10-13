using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace Atlas.Common.ApplicationInsights
{
    /// <summary>
    /// Sets the cloud role name on all telemetry items, so that Application Insights can tell this service apart from others.
    ///
    /// Azure Functions apps get a role name from the platform. Hosts that do not (e.g. Container Apps) need this initializer.
    /// It does nothing if no role name is configured, or if the telemetry item already has a role name.
    /// </summary>
    public class CloudRoleNameTelemetryInitializer : ITelemetryInitializer
    {
        private readonly string cloudRoleName;

        public CloudRoleNameTelemetryInitializer(string cloudRoleName)
        {
            this.cloudRoleName = cloudRoleName;
        }

        public void Initialize(ITelemetry telemetry)
        {
            if (string.IsNullOrWhiteSpace(cloudRoleName) || !string.IsNullOrEmpty(telemetry.Context.Cloud.RoleName))
            {
                return;
            }

            telemetry.Context.Cloud.RoleName = cloudRoleName;
        }
    }
}
