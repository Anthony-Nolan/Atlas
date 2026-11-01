using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders.ScoringInfoBuilders;
using Atlas.MatchingAlgorithm.Services.Search.Scoring;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;
using static Atlas.MatchingAlgorithm.Services.Search.Scoring.ScoringStageTimings;

namespace Atlas.MatchingAlgorithm.Test.Services.Search.Scoring
{
    [TestFixture]
    internal class ScoringStageTimingsTests
    {
        [Test]
        public async Task Time_AddsToTheCollectionOfTheCurrentFlow_AcrossAwaits()
        {
            using var collection = Begin();

            using (Time(ScoringStage.Grading))
            {
                await Task.Delay(20);
            }

            await Task.Run(CountGradeCalculation);
            RecordLookup(Locus.A, TimeSpan.FromMilliseconds(5));
            RecordLookup(Locus.A, TimeSpan.FromTicks(10));

            collection.Timings.Get(ScoringStage.Grading).Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(15));
            collection.Timings.Get(ScoringStage.Confidence).Should().Be(TimeSpan.Zero);
            collection.Timings.GradeCalculations.Should().Be(1);
            collection.Timings.Lookups(Locus.A).Should().Be(2);
            collection.Timings.SlowLookups(Locus.A).Should().Be(1);
        }

        [Test]
        public void Time_WithNoCollectionRunning_DoesNothing()
        {
            var act = () =>
            {
                using (Time(ScoringStage.Ranking))
                {
                }

                CountConfidenceCalculation();
                RecordLookup(Locus.B, TimeSpan.FromSeconds(1));
                RecordDonor(new PhenotypeInfo<IHlaScoringMetadata>(), TimeSpan.FromSeconds(1));
            };

            act.Should().NotThrow();
        }

        [Test]
        public void RecordDonor_CountsTheDonorUnderItsMostAmbiguousTypingKind()
        {
            using var collection = Begin();
            var single = new HlaScoringMetadataBuilder().Build();
            var multiple = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(Enumerable.Range(0, 3).Select(_ => new SingleAlleleScoringInfoBuilder().Build()))
                    .Build())
                .Build();

            RecordDonor(new PhenotypeInfo<IHlaScoringMetadata>(single), TimeSpan.FromMilliseconds(2));
            RecordDonor(new PhenotypeInfo<IHlaScoringMetadata>(single).SetPosition(Locus.B, LocusPosition.One, multiple), TimeSpan.FromMilliseconds(9));

            collection.Timings.Donors.Should().Be(2);
            collection.Timings.DonorsOfKind(DonorTypingKind.SingleAllele).Should().Be(1);
            collection.Timings.DonorsOfKind(DonorTypingKind.MultipleAllele).Should().Be(1);
        }

        [Test]
        public void Send_SendsOneScoringTimingsEventWithStageMetrics()
        {
            var logger = Substitute.For<IAtlasLogger>();
            using var collection = Begin();
            CountAntigenMatchCalculation();

            collection.Timings.Send(logger, "Search", 4, new[] { Locus.A, Locus.B });

            logger.Received(1).SendEvent(
                "Scoring timings",
                LogLevel.Info,
                Arg.Is<Dictionary<string, string>>(p => p["Path"] == "Search" && p["LociToScore"] == "A,B"),
                Arg.Is<Dictionary<string, double>>(m =>
                    m["DistinctDonorPhenotypes"] == 4
                    && m["AntigenMatchCalculations"] == 1
                    && m.ContainsKey("StageMatchStreamWaitMs")
                    && m.ContainsKey("SlowLookupsDpb1")));
        }

        [Test]
        public async Task Begin_InConcurrentFlows_KeepsEachFlowsTimingsApart()
        {
            static async Task<ScoringStageTimings> Request(int calculations)
            {
                using var collection = Begin();
                await Task.Yield();
                for (var i = 0; i < calculations; i++)
                {
                    CountGradeCalculation();
                    await Task.Yield();
                }

                return collection.Timings;
            }

            var results = await Task.WhenAll(Enumerable.Range(1, 5).Select(Request));

            results.Select(t => t.GradeCalculations).Should().Equal(1, 2, 3, 4, 5);
        }
    }
}
