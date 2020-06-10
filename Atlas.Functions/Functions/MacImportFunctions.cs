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
        private readonly IMultipleAlleleCodeDictionary macDictionary;

        public MacImportFunctions(IMultipleAlleleCodeDictionary macDictionary)
        {
            this.macDictionary = macDictionary;
        }

        [FunctionName(nameof(ImportMacs))]
        public async Task ImportMacs([TimerTrigger("0 0 2 * * *")] TimerInfo myTimer)
        {
            await macDictionary.ImportLatestMultipleAlleleCodes();
        }

        [FunctionName(nameof(ImportMacsManual))]
        public async Task ImportMacsManual([HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequestMessage request)
        {
            await macDictionary.ImportLatestMultipleAlleleCodes();
        }

        [FunctionName(nameof(GetMac))]
        public async Task<MultipleAlleleCode> GetMac([HttpTrigger(AuthorizationLevel.Function, "get")]
            HttpRequest request)
        {
            var macCode = request.Query["code"];
            return await macDictionary.GetMultipleAlleleCode(macCode);
        }
    }
}