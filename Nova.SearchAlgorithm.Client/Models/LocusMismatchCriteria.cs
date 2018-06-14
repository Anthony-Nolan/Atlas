using FluentValidation;

namespace Nova.SearchAlgorithm.Client.Models
{
    [FluentValidation.Attributes.Validator(typeof(LocusMismatchCriteriaValidator))]
    public class LocusMismatchCriteria
    {
        /// <summary>
        /// String representation of the 1st search HLA type position at this locus.
        /// </summary>
        public string SearchHla1 { get; set; }

        /// <summary>
        /// String representation of the 2nd search HLA type position at this locus.
        /// </summary>
        public string SearchHla2 { get; set; }

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
            RuleFor(x => x.SearchHla1).NotEmpty();
            RuleFor(x => x.SearchHla2).NotEmpty();
        }
    }
}