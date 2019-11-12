using FluentValidation;
using Nova.SearchAlgorithm.Client.Models.SearchRequests;

namespace Nova.SearchAlgorithm.Validators
{
    public class MismatchCriteriaValidator : AbstractValidator<MismatchCriteria>
    {
        public MismatchCriteriaValidator()
        {
            RuleFor(x => x.LocusMismatchA).NotEmpty().SetValidator(new LocusMismatchCriteriaValidator());
            RuleFor(x => x.LocusMismatchB).NotEmpty().SetValidator(new LocusMismatchCriteriaValidator());
            RuleFor(x => x.LocusMismatchDrb1).NotEmpty().SetValidator(new LocusMismatchCriteriaValidator());
            RuleFor(x => x.LocusMismatchC).SetValidator(new LocusMismatchCriteriaValidator());
            RuleFor(x => x.LocusMismatchDqb1).SetValidator(new LocusMismatchCriteriaValidator());
            RuleFor(x => x.DonorMismatchCount).NotNull().GreaterThanOrEqualTo(0).LessThanOrEqualTo(4);
        }
    }
}