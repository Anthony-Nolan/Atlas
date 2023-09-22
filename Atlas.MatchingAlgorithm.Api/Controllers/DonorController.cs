using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.Donors;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.MatchingAlgorithm.Api.Controllers
{
    [Route("donor")]
    public class DonorController : ControllerBase
    {
        private readonly IDonorService donorService;
        private readonly IActiveDatabaseProvider activeDatabaseProvider;
        private readonly IActiveHlaNomenclatureVersionAccessor hlaVersionAccessor;

        public DonorController(
            IDonorService donorService,
            IActiveDatabaseProvider activeDatabaseProvider,
            IActiveHlaNomenclatureVersionAccessor hlaVersionAccessor)
        {
            this.donorService = donorService;
            this.activeDatabaseProvider = activeDatabaseProvider;
            this.hlaVersionAccessor = hlaVersionAccessor;
        }

        [HttpPut]
        [Route("batch")]
        public async Task CreateOrUpdateDonorBatch([FromBody] DonorInfoBatchTransfer donorBatch)
        {
            await donorService.CreateOrUpdateDonorBatch(
                donorBatch.Donors.Select(d => d.ToDonorInfo()),
                activeDatabaseProvider.GetActiveDatabase(),
                hlaVersionAccessor.GetActiveHlaNomenclatureVersion(),
                true);
        }
    }

    // Need to use a transfer model rather than exposing database model directly, as it contains a PhenotypeInfo, which is immutable and cannot be serialised.
    public class DonorInfoBatchTransfer
    {
        public IEnumerable<DonorInfoTransfer> Donors { get; set; }
    }

    public class DonorInfoTransfer
    {
        public int DonorId { get; set; }
        public DonorType DonorType { get; set; }
        public string ExternalDonorCode { get; set; }
        public string EthnicityCode { get; set; }
        public string RegistryCode { get; set; }
        public PhenotypeInfoTransfer<string> HlaNames { get; set; }
        public bool IsAvailableForSearch { get; set; } = true;
    }

    public static class DonorInfoTransferMapping
    {
        public static DonorInfo ToDonorInfo(this DonorInfoTransfer donorInfoTransfer)
        {
            return new DonorInfo
            {
                DonorId = donorInfoTransfer.DonorId,
                DonorType = donorInfoTransfer.DonorType,
                ExternalDonorCode = donorInfoTransfer.ExternalDonorCode,
                EthnicityCode = donorInfoTransfer.EthnicityCode,
                RegistryCode = donorInfoTransfer.RegistryCode,
                HlaNames = donorInfoTransfer.HlaNames.ToPhenotypeInfo(),
                IsAvailableForSearch = donorInfoTransfer.IsAvailableForSearch
            };
        }
    }
}