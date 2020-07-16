using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;
using FluentValidation;

namespace Atlas.MatchingAlgorithm.Validators.SearchRequest
{
    public class ScoringCriteriaValidator : AbstractValidator<ScoringCriteria>
    {
        public ScoringCriteriaValidator()
        {
            RuleFor(x => x.LociToScore).NotNull();
            RuleFor(x => x.LociToExcludeFromAggregateScore).NotNull();
        }
    }
}
