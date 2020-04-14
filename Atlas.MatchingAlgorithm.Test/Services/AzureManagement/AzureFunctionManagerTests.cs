using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Clients.AzureManagement;
using Atlas.MatchingAlgorithm.Services.AzureManagement;
using Nova.Utils.ApplicationInsights;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.AzureManagement
{
    [TestFixture]
    public class AzureFunctionManagerTests
    {
        private IAzureAppServiceManagementClient azureManagementClient;
        private ILogger logger;

        private IAzureFunctionManager azureFunctionManager;

        [SetUp]
        public void SetUp()
        {
            azureManagementClient = Substitute.For<IAzureAppServiceManagementClient>();
            logger = Substitute.For<ILogger>();

            azureFunctionManager = new AzureFunctionManager(azureManagementClient, logger);
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