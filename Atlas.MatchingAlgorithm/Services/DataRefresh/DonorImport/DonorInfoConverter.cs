using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Extensions;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.MatchingAlgorithm.Validators.DonorInfo;
using FluentValidation;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh.DonorImport
{
    public interface IDonorInfoConverter
    {
        Task<DonorBatchProcessingResult<DonorInfo>> ConvertDonorInfoAsync(
            IEnumerable<SearchableDonorInformation> donorInfos,
            string failureEventName);
    }

    public class DonorInfoConverter :
        DonorBatchProcessor<SearchableDonorInformation, DonorInfo>,
        IDonorInfoConverter
    {
        public DonorInfoConverter(IMatchingAlgorithmImportLogger logger) : base(logger)
        {
        }

        public async Task<DonorBatchProcessingResult<DonorInfo>> ConvertDonorInfoAsync(
            IEnumerable<SearchableDonorInformation> donorInfos,
            string failureEventName)
        {
            return await ProcessBatchAsyncWithAnticipatedExceptions<ValidationException>(
                donorInfos,
                async info => await ConvertDonorInfo(info),
                info => new FailedDonorInfo(info)
                {
                    AtlasDonorId = info.DonorId
                },
                failureEventName);
        }

        private static async Task<DonorInfo> ConvertDonorInfo(SearchableDonorInformation donorInfo)
        {
            await new SearchableDonorInformationValidator().ValidateAndThrowAsync(donorInfo);
            return donorInfo.ToDonorInfo();
        }
    }
}
