using System.Collections.Generic;
using Atlas.MultipleAlleleCodeDictionary.utils;

namespace Atlas.MultipleAlleleCodeDictionary.MacImportService
{
    public interface IMacImporter
    {
        public void ImportLatestMultipleAlleleCodes();
    }
    public class MacImporter : IMacImporter
    {
        
        private IMacRepository MacRepository { get; set; }
        private IMacParser MacCodeParser { get; set; }
        
        public MacImporter()
        {
            MacRepository = new MacRepository();
            MacCodeParser = new MacLineParser();
        }
        public void ImportLatestMultipleAlleleCodes()
        {
            var lastEntryBeforeInsert = MacRepository.GetLastMacEntry();
            var newMacs = MacCodeParser.GetMacsSinceLastEntry(lastEntryBeforeInsert);
            MacRepository.InsertMacs(newMacs);
        }
    }
}