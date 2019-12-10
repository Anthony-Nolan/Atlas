using FluentValidation;
using Nova.DonorService.Client.Models.SearchableDonors;
using Nova.SearchAlgorithm.ApplicationInsights;
using Nova.SearchAlgorithm.Data.Models.DonorInfo;
using Nova.SearchAlgorithm.Extensions;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Services.Donors;
using Nova.SearchAlgorithm.Validators.DonorInfo;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Notifications;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Services.DataRefresh
{
    public interface IDonorInfoConverter
    {
        Task<IEnumerable<DonorInfo>> ConvertDonorInfoAsync(IEnumerable<SearchableDonorInformation> donorInfos);
    }

    public class DonorInfoConverter :
        DonorBatchProcessor<SearchableDonorInformation, DonorInfo, ValidationException>,
        IDonorInfoConverter
    {
        private const Priority LoggerPriority = Priority.Medium;
        private const string AlertSummary = "Donor Information Conversion Failure(s) in Search Algorithm";

        public DonorInfoConverter(
            ILogger logger,
            INotificationsClient notificationsClient)
            : base(logger, notificationsClient, LoggerPriority, AlertSummary)
        {
        }

        public async Task<IEnumerable<DonorInfo>> ConvertDonorInfoAsync(IEnumerable<SearchableDonorInformation> donorInfos)
        {
            return await ProcessBatchAsync(
                donorInfos,
                async info => await ConvertDonorInfo(info),
                exception => new DonorInfoValidationFailureEventModel(exception),
                info => new FailedDonorInfo(info)
                {
                    DonorId = info.DonorId.ToString(),
                    RegistryCode = info.RegistryCode
                });
        }

        private static async Task<DonorInfo> ConvertDonorInfo(SearchableDonorInformation info)
        {
            await new SearchableDonorInformationValidator().ValidateAndThrowAsync(info);

            return info.ToDonorInfo();
        }
    }
}
