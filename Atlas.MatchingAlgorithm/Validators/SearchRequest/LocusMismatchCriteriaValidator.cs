using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;
using FluentValidation;

namespace Atlas.MatchingAlgorithm.Validators.SearchRequest
{
    public class LocusMismatchCriteriaValidator : AbstractValidator<LocusMismatchCriteria>
    {
        public LocusMismatchCriteriaValidator()
        {
            RuleFor(x => x.MismatchCount).GreaterThanOrEqualTo(0).LessThanOrEqualTo(2);
        }
    }
}