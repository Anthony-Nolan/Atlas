using System;
using Atlas.DonorImport.FileSchema.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.DonorImport.Validators;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.ApplicationInsights;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.Models;
using FluentValidation.Results;

namespace Atlas.DonorImport.Services
{
    internal interface IDonorUpdateCategoriser
    {
        /// <summary>
        /// Categorises donor updates into two groups: "valid" and "invalid", as determined by <see cref="SearchableDonorValidator"/>.
        /// Validation errors are logged as events.
        /// </summary>
        Task<DonorUpdateCategoriserResults> Categorise(IEnumerable<DonorUpdate> donorUpdates, string fileName);
    }

    internal class DonorUpdateCategoriserResults
    {
        /// <summary>
        /// Donors that passed validation by <see cref="SearchableDonorValidator"/>
        /// </summary>
        public IReadOnlyCollection<DonorUpdate> ValidDonors { get; set; }

        /// <summary>
        /// Donors that failed validation by <see cref="SearchableDonorValidator"/>
        /// </summary>
        public IReadOnlyCollection<DonorUpdate> InvalidDonors { get; set; }
    }

    internal class DonorUpdateCategoriser : IDonorUpdateCategoriser
    {
        private readonly ILogger logger;
        private readonly IDonorReadRepository donorReadRepository;
        private readonly IDonorImportFailureRepository donorImportFailureRepository;

        public DonorUpdateCategoriser(ILogger logger, IDonorReadRepository donorReadRepository, IDonorImportFailureRepository donorImportFailureRepository)
        {
            this.logger = logger;
            this.donorReadRepository = donorReadRepository;
            this.donorImportFailureRepository = donorImportFailureRepository;
        }
        
        /// <inheritdoc />
        public async Task<DonorUpdateCategoriserResults> Categorise(IEnumerable<DonorUpdate> donorUpdates, string fileName)
        {
            if (!donorUpdates.Any())
            {
                return new DonorUpdateCategoriserResults();
            }

            var existingExternalDonorCodes = await donorReadRepository.GetExistingExternalDonorCodes(donorUpdates.Select(d => d.RecordId));
            var searchableDonorValidator = new SearchableDonorValidator(new SearchableDonorValidatorContext(existingExternalDonorCodes));
            var validationResults = donorUpdates.Select(ValidateDonorIsSearchable).ToList();
            var (validDonors, invalidDonors) = validationResults.ReifyAndSplit(vr => vr.IsValid);

            LogErrors(validationResults);
            await SaveDonorImportFailures(validationResults, fileName);

            return new DonorUpdateCategoriserResults
            {
                ValidDonors = validDonors.Select(d => d.DonorUpdate).ToList(),
                InvalidDonors = invalidDonors.Select(d => d.DonorUpdate).ToList()
            };

            SearchableDonorValidationResult ValidateDonorIsSearchable(DonorUpdate donorUpdate)
            {
                var validationResult = searchableDonorValidator.Validate(donorUpdate);

                return new SearchableDonorValidationResult
                {
                    DonorUpdate = donorUpdate,
                    IsValid = validationResult.IsValid,
                    Errors = validationResult.Errors.ToList(),
                    ErrorMessage = !validationResult.IsValid ? string.Join(";", validationResult.Errors.Select(e => e.ErrorMessage)) : null
                };
            }
        }

        private void LogErrors(IEnumerable<SearchableDonorValidationResult> validationResults)
        {
            var errorMessages = validationResults
                .Where(vr => !vr.IsValid)
                .GroupBy(vr => vr.ErrorMessage)
                .Select(errorMessage => new
                {
                    ErrorMessage = errorMessage.Key,
                    FailedDonorIds = errorMessage.Select(e => e.DonorUpdate.RecordId).ToList()
                });

            foreach (var error in errorMessages)
            {
                var errorEvent = new SearchableDonorValidationErrorEventModel(error.ErrorMessage, error.FailedDonorIds);
                logger.SendEvent(errorEvent);
            }
        }

        private async Task SaveDonorImportFailures(IEnumerable<SearchableDonorValidationResult> validationResults, string fileName)
        {
            var failureTime = DateTimeOffset.UtcNow;
            
            var donorImportFailures = new List<DonorImportFailure>();
            foreach (var validationResult in validationResults.Where(vr => !vr.IsValid))
            {
                donorImportFailures.AddRange(validationResult.Errors.Select(error => new DonorImportFailure
                {
                    ExternalDonorCode = validationResult.DonorUpdate.RecordId,
                    DonorType = validationResult.DonorUpdate.DonorType.ToDatabaseType().ToString(),
                    EthnicityCode = validationResult.DonorUpdate.Ethnicity,
                    RegistryCode = validationResult.DonorUpdate.RegistryCode,
                    UpdateFile = fileName,
                    UpdateProperty = error.PropertyName,
                    FailureReason = error.ErrorMessage,
                    FailureTime = failureTime
                }));
            }

            await donorImportFailureRepository.BulkInsert(donorImportFailures);
        }

        private class SearchableDonorValidationResult
        {
            public DonorUpdate DonorUpdate { get; init; }
            public bool IsValid { get; init; }
            public IReadOnlyCollection<ValidationFailure> Errors { get; init; }
            public string ErrorMessage { get; init; }
        }
    }
}
