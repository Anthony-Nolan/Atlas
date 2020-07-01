using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading.Tasks;
using Atlas.Common.Utils;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
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

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName(nameof(ImportMacs))]
        public async Task ImportMacs([TimerTrigger("0 0 2 * * *")] TimerInfo myTimer)
        {
            await macDictionary.ImportLatestMacs();
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName(nameof(ManuallyImportMacs))]
        public async Task ManuallyImportMacs(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequestMessage request)
        {
            await macDictionary.ImportLatestMacs();
        }

        [QueryStringParameter("macCode", "macCode", DataType = typeof(string))]
        [FunctionName(nameof(GetMac))]
        public async Task<Mac> GetMac(
            [HttpTrigger(AuthorizationLevel.Function, "get")]
            HttpRequest request)
        {
            var macCode = request.Query["macCode"];
            return await macDictionary.GetMac(macCode);
        }

        [QueryStringParameter("macCode", "macCode", DataType = typeof(string))]
        [QueryStringParameter("firstField", "firstField", DataType = typeof(string))]
        [FunctionName(nameof(GetHlaFromMac))]
        public async Task<IEnumerable<string>> GetHlaFromMac(
            [HttpTrigger(AuthorizationLevel.Function, "get")]
            HttpRequest request)
        {
            var macCode = request.Query["macCode"];
            var firstField = request.Query["firstField"];
            return await macDictionary.GetHlaFromMac(firstField, macCode);
        }
        
    }
}