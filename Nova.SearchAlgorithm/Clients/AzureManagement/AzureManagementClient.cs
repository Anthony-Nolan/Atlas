using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.Settings;

namespace Nova.SearchAlgorithm.Clients.AzureManagement
{
    public interface IAzureManagementClient
    {
        Task SetApplicationSetting(string appServiceName, string key, string value);
    }

    public class AzureManagementClient : IAzureManagementClient
    {
        private const string AzureManagementBaseUrl = "https://management.azure.com";
        private static readonly string AzureManagementScope = $"{AzureManagementBaseUrl}/.default";
        private const string AzureApiVersion = "2016-08-01";

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

        private async Task<Dictionary<string, string>> FetchAppSettings(string appServiceName)
        {
            var getAppSettingsUrl = $"{GetAppSettingsUrlPath(appServiceName)}/list?api-version={AzureApiVersion}";

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
            var postAppSettingsUrl = $"{GetAppSettingsUrlPath(appServiceName)}?api-version={AzureApiVersion}";
            var stringContent = new StringContent(JsonConvert.SerializeObject(updateSettingsBody), Encoding.UTF8, "application/json");
            
            var updateResponse = await httpClient.PutAsync(postAppSettingsUrl, stringContent);

            if (!updateResponse.IsSuccessStatusCode)
            {
                throw new AzureManagementException();
            }
        }

        private string GetAppSettingsUrlPath(string appServiceName)
        {
            return
                $"subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroupName}/providers/Microsoft.Web/sites/{appServiceName}/config/appsettings";
        }

        private async Task Authenticate()
        {
            var authToken = await azureAuthenticationClient.GetAuthToken(AzureManagementScope);
            httpClient.DefaultRequestHeaders.Remove("Authorization");
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
        }
    }

    internal class AppSettingsResponse
    {
        public Dictionary<string, string> properties { get; set; }
    }

    internal class UpdateSettingsBody
    {
        public string kind { get; set; }
        public Dictionary<string, string> properties { get; set; }
    }
}