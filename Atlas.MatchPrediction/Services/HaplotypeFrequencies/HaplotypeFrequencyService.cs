using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import;
using System;
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
using HaplotypeFrequencySet = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet.HaplotypeFrequencySet;
using HaplotypeHla = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.LociInfo<string>;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies;

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

    Task<FrequencySetCacheEntry> GetAllHaplotypeFrequencies(int setId);

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
    private readonly IAtlasLogger logger;
    private readonly IHaplotypeFrequencyCache haplotypeFrequencyCache;

    public HaplotypeFrequencyService(
        IFrequencySetImporter frequencySetImporter,
        INotificationSender notificationSender,
        IMatchPredictionLogger<MatchProbabilityLoggingContext> logger,
        IHaplotypeFrequencyCache haplotypeFrequencyCache)
    {
        this.frequencySetImporter = frequencySetImporter;
        this.notificationSender = notificationSender;
        this.logger = logger;
        this.haplotypeFrequencyCache = haplotypeFrequencyCache;
    }

    public async Task ImportFrequencySet(FrequencySetFile file, FrequencySetImportBehaviour importBehaviour)
    {
        importBehaviour ??= new FrequencySetImportBehaviour();

        try
        {
            await frequencySetImporter.Import(file, importBehaviour);
            haplotypeFrequencyCache.RemoveActiveHaplotypeFrequencySets();
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
        var activeSets = await haplotypeFrequencyCache.GetActiveHaplotypeFrequencySets();

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
    public Task<FrequencySetCacheEntry> GetAllHaplotypeFrequencies(int setId)
    {
        return haplotypeFrequencyCache.GetAllHaplotypeFrequencies(setId);
    }

    /// <inheritdoc />
    public async Task<decimal> GetFrequencyForHla(int setId, HaplotypeHla hla, ISet<Locus> excludedLoci)
    {
        var entry = await haplotypeFrequencyCache.GetAllHaplotypeFrequencies(setId);

        // The interner resolves a key when every allele is known to the set individually - but that does NOT
        // guarantee the *combination* is a stored haplotype (e.g. two haplotypes sharing alleles produce
        // resolvable cross-combinations that were never imported). So we must still probe the dictionary
        // rather than indexing into it, otherwise an unrepresented-but-resolvable haplotype throws instead of
        // falling through to the unrepresented (0 / consolidate) handling below.
        if (entry.Interner.TryResolve(a: hla.A, b: hla.B, c: hla.C, dqb1: hla.Dqb1, drb1: hla.Drb1, out var resolvedHaplotypeKey)
         && entry.SetFrequencies.TryGetValue(resolvedHaplotypeKey, out var haplotypeFrequency))
        {
            return haplotypeFrequency.Frequency;
        }

        // If no loci are excluded, there is nothing to calculate - the haplotype is just unrepresented.
        // We do not want to add all unrepresented haplotypes to the cache - this drastically reduces algorithm speed, increases memory, and has no benefit
        if (!excludedLoci.Any())
        {
            return 0;
        }

        return await haplotypeFrequencyCache.GetConsolidatedFrequency(setId, hla, excludedLoci);
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
            eventProperties[nameof(file.UploadedDateTime)] =
                file.UploadedDateTime.Value.UtcDateTime.ToString(CultureInfo.InvariantCulture) + " UTC";
        }

        if (file.ImportedDateTime != null)
        {
            eventProperties[nameof(file.ImportedDateTime)] =
                file.ImportedDateTime.Value.UtcDateTime.ToString(CultureInfo.InvariantCulture) + " UTC";
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
            }
        );

        await notificationSender.SendAlert(
            errorName,
            $"Import of file, '{file.FileName}', failed with the following exception message: \"{ex.GetBaseException().Message}\". "
          + "Full exception info has been logged to Application Insights.",
            Priority.High,
            NotificationConstants.OriginatorName
        );
    }
}