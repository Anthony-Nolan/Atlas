using System.Collections.Generic;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Services.Precompute;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.Precompute;

/// <summary>
/// When a batch can skip the cache warm-up for live imputation (ATL-476).
/// </summary>
[TestFixture]
internal class DonorGenotypeSetBatchContextMayComputeLiveTests
{
    private static readonly PrecomputedPatientGenotypeSetRow PatientRow = new(false, [1]);

    [Test]
    public void MayComputeLive_WhenEveryDonorInputAndThePatientHaveARow_IsFalse()
    {
        var context = Context(new() { { 1, Row() }, { 3, Row() } }, PatientRow);

        context.MayComputeLive([Donor(1, 2), Donor(3)]).Should().BeFalse();
    }

    [Test]
    public void MayComputeLive_WhenADonorInputHasNoRow_IsTrue()
    {
        var context = Context(new() { { 1, Row() } }, PatientRow);

        context.MayComputeLive([Donor(1), Donor(2)]).Should().BeTrue();
    }

    [Test]
    public void MayComputeLive_WhenThePatientHasNoRow_IsTrue()
    {
        var context = Context(new() { { 1, Row() } }, patientRow: null);

        context.MayComputeLive([Donor(1)]).Should().BeTrue();
    }

    [Test]
    public void MayComputeLive_WhenPrecomputeIsDisabled_IsTrue()
    {
        var context = DonorGenotypeSetBatchContext.Disabled(42);

        context.MayComputeLive([Donor(1)]).Should().BeTrue();
    }

    [Test]
    public void MayComputeLive_WhenTheBatchFellBack_IsTrue()
    {
        var context = DonorGenotypeSetBatchContext.Enabled(
            42,
            PrecomputedDonorGenotypeSetLookup.Unavailable(DonorGenotypeSetSource.ActiveDatabaseUnknown));

        context.MayComputeLive([Donor(1)]).Should().BeTrue();
    }

    private static DonorGenotypeSetBatchContext Context(
        Dictionary<int, PrecomputedDonorGenotypeSetRow> rows,
        PrecomputedPatientGenotypeSetRow patientRow) =>
        DonorGenotypeSetBatchContext.Enabled(
            42,
            new PrecomputedDonorGenotypeSetLookup("ABCDrb1Dqb1", null, rows, patientRow));

    private static PrecomputedDonorGenotypeSetRow Row() => new(7, false, [1, 2, 3]);

    private static DonorInput Donor(params int[] donorIds) => new() { DonorIds = [.. donorIds] };
}
