using System;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.MultipleAlleleCodeDictionary.utils;

namespace Atlas.MultipleAlleleCodeDictionary.MacImportService
{
    public interface IMacImporter
    {
        public Task ImportLatestMultipleAlleleCodes();
    }

    public class MacImporter : IMacImporter
    {
        private readonly IMacRepository macRepository;
        private readonly IMacParser macParser;
        private readonly ILogger logger;
        
        public MacImporter(IMacRepository macRepository, IMacParser macParser, ILogger logger)
        {
            this.macRepository = macRepository;
            this.macParser = macParser;
            this.logger = logger;
        }

        public async Task ImportLatestMultipleAlleleCodes()
        {
            const string tracePrefix = "Mac Import: ";
            logger.SendTrace($"{tracePrefix}Mac Import started", LogLevel.Info);
            try
            {
                var lastEntryBeforeInsert = await macRepository.GetLastMacEntry();
                logger.SendTrace($"{tracePrefix}The last MAC entry found was: {lastEntryBeforeInsert}", LogLevel.Info);
                var newMacs = await macParser.GetMacsSinceLastEntry(lastEntryBeforeInsert);
                logger.SendTrace($"{tracePrefix}Attempting to insert {newMacs.Count} new MACs", LogLevel.Info);
                await macRepository.InsertMacs(newMacs);
            }
            catch (Exception e)
            {
                logger.SendEvent(new ErrorEventModel($"{tracePrefix}Failed to finish MAC Import", e));
                throw;
            }
            logger.SendTrace($"{tracePrefix}Successfully finished MAC Import", LogLevel.Info);
        }
    }
}