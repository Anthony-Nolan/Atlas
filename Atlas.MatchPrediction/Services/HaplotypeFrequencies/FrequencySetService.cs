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
        private const string SupportSummaryPrefix = "Haplotype Frequency Set Import";

        private readonly IFrequencySetImporter importer;
        private readonly INotificationsClient notificationsClient;
        private readonly ILogger logger;

        public FrequencySetService(
            IFrequencySetImporter importer,
            INotificationsClient notificationsClient,
            ILogger logger)
        {
            this.importer = importer;
            this.notificationsClient = notificationsClient;
            this.logger = logger;
        }

        public async Task ImportFrequencySet(FrequencySetFile file)
        {
            try
            {
                await importer.Import(file);
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
            var successName = $"{SupportSummaryPrefix} Succeeded";

            logger.SendEvent(new HaplotypeFrequencySetImportEventModel(successName, file));

            await notificationsClient.SendNotification(new Notification(
                successName,
                $"Import of file, '{file.FullPath}', has completed successfully.",
                NotificationConstants.OriginatorName));
        }

        private async Task SendErrorAlert(FrequencySetFile file, Exception ex)
        {
            var errorName = $"{SupportSummaryPrefix} Failure";

            logger.SendEvent(new ErrorEventModel(errorName, ex));

            await notificationsClient.SendAlert(new Alert(
                errorName,
                $"Import of file, '{file.FullPath}', failed with the following exception message: \"{ex.GetBaseException().Message}\". "
                    + "Full exception info has been logged to Application Insights.",
                Priority.High,
                NotificationConstants.OriginatorName));
        }
    }
}