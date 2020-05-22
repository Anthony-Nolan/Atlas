using System;
using System.IO;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.Common.Notifications.MessageModels;

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
            const string errorName = "Haplotype Frequency Set Import Failure in the Match Prediction component";

            try
            {
                var metaData = metadataExtractor.GetMetadataFromFileName(fileName);
                await importer.Import(metaData, stream);
            }
            catch (Exception ex)
            {
                logger.SendEvent(new ErrorEventModel(errorName, ex));
                await notificationsClient.SendAlert(new Alert(
                    errorName,
                    $"Import of file, '{fileName}', failed with the following exception message: {ex.Message} " +
                    "Full exception info has been logged to Application Insights.",
                    Priority.High));
                throw;
            }
        }
    }
}