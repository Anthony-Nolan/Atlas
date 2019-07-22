using FluentValidation;
using Nova.SearchAlgorithm.Client.Models.SearchRequests;

namespace Nova.SearchAlgorithm.Validators
{
    public class LocusMismatchCriteriaValidator : AbstractValidator<LocusMismatchCriteria>
    {
        public LocusMismatchCriteriaValidator()
        {
            RuleFor(x => x.MismatchCount).GreaterThanOrEqualTo(0).LessThanOrEqualTo(2);
        }
    }
}