using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Atlas.Client.Models.SupportMessages;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import.Exceptions;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies
{
    /// <summary>
    /// The haplotype frequency set lookup, plus the import of new frequency sets.
    /// </summary>
    /// <remarks>
    /// Code that only reads frequency sets - the genotype set pipeline - depends on
    /// <see cref="IHaplotypeFrequencyLookupService"/> instead, so its hosts need neither the importer nor the
    /// notification sender.
    /// </remarks>
    public interface IHaplotypeFrequencyService : IHaplotypeFrequencyLookupService
    {
        /// <summary>
        /// Imports all haplotype frequencies from a given frequency set file.
        /// </summary>
        /// <param name="file">Contains both the haplotype frequencies as file contents, as well as metadata about the file itself.</param>
        /// <param name="importBehaviour"></param>
        /// <returns></returns>
        public Task ImportFrequencySet(FrequencySetFile file, FrequencySetImportBehaviour importBehaviour = null);

        /// <summary>
        /// Reads the active sets from the database, and writes them to the distributed cache, as an import does. Use it
        /// after the active sets were changed by hand, with SQL: every process then uses them within
        /// <c>HaplotypeFrequencySetCacheSettings.ActiveSetCacheExpiryMinutes</c>.
        /// </summary>
        /// <returns><c>false</c> if the distributed cache did not store the sets. Then try again later.</returns>
        Task<bool> RefreshActiveHaplotypeFrequencySetCache();
    }

    internal class HaplotypeFrequencyService : HaplotypeFrequencyLookupService, IHaplotypeFrequencyService
    {
        private const string SupportSummaryPrefix = "Haplotype Frequency Set Import";

        /// <summary>
        /// The wait before each new write of the active sets to the distributed cache. It is much longer than another process
        /// can take between its read of the database and its write of what it read (a Redis command fails after 1 s). It is
        /// also as long as the distributed cache waits, after a failed attempt to connect, before it tries again.
        /// </summary>
        internal static readonly TimeSpan ActiveSetCacheRewriteDelay = TimeSpan.FromSeconds(30);

        internal const int MaxActiveSetCacheRewriteAttempts = 4;

        private readonly IFrequencySetImporter frequencySetImporter;
        private readonly INotificationSender notificationSender;
        private readonly TimeProvider timeProvider;

        public HaplotypeFrequencyService(
            IFrequencySetImporter frequencySetImporter,
            INotificationSender notificationSender,
            IMatchPredictionLogger<MatchProbabilityLoggingContext> logger,
            IHaplotypeFrequencyCache haplotypeFrequencyCache,
            TimeProvider timeProvider)
            : base(logger, haplotypeFrequencyCache)
        {
            this.frequencySetImporter = frequencySetImporter;
            this.notificationSender = notificationSender;
            this.timeProvider = timeProvider;
        }

        public async Task ImportFrequencySet(FrequencySetFile file, FrequencySetImportBehaviour importBehaviour)
        {
            importBehaviour ??= new FrequencySetImportBehaviour();

            try
            {
                await frequencySetImporter.Import(file, importBehaviour);
                await TryRefreshActiveSetCache();
                file.ImportedDateTime = DateTimeOffset.UtcNow;

                await SendSuccessNotification(file);

                if (!await RewriteActiveSetCacheUntilStored())
                {
                    await LogErrorAndSendAlert(
                        file,
                        "The new active sets are not confirmed in the distributed cache: the writes after the import failed. Other processes " +
                        "can use the replaced sets for up to " +
                        $"{HaplotypeFrequencies.HaplotypeFrequencyCache.ActiveHaplotypeFrequencySetsDistributedCacheTimeToLive.TotalHours} hours. When the " +
                        "cache is available, call RefreshActiveHaplotypeFrequencySetCache on the match prediction functions app.",
                        null);
                }
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

        /// <inheritdoc />
        public async Task<bool> RefreshActiveHaplotypeFrequencySetCache()
        {
            await TryRefreshActiveSetCache();
            return await RewriteActiveSetCacheUntilStored();
        }

        /// <summary>
        /// Writes the active sets to the distributed cache again, until it stores them: at most
        /// <see cref="MaxActiveSetCacheRewriteAttempts"/> times, each after <see cref="ActiveSetCacheRewriteDelay"/>.
        /// </summary>
        /// <remarks>
        /// The first new write is done also when the write before it was stored. Another process can read the database before
        /// the sets change, and store what it read after that write. The new write replaces it.
        /// </remarks>
        /// <returns>Whether the distributed cache stored the sets.</returns>
        private async Task<bool> RewriteActiveSetCacheUntilStored()
        {
            for (var attempt = 0; attempt < MaxActiveSetCacheRewriteAttempts; attempt++)
            {
                await Task.Delay(ActiveSetCacheRewriteDelay, timeProvider);

                if (await TryRefreshActiveSetCache())
                {
                    return true;
                }
            }

            return false;
        }

        /// <returns>Whether the distributed cache stored the active sets.</returns>
        /// <remarks>
        /// Never throws. After an import, the new sets are already active: if this threw, the import would be reported as
        /// failed, and its file imported again when the message is delivered again.
        /// </remarks>
        private async Task<bool> TryRefreshActiveSetCache()
        {
            try
            {
                if (await HaplotypeFrequencyCache.RefreshActiveHaplotypeFrequencySets())
                {
                    return true;
                }

                Logger.SendTrace("The distributed cache did not store the active haplotype frequency sets.", LogLevel.Warn);
            }
            catch (Exception exception)
            {
                Logger.SendException(exception, LogLevel.Warn);
            }

            return false;
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

            Logger.SendEvent(successName, LogLevel.Info, eventProperties);

            await notificationSender.SendNotification(
                successName,
                $"Import of file, '{file.FileName}', has completed successfully.",
                NotificationConstants.OriginatorName
            );
        }

        private async Task LogErrorAndSendAlert(FrequencySetFile file, string message, string description)
        {
            var messageWithName = $"Import of file '{file.FileName}': {message}";

            Logger.SendTrace(messageWithName, LogLevel.Warn);
            await notificationSender.SendAlert(messageWithName, description, Priority.Medium, NotificationConstants.OriginatorName);
        }

        private async Task SendErrorAlert(FrequencySetFile file, Exception ex)
        {
            var errorName = $"{SupportSummaryPrefix} Failure";

            Logger.SendException(ex, LogLevel.Error, new Dictionary<string, string>
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
}
