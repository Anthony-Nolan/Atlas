using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Clients.AzureManagement.AzureApiModels.Database;
using Atlas.MatchingAlgorithm.Clients.AzureManagement.Extensions;
using Atlas.MatchingAlgorithm.Exceptions.Azure;
using Atlas.MatchingAlgorithm.Models.AzureManagement;
using Atlas.MatchingAlgorithm.Settings.Azure;
using Newtonsoft.Json;

namespace Atlas.MatchingAlgorithm.Clients.AzureManagement
{
    public interface IAzureDatabaseManagementClient
    {
        /// <returns>The DateTime at which the scaling operation began</returns>
        Task<DateTime> TriggerDatabaseScaling(string databaseName, AzureDatabaseSize databaseSize, int? autoPauseDuration);

        Task<IEnumerable<DatabaseOperation>> GetDatabaseOperations(string databaseName);
    }

    
    /// <summary>
    /// See Azure documentation for this API: https://docs.microsoft.com/en-us/rest/api/sql/databases
    /// </summary>
    public class AzureDatabaseManagementClient : AzureManagementClientBase, IAzureDatabaseManagementClient
    {
        protected override string AzureApiVersion => "2019-06-01-preview";

        private readonly string databaseServerName;

        public AzureDatabaseManagementClient(
            AzureDatabaseManagementSettings azureSettings,
            IAzureAuthenticationClient azureAuthenticationClient) : base(azureSettings, azureAuthenticationClient)
        {
            databaseServerName = azureSettings.ServerName;
        }

        public async Task<DateTime> TriggerDatabaseScaling(string databaseName, AzureDatabaseSize databaseSize, int? autoPauseDuration)
        {
            await Authenticate();

            var updateSizeUrl = $"{GetDatabaseUrlPath(databaseName)}?api-version={AzureApiVersion}";

            var response = await HttpClient.PatchAsync(
                updateSizeUrl,
                new StringContent(databaseSize.ToAzureApiUpdateBody(autoPauseDuration), Encoding.UTF8, "application/json")
            );

            if (!response.IsSuccessStatusCode)
            {
                throw new AzureManagementException(
                    $"Failed to trigger database scaling of {databaseName} to size {databaseSize}. {await response.Content.ReadAsStringAsync()}");
            }

            return JsonConvert.DeserializeObject<UpdateDatabaseResponse>(await response.Content.ReadAsStringAsync()).StartTime;
        }

        public async Task<IEnumerable<DatabaseOperation>> GetDatabaseOperations(string databaseName)
        {
            await Authenticate();

            var operationsUrl = $"{GetDatabaseUrlPath(databaseName)}/operations?api-version={AzureApiVersion}";

            var response = await HttpClient.GetAsync(operationsUrl);

            if (!response.IsSuccessStatusCode)
            {
                throw new AzureManagementException(
                    $"Failed to fetch ongoing database operations for {databaseName}. {await response.Content.ReadAsStringAsync()}");
            }

            var operationsResponseData = JsonConvert.DeserializeObject<DatabaseOperationResponse>(await response.Content.ReadAsStringAsync());

            return operationsResponseData.Value.Select(o => new DatabaseOperation
            {
                Operation = o.Properties.Operation,
                State = o.Properties.State,
                PercentComplete = o.Properties.PercentComplete,
                DatabaseName = o.Properties.DatabaseName,
                StartTime = o.Properties.StartTime,
            });
        }

        private string GetDatabaseUrlPath(string databaseName)
        {
            return $"{GetResourceGroupUrlPath()}/providers/Microsoft.Sql/servers/{databaseServerName}/databases/{databaseName}";
        }
    }
}