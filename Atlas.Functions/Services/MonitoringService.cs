using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Functions.Models;
using Atlas.Functions.Settings;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Atlas.Functions.Services
{
    public interface IMonitoringService
    {
        Task<List<SearchRequestTrackingInfo>> GetAllOngoingSearches(IDurableOrchestrationClient durableOrchestrationClient);
    }

    internal class MonitoringService : IMonitoringService
    {
        private readonly AzureStorageSettings messagingServiceBusSettings;

        private readonly BlobMetadataAnalyser blobMetadata;

        public MonitoringService(BlobMetadataAnalyser blobMetadata, AzureStorageSettings messagingServiceBusSettings)
        {
            this.blobMetadata = blobMetadata;
            this.messagingServiceBusSettings = messagingServiceBusSettings;
        }

        public async Task<List<SearchRequestTrackingInfo>> GetAllOngoingSearches(IDurableOrchestrationClient durableOrchestrationClient)
        {
            var queryFilter = new OrchestrationStatusQueryCondition
            {
                RuntimeStatus = new[] { OrchestrationRuntimeStatus.Pending, OrchestrationRuntimeStatus.Running }
            };

            var ongoingOrchestrations = await durableOrchestrationClient.ListInstancesAsync(queryFilter, CancellationToken.None);

            var summaries = new List<SearchRequestTrackingInfo>();
            foreach (var orchestration in ongoingOrchestrations.DurableOrchestrationState)
            {
                summaries.Add(await GenerateSummaryForSearch(orchestration));
            }

            return summaries.OrderBy(s => s.OrchestrationStarted).ToList();
        }

        private async Task<SearchRequestTrackingInfo> GenerateSummaryForSearch(DurableOrchestrationStatus durableOrchestrationStatus)
        {
            var matchingResultsNotification = durableOrchestrationStatus.Input.ToObject<MatchingResultsNotification>();
            var searchRequestId = matchingResultsNotification?.SearchRequestId;
            // This seems to be super slow!
            var numberOfPredictionResults =
                await blobMetadata.NumberOfFilesInFolder(messagingServiceBusSettings.MatchPredictionResultsBlobContainer, searchRequestId);
            var orchestrationStatus = durableOrchestrationStatus.CustomStatus.ToString();
            return new SearchRequestTrackingInfo
            {
                OrchestrationStatus = orchestrationStatus,
                SearchRequestId = searchRequestId,
                MatchPredictionCompletedDonorCount = numberOfPredictionResults,
                OrchestrationStarted = durableOrchestrationStatus.CreatedTime,
                RuntimeStatus = durableOrchestrationStatus.RuntimeStatus,
                NumberOfResults = matchingResultsNotification?.NumberOfResults
            };
        }
    }
}