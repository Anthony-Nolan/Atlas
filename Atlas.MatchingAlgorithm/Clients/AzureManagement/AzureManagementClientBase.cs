using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Atlas.MatchingAlgorithm.Clients.AzureManagement.AzureApiModels.AppSettings;
using Atlas.MatchingAlgorithm.Clients.AzureManagement.AzureApiModels.Database;
using Atlas.MatchingAlgorithm.Exceptions;
using Atlas.MatchingAlgorithm.Models.AzureManagement;
using Atlas.MatchingAlgorithm.ConfigSettings;

namespace Atlas.MatchingAlgorithm.Clients.AzureManagement
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