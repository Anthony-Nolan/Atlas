namespace Atlas.Common.ApplicationInsights
{
    public class ApplicationInsightsSettings
    {
        public string LogLevel { get; set; }

        /// <summary>
        /// Optional. Only needed by hosts that do not get a cloud role name from the platform (e.g. Container Apps).
        /// See <see cref="CloudRoleNameTelemetryInitializer"/>.
        /// </summary>
        public string CloudRoleName { get; set; }
    }
}
