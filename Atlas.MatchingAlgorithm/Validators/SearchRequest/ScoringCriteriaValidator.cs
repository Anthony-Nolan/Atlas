using Atlas.Client.Models.Search.Requests;
using FluentValidation;

namespace Atlas.MatchingAlgorithm.Validators.SearchRequest
{
    internal class ScoringCriteriaValidator : AbstractValidator<ScoringCriteria>
    {
        public ScoringCriteriaValidator()
        {
            RuleFor(x => x.LociToScore).NotNull();
            RuleFor(x => x.LociToExcludeFromAggregateScore).NotNull();
        }
    }
}
