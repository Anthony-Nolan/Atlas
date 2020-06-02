using System;
using System.Threading.Tasks;
using Atlas.MultipleAlleleCodeDictionary.utils;
using Atlas.Common.ApplicationInsights;

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
            logger.SendTrace("Mac Import started", LogLevel.Info);
            try
            {
                var lastEntryBeforeInsert = await macRepository.GetLastMacEntry();
                logger.SendTrace($"The last MAC entry found was: {lastEntryBeforeInsert}", LogLevel.Info);
                var newMacs = await macParser.GetMacsSinceLastEntry(lastEntryBeforeInsert);
                logger.SendTrace($"Attempting to insert {newMacs.Count} new MACs", LogLevel.Info);
                await macRepository.InsertMacs(newMacs);
            }
            catch (Exception e)
            {
                logger.SendEvent(new ErrorEventModel("Failed to finish MAC Import", e));
            }
            logger.SendTrace("Successfully finished MAC Import", LogLevel.Info);
        }
    }
}