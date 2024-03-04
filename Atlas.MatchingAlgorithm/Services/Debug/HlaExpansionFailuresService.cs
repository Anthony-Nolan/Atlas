using Atlas.MatchingAlgorithm.Settings.Azure;
using Azure.Monitor.Query;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Services.Debug
{
    public interface IHlaExpansionFailuresService
    {
        /// <summary>
        /// Returns Hla Expansion Failures from logs for last <paramref name="daysToQuery"/> days
        /// </summary>
        /// <param name="daysToQuery"></param>
        /// <returns></returns>
        public Task<object> Query(int daysToQuery);
    }

    public class HlaExpansionFailuresService : IHlaExpansionFailuresService
    {
        private readonly LogsQueryClient logsQueryClient;
        private readonly AzureMonitoringSettings azureMonitoringSettings;

        private const string HlaExpansionFailuresQuery = @"
            AppEvents
            | where Name startswith ""HLA Expansion""
            | extend
                DonorInfo = parse_json(tostring(Properties[""DonorInfo""])),
                Locus = tostring(Properties[""Locus""]),
                HlaName = tostring(Properties[""HlaName""])
            | distinct
                InvalidHLA = strcat(Locus, HlaName),
                ExternalDonorCode = tostring(DonorInfo[""ExternalDonorCode""]),
                ExceptionType = tostring(Properties[""InnerExceptionType""])
            | summarize
                ExceptionType = make_list(ExceptionType, 10)[0],
                ExternalDonorCodes = make_list(ExternalDonorCode, 1000),
                DonorCount = count() by InvalidHLA
            | order by DonorCount desc";

        public HlaExpansionFailuresService(LogsQueryClient logsQueryClient, IOptions<AzureMonitoringSettings> azureMonitoringSettings)
        {
            this.logsQueryClient = logsQueryClient;
            this.azureMonitoringSettings = azureMonitoringSettings.Value;
        }

        public async Task<object> Query(int daysToQuery)
        {
            var response = await logsQueryClient.QueryWorkspaceAsync(azureMonitoringSettings.WorkspaceId, HlaExpansionFailuresQuery, new QueryTimeRange(TimeSpan.FromDays(daysToQuery)));
            var result = response.Value;
            var output = new JArray();

            foreach (var row in result.Table.Rows)
            {
                var outputRow = new JObject();

                foreach (var (name, value) in result.Table.Columns.Select(col => (name: col.Name, value: row[col.Name])))
                {
                    outputRow.Add(
                        name,
                        value is BinaryData binaryData ? JToken.FromObject(binaryData.ToString()) : JToken.FromObject(value));
                }


                output.Add(outputRow);
            }

            return output;
        }
    }
}
