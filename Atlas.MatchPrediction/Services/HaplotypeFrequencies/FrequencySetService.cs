using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.Models;
using System;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies
{
    public interface IFrequencySetService
    {
        public Task ImportFrequencySet(FrequencySetFile file);
        public Task<HaplotypeFrequencySetResponse> GetHaplotypeFrequencySets(FrequencySetMetadata donorInfo, FrequencySetMetadata patientInfo);
        public Task<HaplotypeFrequencySet> GetSingleHaplotypeFrequencySet(FrequencySetMetadata setMetaData);
    }

    internal class FrequencySetService : IFrequencySetService
    {
        private const string SupportSummaryPrefix = "Haplotype Frequency Set Import";

        private readonly IFrequencySetImporter importer;
        private readonly INotificationSender notificationSender;
        private readonly ILogger logger;
        private readonly IHaplotypeFrequencySetRepository repository;

        public FrequencySetService(
            IFrequencySetImporter importer,
            IHaplotypeFrequencySetRepository repository,
            INotificationSender notificationSender,
            ILogger logger)
        {
            this.importer = importer;
            this.notificationSender = notificationSender;
            this.logger = logger;
            this.repository = repository;
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
        
        public async Task<HaplotypeFrequencySetResponse> GetHaplotypeFrequencySets(FrequencySetMetadata donorInfo, FrequencySetMetadata patientInfo)
        {
            // Patients should use the donors registry.
            patientInfo.RegistryCode ??= donorInfo.RegistryCode;

            var donorSet = await GetSingleHaplotypeFrequencySet(donorInfo);
            var patientSet = await GetSingleHaplotypeFrequencySet(patientInfo);

            patientSet.RegistryCode ??= donorSet.RegistryCode;
            
            return new HaplotypeFrequencySetResponse
            {
                DonorSet = donorSet,
                PatientSet = patientSet
            };
        }

        public async Task<HaplotypeFrequencySet> GetSingleHaplotypeFrequencySet(FrequencySetMetadata setMetaData)
        {
            // Attempt to get the most specific sets first
            var set = await repository.GetActiveSet(setMetaData.RegistryCode, setMetaData.EthnicityCode);
            
            // If we didn't find ethnicity sets, find a generic one for that repository
            set ??= await repository.GetActiveSet(setMetaData.RegistryCode, null);
            
            // If no registry specific set exists, use a generic one.
            set ??= await repository.GetActiveSet(null, null);
            if (set == null)
            {
                logger.SendTrace($"Did not find Haplotype Frequency Set for: Registry: {setMetaData.RegistryCode} Donor Ethnicity: {setMetaData.EthnicityCode}", LogLevel.Error);
                throw new Exception("No Global Haplotype frequency set was found");
            }

            return MapDataModelToClientModel(set);
        }
        
        private static HaplotypeFrequencySet MapDataModelToClientModel(Data.Models.HaplotypeFrequencySet set)
        {
            return new HaplotypeFrequencySet
            {
                EthnicityCode = set.EthnicityCode,
                Id = set.Id,
                Name = set.Name,
                RegistryCode = set.RegistryCode
            };
        }
        

        private async Task SendSuccessNotification(FrequencySetFile file)
        {
            var successName = $"{SupportSummaryPrefix} Succeeded";

            logger.SendEvent(new HaplotypeFrequencySetImportEventModel(successName, file));

            await notificationSender.SendNotification(
                successName,
                $"Import of file, '{file.FullPath}', has completed successfully.",
                NotificationConstants.OriginatorName);
        }

        private async Task SendErrorAlert(FrequencySetFile file, Exception ex)
        {
            var errorName = $"{SupportSummaryPrefix} Failure";

            logger.SendEvent(new ErrorEventModel(errorName, ex));

            await notificationSender.SendAlert(
                errorName,
                $"Import of file, '{file.FullPath}', failed with the following exception message: \"{ex.GetBaseException().Message}\". "
                    + "Full exception info has been logged to Application Insights.",
                Priority.High,
                NotificationConstants.OriginatorName);
        }
    }
}