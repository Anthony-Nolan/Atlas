using System.Collections.Generic;
using System.Threading.Tasks;
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
        
        public MacImporter(IMacRepository macRepository, IMacParser macParser)
        {
            this.macRepository = macRepository;
            this.macParser = macParser;
        }

        public async Task ImportLatestMultipleAlleleCodes()
        {
            var lastEntryBeforeInsert = await macRepository.GetLastMacEntry();
            var newMacs = macParser.GetMacsSinceLastEntry(lastEntryBeforeInsert);
            await macRepository.InsertMacs(newMacs);
        }
    }
}