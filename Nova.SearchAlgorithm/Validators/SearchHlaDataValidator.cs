using FluentValidation;
using Nova.SearchAlgorithm.Client.Models.SearchRequests;

namespace Nova.SearchAlgorithm.Validators
{
    public class SearchHlaDataValidator : AbstractValidator<SearchHlaData>
    {
        public SearchHlaDataValidator()
        {
            RuleFor(x => x.LocusSearchHlaA).NotEmpty().SetValidator(new LocusSearchHlaValidator());
            RuleFor(x => x.LocusSearchHlaB).NotEmpty().SetValidator(new LocusSearchHlaValidator());
            RuleFor(x => x.LocusSearchHlaDrb1).NotEmpty().SetValidator(new LocusSearchHlaValidator());
            RuleFor(x => x.LocusSearchHlaC).SetValidator(new LocusSearchHlaValidator());
            RuleFor(x => x.LocusSearchHlaDqb1).SetValidator(new LocusSearchHlaValidator());
            RuleFor(x => x.LocusSearchHlaDpb1).SetValidator(new LocusSearchHlaValidator());
        }
    }
}