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
    }

    internal class HaplotypeFrequencyService : HaplotypeFrequencyLookupService, IHaplotypeFrequencyService
    {
        private const string SupportSummaryPrefix = "Haplotype Frequency Set Import";

        private readonly IFrequencySetImporter frequencySetImporter;
        private readonly INotificationSender notificationSender;

        public HaplotypeFrequencyService(
            IFrequencySetImporter frequencySetImporter,
            INotificationSender notificationSender,
            IMatchPredictionLogger<MatchProbabilityLoggingContext> logger,
            IHaplotypeFrequencyCache haplotypeFrequencyCache)
            : base(logger, haplotypeFrequencyCache)
        {
            this.frequencySetImporter = frequencySetImporter;
            this.notificationSender = notificationSender;
        }

        public async Task ImportFrequencySet(FrequencySetFile file, FrequencySetImportBehaviour importBehaviour)
        {
            importBehaviour ??= new FrequencySetImportBehaviour();

            try
            {
                await frequencySetImporter.Import(file, importBehaviour);
                HaplotypeFrequencyCache.RemoveActiveHaplotypeFrequencySets();
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
