using Atlas.MultipleAlleleCodeDictionary.MacImportService;
using Microsoft.Azure.WebJobs;

namespace Atlas.Functions.Functions
{
    internal class MacImport
    {
        public IMacImporter MacImporter { get; set; }
        
        public MacImport(IMacImporter macImporter)
        {
            MacImporter = macImporter;
        }

        [FunctionName(nameof(ImportMacs))]
        public void ImportMacs([TimerTrigger("0 0 2 * * *")]TimerInfo myTimer)
        {
            MacImporter.ImportLatestMultipleAlleleCodes();
        }
    }
}