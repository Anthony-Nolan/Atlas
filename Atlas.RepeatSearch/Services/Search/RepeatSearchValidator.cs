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
        /// If the request is valid, the result set of the original search is marked as used, so the retention job keeps it.
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

            // The check that the set is complete also marks it as used, in one statement: the retention job then cannot start to delete
            // the set while this repeat search runs, so the result diff never reads a partly deleted set.
            if (!await canonicalResultSetRepository.MarkResultSetUsed(repeatSearchRequest.OriginalSearchId))
            {
                throw new ValidationException(
                    $"Can not run a repeat search for search request with Id: {repeatSearchRequest.OriginalSearchId}, as there is no record of the original search.");
            }
        }

        public async Task<bool> IsOriginalSearchResultSetStored(string originalSearchId)
        {
            var canonicalSet = await canonicalResultSetRepository.GetCanonicalResultSetSummary(originalSearchId);

            // A set in progress is still being stored, so it is not complete yet: treated as not stored.
            // A set that is being deleted is treated as not stored too.
            return canonicalSet is { Status: CanonicalResultSetStatus.Complete };
        }
    }
}