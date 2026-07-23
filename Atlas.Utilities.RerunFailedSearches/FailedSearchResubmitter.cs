using Atlas.Client.Models.Search.Requests;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.RepeatSearch.Models;
using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;

namespace Atlas.Utilities.RerunFailedSearches
{
    public interface IFailedSearchResubmitter
    {
        Task ResubmitSearch(string originalSearchIdentifier, string requestJson, bool forcedParallelMatchPrediction);

        Task ResubmitRepeatSearch(string repeatSearchIdentifier, string originalSearchIdentifier, string requestJson, bool forcedParallelMatchPrediction);
    }

    /// <summary>
    /// Re-publishes a search directly onto the matching request topics, bypassing the Public API. The message
    /// shape (JSON body + application properties) mirrors <c>SearchServiceBusClient</c> /
    /// <c>RepeatSearchServiceBusClient</c> exactly so the existing consumers process it identically. The
    /// original search identifier is reused (not regenerated) so the re-run keeps the same identity.
    /// </summary>
    public class FailedSearchResubmitter : IFailedSearchResubmitter
    {
        private readonly ServiceBusClient client;
        private readonly RerunSettings settings;

        public FailedSearchResubmitter(ServiceBusClient client, RerunSettings settings)
        {
            this.client = client;
            this.settings = settings;
        }

        public async Task ResubmitSearch(string originalSearchIdentifier, string requestJson, bool forcedParallelMatchPrediction)
        {
            var searchRequest = JsonConvert.DeserializeObject<SearchRequest>(requestJson)
                ?? throw new InvalidOperationException($"Stored RequestJson for search {originalSearchIdentifier} did not deserialize to a SearchRequest.");
            searchRequest.ParallelMatchPrediction = forcedParallelMatchPrediction;

            var identifiedRequest = new IdentifiedSearchRequest
            {
                Id = originalSearchIdentifier,
                SearchRequest = searchRequest
            };

            var message = new ServiceBusMessage(BinaryData.FromString(JsonConvert.SerializeObject(identifiedRequest)))
            {
                ApplicationProperties =
                {
                    { nameof(IdentifiedSearchRequest) + nameof(IdentifiedSearchRequest.Id), originalSearchIdentifier }
                }
            };

            await using var sender = client.CreateSender(settings.SearchRequestsTopic);
            await sender.SendMessageAsync(message);
        }

        public async Task ResubmitRepeatSearch(string repeatSearchIdentifier, string originalSearchIdentifier, string requestJson, bool forcedParallelMatchPrediction)
        {
            var repeatSearchRequest = JsonConvert.DeserializeObject<RepeatSearchRequest>(requestJson)
                ?? throw new InvalidOperationException($"Stored RequestJson for repeat search {repeatSearchIdentifier} did not deserialize to a RepeatSearchRequest.");
            var searchRequest = repeatSearchRequest.SearchRequest
                ?? throw new InvalidOperationException($"Stored RequestJson for repeat search {repeatSearchIdentifier} did not contain a SearchRequest.");
            searchRequest.ParallelMatchPrediction = forcedParallelMatchPrediction;

            var identifiedRequest = new IdentifiedRepeatSearchRequest
            {
                RepeatSearchId = repeatSearchIdentifier,
                OriginalSearchId = originalSearchIdentifier,
                RepeatSearchRequest = repeatSearchRequest
            };

            var message = new ServiceBusMessage(BinaryData.FromString(JsonConvert.SerializeObject(identifiedRequest)));
            message.ApplicationProperties.Add("SearchRequestId", originalSearchIdentifier);
            message.ApplicationProperties.Add("RepeatSearchRequestId", repeatSearchIdentifier);

            await using var sender = client.CreateSender(settings.RepeatSearchRequestsTopic);
            await sender.SendMessageAsync(message);
        }
    }
}
