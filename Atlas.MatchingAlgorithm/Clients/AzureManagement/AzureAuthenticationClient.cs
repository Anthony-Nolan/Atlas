using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Clients.AzureManagement.AzureApiModels;
using Atlas.MatchingAlgorithm.Exceptions.Azure;
using Atlas.MatchingAlgorithm.Settings.Azure;
using Newtonsoft.Json;

namespace Atlas.MatchingAlgorithm.Clients.AzureManagement
{
    public interface IAzureAuthenticationClient
    {
        Task<string> GetAuthToken(string scope);
    }

    public class AzureAuthenticationClient : IAzureAuthenticationClient
    {
        private readonly HttpClient httpClient;
        private readonly AzureAuthenticationSettings settings;

        public AzureAuthenticationClient(AzureAuthenticationSettings azureFunctionOptions)
        {
            settings = azureFunctionOptions;

            httpClient = new HttpClient {BaseAddress = new Uri(azureFunctionOptions.OAuthBaseUrl)};
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
        }

        public async Task<string> GetAuthToken(string scope)
        {
            var values = new Dictionary<string, string>
            {
                {"client_id", settings.ClientId},
                {"client_secret", settings.ClientSecret},
                {"grant_type", "client_credentials"},
                {"scope", scope},
            };
            var content = new FormUrlEncodedContent(values);

            var response = await httpClient.PostAsync("", content);
            if (!response.IsSuccessStatusCode)
            {
                throw new AzureAuthorisationException();
            }

            var responseBody = JsonConvert.DeserializeObject<OAuthResponse>(await response.Content.ReadAsStringAsync());
            return responseBody.AccessToken;
        }
    }
}