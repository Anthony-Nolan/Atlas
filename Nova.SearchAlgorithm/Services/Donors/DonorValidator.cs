using FluentValidation;
using Nova.SearchAlgorithm.ApplicationInsights;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Validators.InputDonor;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Notifications;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Services.Donors
{
    public interface IDonorValidator
    {
        Task<IEnumerable<InputDonor>> ValidateDonorsAsync(IEnumerable<InputDonor> inputDonors);
    }

    public class DonorValidator : InputDonorBatchProcessor<InputDonor, ValidationException>, IDonorValidator
    {
        private const Priority LoggerPriority = Priority.Medium;
        private const string AlertSummary = "Donor Validation Failure(s) in Search Algorithm";

        public DonorValidator(
            ILogger logger, 
            INotificationsClient notificationsClient) 
            : base(logger, notificationsClient, LoggerPriority, AlertSummary)
        {
        }

        public async Task<IEnumerable<InputDonor>> ValidateDonorsAsync(IEnumerable<InputDonor> inputDonors)
        {
            return await ProcessBatchAsync(
                inputDonors,
                async d => await ValidateDonor(d),
                (exception, donor) => new DonorValidationFailureEventModel(exception, $"{donor.DonorId}"));
        }

        private static async Task<InputDonor> ValidateDonor(InputDonor donor)
        {
            await new InputDonorValidator().ValidateAndThrowAsync(donor);
            return donor;
        }
    }
}
