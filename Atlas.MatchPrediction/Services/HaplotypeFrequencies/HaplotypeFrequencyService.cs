using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.Common.Notifications;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import;
using LazyCache;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import.Exceptions;
using HaplotypeFrequencySet = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet.HaplotypeFrequencySet;
using HaplotypeHla = Atlas.Common.GeneticData.PhenotypeInfo.LociInfo<string>;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies
{
    public interface IHaplotypeFrequencyService
    {
        /// <summary>
        /// Imports all haplotype frequencies from a given frequency set file.
        /// </summary>
        /// <param name="file">Contains both the haplotype frequencies as file contents, as well as metadata about the file itself.</param>
        /// <param name="convertToPGroups">
        /// When set, the import process will convert haplotypes to PGroup typing where possible (i.e. when haplotype has no null expressing GGroups).
        /// For any haplotypes that are different at G-Group level, but the same at P-Group, frequency values will be consolidated.
        ///
        /// Defaults to true, as this yields a significantly faster algorithm.
        ///
        /// When set to false, all frequencies will be imported at the original G-Group resolutions.
        /// This is only expected to be used in test code, where it is much easier to keep track of a single set of frequencies,
        /// than of GGroup typed haplotypes *and* their corresponding P-Group typed ones.  
        /// </param>
        /// <returns></returns>
        public Task ImportFrequencySet(FrequencySetFile file, bool convertToPGroups = true);

        public Task<HaplotypeFrequencySetResponse> GetHaplotypeFrequencySets(FrequencySetMetadata donorInfo, FrequencySetMetadata patientInfo);
        public Task<HaplotypeFrequencySet> GetSingleHaplotypeFrequencySet(FrequencySetMetadata setMetaData);

        Task<ConcurrentDictionary<HaplotypeHla, HaplotypeFrequency>> GetAllHaplotypeFrequencies(int setId);

        Task<ConcurrentDictionary<HaplotypeHla, HaplotypeFrequency>> GetHaplotypeFrequencies(
            int setId,
            PhenotypeInfo<IReadOnlyCollection<string>> gGroups,
            PhenotypeInfo<IReadOnlyCollection<string>> pGroups);

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

        private readonly IFrequencySetImporter frequencySetImporter;
        private readonly INotificationSender notificationSender;
        private readonly ILogger logger;
        private readonly IFrequencyConsolidator frequencyConsolidator;
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
            IPersistentCacheProvider persistentCacheProvider,
            IFrequencyConsolidator frequencyConsolidator
        )
        {
            this.frequencySetImporter = frequencySetImporter;
            this.notificationSender = notificationSender;
            this.logger = logger;
            this.frequencyConsolidator = frequencyConsolidator;
            this.frequencySetRepository = frequencySetRepository;
            this.frequencyRepository = frequencyRepository;
            cache = persistentCacheProvider.Cache;
        }

        public async Task ImportFrequencySet(FrequencySetFile file, bool convertToPGroups)
        {
            try
            {
                await frequencySetImporter.Import(file, convertToPGroups);
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
        public async Task<ConcurrentDictionary<HaplotypeHla, HaplotypeFrequency>> GetAllHaplotypeFrequencies(int setId)
        {
            var cacheKey = $"hf-set-{setId}";
            return await cache.GetOrAddAsync(cacheKey, async () =>
            {
                using (logger.RunTimed("Get All Frequencies from HF set - from SQL database", LogLevel.Verbose))
                {
                    var allFrequencies = await frequencyRepository.GetAllHaplotypeFrequencies(setId);
                    return new ConcurrentDictionary<HaplotypeHla, HaplotypeFrequency>(allFrequencies);
                }
            });
        }

        /// <inheritdoc />
        public async Task<ConcurrentDictionary<HaplotypeHla, HaplotypeFrequency>> GetHaplotypeFrequencies(
            int setId,
            PhenotypeInfo<IReadOnlyCollection<string>> gGroups,
            PhenotypeInfo<IReadOnlyCollection<string>> pGroups)
        {
            var combinedGGroups = gGroups.ToLociInfo((l, g1, g2) => g1?.Concat(g2).ToHashSet());
            var combinedPGroups = pGroups.ToLociInfo((l, p1, p2) => p1?.Concat(p2).ToHashSet());

            var combinedGroups = new LociInfo<HashSet<string>>(l => combinedGGroups.GetLocus(l)?.Concat(combinedPGroups.GetLocus(l)).ToHashSet());

            var frequencies = await frequencyRepository.GetHaplotypeFrequencies(setId, combinedGroups);
            return new ConcurrentDictionary<HaplotypeHla, HaplotypeFrequency>(frequencies);
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
            return await cache.GetSingleItemAndScheduleWholeCollectionCacheWarm(
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
                RegistryCode = set.RegistryCode
            };
        }

        private async Task SendSuccessNotification(FrequencySetFile file)
        {
            var successName = $"{SupportSummaryPrefix} Succeeded";

            logger.SendEvent(new HaplotypeFrequencySetImportEventModel(successName, file));

            await notificationSender.SendNotification(
                successName,
                $"Import of file, '{file.FileName}', has completed successfully.",
                NotificationConstants.OriginatorName);
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

            logger.SendEvent(new ErrorEventModel(errorName, ex));

            await notificationSender.SendAlert(
                errorName,
                $"Import of file, '{file.FileName}', failed with the following exception message: \"{ex.GetBaseException().Message}\". "
                + "Full exception info has been logged to Application Insights.",
                Priority.High,
                NotificationConstants.OriginatorName);
        }
    }
}