using Atlas.Common.ApplicationInsights;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;
using Atlas.MultipleAlleleCodeDictionary.Services.MacImport;
using Dasync.Collections;
using System;
using System.Threading.Tasks;
using Atlas.Common.AzureStorage.TableStorage;
using Atlas.Common.Notifications;

namespace Atlas.MultipleAlleleCodeDictionary.ExternalInterface
{
    public interface IMacImporter
    {
        public Task ImportLatestMacs();
        public Task RecreateMacTable();
    }

    internal class MacImporter : IMacImporter
    {
        private const string TracePrefix = "Mac Import: ";

        private readonly IMacRepository macRepository;
        private readonly IMacFetcher macFetcher;
        private readonly ILogger logger;
        private readonly INotificationSender notificationSender;

        public MacImporter(IMacRepository macRepository, IMacFetcher macFetcher, ILogger logger, INotificationSender notificationSender)
        {
            this.macRepository = macRepository;
            this.macFetcher = macFetcher;
            this.logger = logger;
            this.notificationSender = notificationSender;
        }

        public async Task RecreateMacTable()
        {
            await macRepository.TruncateMacTable();
            await ImportLatestMacs();
        }

        public async Task ImportLatestMacs()
        {
            logger.SendTrace($"{TracePrefix}Mac Import started");
            try
            {
                var lastEntryBeforeInsert = await macRepository.GetLastMacEntry();
                logger.SendTrace($"{TracePrefix}The last MAC entry found was: {lastEntryBeforeInsert}");

                var newMacs = await macFetcher.FetchAndLazilyParseMacsSince(lastEntryBeforeInsert).ToListAsync();

                logger.SendTrace($"{TracePrefix}Attempting to insert {newMacs.Count} new MACs");
                await macRepository.InsertMacs(newMacs);
            }
            catch (AzureTableBatchInsertException)
            {
                logger.SendTrace(
                    $"{TracePrefix}Failed to insert MACs. Assuming this is due to de-synced metadata - re-syncing metadata (this may take some time)",
                    LogLevel.Error);
                
                var lastEntryBeforeInsert = await macRepository.GetLastMacEntry(true);
                var newMacs = await macFetcher.FetchAndLazilyParseMacsSince(lastEntryBeforeInsert).ToListAsync();
                
                logger.SendTrace($"{TracePrefix}Attempting to insert {newMacs.Count} new MACs");
                await macRepository.InsertMacs(newMacs);
            }
            catch (Exception e)
            {
                await notificationSender.SendAlert("MAC Import failed", "Failed to import MACs, check AI logs for error details.", Priority.High,
                    nameof(MacImporter));
                logger.SendEvent(new ErrorEventModel($"{TracePrefix}Failed to finish MAC Import", e));
                throw;
            }

            logger.SendTrace($"{TracePrefix}Successfully finished MAC Import");
        }
    }
}