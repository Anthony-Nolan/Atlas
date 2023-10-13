using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.FileSchema.Models;
using FluentValidation;

namespace Atlas.DonorImport.Validators
{
    internal class SearchableDonorValidator : AbstractValidator<DonorUpdate>
    {
        public SearchableDonorValidator(SearchableDonorValidatorContext context)
        {
            When(donorUpdate => donorUpdate.ChangeType != ImportDonorChangeType.Delete, () =>
            {
                RuleFor(d => d.Hla)
                    .NotNull()
                    .SetValidator(new SearchableHlaValidator());
            });

            When(donorUpdate => donorUpdate.ChangeType == ImportDonorChangeType.Edit, () =>
            {
                RuleFor(d => d.RecordId)
                    .Must(context.ExternalDonorCodes.Contains)
                    .WithMessage("Donor is not present in the database.");
            });

            When(donorUpdate => donorUpdate.ChangeType == ImportDonorChangeType.Create && donorUpdate.UpdateMode == UpdateMode.Differential, () =>
            {
                RuleFor(d => d.RecordId)
                    .Must(id => !context.ExternalDonorCodes.Contains(id))
                    .WithMessage("Donor is already present in the database.");
            });
        }
    }

    internal class SearchableHlaValidator : AbstractValidator<ImportedHla>
    {
        public SearchableHlaValidator()
        {
            RuleFor(h => h.A).NotNull().SetValidator(new RequiredImportedLocusValidator(Locus.A));
            RuleFor(h => h.B).NotNull().SetValidator(new RequiredImportedLocusValidator(Locus.B));
            RuleFor(h => h.DRB1).NotNull().SetValidator(new RequiredImportedLocusValidator(Locus.Drb1));

            RuleFor(h => h.C).SetValidator(new OptionalImportedLocusValidator(Locus.C));
            RuleFor(h => h.DPB1).SetValidator(new OptionalImportedLocusValidator(Locus.Dpb1));
            RuleFor(h => h.DQB1).SetValidator(new OptionalImportedLocusValidator(Locus.Dqb1));
        }
    }

    internal enum HlaFieldType
    {
        Dna,
        Serology
    }

    internal class RequiredImportedLocusValidator : AbstractValidator<ImportedLocus>
    {
        public RequiredImportedLocusValidator(Locus locus)
        {
            var errorMessage = $"Required locus {locus}: minimum HLA typing has not been provided";

            RuleFor(l => l.Dna)
                .NotNull()
                .WithMessage(errorMessage)
                .SetValidator(new RequiredTwoFieldStringValidator(locus, HlaFieldType.Dna))
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse - removing the null check leads to null argument exceptions by the validator
                .Unless(l => l.Serology != null && new RequiredTwoFieldStringValidator(locus, HlaFieldType.Serology).Validate(l.Serology).IsValid)
                .WithMessage(errorMessage);
        }
    }

    internal class RequiredTwoFieldStringValidator : AbstractValidator<TwoFieldStringData>
    {
        public RequiredTwoFieldStringValidator(Locus locus, HlaFieldType hlaFieldType)
        {
            RuleFor(x => x.Field1)
                .NotEmpty()
                .WithMessage($"Required locus {locus}, {hlaFieldType}: Field1 cannot be empty");
        }
    }

    internal class OptionalImportedLocusValidator : AbstractValidator<ImportedLocus>
    {
        public OptionalImportedLocusValidator(Locus locus)
        {
            RuleFor(l => l.Dna)
                .SetValidator(new OptionalTwoFieldStringValidator(locus, HlaFieldType.Dna))
                .When(l => l.Dna != null);

            RuleFor(l => l.Serology)
                .SetValidator(new OptionalTwoFieldStringValidator(locus, HlaFieldType.Serology))
                .When(l => l.Serology != null);
        }
    }

    internal class OptionalTwoFieldStringValidator : AbstractValidator<TwoFieldStringData>
    {
        public OptionalTwoFieldStringValidator(Locus locus, HlaFieldType hlaFieldType)
        {
            RuleFor(d => d.Field1)
                .NotEmpty()
                .When(d => !d.Field2.IsNullOrEmpty())
                .WithMessage($"Optional locus {locus}, {hlaFieldType}: Field1 cannot be empty when Field2 is provided");
        }
    }
    
    internal class SearchableDonorValidatorContext
    {
        public SearchableDonorValidatorContext(IReadOnlyCollection<string> externalDonorCodes)
        {
            ExternalDonorCodes = externalDonorCodes;
        }

        public IReadOnlyCollection<string> ExternalDonorCodes { get; }
    }
}