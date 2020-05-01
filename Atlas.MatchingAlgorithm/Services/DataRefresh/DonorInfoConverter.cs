using FluentValidation;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Extensions;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.MatchingAlgorithm.Validators.DonorInfo;
using Atlas.Utils.Core.ApplicationInsights;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh
{
    public interface IDonorInfoConverter
    {
        Task<DonorBatchProcessingResult<DonorInfo>> ConvertDonorInfoAsync(
            IEnumerable<SearchableDonorInformation> donorInfos,
            string failureEventName);
    }

    public class DonorInfoConverter :
        DonorBatchProcessor<SearchableDonorInformation, DonorInfo, ValidationException>,
        IDonorInfoConverter
    {
        public DonorInfoConverter(ILogger logger)
            : base(logger)
        {
        }

        public async Task<DonorBatchProcessingResult<DonorInfo>> ConvertDonorInfoAsync(
            IEnumerable<SearchableDonorInformation> donorInfos,
            string failureEventName)
        {
            return await ProcessBatchAsync(
                donorInfos,
                async info => await ConvertDonorInfo(info),
                info => new FailedDonorInfo(info)
                {
                    DonorId = info.DonorId.ToString()
                },
                failureEventName);
        }

        private static async Task<DonorInfo> ConvertDonorInfo(SearchableDonorInformation info)
        {
            await new SearchableDonorInformationValidator().ValidateAndThrowAsync(info);

            return info.ToDonorInfo();
        }
    }
}
