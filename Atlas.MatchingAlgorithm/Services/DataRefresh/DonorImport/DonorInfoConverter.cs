using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
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
                info => Task.FromResult(ConvertDonorInfo(info)),
                info => new FailedDonorInfo(info)
                {
                    AtlasDonorId = info.DonorId
                },
                failureEventName);
        }

        // Deliberately not SearchableDonorInformationValidator: identical outcome, but this runs once per donor in the registry, and
        // building a FluentValidation rule tree per call dominated the cost of the entire conversion. See the remarks on
        // SearchableDonorInformationFastValidator.
        private static DonorInfo ConvertDonorInfo(SearchableDonorInformation donorInfo)
        {
            SearchableDonorInformationFastValidator.ValidateAndThrow(donorInfo);
            return donorInfo.ToDonorInfo();
        }
    }
}
