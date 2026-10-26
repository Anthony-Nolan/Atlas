using System;
using System.Collections.Generic;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.ExternalInterface
{
    [TestFixture]
    public class MatchPredictionMessagePropertiesTests
    {
        private const string SearchRequestId = "search-request-id";
        private const string MatchPredictionRequestId = "match-prediction-request-id";

        [Test]
        public void ForParallelBatchRequest_ReturnsSearchRequestId()
        {
            var request = new ParallelMatchPredictionBatchRequest { SearchRequestId = SearchRequestId };

            MatchPredictionMessageProperties.ForParallelBatchRequest(request).Should().Equal(new Dictionary<string, object>
            {
                { "SearchRequestId", SearchRequestId }
            });
        }

        [Test]
        public void ForParallelBatchResult_ReturnsSearchIdentifier()
        {
            var searchIdentifier = Guid.NewGuid();
            var result = new ParallelMatchPredictionBatchResult { SearchIdentifier = searchIdentifier };

            MatchPredictionMessageProperties.ForParallelBatchResult(result).Should().Equal(new Dictionary<string, object>
            {
                { "SearchIdentifier", searchIdentifier }
            });
        }

        [Test]
        public void ForRequest_ReturnsMatchPredictionRequestIdAndSearchRequestId()
        {
            var request = new IdentifiedMatchPredictionRequest
            {
                Id = MatchPredictionRequestId,
                SingleDonorMatchProbabilityInput = new SingleDonorMatchProbabilityInput { SearchRequestId = SearchRequestId }
            };

            MatchPredictionMessageProperties.ForRequest(request).Should().Equal(new Dictionary<string, object>
            {
                { "MatchPredictionRequestId", MatchPredictionRequestId },
                { "SearchRequestId", SearchRequestId }
            });
        }

        [Test]
        public void ForRequest_WithoutInput_ReturnsNullSearchRequestId()
        {
            var request = new IdentifiedMatchPredictionRequest { Id = MatchPredictionRequestId };

            MatchPredictionMessageProperties.ForRequest(request).Should().Equal(new Dictionary<string, object>
            {
                { "MatchPredictionRequestId", MatchPredictionRequestId },
                { "SearchRequestId", null }
            });
        }

        [Test]
        public void ForResultLocation_ReturnsMatchPredictionRequestIdAndSearchRequestId()
        {
            var location = new MatchPredictionResultLocation
            {
                MatchPredictionRequestId = MatchPredictionRequestId,
                SearchRequestId = SearchRequestId
            };

            MatchPredictionMessageProperties.ForResultLocation(location).Should().Equal(new Dictionary<string, object>
            {
                { "MatchPredictionRequestId", MatchPredictionRequestId },
                { "SearchRequestId", SearchRequestId }
            });
        }
    }
}
