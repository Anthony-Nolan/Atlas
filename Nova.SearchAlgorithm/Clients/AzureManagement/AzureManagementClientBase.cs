using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Clients.AzureManagement.AzureApiModels.AppSettings;
using Nova.SearchAlgorithm.Clients.AzureManagement.AzureApiModels.Database;
using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.Models.AzureManagement;
using Nova.SearchAlgorithm.Settings;

namespace Nova.SearchAlgorithm.Clients.AzureManagement
{
    public abstract class AzureManagementClientBase
    {
        private const string AzureManagementBaseUrl = "https://management.azure.com";

        private static readonly string AzureManagementScope = $"{AzureManagementBaseUrl}/.default";

        protected abstract string AzureApiVersion { get; }

        private readonly IAzureAuthenticationClient azureAuthenticationClient;
        private readonly AzureManagementSettings settings;

        protected readonly HttpClient HttpClient;

        protected AzureManagementClientBase(AzureManagementSettings azureManagementSettings, IAzureAuthenticationClient azureAuthenticationClient)
        {
            this.azureAuthenticationClient = azureAuthenticationClient;
            settings = azureManagementSettings;

            HttpClient = new HttpClient {BaseAddress = new Uri(AzureManagementBaseUrl)};
        }

        protected async Task Authenticate()
        {
            var authToken = await azureAuthenticationClient.GetAuthToken(AzureManagementScope);
            HttpClient.DefaultRequestHeaders.Remove("Authorization");
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
        }

        protected string GetResourceGroupUrlPath()
        {
            return $"subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroupName}";
        }
    }
}