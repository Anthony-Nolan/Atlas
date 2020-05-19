using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Clients.AzureManagement.AzureApiModels.AppSettings;
using Atlas.MatchingAlgorithm.ConfigSettings;
using Atlas.MatchingAlgorithm.Exceptions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Atlas.MatchingAlgorithm.Clients.AzureManagement
{
    public interface IAzureAppServiceManagementClient
    {
        Task SetApplicationSetting(string appServiceName, string key, string value);
    }

    public class AzureAppServiceManagementClient : AzureManagementClientBase, IAzureAppServiceManagementClient
    {
        protected override string AzureApiVersion => "2016-08-01";

        public AzureAppServiceManagementClient(
            IOptions<AzureAppServiceManagementSettings> azureSettings,
            IAzureAuthenticationClient azureAuthenticationClient
        ) : base(azureSettings.Value, azureAuthenticationClient)
        {
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

            var responseMessage = await HttpClient.PostAsync(getAppSettingsUrl, new StringContent(""));
            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new AzureManagementException(
                    $"Failed to fetch app settings for {appServiceName}. {await responseMessage.Content.ReadAsStringAsync()}");
            }

            return JsonConvert.DeserializeObject<AppSettingsResponse>(await responseMessage.Content.ReadAsStringAsync()).Properties;
        }

        private async Task PostAppSettings(string appServiceName, Dictionary<string, string> appSettings)
        {
            var updateSettingsBody = new UpdateSettingsBody {Properties = appSettings};
            var postAppSettingsUrl = $"{GetAppSettingsUrlPath(appServiceName)}?api-version={AzureApiVersion}";
            var stringContent = new StringContent(JsonConvert.SerializeObject(updateSettingsBody), Encoding.UTF8, "application/json");

            var updateResponse = await HttpClient.PutAsync(postAppSettingsUrl, stringContent);

            if (!updateResponse.IsSuccessStatusCode)
            {
                throw new AzureManagementException(
                    $"Failed to update app settings for {appServiceName}. {await updateResponse.Content.ReadAsStringAsync()}");
            }
        }

        private string GetAppSettingsUrlPath(string appServiceName)
        {
            return $"{GetResourceGroupUrlPath()}/providers/Microsoft.Web/sites/{appServiceName}/config/appsettings";
        }
    }
}