using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.DataRefresh.HlaProcessing;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.MatchingAlgorithm.Api.Controllers
{
    public class DataRefreshController : ControllerBase
    {
        private readonly IHlaProcessor hlaProcessor;
        private readonly IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor;

        public DataRefreshController(
            IHlaProcessor hlaProcessor,
            IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor)
        {
            this.hlaProcessor = hlaProcessor;
            this.hlaNomenclatureVersionAccessor = hlaNomenclatureVersionAccessor;
        }

        [HttpPost]
        [Route("trigger-donor-hla-update")]
        public async Task TriggerSingleImport()
        {
            await hlaProcessor.UpdateDonorHla(hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion(), Task.FromResult);
        }
    }
}
