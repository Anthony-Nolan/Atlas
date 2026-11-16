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

        /// <returns>True if the result set of the original search is stored and complete, so a repeat search of it can run.</returns>
        public Task<bool> IsOriginalSearchResultSetStored(string originalSearchId);
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

            if (!await IsOriginalSearchResultSetStored(repeatSearchRequest.OriginalSearchId))
            {
                throw new ValidationException(
                    $"Can not run a repeat search for search request with Id: {repeatSearchRequest.OriginalSearchId}, as there is no record of the original search.");
            }
        }

        public async Task<bool> IsOriginalSearchResultSetStored(string originalSearchId)
        {
            var canonicalSet = await canonicalResultSetRepository.GetCanonicalResultSetSummary(originalSearchId);

            // A set in progress is still being stored, so it is not complete yet: treated as not stored.
            return canonicalSet is { Status: CanonicalResultSetStatus.Complete };
        }
    }
}