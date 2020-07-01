using Atlas.Common.GeneticData.PhenotypeInfo;
using FluentValidation;

namespace Atlas.MatchingAlgorithm.Validators.SearchRequest
{
    public class SearchHlaDataValidator : AbstractValidator<PhenotypeInfo<string>>
    {
        public SearchHlaDataValidator()
        {
            RuleFor(x => x.A).NotNull().SetValidator(new LocusSearchHlaValidator());
            RuleFor(x => x.B).NotNull().SetValidator(new LocusSearchHlaValidator());
            RuleFor(x => x.Drb1).NotNull().SetValidator(new LocusSearchHlaValidator());
            RuleFor(x => x.C).SetValidator(new LocusSearchHlaValidator());
            RuleFor(x => x.Dqb1).SetValidator(new LocusSearchHlaValidator());
            RuleFor(x => x.Dpb1).SetValidator(new LocusSearchHlaValidator());
        }
    }
    
    public class LocusSearchHlaValidator : AbstractValidator<LocusInfo<string>>
    {
        public LocusSearchHlaValidator()
        {
            RuleFor(x => x.Position1).NotEmpty();
            RuleFor(x => x.Position2).NotEmpty();
        }
    }
}