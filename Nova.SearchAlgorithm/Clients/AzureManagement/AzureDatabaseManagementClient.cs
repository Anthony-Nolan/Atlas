using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Clients.AzureManagement.AzureApiModels.Database;
using Nova.SearchAlgorithm.Clients.AzureManagement.Extensions;
using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.Models.AzureManagement;
using Nova.SearchAlgorithm.Settings;

namespace Nova.SearchAlgorithm.Clients.AzureManagement
{
    public interface IAzureDatabaseManagementClient
    {
        /// <returns>The DateTime at which the scaling operation began</returns>
        Task<DateTime> TriggerDatabaseScaling(string databaseName, AzureDatabaseSize databaseSize);

        Task<IEnumerable<DatabaseOperation>> GetDatabaseOperations(string databaseName);
    }

    public class AzureDatabaseManagementClient : AzureManagementClientBase, IAzureDatabaseManagementClient
    {
        protected override string AzureApiVersion => "2017-10-01-preview";

        private readonly string databaseServerName;

        public AzureDatabaseManagementClient(
            IOptions<AzureDatabaseManagementSettings> azureSettings,
            IAzureAuthenticationClient azureAuthenticationClient) : base(azureSettings.Value, azureAuthenticationClient)
        {
            databaseServerName = azureSettings.Value.ServerName;
        }

        public async Task<DateTime> TriggerDatabaseScaling(string databaseName, AzureDatabaseSize databaseSize)
        {
            await Authenticate();

            var updateSizeUrl = $"{GetDatabaseUrlPath(databaseName)}?api-version={AzureApiVersion}";

            var response = await HttpClient.PatchAsync(
                updateSizeUrl,
                new StringContent(databaseSize.ToAzureApiUpdateBody(), Encoding.UTF8, "application/json")
            );

            if (!response.IsSuccessStatusCode)
            {
                throw new AzureManagementException();
            }

            return JsonConvert.DeserializeObject<UpdateDatabaseResponse>(await response.Content.ReadAsStringAsync()).startTime;
        }

        public async Task<IEnumerable<DatabaseOperation>> GetDatabaseOperations(string databaseName)
        {
            await Authenticate();

            var operationsUrl = $"{GetDatabaseUrlPath(databaseName)}/operations?api-version={AzureApiVersion}";

            var response = await HttpClient.GetAsync(operationsUrl);

            if (!response.IsSuccessStatusCode)
            {
                throw new AzureManagementException();
            }

            var operationsResponseData = JsonConvert.DeserializeObject<DatabaseOperationResponse>(await response.Content.ReadAsStringAsync());

            return operationsResponseData.value.Select(o => new DatabaseOperation
            {
                Operation = o.properties.operation,
                State = o.properties.state,
                PercentComplete = o.properties.percentComplete,
                DatabaseName = o.properties.databaseName,
                StartTime = o.properties.startTime,
            });
        }

        private string GetDatabaseUrlPath(string databaseName)
        {
            return $"{GetResourceGroupUrlPath()}/providers/Microsoft.Sql/servers/{databaseServerName}/databases/{databaseName}";
        }
    }
}