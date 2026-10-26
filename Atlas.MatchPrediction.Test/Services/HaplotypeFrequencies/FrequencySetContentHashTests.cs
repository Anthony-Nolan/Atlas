using System.Collections.Generic;
using System.Linq;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using AutoFixture;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.HaplotypeFrequencies;

[TestFixture]
internal class FrequencySetContentHashTests
{
    private Fixture fixture;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
    }

    /// <summary>
    /// SQL keeps the hash of each set, so the form of the hashed bytes must not change: a hash of a new form would not
    /// match the stored hashes. So this test has fixed rows, not generated ones, and a fixed value.
    /// </summary>
    [Test]
    public void Calculate_OfFixedRows_GivesTheHashOfTheFixedForm()
    {
        var rows = new List<LightweightHaplotypeFrequencyRecord>
        {
            new()
            {
                A = "01:01:01G", B = "08:01:01G", C = "07:01:01G", DQB1 = "02:01:01G", DRB1 = "03:01:01G",
                Frequency = 0.01234567890123456789m, TypingCategory = HaplotypeTypingCategory.GGroup
            },
            new()
            {
                A = "02:01P", B = "", C = null, DQB1 = "03:01P", DRB1 = "04:01P",
                Frequency = 0.50m, TypingCategory = HaplotypeTypingCategory.PGroup
            }
        };

        var hash = FrequencySetContentHash.Calculate(rows);

        FrequencySetContentHash.ToHex(hash).Should().Be("81a9ac8961adb7bef564060787280da6038413f0b1e3182c48b1bd0b5cadc3fe");
    }

    [Test]
    public void Calculate_GivesAHashOfThirtyTwoBytes()
    {
        var hash = FrequencySetContentHash.Calculate(NewRows());

        hash.Should().HaveCount(FrequencySetContentHash.SizeInBytes);
    }

    [Test]
    public void Calculate_OfTheSameRows_GivesTheSameHash()
    {
        var rows = NewRows();

        var hashOfACopy = FrequencySetContentHash.Calculate(rows.Select(row => row with { }).ToList());

        hashOfACopy.Should().Equal(FrequencySetContentHash.Calculate(rows));
    }

    [Test]
    public void Calculate_OfTheSameRowsOfAnotherSet_GivesTheSameHash()
    {
        var rows = NewRows();

        var hashOfAnotherSet = FrequencySetContentHash.Calculate(rows.Select(row => row with { SetId = fixture.Create<int>() }).ToList());

        hashOfAnotherSet.Should().Equal(FrequencySetContentHash.Calculate(rows));
    }

    /// <summary>The order of the rows is the tie-break of truncation, so it is part of the content.</summary>
    [Test]
    public void Calculate_OfTheSameRowsInAnotherOrder_GivesAnotherHash()
    {
        var rows = NewRows();

        var hashInAnotherOrder = FrequencySetContentHash.Calculate(rows.AsEnumerable().Reverse().ToList());

        hashInAnotherOrder.Should().NotEqual(FrequencySetContentHash.Calculate(rows));
    }

    [Test]
    public void Calculate_WithANullNameInPlaceOfAnEmptyName_GivesAnotherHash()
    {
        var row = fixture.Create<LightweightHaplotypeFrequencyRecord>();

        var hashWithNull = FrequencySetContentHash.Calculate([row with { C = null }]);

        hashWithNull.Should().NotEqual(FrequencySetContentHash.Calculate([row with { C = "" }]));
    }

    /// <summary>The length of each name is in the hashed bytes, so a name cannot run into the next one.</summary>
    [Test]
    public void Calculate_WithPartOfANameInTheNextName_GivesAnotherHash()
    {
        var row = fixture.Create<LightweightHaplotypeFrequencyRecord>();
        var name = fixture.Create<string>();

        var hashWithOneLongName = FrequencySetContentHash.Calculate([row with { A = name + name, B = "" }]);

        hashWithOneLongName.Should().NotEqual(FrequencySetContentHash.Calculate([row with { A = name, B = name }]));
    }

    /// <summary>The scale changes the arithmetic, so it is part of the content.</summary>
    [Test]
    public void Calculate_WithAnEqualFrequencyOfAnotherScale_GivesAnotherHash()
    {
        var row = fixture.Create<LightweightHaplotypeFrequencyRecord>();

        var hashOfAnotherScale = FrequencySetContentHash.Calculate([row with { Frequency = row.Frequency * 1.00m }]);

        hashOfAnotherScale.Should().NotEqual(FrequencySetContentHash.Calculate([row]));
    }

    [Test]
    public void Calculate_WithAnotherTypingCategory_GivesAnotherHash()
    {
        var row = fixture.Create<LightweightHaplotypeFrequencyRecord>() with { TypingCategory = HaplotypeTypingCategory.GGroup };

        var hashOfAnotherCategory = FrequencySetContentHash.Calculate([row with { TypingCategory = HaplotypeTypingCategory.PGroup }]);

        hashOfAnotherCategory.Should().NotEqual(FrequencySetContentHash.Calculate([row]));
    }

    private List<LightweightHaplotypeFrequencyRecord> NewRows() => fixture.CreateMany<LightweightHaplotypeFrequencyRecord>(10).ToList();
}
