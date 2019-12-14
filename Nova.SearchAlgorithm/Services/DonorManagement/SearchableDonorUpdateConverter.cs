using FluentValidation;
using Nova.DonorService.Client.Models.DonorUpdate;
using Nova.SearchAlgorithm.ApplicationInsights;
using Nova.SearchAlgorithm.Extensions;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Services.Donors;
using Nova.SearchAlgorithm.Validators.DonorInfo;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.ServiceBus.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Services.DonorManagement
{
    public interface ISearchableDonorUpdateConverter
    {
        Task<DonorBatchProcessingResult<DonorAvailabilityUpdate>> ConvertSearchableDonorUpdatesAsync(
            IEnumerable<ServiceBusMessage<SearchableDonorUpdateModel>> updates);
    }

    public class SearchableDonorUpdateConverter :
        DonorBatchProcessor<ServiceBusMessage<SearchableDonorUpdateModel>, DonorAvailabilityUpdate, ValidationException>,
        ISearchableDonorUpdateConverter
    {
        public SearchableDonorUpdateConverter(ILogger logger): base(logger)
        {
        }

        public async Task<DonorBatchProcessingResult<DonorAvailabilityUpdate>> ConvertSearchableDonorUpdatesAsync(
            IEnumerable<ServiceBusMessage<SearchableDonorUpdateModel>> updates)
        {
            return await ProcessBatchAsync(
                updates,
                async update => await GetDonorAvailabilityUpdate(update),
                exception => new DonorInfoValidationFailureEventModel(exception),
                update => new FailedDonorInfo(update)
                {
                    DonorId = update.DeserializedBody?.DonorId,
                    RegistryCode = update.DeserializedBody?.SearchableDonorInformation?.RegistryCode
                });
        }

        private static async Task<DonorAvailabilityUpdate> GetDonorAvailabilityUpdate(ServiceBusMessage<SearchableDonorUpdateModel> update)
        {
            await new DonorUpdateMessageValidator().ValidateAndThrowAsync(update);

            var body = update.DeserializedBody;

            return new DonorAvailabilityUpdate
            {
                UpdateSequenceNumber = update.SequenceNumber,
                DonorId = int.Parse(body.DonorId),
                DonorInfo = body.SearchableDonorInformation?.ToDonorInfo(),
                IsAvailableForSearch = body.IsAvailableForSearch
            };
        }
    }
}
