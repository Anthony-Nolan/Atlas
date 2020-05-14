using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.HlaMetadataDictionary.Exceptions;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.MatchingDictionary;
using Atlas.Utils.Core.ApplicationInsights;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Services.Donors
{
    public interface IDonorHlaExpander
    {
        Task<DonorBatchProcessingResult<DonorInfoWithExpandedHla>> ExpandDonorHlaBatchAsync(
            IEnumerable<DonorInfo> donorInfos,
            string failureEventName,
            string hlaDatabaseVersion = null);
    }

    public class DonorHlaExpander : DonorBatchProcessor<DonorInfo, DonorInfoWithExpandedHla>, IDonorHlaExpander
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
            return await ProcessBatchAsyncWithAnticipatedExceptions<MatchingDictionaryException>(
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
                HlaNames = donorInfo.HlaNames,
                MatchingHla = expandedHla
            };
        }
    }
}
