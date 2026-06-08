using System.Collections.Generic;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.HaplotypeFrequencies;

[TestFixture]
public class FrequencyConsolidatorTests
{
    private IFrequencyConsolidator frequencyConsolidator;

    [SetUp]
    public void SetUp()
    {
        frequencyConsolidator = new FrequencyConsolidator();
    }


    [Test]
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

        // var consolidatedFrequencies = frequencyConsolidator.PreConsolidateFrequenciesForCommonMissingLoci(frequencies);
        //
        // consolidatedFrequencies[haplotypeBuilder.Build()].Should().Be(1);
        // consolidatedFrequencies[haplotypeBuilder.WithDataAt(null, Locus.C).Build()].Should().Be(21);
        // consolidatedFrequencies[haplotypeBuilder.WithDataAt(null, Locus.Dqb1).Build()].Should().Be(301);
        // consolidatedFrequencies[haplotypeBuilder.WithDataAt(null, Locus.Dqb1, Locus.C).Build()].Should().Be(4001);
    }
}