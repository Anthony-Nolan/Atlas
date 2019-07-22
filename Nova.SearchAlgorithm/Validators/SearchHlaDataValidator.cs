using FluentValidation;
using Nova.SearchAlgorithm.Client.Models.SearchRequests;

namespace Nova.SearchAlgorithm.Validators
{
    public class SearchHlaDataValidator : AbstractValidator<SearchHlaData>
    {
        public SearchHlaDataValidator()
        {
            RuleFor(x => x.LocusSearchHlaA).NotNull();
            RuleFor(x => x.LocusSearchHlaB).NotNull();
            RuleFor(x => x.LocusSearchHlaDrb1).NotNull();
        }
    }
}