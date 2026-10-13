using System.Collections.Generic;
using System.Linq;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using AwesomeAssertions;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.ExternalInterface.Models;

[TestFixture]
internal class MatchProbabilityRequestTests
{
    private const int DataRefreshRecordId = 42;

    [Test]
    public void SingleDonorMatchProbabilityInputs_CopyMatchingAlgorithmDataRefreshRecordId()
    {
        var input = new MultipleDonorMatchProbabilityInput(new IdentifiedMatchProbabilityRequest
        {
            SearchRequestId = "search-id",
            MatchingAlgorithmDataRefreshRecordId = DataRefreshRecordId
        })
        {
            Donors = new List<DonorInput> { new() { DonorId = 1 } }
        };

        input.MatchingAlgorithmDataRefreshRecordId.Should().Be(DataRefreshRecordId);
        input.SingleDonorMatchProbabilityInputs.Single().MatchingAlgorithmDataRefreshRecordId.Should().Be(DataRefreshRecordId);
    }

    [Test]
    public void SingleDonorMatchProbabilityInputs_CopyPatientGenotypeSetKey()
    {
        var key = new PatientGenotypeSetKey("typing-key", 7, "ABCDrb1Dqb1");
        var input = new MultipleDonorMatchProbabilityInput(new IdentifiedMatchProbabilityRequest { PatientGenotypeSetKey = key })
        {
            Donors = new List<DonorInput> { new() { DonorId = 1 } }
        };

        input.PatientGenotypeSetKey.Should().Be(key);
        input.SingleDonorMatchProbabilityInputs.Single().PatientGenotypeSetKey.Should().Be(key);
    }

    /// <summary>The key travels to both match prediction paths in the batch blob, which is written and read with Json.NET.</summary>
    [Test]
    public void PatientGenotypeSetKey_SurvivesTheBatchBlobsJsonRoundTrip()
    {
        var input = new MultipleDonorMatchProbabilityInput(new IdentifiedMatchProbabilityRequest
        {
            PatientGenotypeSetKey = new PatientGenotypeSetKey("typing-key", 7, "ABCDrb1Dqb1")
        })
        {
            Donors = new List<DonorInput> { new() { DonorId = 1 } }
        };

        var roundTripped = JsonConvert.DeserializeObject<MultipleDonorMatchProbabilityInput>(JsonConvert.SerializeObject(input));

        roundTripped.PatientGenotypeSetKey.Should().Be(input.PatientGenotypeSetKey);
    }

    /// <summary>
    /// The record id only has meaning for search, so a standalone match prediction request has no way to set it.
    /// </summary>
    [Test]
    public void ToSingleDonorMatchProbabilityInputs_ForStandaloneRequest_LeavesMatchingAlgorithmDataRefreshRecordIdNull()
    {
        var batch = new BatchedMatchPredictionRequests
        {
            Donors = new[] { new Donor { Id = 1 } }
        };

        var inputs = batch.ToSingleDonorMatchProbabilityInputs();

        inputs.Single().MatchingAlgorithmDataRefreshRecordId.Should().BeNull();
    }

    [TestCase(true)]
    [TestCase(false)]
    [TestCase(null)]
    public void SingleDonorMatchProbabilityInputs_CopyUsePrecomputedGenotypeSets(bool? usePrecomputedGenotypeSets)
    {
        var input = new MultipleDonorMatchProbabilityInput(new IdentifiedMatchProbabilityRequest
        {
            UsePrecomputedGenotypeSets = usePrecomputedGenotypeSets
        })
        {
            Donors = new List<DonorInput> { new() { DonorId = 1 } }
        };

        input.UsePrecomputedGenotypeSets.Should().Be(usePrecomputedGenotypeSets);
        input.SingleDonorMatchProbabilityInputs.Single().UsePrecomputedGenotypeSets.Should().Be(usePrecomputedGenotypeSets);
    }

    /// <summary>
    /// Unlike the record id, the override is on the base request, so the standalone endpoint can set it too.
    /// </summary>
    [Test]
    public void ToSingleDonorMatchProbabilityInputs_ForStandaloneRequest_CopiesUsePrecomputedGenotypeSets()
    {
        var batch = new BatchedMatchPredictionRequests
        {
            UsePrecomputedGenotypeSets = true,
            Donors = new[] { new Donor { Id = 1 } }
        };

        var inputs = batch.ToSingleDonorMatchProbabilityInputs();

        inputs.Single().UsePrecomputedGenotypeSets.Should().BeTrue();
    }
}
