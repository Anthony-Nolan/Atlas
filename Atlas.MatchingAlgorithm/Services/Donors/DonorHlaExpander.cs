using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Models;

namespace Atlas.MatchingAlgorithm.Services.Donors
{
    public interface IDonorHlaExpander
    {
        Task<DonorBatchProcessingResult<DonorInfoWithExpandedHla>> ExpandDonorHlaBatchAsync(IEnumerable<DonorInfo> donorInfos, string failureEventName);
        Task<DonorInfoWithExpandedHla> ExpandDonorHlaAsync(DonorInfo donorInfo);
    }

    public class DonorHlaExpander : DonorBatchProcessor<DonorInfo, DonorInfoWithExpandedHla>, IDonorHlaExpander
    {
        private readonly IHlaMetadataDictionary hlaMetadataDictionary;

        /// <param name="hlaMetadataDictionary">
        /// Calling code is responsible for providing an appropriately versioned library.
        /// In practice this is achieved by using the <see cref="DonorHlaExpanderFactory"/>
        /// </param>
        /// <param name="logger">an Atlas ILogger</param>
        public DonorHlaExpander(
            IHlaMetadataDictionary hlaMetadataDictionary,
            IMatchingAlgorithmImportLogger logger)
            : base(logger)
        {
            this.hlaMetadataDictionary = hlaMetadataDictionary;
        }

        public async Task<DonorBatchProcessingResult<DonorInfoWithExpandedHla>> ExpandDonorHlaBatchAsync(
            IEnumerable<DonorInfo> donorInfos,
            string failureEventName)
        {
            return await ProcessBatchAsyncWithAnticipatedExceptions<HlaMetadataDictionaryException>(
                donorInfos,
                async d => await ExpandDonorHlaAsync(d),
                d => d.ToFailedDonorInfo(),
                failureEventName
            );
        }

        public async Task<DonorInfoWithExpandedHla> ExpandDonorHlaAsync(DonorInfo donorInfo)
        {
            var expandedHla = await donorInfo.HlaNames.MapByLocusAsync(GetExpandedHla);

            return new DonorInfoWithExpandedHla
            {
                DonorId = donorInfo.DonorId,
                DonorType = donorInfo.DonorType,
                HlaNames = donorInfo.HlaNames,
                MatchingHla = expandedHla
            };
        }

        private async Task<LocusInfo<IHlaMatchingMetadata>> GetExpandedHla(Locus locus, LocusInfo<string> hla)
        {
            if (string.IsNullOrEmpty(hla.Position1) || string.IsNullOrEmpty(hla.Position2))
            {
                return new LocusInfo<IHlaMatchingMetadata>(null);
            }

            return await hlaMetadataDictionary.GetLocusHlaMatchingMetadata(locus, hla);
        }

    }
}
