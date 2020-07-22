using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.Common.Notifications;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import;
using LazyCache;
using HaplotypeHla = Atlas.Common.GeneticData.PhenotypeInfo.LociInfo<string>;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies
{
    public interface IHaplotypeFrequencyService
    {
        public Task ImportFrequencySet(FrequencySetFile file);
        public Task<HaplotypeFrequencySetResponse> GetHaplotypeFrequencySets(FrequencySetMetadata donorInfo, FrequencySetMetadata patientInfo);
        public Task<HaplotypeFrequencySet> GetSingleHaplotypeFrequencySet(FrequencySetMetadata setMetaData);

        Task<Dictionary<HaplotypeHla, decimal>> GetAllHaplotypeFrequencies(int setId);
    }

    internal class HaplotypeFrequencyService : IHaplotypeFrequencyService
    {
        private const string SupportSummaryPrefix = "Haplotype Frequency Set Import";

        private readonly IFrequencySetImporter frequencySetImporter;
        private readonly INotificationSender notificationSender;
        private readonly ILogger logger;
        private readonly IHaplotypeFrequencySetRepository frequencySetRepository;
        private readonly IHaplotypeFrequenciesRepository frequencyRepository;
        private readonly IAppCache cache;

        public HaplotypeFrequencyService(
            IFrequencySetImporter frequencySetImporter,
            IHaplotypeFrequencySetRepository frequencySetRepository,
            IHaplotypeFrequenciesRepository frequencyRepository,
            INotificationSender notificationSender,
            // ReSharper disable once SuggestBaseTypeForParameter
            IMatchPredictionLogger logger,
            // ReSharper disable once SuggestBaseTypeForParameter
            IPersistentCacheProvider persistentCacheProvider)
        {
            this.frequencySetImporter = frequencySetImporter;
            this.notificationSender = notificationSender;
            this.logger = logger;
            this.frequencySetRepository = frequencySetRepository;
            this.frequencyRepository = frequencyRepository;
            cache = persistentCacheProvider.Cache;
        }

        public async Task ImportFrequencySet(FrequencySetFile file)
        {
            try
            {
                await frequencySetImporter.Import(file);
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
            donorInfo ??= new FrequencySetMetadata();
            patientInfo ??= new FrequencySetMetadata();

            // Patients should use the donors registry.
            patientInfo.RegistryCode ??= donorInfo.RegistryCode;
            var donorSet = await GetSingleHaplotypeFrequencySet(donorInfo);
            var patientSet = await GetSingleHaplotypeFrequencySet(patientInfo);

            // If the patient's registry code was provided but not recognised, patientSet will end up using the global haplotype frequency set.
            // Instead, use the haplotype frequency set with the donor's registry code, if one was found.
            if (patientSet.RegistryCode == null && donorSet.RegistryCode != null)
            {
                patientInfo.RegistryCode = donorInfo.RegistryCode;
                patientSet = await GetSingleHaplotypeFrequencySet(patientInfo);
            }


            return new HaplotypeFrequencySetResponse
            {
                DonorSet = donorSet,
                PatientSet = patientSet
            };
        }

        public async Task<HaplotypeFrequencySet> GetSingleHaplotypeFrequencySet(FrequencySetMetadata setMetaData)
        {
            // Attempt to get the most specific sets first
            var set = await frequencySetRepository.GetActiveSet(setMetaData.RegistryCode, setMetaData.EthnicityCode);

            // If we didn't find ethnicity sets, find a generic one for that repository
            set ??= await frequencySetRepository.GetActiveSet(setMetaData.RegistryCode, null);

            // If no registry specific set exists, use a generic one.
            set ??= await frequencySetRepository.GetActiveSet(null, null);
            if (set == null)
            {
                logger.SendTrace(
                    $"Did not find Haplotype Frequency Set for: Registry: {setMetaData.RegistryCode} Donor Ethnicity: {setMetaData.EthnicityCode}",
                    LogLevel.Error);
                throw new Exception("No Global Haplotype frequency set was found");
            }

            return MapDataModelToClientModel(set);
        }

        /// <inheritdoc />
        public async Task<Dictionary<HaplotypeHla, decimal>> GetAllHaplotypeFrequencies(int setId)
        {
            var cacheKey = $"hf-set-{setId}";
            return await cache.GetOrAddAsync(cacheKey, async () => await frequencyRepository.GetAllHaplotypeFrequencies(setId));
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