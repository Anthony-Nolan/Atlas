#nullable enable

using System.Linq;
using Atlas.MatchingAlgorithm.Services.Debug.Precompute;
using FluentValidation;

namespace Atlas.MatchingAlgorithm.Validators.Debug;

internal class GenotypeSetDebugRequestValidator : AbstractValidator<GenotypeSetDebugRequest>
{
    /// <summary>
    /// Each donor is imputed once for each locus combination, and the request must finish within the HTTP time limit of
    /// the functions app (230 seconds).
    /// </summary>
    internal const int MaxExternalDonorCodes = 20;

    public GenotypeSetDebugRequestValidator()
    {
        RuleFor(r => r.HlaTyping)
            .NotNull()
            .When(r => !HasDonorCodes(r))
            .WithMessage("Give an HLA typing or external donor codes.");

        RuleFor(r => r.HlaTyping)
            .Null()
            .When(HasDonorCodes)
            .WithMessage("Give an HLA typing or external donor codes, not both.");

        RuleFor(r => r.ExternalDonorCodes)
            .Must(codes => codes!.Distinct().Count() <= MaxExternalDonorCodes)
            .When(HasDonorCodes)
            .WithMessage($"Give at most {MaxExternalDonorCodes} external donor codes.");

        RuleForEach(r => r.ExternalDonorCodes)
            .NotEmpty()
            .WithMessage("An external donor code is empty.");

        RuleFor(r => r.FrequencySetMetadata)
            .Null()
            .When(HasDonorCodes)
            .WithMessage("A donor's frequency set is chosen from the donor's own codes. To use another set, give its id.");

        RuleFor(r => r.FrequencySetMetadata)
            .Null()
            .When(r => r.HaplotypeFrequencySetId != null)
            .WithMessage("Give frequency set metadata or a frequency set id, not both.");

        RuleForEach(r => r.AllowedLociKeys)
            .IsInEnum();
    }

    private static bool HasDonorCodes(GenotypeSetDebugRequest request) => request.ExternalDonorCodes is { Count: > 0 };
}
