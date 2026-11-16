using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.RepeatSearch.Data.Models;
using Atlas.RepeatSearch.Data.Repositories;
using Atlas.RepeatSearch.Validators;
using FluentValidation;

namespace Atlas.RepeatSearch.Services.Search
{
    public interface IRepeatSearchValidator
    {
        /// <summary>
        /// Will throw a validation exception if the provided request is not valid.
        /// </summary>
        public Task ValidateRepeatSearchAndThrow(RepeatSearchRequest repeatSearchRequest);
    }

    internal class RepeatSearchValidator : IRepeatSearchValidator
    {
        private readonly ICanonicalResultSetRepository canonicalResultSetRepository;

        public RepeatSearchValidator(ICanonicalResultSetRepository canonicalResultSetRepository)
        {
            this.canonicalResultSetRepository = canonicalResultSetRepository;
        }

        public async Task ValidateRepeatSearchAndThrow(RepeatSearchRequest repeatSearchRequest)
        {
            var validator = new RepeatSearchRequestValidator();
            await validator.ValidateAndThrowAsync(repeatSearchRequest);

            var canonicalSet = await canonicalResultSetRepository.GetCanonicalResultSetSummary(repeatSearchRequest.OriginalSearchId);

            // A set in progress is still being stored, so it is not complete yet: treated as not stored.
            if (canonicalSet == null || canonicalSet.Status != CanonicalResultSetStatus.Complete)
            {
                throw new ValidationException(
                    $"Can not run a repeat search for search request with Id: {repeatSearchRequest.OriginalSearchId}, as there is no record of the original search.");
            }
        }
    }
}