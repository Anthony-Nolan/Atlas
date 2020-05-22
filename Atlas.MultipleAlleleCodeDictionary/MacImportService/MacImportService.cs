using System.Collections.Generic;
using Atlas.MultipleAlleleCodeDictionary.utils;

namespace Atlas.MultipleAlleleCodeDictionary.MacImportService
{
    public class MacImporter
    {
        
        private IMacCodeRepository MacCodeRepository { get; set; }
        private INmdpCodeParser MacCodeParser { get; set; }
        
        public MacImporter()
        {
            MacCodeRepository = new MacCodeRepository();
            MacCodeParser = new NmdpCodeLineParser();
        }
        public void ImportLatestMultipleAlleleCodes()
        {
            var lastEntryBeforeInsert = MacCodeRepository.GetLastMacCodeEntry();
            var blank = MacCodeParser.ParseNmdpCodeLinesToModelSet(lastEntryBeforeInsert);
            MacCodeRepository.InsertMac(blank);
        }
    }
}