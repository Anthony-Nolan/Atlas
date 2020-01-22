using FluentValidation;
using Nova.DonorService.Client.Models.DonorUpdate;
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
            IEnumerable<ServiceBusMessage<SearchableDonorUpdate>> updates, string failureEventName);
    }

    public class SearchableDonorUpdateConverter :
        DonorBatchProcessor<ServiceBusMessage<SearchableDonorUpdate>, DonorAvailabilityUpdate, ValidationException>,
        ISearchableDonorUpdateConverter
    {
        public SearchableDonorUpdateConverter(ILogger logger) : base(logger)
        {
        }

        public async Task<DonorBatchProcessingResult<DonorAvailabilityUpdate>> ConvertSearchableDonorUpdatesAsync(
            IEnumerable<ServiceBusMessage<SearchableDonorUpdate>> updates, string failureEventName)
        {
            return await ProcessBatchAsync(
                updates,
                async update => await GetDonorAvailabilityUpdate(update),
                update => new FailedDonorInfo(update)
                {
                    DonorId = update.DeserializedBody?.DonorId,
                    RegistryCode = update.DeserializedBody?.SearchableDonorInformation?.RegistryCode
                },
                failureEventName);
        }

        private static async Task<DonorAvailabilityUpdate> GetDonorAvailabilityUpdate(ServiceBusMessage<SearchableDonorUpdate> update)
        {
            await new DonorUpdateMessageValidator().ValidateAndThrowAsync(update);

            var body = update.DeserializedBody;

            return new DonorAvailabilityUpdate
            {
                UpdateSequenceNumber = update.SequenceNumber,
                UpdateDateTime = body.PublishedDateTime.Value,
                DonorId = int.Parse(body.DonorId),
                DonorInfo = body.SearchableDonorInformation?.ToDonorInfo(),
                IsAvailableForSearch = body.IsAvailableForSearch
            };
        }
    }
}
