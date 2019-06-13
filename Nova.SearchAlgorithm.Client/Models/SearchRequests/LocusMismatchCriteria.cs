using FluentValidation;
using FluentValidation.Attributes;

namespace Nova.SearchAlgorithm.Client.Models.SearchRequests
{
    [Validator(typeof(LocusMismatchCriteriaValidator))]
    public class LocusMismatchCriteria
    {
        /// <summary>
        /// Total number of mismatches permitted, either 0, 1 or 2.
        /// </summary>
        public int MismatchCount { get; set; }
    }

    public class LocusMismatchCriteriaValidator : AbstractValidator<LocusMismatchCriteria>
    {
        public LocusMismatchCriteriaValidator()
        {
            RuleFor(x => x.MismatchCount).GreaterThanOrEqualTo(0).LessThanOrEqualTo(2);
        }
    }
}