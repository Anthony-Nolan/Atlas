using System.Threading.Tasks;
using Nova.SearchAlgorithm.Clients.AzureManagement;

namespace Nova.SearchAlgorithm.Services.AzureManagement
{
    public interface IAzureFunctionManager
    {
        Task StartFunction(string functionName);
        Task StopFunction(string functionName);
    }

    public class AzureFunctionManager : IAzureFunctionManager
    {
        private readonly IAzureManagementClient azureManagementClient;

        public AzureFunctionManager(IAzureManagementClient azureManagementClient)
        {
            this.azureManagementClient = azureManagementClient;
        }

        public async Task StartFunction(string functionName)
        {
            await azureManagementClient.SetApplicationSetting(GetDisabledAppSetting(functionName), "false");
        }

        public async Task StopFunction(string functionName)
        {
            await azureManagementClient.SetApplicationSetting(GetDisabledAppSetting(functionName), "true");
        }

        private static string GetDisabledAppSetting(string functionName)
        {
            return $"AzureWebJobs.{functionName}.Disabled";
        }
    }
}