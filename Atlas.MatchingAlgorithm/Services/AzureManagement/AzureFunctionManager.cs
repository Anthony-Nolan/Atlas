using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.MatchingAlgorithm.Clients.AzureManagement;

namespace Atlas.MatchingAlgorithm.Services.AzureManagement
{
    public interface IAzureFunctionManager
    {
        Task StartFunction(string functionsAppName, string functionName);
        Task StopFunction(string functionsAppName, string functionName);
    }

    public class AzureFunctionManager : IAzureFunctionManager
    {
        private const string LocalAppName = "local";

        private readonly IAzureAppServiceManagementClient azureManagementClient;
        private readonly ILogger logger;

        public AzureFunctionManager(IAzureAppServiceManagementClient azureManagementClient, ILogger logger)
        {
            this.azureManagementClient = azureManagementClient;
            this.logger = logger;
        }

        public async Task StartFunction(string functionsAppName, string functionName)
        {
            if (IsLocal(functionsAppName))
            {
                logger.SendTrace("Running locally - will not update functions app", LogLevel.Trace);
                // If running locally, we don't want to make changes to Azure infrastructure
                return;
            }

            logger.SendTrace($"Starting function: {functionName}");
            await azureManagementClient.SetApplicationSetting(functionsAppName, GetDisabledAppSetting(functionName), "false");
            logger.SendTrace($"Function started: {functionName}");
        }

        public async Task StopFunction(string functionsAppName, string functionName)
        {
            if (IsLocal(functionsAppName))
            {
                // If running locally, we don't want to make changes to Azure infrastructure
                return;
            }

            logger.SendTrace($"Stopping function: {functionName}");
            await azureManagementClient.SetApplicationSetting(functionsAppName, GetDisabledAppSetting(functionName), "true");
            logger.SendTrace($"Function stopped: {functionName}");
        }

        private static bool IsLocal(string functionsAppName)
        {
            return functionsAppName == LocalAppName;
        }

        private static string GetDisabledAppSetting(string functionName)
        {
            return $"AzureWebJobs.{functionName}.Disabled";
        }
    }
}