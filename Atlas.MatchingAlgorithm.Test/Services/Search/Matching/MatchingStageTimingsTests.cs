using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Search.Matching
{
    /// <summary>ATL-510 experiment: checks the collection reaches nested async iterators, as matching uses them.</summary>
    [TestFixture]
    public class MatchingStageTimingsTests
    {
        [Test]
        public async Task Begin_WhenStreamIsCountedThenEnumerated_SendsOneEventPerSubSearchRunWithRunNumbers()
        {
            var logger = Substitute.For<IAtlasLogger>();
            var sent = new List<(Dictionary<string, string> Props, Dictionary<string, double> Metrics)>();
            logger.When(l => l.SendEvent("Matching timings", Arg.Any<LogLevel>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<Dictionary<string, double>>()))
                .Do(c => sent.Add((c.ArgAt<Dictionary<string, string>>(2), c.ArgAt<Dictionary<string, double>>(3))));

            await RunSearch(logger, subSearchCount: 2);

            sent.Should().HaveCount(4);
            sent.Select(s => (s.Props["Run"], s.Props["SubSearch"])).Should().Equal(("1", "0"), ("1", "1"), ("2", "0"), ("2", "1"));
            sent.Should().OnlyContain(s => s.Metrics["Rows"] == 3 && s.Metrics["Drb1Rows"] == 3 && s.Props["LociOrder"] == "Drb1");
        }

        [Test]
        public async Task Run_WhenNoCollection_IsNull()
        {
            var logger = Substitute.For<IAtlasLogger>();

            await foreach (var _ in SubSearch(logger))
            {
            }

            MatchingStageTimings.Run.Should().BeNull();
            logger.DidNotReceiveWithAnyArgs().SendEvent(default);
        }

        private static async Task RunSearch(IAtlasLogger logger, int subSearchCount)
        {
            using var collection = MatchingStageTimings.Begin(subSearchCount);
            var stream = SubSearches(logger, subSearchCount);

            // Same shape as MatchScoringService today (ATL-502): count the stream, then enumerate it again.
            await stream.CountAsync();
            await foreach (var _ in stream)
            {
                await Task.Yield();
            }
        }

        private static async IAsyncEnumerable<int> SubSearches(IAtlasLogger logger, int count)
        {
            for (var i = 0; i < count; i++)
            {
                await foreach (var donor in SubSearch(logger))
                {
                    yield return donor;
                }
            }
        }

        private static async IAsyncEnumerable<int> SubSearch(IAtlasLogger logger)
        {
            var run = MatchingStageTimings.StartRun();
            await foreach (var donor in LocusStream())
            {
                yield return donor;
            }

            run?.Send(logger, null);
        }

        private static async IAsyncEnumerable<int> LocusStream()
        {
            var run = MatchingStageTimings.Run;
            run?.StartLocus(Locus.Drb1);
            await Task.Yield();
            for (var i = 0; i < 3; i++)
            {
                await Task.Yield();
                // Read Run again after a resume, as the repository does inside its row loop.
                MatchingStageTimings.Run.Should().BeSameAs(run);
                yield return i;
            }

            run?.AddSqlRead(Locus.Drb1, 0, 3);
        }
    }
}
