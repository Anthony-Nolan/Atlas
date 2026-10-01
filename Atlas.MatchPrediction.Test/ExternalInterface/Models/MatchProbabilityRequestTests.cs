using System.Collections.Generic;
using System.Linq;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using AwesomeAssertions;
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
}
