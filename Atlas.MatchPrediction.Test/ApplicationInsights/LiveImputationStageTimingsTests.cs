using System;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchPrediction.ApplicationInsights;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.ApplicationInsights;

[TestFixture]
internal class LiveImputationStageTimingsTests
{
    [Test]
    public async Task Time_AddsToTheCollectionOfTheCurrentFlow_AcrossAwaits()
    {
        using var collection = LiveImputationStageTimings.Begin();

        using (LiveImputationStageTimings.Time(LiveImputationStage.FrequencySetFetch))
        {
            await Task.Delay(20);
        }

        await Task.Run(() => LiveImputationStageTimings.CountSurvivors(7));
        LiveImputationStageTimings.CountConsolidationFallback();
        LiveImputationStageTimings.CountConsolidationFallback();

        collection.Timings.Get(LiveImputationStage.FrequencySetFetch).Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(15));
        collection.Timings.Get(LiveImputationStage.Pairing).Should().Be(TimeSpan.Zero);
        collection.Timings.Survivors.Should().Be(7);
        collection.Timings.ConsolidationFallbacks.Should().Be(2);
    }

    [Test]
    public void Time_WithNoCollectionRunning_DoesNothing()
    {
        var act = () =>
        {
            using (LiveImputationStageTimings.Time(LiveImputationStage.Pairing))
            {
            }

            LiveImputationStageTimings.CountConsolidationFallback();
            LiveImputationStageTimings.CountSurvivors(3);
        };

        act.Should().NotThrow();
    }

    [Test]
    public void Begin_WhenDisposed_StopsCollecting()
    {
        LiveImputationStageTimings timings;
        using (var collection = LiveImputationStageTimings.Begin())
        {
            timings = collection.Timings;
        }

        LiveImputationStageTimings.CountConsolidationFallback();

        timings.ConsolidationFallbacks.Should().Be(0);
    }

    [Test]
    public async Task Begin_InConcurrentFlows_KeepsEachFlowsTimingsApart()
    {
        static async Task<LiveImputationStageTimings> Donor(int fallbacks)
        {
            using var collection = LiveImputationStageTimings.Begin();
            await Task.Yield();
            for (var i = 0; i < fallbacks; i++)
            {
                LiveImputationStageTimings.CountConsolidationFallback();
                await Task.Yield();
            }

            return collection.Timings;
        }

        var results = await Task.WhenAll(Enumerable.Range(1, 5).Select(Donor));

        results.Select(t => t.ConsolidationFallbacks).Should().Equal(1, 2, 3, 4, 5);
    }
}
