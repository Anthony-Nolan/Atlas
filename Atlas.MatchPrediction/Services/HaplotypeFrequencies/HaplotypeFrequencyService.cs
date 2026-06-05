using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.Common.Notifications;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import;
using LazyCache;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.SupportMessages;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using HaplotypeFrequencySet = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet.HaplotypeFrequencySet;
using HaplotypeHla = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.LociInfo<string>;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies
{
    public interface IHaplotypeFrequencyService
    {
        /// <summary>
        /// Imports all haplotype frequencies from a given frequency set file.
        /// </summary>
        /// <param name="file">Contains both the haplotype frequencies as file contents, as well as metadata about the file itself.</param>
        /// <param name="importBehaviour"></param>
        /// <returns></returns>
        public Task ImportFrequencySet(FrequencySetFile file, FrequencySetImportBehaviour importBehaviour = null);

        public Task<HaplotypeFrequencySetResponse> GetHaplotypeFrequencySets(FrequencySetMetadata donorInfo, FrequencySetMetadata patientInfo);

        public Task<HaplotypeFrequencySet> GetSingleHaplotypeFrequencySet(FrequencySetMetadata setMetaData);

        Task<ConcurrentDictionary<HaplotypeHla, HaplotypeFrequency>> GetAllHaplotypeFrequencies(int setId);

        /// <param name="setId"></param>
        /// <param name="hla"></param>
        /// <param name="excludedLoci">
        /// Any loci specified here will not be considered when fetching frequencies.
        /// If multiple haplotypes match the provided hla at all other loci, such frequencies will be summed. 
        /// </param>
        /// <returns>
        /// The haplotype frequency for the given haplotype hla, from the given set.
        /// If the given hla is unrepresented in the set, will return 0. 
        /// </returns>
        // ReSharper disable once ParameterTypeCanBeEnumerable.Global
        Task<decimal> GetFrequencyForHla(int setId, HaplotypeHla hla, ISet<Locus> excludedLoci);
    }

    internal class HaplotypeFrequencyService : IHaplotypeFrequencyService
    {
        private const string SupportSummaryPrefix = "Haplotype Frequency Set Import";
        private const string ActiveHaplotypeFrequencySetsCacheKey = "hf-active-sets";

        private readonly IFrequencySetImporter frequencySetImporter;
        private readonly INotificationSender notificationSender;
        private readonly IAtlasLogger logger;
        private readonly IFrequencyConsolidator frequencyConsolidator;
        private readonly IHaplotypeFrequencySetRepository frequencySetRepository;
        private readonly IHaplotypeFrequenciesRepository frequencyRepository;
        private readonly IPersistentCacheProvider persistentCacheProvider;
        private readonly HaplotypeFrequencySetCacheSettings haplotypeFrequencySetCacheSettings;

        public HaplotypeFrequencyService(
            IFrequencySetImporter frequencySetImporter,
            IHaplotypeFrequencySetRepository frequencySetRepository,
            IHaplotypeFrequenciesRepository frequencyRepository,
            INotificationSender notificationSender,
            IMatchPredictionLogger<MatchProbabilityLoggingContext> logger,
            IPersistentCacheProvider persistentCacheProvider,
            IFrequencyConsolidator frequencyConsolidator,
            IOptions<HaplotypeFrequencySetCacheSettings> haplotypeFrequencySetCacheSettings
        )
        {
            this.frequencySetImporter = frequencySetImporter;
            this.notificationSender = notificationSender;
            this.logger = logger;
            this.frequencyConsolidator = frequencyConsolidator;
            this.frequencySetRepository = frequencySetRepository;
            this.frequencyRepository = frequencyRepository;
            this.haplotypeFrequencySetCacheSettings = haplotypeFrequencySetCacheSettings.Value;
            this.persistentCacheProvider = persistentCacheProvider;
        }

        public async Task ImportFrequencySet(FrequencySetFile file, FrequencySetImportBehaviour importBehaviour)
        {
            importBehaviour ??= new FrequencySetImportBehaviour();

            try
            {
                await frequencySetImporter.Import(file, importBehaviour);
                persistentCacheProvider.Cache.Remove(ActiveHaplotypeFrequencySetsCacheKey);
                file.ImportedDateTime = DateTimeOffset.UtcNow;

                await SendSuccessNotification(file);
            }
            catch (EmptyHaplotypeFileException ex)
            {
                const string summary = "Haplotype file was present but it was empty.";
                await LogErrorAndSendAlert(file, summary, ex.StackTrace);
            }
            catch (MalformedHaplotypeFileException ex)
            {
                await LogErrorAndSendAlert(file, ex.Message, ex.StackTrace);
            }
            catch (HaplotypeFormatException ex)
            {
                await LogErrorAndSendAlert(file, ex.Message, ex.InnerException?.Message);
            }
            catch (DuplicateHaplotypeImportException ex)
            {
                await LogErrorAndSendAlert(file, ex.Message, ex.StackTrace);
            }
            catch (HlaMetadataDictionaryException ex)
            {
                await LogErrorAndSendAlert(file, $"{ex.Message} Locus: {ex.Locus}, GGroup: {ex.HlaName}", ex.StackTrace);
            }
            catch (Exception ex)
            {
                await SendErrorAlert(file, ex);
                throw;
            }
        }

        public async Task<HaplotypeFrequencySetResponse> GetHaplotypeFrequencySets(FrequencySetMetadata donorInfo, FrequencySetMetadata patientInfo)
        {
            using (logger.RunTimed("Get HF Sets", LogLevel.Verbose))
            {
                donorInfo ??= new FrequencySetMetadata();
                patientInfo ??= new FrequencySetMetadata();

                var donorSet = await GetSingleHaplotypeFrequencySet(donorInfo);
                var patientSet = await GetSingleHaplotypeFrequencySet(patientInfo);

                logger.SendTrace($"Frequency Set Selection: Donor {donorSet.RegistryCode}/{donorSet.EthnicityCode}/{donorSet.Id}");
                logger.SendTrace($"Frequency Set Selection: Patient {patientSet.RegistryCode}/{patientSet.EthnicityCode}/{patientSet.Id}");

                return new HaplotypeFrequencySetResponse
                {
                    DonorSet = donorSet,
                    PatientSet = patientSet
                };
            }
        }

        public async Task<HaplotypeFrequencySet> GetSingleHaplotypeFrequencySet(FrequencySetMetadata setMetaData)
        {
            var activeSets = await GetActiveHaplotypeFrequencySets();

            // Attempt to get the most specific sets first
            var set = activeSets.GetValueOrDefault((setMetaData.RegistryCode, setMetaData.EthnicityCode));

            // If we didn't find ethnicity sets, find a generic one for that repository
            set ??= activeSets.GetValueOrDefault((setMetaData.RegistryCode, (string)null));

            // If no registry specific set exists, use a generic one.
            set ??= activeSets.GetValueOrDefault(((string)null, (string)null));
            if (set == null)
            {
                logger.SendTrace(
                    $"Did not find Haplotype Frequency Set for: Registry: {setMetaData.RegistryCode} Donor Ethnicity: {setMetaData.EthnicityCode}",
                    LogLevel.Error
                );
                throw new Exception("No Global Haplotype frequency set was found");
            }

            return set;
        }

        /// <inheritdoc />
        public async Task<ConcurrentDictionary<HaplotypeHla, HaplotypeFrequency>> GetAllHaplotypeFrequencies(int setId)
        {
            var cacheKey = $"hf-set-{setId}";
            return await persistentCacheProvider.Cache.GetOrAddAsync(cacheKey, async () =>
                {
                    using (logger.RunTimed("Get All Frequencies from HF set - from SQL database", LogLevel.Verbose))
                    {
                        var allFrequencies = await frequencyRepository.GetAllHaplotypeFrequencies(setId);
                        return new ConcurrentDictionary<HaplotypeHla, HaplotypeFrequency>(allFrequencies);
                    }
                }
            );
        }

        /// <inheritdoc />
        public async Task<decimal> GetFrequencyForHla(int setId, HaplotypeHla hla, ISet<Locus> excludedLoci)
        {
            var frequencies = await GetAllHaplotypeFrequencies(setId);

            if (!frequencies.TryGetValue(hla, out var frequency))
            {
                // If no loci are excluded, there is nothing to calculate - the haplotype is just unrepresented.
                // We do not want to add all unrepresented haplotypes to the cache - this drastically reduces algorithm speed, increases memory, and has no benefit
                if (!excludedLoci.Any())
                {
                    return 0;
                }

                return await GetConsolidatedFrequency(setId, hla, excludedLoci);
            }

            return frequency?.Frequency ?? 0;
        }

        /// <summary>
        /// "Consolidated frequencies" are haplotypes that do not have an associated record in the stored haplotype set,
        /// but have been calculated by consolidating other frequencies.
        /// </summary>
        private async Task<decimal> GetConsolidatedFrequency(int setId, HaplotypeHla hla, ISet<Locus> excludedLoci)
        {
            var cacheKey = $"hf-set-consolidated-{setId}";
            // It is significantly faster to calculate all consolidated values up front than to calculate on the fly, even when caching individual values. 
            // Many consolidated haplotypes may be inferable from the input data, but not actually represented in the haplotype frequency dataset  
            return await persistentCacheProvider.Cache.GetSingleItemAndScheduleWholeCollectionCacheWarm(
                cacheKey,
                async () =>
                {
                    using (logger.RunTimed($"Calculating consolidated frequencies with missing loci for set: {setId}"))
                    {
                        return frequencyConsolidator.PreConsolidateFrequencies(await GetAllHaplotypeFrequencies(setId));
                    }
                },
                d =>
                {
                    var hlaAtLoci = hla.SetLoci(null, excludedLoci.ToArray());
                    d.TryGetValue(hlaAtLoci, out var result);
                    return result;
                },
                async () =>
                {
                    var frequencies = (await GetAllHaplotypeFrequencies(setId));
                    return frequencyConsolidator.ConsolidateFrequenciesForHaplotype(frequencies, hla, excludedLoci);
                }
            );
        }

        private static HaplotypeFrequencySet MapDataModelToClientModel(Data.Models.HaplotypeFrequencySet set)
        {
            return new HaplotypeFrequencySet
            {
                HlaNomenclatureVersion = set.HlaNomenclatureVersion,
                EthnicityCode = set.EthnicityCode,
                Id = set.Id,
                Name = set.Name,
                RegistryCode = set.RegistryCode,
                PopulationId = set.PopulationId
            };
        }

        private async Task<IReadOnlyDictionary<(string RegistryCode, string EthnicityCode), HaplotypeFrequencySet>> GetActiveHaplotypeFrequencySets()
        {
            return await persistentCacheProvider.Cache.GetOrAddAsync(
                ActiveHaplotypeFrequencySetsCacheKey,
                async () =>
                {
                    using (logger.RunTimed("Get active HF sets - from SQL database", LogLevel.Verbose))
                    {
                        var activeSets = await frequencySetRepository.GetAllActiveSets();
                        return activeSets.ToDictionary(
                            set => (set.RegistryCode, set.EthnicityCode),
                            MapDataModelToClientModel
                        );
                    }
                },
                new MemoryCacheEntryOptions
                {

                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(haplotypeFrequencySetCacheSettings.ActiveSetCacheExpiryMinutes)
                }
            );
        }

        private async Task SendSuccessNotification(FrequencySetFile file)
        {
            var successName = $"{SupportSummaryPrefix} Succeeded";

            var timeSpan = file.ImportedDateTime - file.UploadedDateTime;
            var durationMs = timeSpan == null
                ? "Unknown"
                : ((int)Math.Round(timeSpan.Value.TotalMilliseconds)).ToString();

            var eventProperties = new Dictionary<string, string>
            {
                { nameof(file.FileName), file.FileName },
                { "TotalImportDurationInMs", durationMs },
            };

            if (file.UploadedDateTime != null)
            {
                eventProperties[nameof(file.UploadedDateTime)] = file.UploadedDateTime.Value.UtcDateTime.ToString(CultureInfo.InvariantCulture) + " UTC";
            }

            if (file.ImportedDateTime != null)
            {
                eventProperties[nameof(file.ImportedDateTime)] = file.ImportedDateTime.Value.UtcDateTime.ToString(CultureInfo.InvariantCulture) + " UTC";
            }

            logger.SendEvent(successName, LogLevel.Info, eventProperties);

            await notificationSender.SendNotification(
                successName,
                $"Import of file, '{file.FileName}', has completed successfully.",
                NotificationConstants.OriginatorName
            );
        }

        private async Task LogErrorAndSendAlert(FrequencySetFile file, string message, string description)
        {
            var messageWithName = $"Import of file '{file.FileName}': {message}";

            logger.SendTrace(messageWithName, LogLevel.Warn);
            await notificationSender.SendAlert(messageWithName, description, Priority.Medium, NotificationConstants.OriginatorName);
        }

        private async Task SendErrorAlert(FrequencySetFile file, Exception ex)
        {
            var errorName = $"{SupportSummaryPrefix} Failure";

            logger.SendException(ex, LogLevel.Error, new Dictionary<string, string>
            {
                { "FileName", file.FileName },
            });

            await notificationSender.SendAlert(
                errorName,
                $"Import of file, '{file.FileName}', failed with the following exception message: \"{ex.GetBaseException().Message}\". "
              + "Full exception info has been logged to Application Insights.",
                Priority.High,
                NotificationConstants.OriginatorName
            );
        }
    }
}