using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.ServiceBus;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.MatchingAlgorithm.Validators.DonorInfo;
using FluentValidation;

namespace Atlas.MatchingAlgorithm.Services.DonorManagement
{
    public interface ISearchableDonorUpdateConverter
    {
        Task<DonorBatchProcessingResult<DonorAvailabilityUpdate>> ConvertSearchableDonorUpdatesAsync(
            IEnumerable<ServiceBusMessage<SearchableDonorUpdate>> updates);
    }

    public class SearchableDonorUpdateConverter :
        DonorBatchProcessor<ServiceBusMessage<SearchableDonorUpdate>, DonorAvailabilityUpdate>,
        ISearchableDonorUpdateConverter
    {
        private const string UpdateFailureEventName = "Searchable Donor Update parsing Failure(s) in the Matching Algorithm's Continuous Donor Update sytem";

        public SearchableDonorUpdateConverter(IMatchingAlgorithmImportLogger logger) : base(logger)
        {
        }

        public async Task<DonorBatchProcessingResult<DonorAvailabilityUpdate>> ConvertSearchableDonorUpdatesAsync(
            IEnumerable<ServiceBusMessage<SearchableDonorUpdate>> updates)
        {
            return await ProcessBatchAsyncWithAnticipatedExceptions<ValidationException>(
                updates,
                async update => await GetDonorAvailabilityUpdate(update),
                update => new FailedDonorInfo(update)
                {
                    AtlasDonorId = update.DeserializedBody?.DonorId
                },
                UpdateFailureEventName);
        }

        private static async Task<DonorAvailabilityUpdate> GetDonorAvailabilityUpdate(ServiceBusMessage<SearchableDonorUpdate> update)
        {
            await new DonorUpdateMessageValidator().ValidateAndThrowAsync(update);

            var body = update.DeserializedBody;

            return new DonorAvailabilityUpdate
            {
                UpdateSequenceNumber = update.SequenceNumber,
                UpdateDateTime = body.PublishedDateTime,
                DonorId = body.DonorId,
                DonorInfo = body.SearchableDonorInformation?.ToDonorInfo(),
                IsAvailableForSearch = body.IsAvailableForSearch
            };
        }
    }
}
