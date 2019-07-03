using FluentValidation;
using Nova.SearchAlgorithm.Client.Models.SearchRequests;

namespace Nova.SearchAlgorithm.Validators
{
    public class LocusSearchHlaValidator : AbstractValidator<LocusSearchHla>
    {
        public LocusSearchHlaValidator()
        {
            RuleFor(x => x.SearchHla1).NotEmpty().NotNull();
            RuleFor(x => x.SearchHla2).NotEmpty().NotNull();
        }
    }
}