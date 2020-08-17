using Atlas.DonorImport.Models.FileSchema;
using FluentValidation;

namespace Atlas.DonorImport.Validators
{
    internal class SearchableDonorValidator : AbstractValidator<DonorUpdate>
    {
        public SearchableDonorValidator()
        {
            RuleFor(d => d.Hla)
                .SetValidator(new SearchableHlaValidator())
                .When(d => d.ChangeType == ImportDonorChangeType.Create || d.ChangeType == ImportDonorChangeType.Edit );
        }
    }

    internal class SearchableHlaValidator : AbstractValidator<ImportedHla>
    {
        public SearchableHlaValidator()
        {
            RuleFor(h => h.A).NotEmpty().SetValidator(new ImportedLocusValidator());
            RuleFor(h => h.B).NotEmpty().SetValidator(new ImportedLocusValidator());
            RuleFor(h => h.DRB1).NotEmpty().SetValidator(new ImportedLocusValidator());
        }
    }

    internal class ImportedLocusValidator : AbstractValidator<ImportedLocus>
    {
        public ImportedLocusValidator()
        {
            RuleFor(l => l.Dna).NotEmpty().SetValidator(new TwoFieldStringValidator());
        }   
    }
    internal class TwoFieldStringValidator : AbstractValidator<TwoFieldStringData>
    {
        public TwoFieldStringValidator()
        {
            RuleFor(d => d.Field1).NotEmpty();
            RuleFor(d => d.Field2).NotEmpty();
        }   
    }
    
}