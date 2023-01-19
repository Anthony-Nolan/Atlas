using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.FileSchema.Models;
using FluentValidation;

namespace Atlas.DonorImport.Validators
{
    internal class SearchableDonorValidator : AbstractValidator<DonorUpdate>
    {
        public SearchableDonorValidator()
        {
            When(donorUpdate => donorUpdate.ChangeType != ImportDonorChangeType.Delete, () =>
            {
                RuleFor(d => d.Hla)
                    .NotEmpty()
                    .SetValidator(new SearchableHlaValidator());
            });
        }
    }

    internal class SearchableHlaValidator : AbstractValidator<ImportedHla>
    {
        public SearchableHlaValidator()
        {
            RuleFor(h => h.A).NotEmpty().SetValidator(new RequiredImportedLocusValidator());
            RuleFor(h => h.B).NotEmpty().SetValidator(new RequiredImportedLocusValidator());
            RuleFor(h => h.DRB1).NotEmpty().SetValidator(new RequiredImportedLocusValidator());

            RuleFor(h => h.C).SetValidator(new OptionalImportedLocusValidator());
            RuleFor(h => h.DPB1).SetValidator(new OptionalImportedLocusValidator());
            RuleFor(h => h.DQB1).SetValidator(new OptionalImportedLocusValidator());
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
        }
    }

    internal class OptionalImportedLocusValidator : AbstractValidator<ImportedLocus>
    {
        public OptionalImportedLocusValidator()
        {
            RuleFor(l => l.Dna)
                .SetValidator(new OptionalTwoFieldStringValidator())
                .When(l => l.Dna != null);

            RuleFor(l => l.Serology)
                .SetValidator(new OptionalTwoFieldStringValidator())
                .When(l => l.Serology != null);
        }
    }

    internal class OptionalTwoFieldStringValidator : AbstractValidator<TwoFieldStringData>
    {
        public OptionalTwoFieldStringValidator()
        {
            RuleFor(d => d.Field1)
                .NotEmpty()
                .When(d => !d.Field2.IsNullOrEmpty());
        }
    }
}