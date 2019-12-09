using Nova.SearchAlgorithm.ApplicationInsights;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Models.DonorInfo;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.Services.MatchingDictionary;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Notifications;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Services.Donors
{
    public interface IDonorHlaExpander
    {
        Task<IEnumerable<DonorInfoWithExpandedHla>> ExpandDonorHlaBatchAsync(IEnumerable<DonorInfo> donorInfos);
    }

    public class DonorHlaExpander : DonorBatchProcessor<DonorInfo, DonorInfoWithExpandedHla, MatchingDictionaryException>, IDonorHlaExpander
    {
        private const Priority LoggerPriority = Priority.Medium;
        private const string AlertSummary = "HLA Expansion Failure(s) in Search Algorithm";

        private readonly IExpandHlaPhenotypeService expandHlaPhenotypeService;

        public DonorHlaExpander(
            IExpandHlaPhenotypeService expandHlaPhenotypeService,
            ILogger logger,
            INotificationsClient notificationsClient)
            : base(logger, notificationsClient, LoggerPriority, AlertSummary)
        {
            this.expandHlaPhenotypeService = expandHlaPhenotypeService;
        }

        public async Task<IEnumerable<DonorInfoWithExpandedHla>> ExpandDonorHlaBatchAsync(IEnumerable<DonorInfo> donorInfos)
        {
            return await ProcessBatchAsync(
                donorInfos,
                async d => await CombineDonorAndExpandedHla(d),
                (exception, donor) => new MatchingDictionaryLookupFailureEventModel(exception, $"{donor.DonorId}"),
                d => d.DonorId.ToString());
        }

        private async Task<DonorInfoWithExpandedHla> CombineDonorAndExpandedHla(DonorInfo donorInfo)
        {
            var expandedHla = await expandHlaPhenotypeService.GetPhenotypeOfExpandedHla(
                new PhenotypeInfo<string>(donorInfo.HlaNames));

            return new DonorInfoWithExpandedHla
            {
                DonorId = donorInfo.DonorId,
                DonorType = donorInfo.DonorType,
                RegistryCode = donorInfo.RegistryCode,
                HlaNames = donorInfo.HlaNames,
                MatchingHla = expandedHla,
            };
        }
    }
}
