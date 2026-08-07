using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.DonorImport.ExternalInterface.Models;
using FluentValidation;
using FluentValidation.Results;

namespace Atlas.MatchingAlgorithm.Validators.DonorInfo;

/// <summary>
/// Allocation-free equivalent of <see cref="SearchableDonorInformationValidator"/>, for the data refresh donor import hot path
/// (<c>DonorInfoConverter</c>), which validates every donor in the registry - tens of millions of times per refresh.
/// </summary>
/// <remarks>
/// <para>
/// Constructing a <see cref="SearchableDonorInformationValidator"/> builds 8 validator objects and 20 <c>RuleFor</c> expression chains
/// in order to run 12 non-empty string checks, and was measured at ~64 microseconds and ~55 KB per donor (ATL-276). This class runs the
/// same 12 checks directly, in single-digit nanoseconds and with zero allocations.
/// </para>
/// <para>
/// This is an addition, not a replacement. <see cref="SearchableDonorInformationValidator"/> remains in use by
/// <see cref="SearchableDonorUpdateValidator"/>, and remains the definition of "a valid searchable donor". The two are held in step by
/// <c>SearchableDonorInformationFastValidatorTests</c>, which asserts identical outcomes - down to exception message, property names,
/// error codes and ordering - across an exhaustive per-locus matrix. If a rule is added to
/// <see cref="SearchableDonorInformationValidator"/>, or to the <c>PhenotypeHlaNamesValidator</c> family in Atlas.Common that it
/// delegates to, that test will fail: mirror the new rule here.
/// </para>
/// </remarks>
internal static class SearchableDonorInformationFastValidator
{
    /// <summary>
    /// FluentValidation's default message for both <c>NotNull()</c> and <c>NotEmpty()</c>. <c>{PropertyName}</c> is substituted with the
    /// property's display name, which for "Position1"/"Position2" is the property name unchanged.
    /// </summary>
    private const string NotEmptyMessageFormat = "'{0}' must not be empty.";

    private const string NotEmptyErrorCode = "NotEmptyValidator";

    // Reported property names are those of the PhenotypeInfoTransfer that SearchableDonorInformationValidator projects each donor into
    // before validating it, not those of SearchableDonorInformation itself - hence "Drb1.Position1" rather than "DRB1_1".
    private const string Position1 = nameof(LocusInfoTransfer<string>.Position1);
    private const string Position2 = nameof(LocusInfoTransfer<string>.Position2);
    private const string LocusA = nameof(PhenotypeInfoTransfer<string>.A);
    private const string LocusB = nameof(PhenotypeInfoTransfer<string>.B);
    private const string LocusC = nameof(PhenotypeInfoTransfer<string>.C);
    private const string LocusDpb1 = nameof(PhenotypeInfoTransfer<string>.Dpb1);
    private const string LocusDqb1 = nameof(PhenotypeInfoTransfer<string>.Dqb1);
    private const string LocusDrb1 = nameof(PhenotypeInfoTransfer<string>.Drb1);

    /// <summary>
    /// Throws the same <see cref="ValidationException"/> that
    /// <c>new SearchableDonorInformationValidator().ValidateAndThrowAsync(donorInfo)</c> would throw.
    /// </summary>
    public static void ValidateAndThrow(SearchableDonorInformation donorInfo)
    {
        if (IsValid(donorInfo))
        {
            return;
        }

        throw BuildValidationException(donorInfo);
    }

    /// <summary>
    /// Mirrors <c>PhenotypeHlaNamesValidator</c>. The only rule of
    /// <see cref="SearchableDonorInformationValidator"/> not represented here is <c>RuleFor(x => x.DonorId).NotNull()</c>, on a
    /// non-nullable <see cref="int"/>: it can never fail.
    /// </summary>
    private static bool IsValid(SearchableDonorInformation donorInfo) =>
        IsRequiredLocusValid(donorInfo.A_1, donorInfo.A_2) &&
        IsRequiredLocusValid(donorInfo.B_1, donorInfo.B_2) &&
        IsRequiredLocusValid(donorInfo.DRB1_1, donorInfo.DRB1_2) &&
        IsOptionalLocusValid(donorInfo.C_1, donorInfo.C_2) &&
        IsOptionalLocusValid(donorInfo.DQB1_1, donorInfo.DQB1_2) &&
        IsOptionalLocusValid(donorInfo.DPB1_1, donorInfo.DPB1_2);

    /// <summary>Mirrors <c>RequiredLocusHlaNamesValidator</c>: both positions must always be populated.</summary>
    private static bool IsRequiredLocusValid(string position1, string position2) =>
        !FailsNotEmpty(position1) && !FailsNotEmpty(position2);

    /// <summary>Mirrors <c>OptionalLocusHlaNamesValidator</c>: a locus may be untyped, but not half-typed.</summary>
    private static bool IsOptionalLocusValid(string position1, string position2) =>
        !OptionalPositionFailsNotEmpty(position1, position2) && !OptionalPositionFailsNotEmpty(position2, position1);

    /// <summary>
    /// An optional position is only required when the other position at that locus is typed.
    /// </summary>
    private static bool OptionalPositionFailsNotEmpty(string position, string otherPosition) =>
        !IsUntyped(otherPosition) && FailsNotEmpty(position);

    /// <summary>FluentValidation's <c>NotEmpty()</c> rejects null, empty and whitespace-only strings.</summary>
    private static bool FailsNotEmpty(string hlaName) => string.IsNullOrWhiteSpace(hlaName);

    /// <summary>
    /// The <c>When()</c> guards in <c>OptionalLocusHlaNamesValidator</c> use Atlas.Common's <c>IsNullOrEmpty()</c>, which - unlike
    /// <c>NotEmpty()</c> - counts a whitespace-only string as populated. The asymmetry is deliberately preserved: whitespace at one
    /// position makes the other position required, and then fails <c>NotEmpty()</c> on its own account.
    /// </summary>
    private static bool IsUntyped(string hlaName) => string.IsNullOrEmpty(hlaName);

    /// <remarks>
    /// Kept out of <see cref="ValidateAndThrow"/> so that the success path stays small enough to inline, and allocates nothing.
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ValidationException BuildValidationException(SearchableDonorInformation donorInfo)
    {
        // Locus order, and position order within a locus, follow the RuleFor declaration order in PhenotypeHlaNamesValidator, so that
        // logged validation errors read exactly as they did when FluentValidation produced them.
        var failures = new List<ValidationFailure>();

        AddRequiredLocusFailures(failures, LocusA, donorInfo.A_1, donorInfo.A_2);
        AddRequiredLocusFailures(failures, LocusB, donorInfo.B_1, donorInfo.B_2);
        AddRequiredLocusFailures(failures, LocusDrb1, donorInfo.DRB1_1, donorInfo.DRB1_2);
        AddOptionalLocusFailures(failures, LocusC, donorInfo.C_1, donorInfo.C_2);
        AddOptionalLocusFailures(failures, LocusDqb1, donorInfo.DQB1_1, donorInfo.DQB1_2);
        AddOptionalLocusFailures(failures, LocusDpb1, donorInfo.DPB1_1, donorInfo.DPB1_2);

        return new ValidationException(failures);
    }

    private static void AddRequiredLocusFailures(
        ICollection<ValidationFailure> failures,
        string locusName,
        string position1,
        string position2)
    {
        if (FailsNotEmpty(position1))
        {
            failures.Add(NotEmptyFailure(locusName, Position1, position1));
        }

        if (FailsNotEmpty(position2))
        {
            failures.Add(NotEmptyFailure(locusName, Position2, position2));
        }
    }

    private static void AddOptionalLocusFailures(
        ICollection<ValidationFailure> failures,
        string locusName,
        string position1,
        string position2)
    {
        if (OptionalPositionFailsNotEmpty(position1, position2))
        {
            failures.Add(NotEmptyFailure(locusName, Position1, position1));
        }

        if (OptionalPositionFailsNotEmpty(position2, position1))
        {
            failures.Add(NotEmptyFailure(locusName, Position2, position2));
        }
    }

    private static ValidationFailure NotEmptyFailure(string locusName, string positionName, string attemptedValue) =>
        new($"{locusName}.{positionName}", string.Format(NotEmptyMessageFormat, positionName), attemptedValue)
        {
            ErrorCode = NotEmptyErrorCode
        };
}
