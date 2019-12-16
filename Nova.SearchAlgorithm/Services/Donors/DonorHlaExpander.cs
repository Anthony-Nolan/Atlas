using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Models.DonorInfo;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Services.MatchingDictionary;
using Nova.Utils.ApplicationInsights;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Services.Donors
{
    public interface IDonorHlaExpander
    {
        Task<DonorBatchProcessingResult<DonorInfoWithExpandedHla>> ExpandDonorHlaBatchAsync(
            IEnumerable<DonorInfo> donorInfos,
            string failureEventName,
            string hlaDatabaseVersion = null);
    }

    public class DonorHlaExpander : DonorBatchProcessor<DonorInfo, DonorInfoWithExpandedHla, MatchingDictionaryException>, IDonorHlaExpander
    {
        private readonly IExpandHlaPhenotypeService expandHlaPhenotypeService;

        public DonorHlaExpander(
            IExpandHlaPhenotypeService expandHlaPhenotypeService,
            ILogger logger)
            : base(logger)
        {
            this.expandHlaPhenotypeService = expandHlaPhenotypeService;
        }

        public async Task<DonorBatchProcessingResult<DonorInfoWithExpandedHla>> ExpandDonorHlaBatchAsync(
            IEnumerable<DonorInfo> donorInfos,
            string failureEventName,
            string hlaDatabaseVersion = null)
        {
            return await ProcessBatch(
                donorInfos,
                async d => await CombineDonorAndExpandedHla(d, hlaDatabaseVersion),
                d => d.ToFailedDonorInfo(),
                failureEventName
            );
        }

        private async Task<DonorInfoWithExpandedHla> CombineDonorAndExpandedHla(DonorInfo donorInfo, string hlaDatabaseVersion)
        {
            var expandedHla = await expandHlaPhenotypeService.GetPhenotypeOfExpandedHla(
                    new PhenotypeInfo<string>(donorInfo.HlaNames), hlaDatabaseVersion);

            return new DonorInfoWithExpandedHla
            {
                DonorId = donorInfo.DonorId,
                DonorType = donorInfo.DonorType,
                RegistryCode = donorInfo.RegistryCode,
                HlaNames = donorInfo.HlaNames,
                MatchingHla = expandedHla
            };
        }
    }
}
