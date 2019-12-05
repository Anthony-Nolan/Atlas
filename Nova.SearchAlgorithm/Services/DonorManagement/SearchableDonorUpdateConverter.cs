using FluentValidation;
using Nova.DonorService.Client.Models.DonorUpdate;
using Nova.SearchAlgorithm.ApplicationInsights;
using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.Extensions;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Services.Donors;
using Nova.SearchAlgorithm.Validators.DonorInfo;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Notifications;
using Nova.Utils.ServiceBus.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Services.DonorManagement
{
    public interface ISearchableDonorUpdateConverter
    {
        Task<IEnumerable<DonorAvailabilityUpdate>> ConvertSearchableDonorUpdatesAsync(
            IEnumerable<ServiceBusMessage<SearchableDonorUpdateModel>> updates);
    }

    public class SearchableDonorUpdateConverter : 
        DonorBatchProcessor<ServiceBusMessage<SearchableDonorUpdateModel>, DonorAvailabilityUpdate, DonorUpdateValidationException>,
        ISearchableDonorUpdateConverter
    {
        private const Priority LoggerPriority = Priority.Medium;
        private const string AlertSummary = "Searchable Donor Update Conversion Failure(s) in Search Algorithm";

        public SearchableDonorUpdateConverter(
            ILogger logger, 
            INotificationsClient notificationsClient) 
            : base(logger, notificationsClient, LoggerPriority, AlertSummary)
        {
        }

        public async Task<IEnumerable<DonorAvailabilityUpdate>> ConvertSearchableDonorUpdatesAsync(
            IEnumerable<ServiceBusMessage<SearchableDonorUpdateModel>> updates)
        {
            return await ProcessBatchAsync(
                updates,
                async update => await GetDonorAvailabilityUpdate(update),
                (exception, update) => new DonorUpdateFailureEventModel(exception, $"{update.DeserializedBody?.DonorId}"), 
                update => update.DeserializedBody?.DonorId);
        }

        private static async Task<DonorAvailabilityUpdate> GetDonorAvailabilityUpdate(ServiceBusMessage<SearchableDonorUpdateModel> update)
        {
            try
            {
                await new DonorUpdateMessageValidator().ValidateAndThrowAsync(update);
            }
            catch (ValidationException ex)
            {
                throw new DonorUpdateValidationException(update, ex);
            }

            var body = update.DeserializedBody;

            return new DonorAvailabilityUpdate
            {
                UpdateSequenceNumber = update.SequenceNumber,
                DonorId = int.Parse(body.DonorId),
                DonorInfo = body.SearchableDonorInformation?.ToInputDonor(),
                IsAvailableForSearch = body.IsAvailableForSearch
            };
        }
    }
}
