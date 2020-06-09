using System.Net.Http;
using System.Threading.Tasks;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using Atlas.MultipleAlleleCodeDictionary.MacImportServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Atlas.Functions.Functions
{
    internal class MacImportFunctions
    {
        private IMultipleAlleleCodeDictionary MacDictionary { get; set; }
        
        public MacImportFunctions(IMultipleAlleleCodeDictionary macDictionary)
        {
            MacDictionary = macDictionary;
        }

        [FunctionName(nameof(ImportMacs))]
        public async Task ImportMacs([TimerTrigger("0 0 2 * * *")] TimerInfo myTimer)
        {
            await MacDictionary.ImportLatestMultipleAlleleCodes();
        }
        
        [FunctionName(nameof(ImportMacsManual))]
        public async Task ImportMacsManual([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestMessage request)
        {
            await MacDictionary.ImportLatestMultipleAlleleCodes();
        }
        
        [FunctionName(nameof(GetMac))]
        public async Task<MultipleAlleleCode> GetMac([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest request)
        {
            var macCode = request.Query["code"];
            return await MacDictionary.GetMultipleAlleleCode(macCode);
        }
        
        [FunctionName(nameof(UpdateMacCache))]
        public async Task UpdateMacCache([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest request)
        {
            await MacDictionary.GenerateMacCache();
        }
    }
}