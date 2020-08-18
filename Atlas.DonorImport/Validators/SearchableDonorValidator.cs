using System.Data;
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
                .When(d => d.ChangeType == ImportDonorChangeType.Create || d.ChangeType == ImportDonorChangeType.Edit);
        }
    }

    internal class SearchableHlaValidator : AbstractValidator<ImportedHla>
    {
        public SearchableHlaValidator()
        {
            RuleFor(h => h.A).NotEmpty().SetValidator(new RequiredImportedLocusValidator());
            RuleFor(h => h.B).NotEmpty().SetValidator(new RequiredImportedLocusValidator());
            RuleFor(h => h.DRB1).NotEmpty().SetValidator(new RequiredImportedLocusValidator());
        }
    }

    internal class RequiredImportedLocusValidator : AbstractValidator<ImportedLocus>
    {
        public RequiredImportedLocusValidator()
        {
            RuleFor(l => l)
                .Must(l => l.Dna != null && new RequiredTwoFieldStringValidator().Validate(l.Dna).IsValid)
                .Unless(l => l.Serology != null && new RequiredTwoFieldStringValidator().Validate(l.Serology).IsValid);
        }   
    }
    internal class RequiredTwoFieldStringValidator : AbstractValidator<TwoFieldStringData>
    {
        public RequiredTwoFieldStringValidator()
        {
            RuleFor(d => d.Field1).NotEmpty();
            RuleFor(d => d.Field2).NotEmpty();
        }   
    }
    
}