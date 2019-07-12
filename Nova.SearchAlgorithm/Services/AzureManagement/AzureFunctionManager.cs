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
        private readonly IAzureManagementClient azureManagementClient;

        public AzureFunctionManager(IAzureManagementClient azureManagementClient)
        {
            this.azureManagementClient = azureManagementClient;
        }

        public async Task StartFunction(string functionsAppName, string functionName)
        {
            await azureManagementClient.SetApplicationSetting(functionsAppName, GetDisabledAppSetting(functionName), "false");
        }

        public async Task StopFunction(string functionsAppName, string functionName)
        {
            await azureManagementClient.SetApplicationSetting(functionsAppName, GetDisabledAppSetting(functionName), "true");
        }

        private static string GetDisabledAppSetting(string functionName)
        {
            return $"AzureWebJobs.{functionName}.Disabled";
        }
    }
}