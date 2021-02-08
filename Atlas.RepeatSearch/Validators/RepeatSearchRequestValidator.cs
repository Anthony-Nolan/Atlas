using Atlas.Client.Models.Search.Requests;
using Atlas.MatchingAlgorithm.Validators.SearchRequest;
using FluentValidation;

namespace Atlas.RepeatSearch.Validators
{
    public class RepeatSearchRequestValidator : AbstractValidator<RepeatSearchRequest>
    {
        public RepeatSearchRequestValidator()
        {
            RuleFor(r => r.SearchRequest).SetValidator(new SearchRequestValidator());
            RuleFor(r => r.OriginalSearchId).NotEmpty();
            RuleFor(r => r.SearchCutoffDate).NotEmpty();
        }
    }
}