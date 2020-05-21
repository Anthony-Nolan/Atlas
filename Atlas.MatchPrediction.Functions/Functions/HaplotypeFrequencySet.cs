using Atlas.Common.ApplicationInsights;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Microsoft.Azure.WebJobs;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Functions.Functions
{
    public class HaplotypeFrequencySet
    {
        private readonly IHaplotypeFrequencySetMetaDataService metaDataService;
        private readonly IHaplotypeFrequencySetImportService importService;
        private readonly IFailedImportNotificationSender failedImportNotificationSender;
        private readonly ILogger logger;

        public HaplotypeFrequencySet(
            IHaplotypeFrequencySetMetaDataService metaDataService,
            IHaplotypeFrequencySetImportService importService,
            IFailedImportNotificationSender failedImportNotificationSender,
            ILogger logger)
        {
            this.metaDataService = metaDataService;
            this.importService = importService;
            this.failedImportNotificationSender = failedImportNotificationSender;
            this.logger = logger;
        }

        [FunctionName(nameof(ImportHaplotypeFrequencySet))]
        public async Task ImportHaplotypeFrequencySet(
            [BlobTrigger("%AzureStorage:HaplotypeFrequencySetImportContainer%/{fileName}", Connection = "AzureStorage:ConnectionString")] Stream blob,
            string fileName)
        {
            const string errorName = "Haplotype Frequency Set Import Failure in the Match Prediction component";

            try
            {
                var metaData = metaDataService.GetMetadataFromFileName(fileName);

                await importService.Import(metaData, blob);
            }
            catch (Exception ex)
            {
                logger.SendEvent(new ErrorEventModel(errorName, ex));
                await failedImportNotificationSender.SendFailedImportAlert(errorName, fileName, ex);
            }
        }
    }
}
