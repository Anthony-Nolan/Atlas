using System.Threading.Tasks;
using Nova.SearchAlgorithm.Clients.AzureManagement;
using Nova.SearchAlgorithm.Services.AzureManagement;
using NSubstitute;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Services.AzureManagement
{
    [TestFixture]
    public class AzureFunctionManagerTests
    {
        private IAzureManagementClient azureManagementClient;

        private IAzureFunctionManager azureFunctionManager;

        [SetUp]
        public void SetUp()
        {
            azureManagementClient = Substitute.For<IAzureManagementClient>();

            azureFunctionManager = new AzureFunctionManager(azureManagementClient);
        }

        [Test]
        public async Task StartFunction_SetsFunctionDisabledAppSettingToFalse()
        {
            const string functionAppName = "TEST-FUNCTIONS-APP";
            const string functionName = "TestFunction";

            await azureFunctionManager.StartFunction(functionAppName, functionName);

            await azureManagementClient.Received().SetApplicationSetting(functionAppName, $"AzureWebJobs.{functionName}.Disabled", "false");
        }

        [Test]
        public async Task StopFunction_SetsFunctionDisabledAppSettingToTrue()
        {
            const string functionAppName = "TEST-FUNCTIONS-APP";
            const string functionName = "TestFunction";

            await azureFunctionManager.StopFunction(functionAppName, functionName);

            await azureManagementClient.Received().SetApplicationSetting(functionAppName, $"AzureWebJobs.{functionName}.Disabled", "true");
        }
    }
}