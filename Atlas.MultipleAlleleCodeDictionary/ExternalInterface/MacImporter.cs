using Atlas.Common.ApplicationInsights;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;
using Atlas.MultipleAlleleCodeDictionary.Services.MacImport;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MultipleAlleleCodeDictionary.ExternalInterface
{
    public interface IMacImporter
    {
        public Task ImportLatestMacs();
        public Task RecreateMacTable();
    }

    internal class MacImporter : IMacImporter
    {
        private readonly IMacRepository macRepository;
        private readonly IMacStreamer macStreamer;
        private readonly ILogger logger;

        public MacImporter(IMacRepository macRepository, IMacStreamer macStreamer, ILogger logger)
        {
            this.macRepository = macRepository;
            this.macStreamer = macStreamer;
            this.logger = logger;
        }

        public async Task RecreateMacTable()
        {
            await macRepository.TruncateMacTable();
            await ImportLatestMacs();
        }

        public async Task ImportLatestMacs()
        {
            const string tracePrefix = "Mac Import: ";
            logger.SendTrace($"{tracePrefix}Mac Import started");
            try
            {
                var lastEntryBeforeInsert = await macRepository.GetLastMacEntry();
                logger.SendTrace($"{tracePrefix}The last MAC entry found was: {lastEntryBeforeInsert}");

                var newMacs = await (await macStreamer.StreamMacsSince(lastEntryBeforeInsert)).ToListAsync();

                logger.SendTrace($"{tracePrefix}Attempting to insert {newMacs.Count} new MACs");
                await macRepository.InsertMacs(newMacs);
            }
            catch (Exception e)
            {
                logger.SendEvent(new ErrorEventModel($"{tracePrefix}Failed to finish MAC Import", e));
                throw;
            }

            logger.SendTrace($"{tracePrefix}Successfully finished MAC Import");
        }
    }
}