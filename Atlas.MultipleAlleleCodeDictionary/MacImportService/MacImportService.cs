using System;
using System.Threading.Tasks;
using Atlas.MultipleAlleleCodeDictionary.utils;
using Atlas.Common.ApplicationInsights;
using Atlas.MultipleAlleleCodeDictionary.ApplicationInsights;

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
            logger.SendEvent(new MacImportEventModel("Mac Import started"));
            try
            {
                var lastEntryBeforeInsert = await macRepository.GetLastMacEntry();
                logger.SendEvent(new MacImportEventModel($"The last MAC entry found was: {lastEntryBeforeInsert}"));
                var newMacs = await macParser.GetMacsSinceLastEntry(lastEntryBeforeInsert);
                logger.SendEvent(new MacImportEventModel($"Attempting to insert {newMacs.Count} new MACs"));
                await macRepository.InsertMacs(newMacs);
            }
            catch (Exception e)
            {
                logger.SendEvent(new MacImportEventModel(e, "Failed to finish MAC Import"));
            }
            logger.SendEvent(new MacImportEventModel("Successfully finished MAC Import"));
        }
    }
}