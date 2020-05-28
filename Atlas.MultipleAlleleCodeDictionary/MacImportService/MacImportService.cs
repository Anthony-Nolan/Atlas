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
        private readonly IMacParser macCodeParser;
        
        {
            this.macRepository = macRepository;
            macCodeParser = macParser;
        }
        
        public async Task ImportLatestMultipleAlleleCodes()
        {
            var lastEntryBeforeInsert = macRepository.GetLastMacEntry();
            var newMacs = macCodeParser.GetMacsSinceLastEntry(lastEntryBeforeInsert);
            await macRepository.InsertMacs(newMacs);
        }
    }
}