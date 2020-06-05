using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.Common.Notifications.MessageModels;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.Models;
using System;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies
{
    public interface IFrequencySetService
    {
        public Task ImportFrequencySet(FrequencySetFile file);
    }

    internal class FrequencySetService : IFrequencySetService
    {
        private const string SummaryPrefix = "Haplotype Frequency Set Import";

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

        public async Task ImportFrequencySet(FrequencySetFile file)
        {
            try
            {
                var metaData = metadataExtractor.GetMetadataFromFileName(file.FileName);
                await importer.Import(metaData, file.Contents);
                file.ImportedDateTime = DateTimeOffset.UtcNow;

                await SendSuccessNotification(file);
            }
            catch (Exception ex)
            {
                await SendErrorAlert(file, ex);
                throw;
            }
        }

        private async Task SendSuccessNotification(FrequencySetFile file)
        {
            var successName = $"{SummaryPrefix} Succeeded";

            logger.SendEvent(new HaplotypeFrequencySetImportEventModel(successName, file));

            await notificationsClient.SendNotification(new Notification(
                successName,
                $"Import of file, '{file.FileName}', has completed successfully.",
                NotificationConstants.OriginatorName));
        }

        private async Task SendErrorAlert(FrequencySetFile file, Exception ex)
        {
            var errorName = $"{SummaryPrefix} Failure in the Match Prediction component";

            logger.SendEvent(new ErrorEventModel(errorName, ex));

            await notificationsClient.SendAlert(new Alert(
                errorName,
                $"Import of file, '{file.FileName}', failed with the following exception message: \"{ex.InnermostMessage()}\". "
                    + "Full exception info has been logged to Application Insights.",
                Priority.High,
                NotificationConstants.OriginatorName));
        }
    }
}