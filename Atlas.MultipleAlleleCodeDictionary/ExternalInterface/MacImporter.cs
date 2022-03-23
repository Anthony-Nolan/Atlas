using Atlas.Common.ApplicationInsights;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;
using Atlas.MultipleAlleleCodeDictionary.Services.MacImport;
using Dasync.Collections;
using System;
using System.Threading.Tasks;
using Atlas.Common.Notifications;
using Atlas.Common.Notifications.MessageModels;

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
        private readonly INotificationsClient notificationsClient;

        public MacImporter(IMacRepository macRepository, IMacFetcher macFetcher, ILogger logger, INotificationsClient notificationsClient)
        {
            this.macRepository = macRepository;
            this.macFetcher = macFetcher;
            this.logger = logger;
            this.notificationsClient = notificationsClient;
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
            catch (Exception e)
            {
                var alert = new Alert("MAC Import failed", "Failed to import MACs, check AI logs for error details.", Priority.High);
                await notificationsClient.SendAlert(alert);
                logger.SendEvent(new ErrorEventModel($"{TracePrefix}Failed to finish MAC Import", e));
                throw;
            }

            logger.SendTrace($"{TracePrefix}Successfully finished MAC Import");
        }
    }
}