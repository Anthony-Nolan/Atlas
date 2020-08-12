using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.HaplotypeFrequencies
{
    [TestFixture]
    public class FrequencyConsolidatorTests
    {
        private IFrequencyConsolidator frequencyConsolidator;

        [SetUp]
        public void SetUp()
        {
            frequencyConsolidator = new FrequencyConsolidator();
        }

        [TestCase(new[] {Locus.A})]
        [TestCase(new[] {Locus.B})]
        [TestCase(new[] {Locus.C})]
        [TestCase(new[] {Locus.Dqb1})]
        [TestCase(new[] {Locus.Drb1})]
        [TestCase(new[] {Locus.C, Locus.Dqb1})]
        public void ConsolidateFrequenciesForHaplotype_ConsolidatesFrequenciesAtExcludedLoci(Locus[] excludedLoci)
        {
            var haplotypeBuilder = new LociInfoBuilder<string>("default-hla");

            var matchingHla = haplotypeBuilder.Build();
            var mismatchAtExcludedLociHla = haplotypeBuilder.WithDataAt("other-hla", excludedLoci).Build();

            const int matchingFrequency = 1;
            const int mismatchAtExcludedLociFrequency = 45;

            var frequencies = new Dictionary<LociInfo<string>, HaplotypeFrequency>
            {
                {
                    matchingHla,
                    HaplotypeFrequencyBuilder.New.WithFrequency(matchingFrequency).WithHaplotype(matchingHla).Build()
                },
                {
                    mismatchAtExcludedLociHla,
                    HaplotypeFrequencyBuilder.New.WithFrequency(mismatchAtExcludedLociFrequency).WithHaplotype(mismatchAtExcludedLociHla).Build()
                },
            };

            var consolidatedFrequencies = frequencyConsolidator.ConsolidateFrequenciesForHaplotype(
                frequencies,
                haplotypeBuilder.Build(),
                excludedLoci.ToHashSet()
            );

            consolidatedFrequencies.Should().Be(matchingFrequency + mismatchAtExcludedLociFrequency);
        }

        public void PreConsolidateFrequencies_PreConsolidatesFrequenciesAtExpectedLociSets()
        {
            var haplotypeBuilder = new LociInfoBuilder<string>("default-hla");

            var matchingHla = haplotypeBuilder.Build();
            var mismatchAtC = haplotypeBuilder.WithDataAt("other-hla", Locus.C).Build();
            var mismatchAtDqb1 = haplotypeBuilder.WithDataAt("other-hla", Locus.Dqb1).Build();
            var mismatchAtCAndDqb1 = haplotypeBuilder.WithDataAt("other-hla", Locus.Dqb1, Locus.C).Build();

            var frequencies = new Dictionary<LociInfo<string>, HaplotypeFrequency>
            {
                {
                    matchingHla,
                    HaplotypeFrequencyBuilder.New.WithFrequency(1).WithHaplotype(matchingHla).Build()
                },
                {
                    mismatchAtC,
                    HaplotypeFrequencyBuilder.New.WithFrequency(20).WithHaplotype(mismatchAtC).Build()
                },
                {
                    mismatchAtDqb1,
                    HaplotypeFrequencyBuilder.New.WithFrequency(300).WithHaplotype(mismatchAtDqb1).Build()
                },
                {
                    mismatchAtCAndDqb1,
                    HaplotypeFrequencyBuilder.New.WithFrequency(4000).WithHaplotype(mismatchAtCAndDqb1).Build()
                },
            };

            var consolidatedFrequencies = frequencyConsolidator.PreConsolidateFrequencies(frequencies);

            consolidatedFrequencies[haplotypeBuilder.Build()].Should().Be(1);
            consolidatedFrequencies[haplotypeBuilder.WithDataAt(null, Locus.C).Build()].Should().Be(21);
            consolidatedFrequencies[haplotypeBuilder.WithDataAt(null, Locus.Dqb1).Build()].Should().Be(301);
            consolidatedFrequencies[haplotypeBuilder.WithDataAt(null, Locus.Dqb1, Locus.C).Build()].Should().Be(4001);
        }
    }
}