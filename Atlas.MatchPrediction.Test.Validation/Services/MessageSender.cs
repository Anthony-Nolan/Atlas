using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.ResultsUpload;
using Atlas.MatchPrediction.Test.Validation.Data.Repositories;
using Atlas.MatchPrediction.Test.Validation.Models;

namespace Atlas.MatchPrediction.Test.Validation.Services
{
    public interface IMessageSender
    {
        Task SendNotificationsForMissingResults();
    }

    internal class MessageSender : IMessageSender
    {
        private readonly IValidationRepository validationRepository;
        private readonly IBulkMessagePublisher<MatchPredictionResultLocation> messagePublisher;
        private readonly string resultsContainer;

        public MessageSender(
            IValidationRepository validationRepository,
            IBulkMessagePublisher<MatchPredictionResultLocation> messagePublisher,
            ValidationAzureStorageSettings settings)
        {
            this.validationRepository = validationRepository;
            this.messagePublisher = messagePublisher;
            resultsContainer = settings.MatchPredictionResultsBlobContainer;
        }

        public async Task SendNotificationsForMissingResults()
        {
            var algorithmIds = await validationRepository.GetAlgorithmIdsOfRequestsMissingResults();
            var locations = algorithmIds.Select(id => ResultLocationBuilder.BuildMatchPredictionRequestResultLocation(id, resultsContainer));
            await messagePublisher.BatchPublish(locations);
        }
    }
}
