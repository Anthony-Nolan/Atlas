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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import.Exceptions;
using CsvHelper;
using CsvHelper.Configuration;
using HaplotypeFrequencySet = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet.HaplotypeFrequencySet;
using HaplotypeHla = Atlas.Common.GeneticData.PhenotypeInfo.LociInfo<string>;
// ReSharper disable InconsistentNaming

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies
{
    public interface IHaplotypeFrequencyService
    {
        /// <summary>
        /// Converts the CSV haplotype files to JSON
        /// </summary>
        /// <param name="filePath">Csv file that is being converted to json</param>
        /// <returns></returns>
        public SerializableFrequencyFile ConvertToJson(string filePath);

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
    
    public class SerializableRecord
    {
        public string a { get; set; }
        public string b { get; set; }
        public string c { get; set; }
        public string dqb1 { get; set; }
        public string drb1 { get; set; }
        public decimal frequency { get; set; }
    }

    public class SerializableFrequencyFile
    {
        public string nomenclatureVersion { get; set; }
        public string[] donPool { get; set; }
        public string[] ethn { get; set; }
        public int populationId { get; set; }
        public List<SerializableRecord> frequencies { get; set; }
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
        public SerializableFrequencyFile ConvertToJson(string filePath)
        {
            if (filePath == null)
            {
                throw new EmptyHaplotypeFileException();
            }

            // Load all frequencies into memory, to perform aggregation by PGroup.
            // Largest known HF set is ~300,000 entries, which is reasonable to load into memory here.
            var haplotypeFrequencyFile = GetFrequencies(filePath).ToList();

            return new SerializableFrequencyFile
            {
                nomenclatureVersion = "[OVERRIDE]",
                donPool = null,
                ethn = null,
                populationId = 0,
                frequencies = haplotypeFrequencyFile
            };
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
                await LogErrorAndSendAlert(file, $"{ex.Message} Locus: {ex.Locus}, GGroup: { ex.HlaName}", ex.StackTrace);
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
                var allFrequencies = await frequencyRepository.GetAllHaplotypeFrequencies(setId);
                return new ConcurrentDictionary<HaplotypeHla, HaplotypeFrequency>(allFrequencies);
            });
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
            logger.SendTrace(message, LogLevel.Warn);
            await notificationSender.SendAlert(message, description, Priority.Medium, NotificationConstants.OriginatorName);
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

        private static IEnumerable<SerializableRecord> GetFrequencies(string filePath)
        {
            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader))
            {
                ConfigureCsvReader(csv);
                while (TryRead(csv))
                {
                    SerializableRecord haplotypeFrequency = null;

                    try
                    {
                        haplotypeFrequency = csv.GetRecord<SerializableRecord>();
                    }
                    catch (CsvHelperException e)
                    {
                        throw new HaplotypeFormatException(e);
                    }

                    if (haplotypeFrequency == null)
                    {
                        throw new MalformedHaplotypeFileException("Haplotype in input file could not be parsed.");
                    }

                    if (haplotypeFrequency.frequency == 0m)
                    {
                        throw new MalformedHaplotypeFileException($"Haplotype property frequency cannot be 0.");
                    }

                    if (haplotypeFrequency.a == null ||
                        haplotypeFrequency.b == null ||
                        haplotypeFrequency.c == null ||
                        haplotypeFrequency.dqb1 == null ||
                        haplotypeFrequency.drb1 == null)
                    {
                        throw new MalformedHaplotypeFileException($"Haplotype loci cannot be null.");
                    }

                    yield return haplotypeFrequency;
                }
            }
        }

        private static void ConfigureCsvReader(IReaderRow csvReader)
        {
            csvReader.Configuration.Delimiter = ";";
            csvReader.Configuration.TypeConverterOptionsCache.GetOptions<string>().NullValues.Add("");
            csvReader.Configuration.PrepareHeaderForMatch = (header, index) => header.ToUpper();
            csvReader.Configuration.RegisterClassMap<HaplotypeFrequencyMap>();
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private sealed class HaplotypeFrequencyMap : ClassMap<SerializableRecord>
        {
            public HaplotypeFrequencyMap()
            {
                Map(m => m.a);
                Map(m => m.b);
                Map(m => m.c);
                Map(m => m.dqb1);
                Map(m => m.drb1);
                Map(m => m.frequency).Name("freq");
            }
        }

        private static bool TryRead(CsvReader reader)
        {
            try
            {
                return reader.Read();
            }
            catch (BadDataException e)
            {
                throw new MalformedHaplotypeFileException($"Invalid CSV was encountered: {e.Message}");
            }
        }
    }
}