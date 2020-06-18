using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.Hla.Models.MolecularHlaTyping;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Atlas.Functions.Functions
{
    internal class MacDictionaryFunctions
    {
        private readonly IMacDictionary macDictionary;

        public MacDictionaryFunctions(IMacDictionary macDictionary)
        {
            this.macDictionary = macDictionary;
        }

        [FunctionName(nameof(ImportMacs))]
        public async Task ImportMacs([TimerTrigger("0 0 2 * * *")] TimerInfo myTimer)
        {
            await macDictionary.ImportLatestMacs();
        }

        [FunctionName(nameof(ManuallyImportMacs))]
        public async Task ManuallyImportMacs([HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequestMessage request)
        {
            await macDictionary.ImportLatestMacs();
        }

        [FunctionName(nameof(GetMac))]
        public async Task<Mac> GetMac([HttpTrigger(AuthorizationLevel.Function, "get")]
            HttpRequest request)
        {
            var macCode = request.Query["code"];
            return await macDictionary.GetMac(macCode);
        }

        [FunctionName(nameof(GetHlaFromMac))]
        public async Task<IEnumerable<string>> GetHlaFromMac([HttpTrigger(AuthorizationLevel.Function, "get")]
            HttpRequest request)
        {
            var macCode = request.Query["code"];
            var firstField = request.Query["firstField"];
            return await macDictionary.GetHlaFromMac(firstField, macCode);
        }
        
    }
}