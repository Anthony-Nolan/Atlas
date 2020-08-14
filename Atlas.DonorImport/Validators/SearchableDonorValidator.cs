using Atlas.DonorImport.Models.FileSchema;
using FluentValidation;

namespace Atlas.DonorImport.Validators
{
    internal class SearchableDonorValidator : AbstractValidator<DonorUpdate>
    {
        public SearchableDonorValidator()
        {
            RuleFor(d => d.Hla).SetValidator(new SearchableHlaValidator());
        }
    }

    internal class SearchableHlaValidator : AbstractValidator<ImportedHla>
    {
        public SearchableHlaValidator()
        {
            RuleFor(h => h.A).NotEmpty();
            RuleFor(h => h.B).NotEmpty();
            RuleFor(h => h.DRB1).NotEmpty();
        }
    }
}