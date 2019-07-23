using System.Threading.Tasks;
using Nova.SearchAlgorithm.Clients.AzureManagement;

namespace Nova.SearchAlgorithm.Services.AzureManagement
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

        public AzureFunctionManager(IAzureAppServiceManagementClient azureManagementClient)
        {
            this.azureManagementClient = azureManagementClient;
        }

        public async Task StartFunction(string functionsAppName, string functionName)
        {
            if (IsLocal(functionsAppName))
            {
                // If running locally, we don't want to make changes to Azure infrastructure
                return;
            }

            await azureManagementClient.SetApplicationSetting(functionsAppName, GetDisabledAppSetting(functionName), "false");
        }

        public async Task StopFunction(string functionsAppName, string functionName)
        {
            if (IsLocal(functionsAppName))
            {
                // If running locally, we don't want to make changes to Azure infrastructure
                return;
            }

            await azureManagementClient.SetApplicationSetting(functionsAppName, GetDisabledAppSetting(functionName), "true");
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