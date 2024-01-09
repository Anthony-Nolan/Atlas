using System.Threading.Tasks;
using Atlas.Common.ServiceBus;
using Atlas.ManualTesting.Common.Services;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.ResultsUpload;
using Atlas.MatchPrediction.Test.Validation.Data.Repositories;

namespace Atlas.MatchPrediction.Test.Validation.Services.Exercise3
{
    public interface IMatchPredictionLocationSender
    {
        Task PublishLocationsForMatchPredictionRequestMissingResults();
    }

    internal class MatchPredictionLocationSender : MessageSender<MatchPredictionResultLocation>, IMatchPredictionLocationSender
    {
        private readonly IValidationRepository validationRepository;

        public MatchPredictionLocationSender(
            IValidationRepository validationRepository,
            IMessageBatchPublisher<MatchPredictionResultLocation> messagePublisher,
            string resultsBlobContainerName) : base(messagePublisher, resultsBlobContainerName)
        {
            this.validationRepository = validationRepository;
        }

        public async Task PublishLocationsForMatchPredictionRequestMissingResults()
        {
            var ids = await validationRepository.GetAlgorithmIdsOfMatchPredictionRequestsMissingResults();
            await BuildAndSendMessages(ids);
        }

        /// <inheritdoc />
        protected override MatchPredictionResultLocation BuildMessage(string requestId, string resultsBlobContainerName)
        {
            return ResultLocationBuilder.BuildMatchPredictionRequestResultLocation(requestId, resultsBlobContainerName);
        }
    }
}