using FluentValidation;

namespace Atlas.MatchingAlgorithm.Validators.SearchRequest
{
    public class SearchRequestValidator : AbstractValidator<Client.Models.SearchRequests.MatchingRequest>
    {
        public SearchRequestValidator()
        {
            RuleFor(x => x.SearchType).NotNull().IsInEnum();
            RuleFor(x => x.MatchCriteria).NotNull().SetValidator(new MismatchCriteriaValidator());
            RuleFor(x => x.SearchHlaData).NotNull().SetValidator(new SearchHlaDataValidator());

            RuleFor(x => x.SearchHlaData.C).NotNull().When(x => x.MatchCriteria?.LocusMismatchCounts.C != null);
            RuleFor(x => x.SearchHlaData.Dqb1).NotNull().When(x => x.MatchCriteria?.LocusMismatchCounts.C != null);
            RuleFor(x => x.SearchHlaData.Dqb1).NotNull().When(x => x.MatchCriteria?.LocusMismatchCounts.Dqb1 != null);
            
            RuleFor(x => x.LociToExcludeFromAggregateScore).NotNull();
        }
    }
}