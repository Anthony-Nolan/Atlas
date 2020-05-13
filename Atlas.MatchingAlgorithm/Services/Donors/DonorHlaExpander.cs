using System;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.HlaMetadataDictionary.Exceptions;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.MatchingDictionary;
using Atlas.Utils.Core.ApplicationInsights;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.Models.Lookups.MatchingLookup;

namespace Atlas.MatchingAlgorithm.Services.Donors
{
    public interface IDonorHlaExpander
    {
        Task<DonorBatchProcessingResult<DonorInfoWithExpandedHla>> ExpandDonorHlaBatchAsync(
            IEnumerable<DonorInfo> donorInfos,
            string failureEventName,
            string hlaDatabaseVersion);
    }

    public class DonorHlaExpander : DonorBatchProcessor<DonorInfo, DonorInfoWithExpandedHla>, IDonorHlaExpander
    {
        private readonly IHlaMetadataDictionary hlaMetadataDictionary;

        public DonorHlaExpander(
            IHlaMetadataDictionary hlaMetadataDictionary, //QQ this becomes a factory, which is given the version string below. //QQ name "new" for clarity.
            ILogger logger)
            : base(logger)
        {
            this.hlaMetadataDictionary = hlaMetadataDictionary;
        }

        public async Task<DonorBatchProcessingResult<DonorInfoWithExpandedHla>> ExpandDonorHlaBatchAsync(
            IEnumerable<DonorInfo> donorInfos,
            string failureEventName,
            string hlaDatabaseVersion)
        {
            return await ProcessBatchAsyncWithAnticipatedExceptions<MatchingDictionaryException>(
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
