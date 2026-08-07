using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.Validators.DonorInfo;
using AutoFixture;
using AwesomeAssertions;
using FluentValidation;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Validators.DonorUpdates;

/// <summary>
/// Pins <see cref="SearchableDonorInformationFastValidator"/> to <see cref="SearchableDonorInformationValidator"/>, which it replaces on
/// the data refresh donor import hot path. Equivalence is asserted on everything observable: whether a <see cref="ValidationException"/>
/// is thrown, its message, and the property name, message, error code and attempted value of every failure, in order.
/// </summary>
/// <remarks>
/// The matrix is exhaustive by construction. Donor validation reads only the 12 HLA name fields; each locus is validated independently of
/// the others; and each field's outcome depends only on which of four classes its value falls into (null, empty, whitespace-only, or
/// populated). 6 loci x 4 x 4 = 96 cases therefore cover every reachable single-locus outcome, and the remaining tests cover donor id
/// (which has a rule of its own) plus accumulation and ordering of failures across loci.
/// </remarks>
[TestFixture]
public class SearchableDonorInformationFastValidatorTests
{
    public enum HlaName
    {
        Null,
        Empty,
        Whitespace,
        Populated
    }

    private Fixture fixture;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
    }

    private static IEnumerable<TestCaseData> EveryHlaNamePairAtEveryLocus() =>
        from locus in Enum.GetValues<Locus>()
        from position1 in Enum.GetValues<HlaName>()
        from position2 in Enum.GetValues<HlaName>()
        select new TestCaseData(locus, position1, position2);

    [TestCaseSource(nameof(EveryHlaNamePairAtEveryLocus))]
    public async Task ValidateAndThrow_ForEveryHlaNamePairAtEveryLocus_BehavesIdenticallyToFluentValidationValidator(
        Locus locus,
        HlaName position1,
        HlaName position2)
    {
        var donor = FullyTypedDonor();
        SetHlaAt(donor, locus, HlaNameOf(position1), HlaNameOf(position2));

        await AssertBehavesIdenticallyToFluentValidationValidator(donor);
    }

    [TestCase(int.MinValue)]
    [TestCase(-1)]
    [TestCase(0)]
    [TestCase(int.MaxValue)]
    public async Task ValidateAndThrow_ForAnyDonorId_BehavesIdenticallyToFluentValidationValidator(int donorId)
    {
        var donor = FullyTypedDonor();
        donor.DonorId = donorId;

        await AssertBehavesIdenticallyToFluentValidationValidator(donor);
    }

    [Test]
    public async Task ValidateAndThrow_ForFullyTypedDonor_BehavesIdenticallyToFluentValidationValidator()
    {
        await AssertBehavesIdenticallyToFluentValidationValidator(FullyTypedDonor());
    }

    [Test]
    public async Task ValidateAndThrow_ForDonorWithNoHlaAtAll_BehavesIdenticallyToFluentValidationValidator()
    {
        await AssertBehavesIdenticallyToFluentValidationValidator(new SearchableDonorInformation { DonorId = fixture.Create<int>() });
    }

    [Test]
    public async Task ValidateAndThrow_ForDonorWithWhitespaceHlaAtEveryPosition_BehavesIdenticallyToFluentValidationValidator()
    {
        var donor = FullyTypedDonor();
        foreach (var locus in Enum.GetValues<Locus>())
        {
            SetHlaAt(donor, locus, "  ", "  ");
        }

        await AssertBehavesIdenticallyToFluentValidationValidator(donor);
    }

    [Test]
    public async Task ValidateAndThrow_ForDonorFailingAtEveryLocus_BehavesIdenticallyToFluentValidationValidator()
    {
        var donor = FullyTypedDonor();
        SetHlaAt(donor, Locus.A, null, "  ");
        SetHlaAt(donor, Locus.B, string.Empty, fixture.Create<string>());
        SetHlaAt(donor, Locus.Drb1, "  ", null);
        SetHlaAt(donor, Locus.C, fixture.Create<string>(), null);
        SetHlaAt(donor, Locus.Dqb1, null, fixture.Create<string>());
        SetHlaAt(donor, Locus.Dpb1, "  ", fixture.Create<string>());

        await AssertBehavesIdenticallyToFluentValidationValidator(donor);
    }

    [Test]
    public async Task ValidateAndThrow_ForDonorWithEveryOptionalLocusHalfTyped_BehavesIdenticallyToFluentValidationValidator()
    {
        var donor = FullyTypedDonor();
        SetHlaAt(donor, Locus.C, fixture.Create<string>(), null);
        SetHlaAt(donor, Locus.Dqb1, fixture.Create<string>(), null);
        SetHlaAt(donor, Locus.Dpb1, fixture.Create<string>(), null);

        await AssertBehavesIdenticallyToFluentValidationValidator(donor);
    }

    [Test]
    public void ValidateAndThrow_ForValidDonor_AllocatesNothing()
    {
        const int warmUpIterations = 1_000;
        const int measuredIterations = 10_000;

        var donor = FullyTypedDonor();

        for (var i = 0; i < warmUpIterations; i++)
        {
            SearchableDonorInformationFastValidator.ValidateAndThrow(donor);
        }

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < measuredIterations; i++)
        {
            SearchableDonorInformationFastValidator.ValidateAndThrow(donor);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        allocated.Should().Be(0, "the whole point of this validator is that the data refresh can run it once per donor for free");
    }

    private static async Task AssertBehavesIdenticallyToFluentValidationValidator(SearchableDonorInformation donor)
    {
        var expected = await FluentValidationOutcomeFor(donor);
        var actual = FastValidatorOutcomeFor(donor);

        actual.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
    }

    private static async Task<ValidationOutcome> FluentValidationOutcomeFor(SearchableDonorInformation donor)
    {
        try
        {
            await new SearchableDonorInformationValidator().ValidateAndThrowAsync(donor);
            return ValidationOutcome.Valid;
        }
        catch (ValidationException e)
        {
            return ValidationOutcome.From(e);
        }
    }

    private static ValidationOutcome FastValidatorOutcomeFor(SearchableDonorInformation donor)
    {
        try
        {
            SearchableDonorInformationFastValidator.ValidateAndThrow(donor);
            return ValidationOutcome.Valid;
        }
        catch (ValidationException e)
        {
            return ValidationOutcome.From(e);
        }
    }

    private SearchableDonorInformation FullyTypedDonor() =>
        new()
        {
            DonorId = fixture.Create<int>(),
            A_1 = fixture.Create<string>(),
            A_2 = fixture.Create<string>(),
            B_1 = fixture.Create<string>(),
            B_2 = fixture.Create<string>(),
            C_1 = fixture.Create<string>(),
            C_2 = fixture.Create<string>(),
            DQB1_1 = fixture.Create<string>(),
            DQB1_2 = fixture.Create<string>(),
            DPB1_1 = fixture.Create<string>(),
            DPB1_2 = fixture.Create<string>(),
            DRB1_1 = fixture.Create<string>(),
            DRB1_2 = fixture.Create<string>()
        };

    private string HlaNameOf(HlaName hlaName) =>
        hlaName switch
        {
            HlaName.Null => null,
            HlaName.Empty => string.Empty,
            HlaName.Whitespace => "  ",
            HlaName.Populated => fixture.Create<string>(),
            _ => throw new ArgumentOutOfRangeException(nameof(hlaName), hlaName, null)
        };

    private static void SetHlaAt(SearchableDonorInformation donor, Locus locus, string position1, string position2)
    {
        switch (locus)
        {
            case Locus.A:
                donor.A_1 = position1;
                donor.A_2 = position2;
                break;
            case Locus.B:
                donor.B_1 = position1;
                donor.B_2 = position2;
                break;
            case Locus.C:
                donor.C_1 = position1;
                donor.C_2 = position2;
                break;
            case Locus.Dpb1:
                donor.DPB1_1 = position1;
                donor.DPB1_2 = position2;
                break;
            case Locus.Dqb1:
                donor.DQB1_1 = position1;
                donor.DQB1_2 = position2;
                break;
            case Locus.Drb1:
                donor.DRB1_1 = position1;
                donor.DRB1_2 = position2;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(locus), locus, null);
        }
    }

    private record ValidationOutcome(string ExceptionMessage, IReadOnlyList<Failure> Failures)
    {
        public static readonly ValidationOutcome Valid = new(null, Array.Empty<Failure>());

        public static ValidationOutcome From(ValidationException exception) =>
            new(
                exception.Message,
                exception.Errors.Select(e => new Failure(e.PropertyName, e.ErrorMessage, e.ErrorCode, e.AttemptedValue)).ToList());
    }

    private record Failure(string PropertyName, string ErrorMessage, string ErrorCode, object AttemptedValue);
}
