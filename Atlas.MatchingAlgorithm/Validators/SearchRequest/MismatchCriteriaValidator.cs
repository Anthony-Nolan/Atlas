using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;
using FluentValidation;

namespace Atlas.MatchingAlgorithm.Validators.SearchRequest
{
    public class MismatchCriteriaValidator : AbstractValidator<MismatchCriteria>
    {
        public MismatchCriteriaValidator()
        {
            RuleFor(x => x.LocusMismatchA).NotNull().SetValidator(new LocusMismatchCriteriaValidator());
            RuleFor(x => x.LocusMismatchB).NotNull().SetValidator(new LocusMismatchCriteriaValidator());
            RuleFor(x => x.LocusMismatchDrb1).NotNull().SetValidator(new LocusMismatchCriteriaValidator());
            RuleFor(x => x.LocusMismatchC).SetValidator(new LocusMismatchCriteriaValidator());
            RuleFor(x => x.LocusMismatchDqb1).SetValidator(new LocusMismatchCriteriaValidator());
            RuleFor(x => x.DonorMismatchCount).NotNull().GreaterThanOrEqualTo(0).LessThanOrEqualTo(4);
        }
    }
}