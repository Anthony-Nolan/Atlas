using System;
using System.Net.Http;
using Atlas.MultipleAlleleCodeDictionary.MacImportService;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Atlas.Functions.Functions
{
    internal class MacImportFunctions
    {
        public IMacImporter MacImporter { get; set; }
        
        public MacImportFunctions(IMacImporter macImporter)
        {
            MacImporter = macImporter;
        }

        [FunctionName(nameof(ImportMacs))]
        public async Task ImportMacs([TimerTrigger("0 0 2 * * *")] TimerInfo myTimer)
        {
            await MacImporter.ImportLatestMultipleAlleleCodes();
        }
        
        [FunctionName(nameof(ImportMacsManual))]
        public async Task ImportMacsManual([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestMessage request)
        {
            await MacImporter.ImportLatestMultipleAlleleCodes();
        }
    }
}