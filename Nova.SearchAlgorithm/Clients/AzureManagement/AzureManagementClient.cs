using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Clients.AzureManagement.AzureApiModels;
using Nova.SearchAlgorithm.Clients.AzureManagement.AzureApiModels.AppSettings;
using Nova.SearchAlgorithm.Clients.AzureManagement.AzureApiModels.Database;
using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.Models.AzureManagement;
using Nova.SearchAlgorithm.Settings;

namespace Nova.SearchAlgorithm.Clients.AzureManagement
{
    public interface IAzureManagementClient
    {
        Task SetApplicationSetting(string appServiceName, string key, string value);

        /// <returns>The DateTime at which the scaling operation began</returns>
        Task<DateTime> UpdateDatabaseSize(string databaseName, AzureDatabaseSize databaseSize);

        Task<IEnumerable<DatabaseOperation>> GetDatabaseOperations(string databaseName);
    }

    public class AzureManagementClient : IAzureManagementClient
    {
        private const string AzureManagementBaseUrl = "https://management.azure.com";

        private static readonly string AzureManagementScope = $"{AzureManagementBaseUrl}/.default";

        // We are using the api versions recommended by the azure api documentation - these differ for database and app service management
        private const string AzureAppSettingsApiVersion = "2016-08-01";
        private const string AzureDatabaseApiVersion = "2017-10-01-preview";

        private readonly IAzureAuthenticationClient azureAuthenticationClient;
        private readonly AzureManagementSettings settings;

        private readonly HttpClient httpClient;

        public AzureManagementClient(IOptions<AzureManagementSettings> azureFunctionOptions, IAzureAuthenticationClient azureAuthenticationClient)
        {
            this.azureAuthenticationClient = azureAuthenticationClient;
            settings = azureFunctionOptions.Value;

            httpClient = new HttpClient {BaseAddress = new Uri(AzureManagementBaseUrl)};
        }

        public async Task SetApplicationSetting(string appServiceName, string key, string value)
        {
            await Authenticate();

            var appSettings = await FetchAppSettings(appServiceName);

            appSettings[key] = value;

            await PostAppSettings(appServiceName, appSettings);
        }

        public async Task<DateTime> UpdateDatabaseSize(string databaseName, AzureDatabaseSize databaseSize)
        {
            await Authenticate();

            var updateSizeUrl = $"{GetDatabaseUrlPath(databaseName)}?api-version={AzureDatabaseApiVersion}";

            var response = await httpClient.PatchAsync(
                updateSizeUrl,
                new StringContent(GetDatabaseUpdateBody(databaseSize), Encoding.UTF8, "application/json")
            );

            if (!response.IsSuccessStatusCode)
            {
                throw new AzureManagementException();
            }

            return JsonConvert.DeserializeObject<UpdateDatabaseResponse>(await response.Content.ReadAsStringAsync()).startTime;
        }

        private string GetDatabaseUpdateBody(AzureDatabaseSize databaseSize)
        {
            string name;
            string tier;
            int capacity;

            switch (databaseSize)
            {
                case AzureDatabaseSize.S0:
                    name = tier = "Standard";
                    capacity = 10;
                    break;
                case AzureDatabaseSize.S3:
                    name = tier = "Standard";
                    capacity = 100;
                    break;
                case AzureDatabaseSize.S4:
                    name = tier = "Standard";
                    capacity = 200;
                    break;
                case AzureDatabaseSize.P15:
                    name = tier = "Premium";
                    capacity = 4000;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(databaseSize), databaseSize, null);
            }

            dynamic skuObject = new System.Dynamic.ExpandoObject();
            skuObject.name = name;
            skuObject.tier = tier;
            skuObject.capacity = capacity;

            dynamic body = new System.Dynamic.ExpandoObject();
            body.sku = skuObject;

            return JsonConvert.SerializeObject(body);
        }

        public async Task<IEnumerable<DatabaseOperation>> GetDatabaseOperations(string databaseName)
        {
            throw new NotImplementedException();
        }

        private async Task<Dictionary<string, string>> FetchAppSettings(string appServiceName)
        {
            var getAppSettingsUrl = $"{GetAppSettingsUrlPath(appServiceName)}/list?api-version={AzureAppSettingsApiVersion}";

            var responseMessage = await httpClient.PostAsync(getAppSettingsUrl, new StringContent(""));
            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new AzureManagementException();
            }

            var appSettings = JsonConvert.DeserializeObject<AppSettingsResponse>(await responseMessage.Content.ReadAsStringAsync()).properties;
            return appSettings;
        }

        private async Task PostAppSettings(string appServiceName, Dictionary<string, string> appSettings)
        {
            var updateSettingsBody = new UpdateSettingsBody {properties = appSettings};
            var postAppSettingsUrl = $"{GetAppSettingsUrlPath(appServiceName)}?api-version={AzureAppSettingsApiVersion}";
            var stringContent = new StringContent(JsonConvert.SerializeObject(updateSettingsBody), Encoding.UTF8, "application/json");

            var updateResponse = await httpClient.PutAsync(postAppSettingsUrl, stringContent);

            if (!updateResponse.IsSuccessStatusCode)
            {
                throw new AzureManagementException();
            }
        }

        private string GetAppSettingsUrlPath(string appServiceName)
        {
            return $"{GetResourceGroupUrlPath(settings.ResourceGroupName)}/providers/Microsoft.Web/sites/{appServiceName}/config/appsettings";
        }

        private string GetDatabaseUrlPath(string databaseName)
        {
            return
                $"{GetResourceGroupUrlPath(settings.DatabaseResourceGroupName)}/providers/Microsoft.Sql/servers/{settings.DatabaseServerName}/databases/{databaseName}";
        }

        private string GetResourceGroupUrlPath(string resourceGroupName)
        {
            return $"subscriptions/{settings.SubscriptionId}/resourceGroups/{resourceGroupName}";
        }

        private async Task Authenticate()
        {
            var authToken = await azureAuthenticationClient.GetAuthToken(AzureManagementScope);
            httpClient.DefaultRequestHeaders.Remove("Authorization");
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
        }
    }
}