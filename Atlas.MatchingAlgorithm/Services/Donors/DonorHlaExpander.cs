using System;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.HlaMetadataDictionary.Exceptions;
using Atlas.MatchingAlgorithm.Models;
using Atlas.HlaMetadataDictionary;
using Atlas.Utils.Core.ApplicationInsights;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.Models.Lookups.MatchingLookup;
using Atlas.Utils.Models;

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
            ILogger logger)
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
            var hlaNamesCopy = new PhenotypeInfo<string>(donorInfo.HlaNames);
            var expandedHla = await hlaNamesCopy.WhenAllLoci(GetExpandedHla);

            return new DonorInfoWithExpandedHla
            {
                DonorId = donorInfo.DonorId,
                DonorType = donorInfo.DonorType,
                HlaNames = donorInfo.HlaNames,
                MatchingHla = expandedHla
            };
        }

        private async Task<Tuple<IHlaMatchingLookupResult, IHlaMatchingLookupResult>> GetExpandedHla(Locus locus, string hla1, string hla2)
        {
            if (string.IsNullOrEmpty(hla1) || string.IsNullOrEmpty(hla2))
            {
                return new Tuple<IHlaMatchingLookupResult, IHlaMatchingLookupResult>(null, null);
            }

            return await hlaMetadataDictionary.GetLocusHlaMatchingLookupResults(locus, new Tuple<string, string>(hla1, hla2));
        }

    }
}
