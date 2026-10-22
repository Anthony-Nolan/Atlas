using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using AutoFixture;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.HaplotypeFrequencies;

[TestFixture]
internal class FrequencySetPayloadTests
{
    private Fixture fixture;
    private Random random;
    private int setId;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        random = new Random(fixture.Create<int>());
        setId = fixture.Create<int>();
    }

    [Test]
    public void Decode_OfEncode_ReturnsTheRowsInTheirOrder()
    {
        var rows = NewRows(fixture.Create<int>());

        var decoded = RoundTrip(rows);

        decoded.Should().Equal(rows);
    }

    [Test]
    public void Decode_OfEncode_KeepsTheBitsOfEachFrequency_WithItsScale()
    {
        // Equal values with different scales: equal decimals, but not the same bits.
        var frequency = fixture.Create<decimal>();
        var rows = new List<LightweightHaplotypeFrequencyRecord>
        {
            NewRow() with { Frequency = frequency },
            NewRow() with { Frequency = frequency * 1.000m },
            NewRow() with { Frequency = NewFrequencyWithScaleTwenty() }
        };

        var decoded = RoundTrip(rows);

        decoded.Select(row => decimal.GetBits(row.Frequency)).Should().BeEquivalentTo(
            rows.Select(row => decimal.GetBits(row.Frequency)),
            options => options.WithStrictOrdering());
    }

    [Test]
    public void Decode_OfEncode_KeepsANullNameApartFromAnEmptyName()
    {
        var rows = new List<LightweightHaplotypeFrequencyRecord>
        {
            NewRow() with { C = null, DQB1 = "" },
            NewRow() with { C = "", DQB1 = null }
        };

        var decoded = RoundTrip(rows);

        decoded[0].C.Should().BeNull();
        decoded[0].DQB1.Should().BeEmpty();
        decoded[1].C.Should().BeEmpty();
        decoded[1].DQB1.Should().BeNull();
    }

    [Test]
    public void Decode_OfEncode_KeepsEachTypingCategory()
    {
        var rows = Enum.GetValues<HaplotypeTypingCategory>().Select(category => NewRow() with { TypingCategory = category }).ToList();

        var decoded = RoundTrip(rows);

        decoded.Select(row => row.TypingCategory).Should().Equal(rows.Select(row => row.TypingCategory));
    }

    [Test]
    public void Decode_OfEncode_OfASetWithNoRows_ReturnsNoRows()
    {
        var decoded = RoundTrip([]);

        decoded.Should().BeEmpty();
    }

    /// <summary>So that processes that store one set at the same time store the same chunks.</summary>
    [Test]
    public void Encode_OfTheSameRows_GivesTheSameBytes()
    {
        var rows = NewRows(fixture.Create<int>());

        var payload = FrequencySetPayload.Encode(setId, rows);
        var payloadOfACopy = FrequencySetPayload.Encode(setId, rows.Select(row => row with { }).ToList());

        payloadOfACopy.Should().Equal(payload);
    }

    [Test]
    public void Decode_ForAnotherSet_Throws()
    {
        var payload = FrequencySetPayload.Encode(setId, NewRows(fixture.Create<int>()));

        var decode = () => FrequencySetPayload.Decode(setId + 1, payload);

        decode.Should().Throw<InvalidDataException>().WithMessage($"*set {setId}*");
    }

    [Test]
    public void Decode_OfAnotherFormatVersion_Throws()
    {
        var payload = FrequencySetPayload.Encode(setId, NewRows(fixture.Create<int>()));
        payload[0] = FrequencySetPayload.FormatVersion + 1;

        var decode = () => FrequencySetPayload.Decode(setId, payload);

        decode.Should().Throw<InvalidDataException>().WithMessage("*Format version*");
    }

    [Test]
    public void Decode_OfAPayloadWithAChangedByte_Throws()
    {
        var payload = FrequencySetPayload.Encode(setId, NewRows(100));
        payload[payload.Length / 2] ^= 0xFF;

        var decode = () => FrequencySetPayload.Decode(setId, payload);

        decode.Should().Throw<InvalidDataException>();
    }

    [TestCase(1)]
    [TestCase(8)]
    [TestCase(9)]
    public void Decode_OfATruncatedPayload_Throws(int missingBytes)
    {
        var payload = FrequencySetPayload.Encode(setId, NewRows(100));

        var decode = () => FrequencySetPayload.Decode(setId, payload[..^missingBytes]);

        decode.Should().Throw<InvalidDataException>();
    }

    [Test]
    public void Decode_OfAPayloadTooShortForTheGzipStream_Throws()
    {
        var decode = () => FrequencySetPayload.Decode(setId, fixture.Create<byte[]>());

        decode.Should().Throw<InvalidDataException>().WithMessage("*too short*");
    }

    /// <summary>
    /// A guard against a change of the layout that makes the values much larger - not a measure of the real sets, which
    /// compress better. Rows over pools of names, with random frequencies of scale 20: the bound is above what this
    /// layout gives for them, and below what names written in full, or bytes not compressed, give.
    /// </summary>
    [Test]
    public void Encode_OfALargeSet_TakesAFewBytesForEachRow()
    {
        const int rowCount = 100_000;
        const double maximumBytesPerRow = 16;
        var namesByLocus = new[] { 300, 600, 250, 100, 400 }.Select(count => fixture.CreateMany<string>(count).ToArray()).ToArray();

        var rows = Enumerable.Range(0, rowCount)
            .Select(_ => new LightweightHaplotypeFrequencyRecord
            {
                A = Pick(namesByLocus[0]),
                B = Pick(namesByLocus[1]),
                C = Pick(namesByLocus[2]),
                DQB1 = Pick(namesByLocus[3]),
                DRB1 = Pick(namesByLocus[4]),
                SetId = setId,
                Frequency = NewFrequencyWithScaleTwenty(),
                TypingCategory = HaplotypeTypingCategory.SmallGGroup
            })
            .ToList();

        var payload = FrequencySetPayload.Encode(setId, rows);

        (payload.Length / (double)rowCount).Should().BeLessThan(maximumBytesPerRow);

        string Pick(string[] names) => names[random.Next(names.Length)];
    }

    private List<LightweightHaplotypeFrequencyRecord> RoundTrip(List<LightweightHaplotypeFrequencyRecord> rows) =>
        FrequencySetPayload.Decode(setId, FrequencySetPayload.Encode(setId, rows));

    private LightweightHaplotypeFrequencyRecord NewRow() =>
        fixture.Build<LightweightHaplotypeFrequencyRecord>().With(x => x.SetId, setId).Create();

    private List<LightweightHaplotypeFrequencyRecord> NewRows(int rowCount) =>
        Enumerable.Range(0, rowCount).Select(_ => NewRow() with { Frequency = NewFrequencyWithScaleTwenty() }).ToList();

    /// <summary>As SQL gives a decimal(20,20).</summary>
    private decimal NewFrequencyWithScaleTwenty()
    {
        var mantissa = random.NextInt64(1_000_000_000_000, 100_000_000_000_000_000);
        return new decimal((int)(mantissa & uint.MaxValue), (int)(mantissa >> 32), 0, false, 20);
    }
}
