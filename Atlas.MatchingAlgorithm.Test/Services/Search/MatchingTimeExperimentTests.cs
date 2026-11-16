using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Common.ApplicationInsights;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchRequests;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Search
{
    [TestFixture]
    public class MatchingTimeExperimentTests
    {
        private IAtlasLogger logger;
        private Dictionary<string, string> sentProperties;
        private Dictionary<string, double> sentMetrics;
        private int scoringCalls;

        [SetUp]
        public void SetUp()
        {
            logger = Substitute.For<IAtlasLogger>();
            logger.SendEvent(MatchingTimeExperiment.EventName, Arg.Any<LogLevel>(), Arg.Do<Dictionary<string, string>>(p => sentProperties = p),
                Arg.Do<Dictionary<string, double>>(m => sentMetrics = m));
            scoringCalls = 0;
        }

        [Test]
        public async Task Run_MatchingOnly_ReadsAllMatchesOnceAndDoesNotScore()
        {
            await MatchingTimeExperiment.Run(
                MatchingTimeExperimentMode.MatchingOnly, Request(), 2, Matches(1, 2, 2), Scoring, logger);

            scoringCalls.Should().Be(0);
            sentMetrics["Rows"].Should().Be(3);
            sentMetrics["DistinctDonors"].Should().Be(2);
            sentMetrics["SubSearchCount"].Should().Be(2);
            sentMetrics["FirstDonorMs"].Should().BeGreaterThanOrEqualTo(0);
            sentMetrics["MatchingEndMs"].Should().BeGreaterThanOrEqualTo(sentMetrics["FirstDonorMs"]);
            sentProperties["Mode"].Should().Be(nameof(MatchingTimeExperimentMode.MatchingOnly));
            sentProperties["Outcome"].Should().Be("Completed");
        }

        [Test]
        public async Task Run_MatchingAndScoring_ScoresTheMatches()
        {
            await MatchingTimeExperiment.Run(
                MatchingTimeExperimentMode.MatchingAndScoring, Request(), 1, Matches(1, 2, 3), Scoring, logger);

            scoringCalls.Should().Be(1);
            sentMetrics["ScoredDonors"].Should().Be(3);
            sentMetrics["DistinctDonors"].Should().Be(3);
        }

        [Test]
        public async Task Run_WhenMatchingFails_SendsTheExceptionTypeOnlyAndDoesNotThrow()
        {
            const string message = "message that must not be sent";

            await MatchingTimeExperiment.Run(
                MatchingTimeExperimentMode.MatchingOnly, Request(), 1, FailingMatches(message), Scoring, logger);

            sentProperties["Outcome"].Should().Be("Failed");
            sentProperties["ExceptionType"].Should().Be(typeof(InvalidOperationException).FullName);
            sentProperties.Values.Should().NotContain(v => v.Contains(message));
            sentMetrics["Rows"].Should().Be(1);
        }

        [Test]
        public async Task Run_SendsTheRequestShapeWithoutPatientHla()
        {
            var request = Request();

            await MatchingTimeExperiment.Run(MatchingTimeExperimentMode.MatchingOnly, request, 1, Matches(1), Scoring, logger);

            sentProperties["LocusMismatches"].Should().NotBeNullOrEmpty();
            sentMetrics["DonorMismatchCount"].Should().Be(request.MatchCriteria.DonorMismatchCount);
            var hla = request.SearchHlaData.A.Position1;
            sentProperties.Values.Should().NotContain(v => v.Contains(hla));
        }

        [Test]
        public void Mode_WhenNotSet_IsNormal()
        {
            MatchingTimeExperiment.ModeOverride = null;
            Environment.SetEnvironmentVariable(MatchingTimeExperiment.ModeSettingName, null);

            MatchingTimeExperiment.Mode.Should().Be(MatchingTimeExperimentMode.Normal);
        }

        private static SearchRequest Request() => new SearchRequestBuilder().Build();

        private Task<IEnumerable<MatchAndScoreResult>> Scoring(IAsyncEnumerable<MatchResult> matches)
        {
            scoringCalls++;
            return ScoreAll(matches);
        }

        private static async Task<IEnumerable<MatchAndScoreResult>> ScoreAll(IAsyncEnumerable<MatchResult> matches)
        {
            var results = new List<MatchAndScoreResult>();
            await foreach (var match in matches)
            {
                results.Add(new MatchAndScoreResult { MatchResult = match });
            }

            return results;
        }

        private static async IAsyncEnumerable<MatchResult> Matches(params int[] donorIds)
        {
            foreach (var donorId in donorIds.Select(id => new MatchResult(id)))
            {
                await Task.Yield();
                yield return donorId;
            }
        }

        private static async IAsyncEnumerable<MatchResult> FailingMatches(string message)
        {
            await Task.Yield();
            yield return new MatchResult(1);
            throw new InvalidOperationException(message);
        }
    }
}
