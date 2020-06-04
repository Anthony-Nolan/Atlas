using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.Common.Notifications.MessageModels;
using Atlas.MatchPrediction.Config;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies
{
    public interface IFrequencySetService
    {
        public Task ImportFrequencySet(Stream stream, string fileName);
    }

    internal class FrequencySetService : IFrequencySetService
    {
        private readonly IFrequencySetMetadataExtractor metadataExtractor;
        private readonly IFrequencySetImporter importer;
        private readonly INotificationsClient notificationsClient;
        private readonly ILogger logger;

        public FrequencySetService(
            IFrequencySetMetadataExtractor metadataExtractor,
            IFrequencySetImporter importer,
            INotificationsClient notificationsClient,
            ILogger logger)
        {
            this.metadataExtractor = metadataExtractor;
            this.importer = importer;
            this.notificationsClient = notificationsClient;
            this.logger = logger;
        }

        public async Task ImportFrequencySet(Stream stream, string fileName)
        {
            const string supportSummaryPrefix = "Haplotype Frequency Set Import";

            try
            {
                var metaData = metadataExtractor.GetMetadataFromFileName(fileName);

                await importer.Import(metaData, stream);

                await notificationsClient.SendNotification(new Notification(
                    $"{supportSummaryPrefix} Succeeded",
                    $"Import of file, '{fileName}', has completed successfully.",
                    NotificationConstants.OriginatorName));
            }
            catch (Exception ex)
            {
                var errorName = $"{supportSummaryPrefix} Failure in the Match Prediction component";

                logger.SendEvent(new ErrorEventModel(errorName, ex));

                await notificationsClient.SendAlert(new Alert(
                    errorName,
                    $"Import of file, '{fileName}', failed with the following exception message: {ex.Message} " +
                    "Full exception info has been logged to Application Insights.",
                    Priority.High,
                    NotificationConstants.OriginatorName));

                throw;
            }
        }
    }
}