using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.Precompute;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using AutoFixture;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.Precompute;

/// <summary>
/// The V1 payload format's contract.
///
/// <para>
/// <b>Why these assert the way they do.</b> Three traps rule out the obvious <c>BeEquivalentTo</c>:
/// </para>
///
/// <list type="number">
/// <item><c>1.23m.Equals(1.2300m)</c> is <c>true</c>, so a value-based assertion passes even when the encoder has
/// normalised the scale away - the exact failure the "no precision loss" criterion exists to catch. Decimals are
/// therefore compared as <c>decimal.GetBits</c>.</item>
/// <item><c>BeEquivalentTo</c> on a collection is order-insensitive by default, and genotype order is part of the
/// result (downstream sums run over the sequence).</item>
/// <item>Neither <see cref="SubjectGenotypeSet"/> nor <see cref="GenotypeAtDesiredResolutions"/> overrides
/// <c>Equals</c>. <see cref="PhenotypeInfo{T}"/> does, structurally and ordinally, so <c>Be</c> on the two
/// resolutions is both correct and strong.</item>
/// </list>
/// </summary>
[TestFixture]
public class SubjectGenotypeSetPayloadTests
{
    private Fixture fixture;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
    }

    #region Round trip - the acceptance criterion

    [Test]
    public void EncodeDecode_RestoresTheSetExactly()
    {
        var original = new SubjectGenotypeSet(
            false,
            [
                AnyGenotype(),
                AnyGenotype(),
                AnyGenotype()
            ],
            fixture.Create<decimal>());

        AssertRoundTrips(original);
    }

    [Test]
    public void EncodeDecode_ForUnrepresentedSet_RestoresTheFlag()
    {
        // The shape GenotypeSetService returns when imputation yields nothing.
        AssertRoundTrips(new SubjectGenotypeSet(true, [], 0m));
    }

    [Test]
    public void EncodeDecode_ForRepresentedButEmptySet_RestoresIt()
    {
        AssertRoundTrips(new SubjectGenotypeSet(false, [], fixture.Create<decimal>()));
    }

    [Test]
    public void EncodeDecode_WhenEverySlotIsNull_RestoresEverySlotAsNull()
    {
        var allNull = new PhenotypeInfo<string>();

        var decoded = AssertRoundTrips(new SubjectGenotypeSet(
            false,
            [BuildGenotype(allNull, allNull, fixture.Create<decimal>())],
            fixture.Create<decimal>()));

        decoded.Genotypes.Single().StringMatchableResolution.ToEnumerable().Should().AllSatisfy(hla => hla.Should().BeNull());
    }

    [Test]
    public void EncodeDecode_DistinguishesNullFromEmptyString()
    {
        // Only the sentinel separates these two. An encoder that treated "" as absent would round-trip it to null.
        var resolution = new PhenotypeInfoBuilder<string>()
            .WithDataAt(Locus.A, null, string.Empty)
            .WithDataAt(Locus.B, string.Empty, "B*07:02P")
            .Build();

        var decoded = AssertRoundTrips(new SubjectGenotypeSet(
            false,
            [BuildGenotype(resolution, resolution, fixture.Create<decimal>())],
            fixture.Create<decimal>()));

        var restored = decoded.Genotypes.Single().StringMatchableResolution;
        restored.A.Position1.Should().BeNull();
        restored.A.Position2.Should().BeEmpty();
        restored.B.Position1.Should().BeEmpty();
    }

    [Test]
    public void EncodeDecode_WithDivergentHaplotypeResolution_RestoresBothResolutionsIndependently()
    {
        // A SmallGGroup frequency set diverges at every typed position, so the override list is full rather than empty.
        var stringMatchable = PhenotypeOf("P");
        var haplotype = PhenotypeOf("G");

        var decoded = AssertRoundTrips(new SubjectGenotypeSet(
            false,
            [BuildGenotype(haplotype, stringMatchable, fixture.Create<decimal>())],
            fixture.Create<decimal>()));

        var genotype = decoded.Genotypes.Single();
        genotype.HaplotypeResolution.Should().NotBe(genotype.StringMatchableResolution);
    }

    [Test]
    public void EncodeDecode_WhenGenotypesShareAHaplotypeResolutionInstance_RestoresEqualValues()
    {
        // Upstream shares one Names reference across genotypes that differ only in typing category. The format stores
        // values, not references, so equality must survive - identity need not, and is deliberately not asserted.
        var shared = PhenotypeOf("shared");

        var decoded = AssertRoundTrips(new SubjectGenotypeSet(
            false,
            [
                BuildGenotype(shared, PhenotypeOf("first"), fixture.Create<decimal>()),
                BuildGenotype(shared, PhenotypeOf("second"), fixture.Create<decimal>())
            ],
            fixture.Create<decimal>()));

        decoded.Genotypes.First().HaplotypeResolution.Should().Be(decoded.Genotypes.Last().HaplotypeResolution);
    }

    [Test]
    public void EncodeDecode_WithTwoIdenticalGenotypes_KeepsBoth()
    {
        // The pool de-duplicates STRINGS. It must not de-duplicate genotypes: two identical entries are two entries,
        // and the likelihood sum downstream counts them both.
        var resolution = PhenotypeOf("same");
        var likelihood = fixture.Create<decimal>();

        var decoded = AssertRoundTrips(new SubjectGenotypeSet(
            false,
            [
                BuildGenotype(resolution, resolution, likelihood),
                BuildGenotype(resolution, resolution, likelihood)
            ],
            fixture.Create<decimal>()));

        decoded.Genotypes.Should().HaveCount(2);
    }

    [Test]
    public void EncodeDecode_PreservesGenotypeOrder()
    {
        var genotypes = Enumerable.Range(0, 20).Select(i => BuildGenotype(PhenotypeOf($"h{i}"), PhenotypeOf($"s{i}"), i + 1m)).ToList();

        var decoded = AssertRoundTrips(new SubjectGenotypeSet(false, genotypes, fixture.Create<decimal>()));

        decoded.Genotypes.Select(g => g.StringMatchableResolution.A.Position1)
            .Should().ContainInOrder(genotypes.Select(g => g.StringMatchableResolution.A.Position1));
    }

    #endregion

    #region Pool width

    // 255 real entries still fit in a one-byte index because id 0 is the null sentinel; 256 do not. 260 proves the
    // wide path is exercised beyond its own boundary rather than only at it.
    [TestCase(255, TestName = "EncodeDecode_AtTheLastNarrowPoolSize_RoundTrips")]
    [TestCase(256, TestName = "EncodeDecode_AtTheFirstWidePoolSize_RoundTrips")]
    [TestCase(260, TestName = "EncodeDecode_AboveTheWideBoundary_RoundTrips")]
    public void EncodeDecode_AcrossThePoolWidthBoundary_RoundTrips(int distinctNameCount)
    {
        var set = SetWithDistinctNames(distinctNameCount);

        SubjectGenotypeSetPayload.EncodeBody(set).Should().NotBeEmpty();
        AssertRoundTrips(set);
    }

    [TestCase(255, 1)]
    [TestCase(256, 2)]
    public void EncodeBody_UsesTheExpectedIndexWidth(int distinctNameCount, int expectedWidthInBytes)
    {
        var body = SubjectGenotypeSetPayload.EncodeBody(SetWithDistinctNames(distinctNameCount));

        var wideFlagSet = (body[1] & SubjectGenotypeSetPayloadFormat.WideIndicesFlag) != 0;

        SubjectGenotypeSetPayloadFormat.IndexWidthInBytes(wideFlagSet).Should().Be(expectedWidthInBytes);
    }

    #endregion

    #region Decimals

    // decimal.GetBits distinguishes scale and negative zero, so these are all distinct bit patterns that must survive.
    [TestCase("1.23")]
    [TestCase("1.2300")]
    [TestCase("0")]
    [TestCase("-0")]
    [TestCase("0.0000000000000000000000000001")]
    [TestCase("79228162514264337593543950335")]
    [TestCase("-79228162514264337593543950335")]
    public void EncodeDecode_PreservesDecimalBitsExactly(string value)
    {
        var likelihood = decimal.Parse(value);

        var decoded = AssertRoundTrips(new SubjectGenotypeSet(
            false,
            [BuildGenotype(PhenotypeOf("hla"), PhenotypeOf("hla"), likelihood)],
            likelihood));

        // AssertRoundTrips already compares by GetBits; restated here so the failure names the case.
        decimal.GetBits(decoded.Genotypes.Single().GenotypeLikelihood).Should().Equal(decimal.GetBits(likelihood));
        decimal.GetBits(decoded.SumOfLikelihoods).Should().Equal(decimal.GetBits(likelihood));
    }

    [Test]
    public void Encode_ForDecimalsDifferingOnlyInScale_ProducesDifferentPayloads()
    {
        // 1.23m.Equals(1.2300m) is true, so this is the only assertion that can catch scale normalisation.
        var coarse = SubjectGenotypeSetPayload.Encode(SetWithSumOfLikelihoods(1.23m));
        var fine = SubjectGenotypeSetPayload.Encode(SetWithSumOfLikelihoods(1.2300m));

        coarse.Should().NotEqual(fine);
    }

    [Test]
    public void EncodeDecode_KeepsSumOfLikelihoodsIndependentOfTheGenotypeLikelihoods()
    {
        // Upstream sums over surviving NAME forms, not over Genotypes, so the two genuinely disagree and the format
        // must not "helpfully" recompute one from the other.
        var decoded = AssertRoundTrips(new SubjectGenotypeSet(
            false,
            [
                BuildGenotype(PhenotypeOf("a"), PhenotypeOf("a"), 0.25m),
                BuildGenotype(PhenotypeOf("b"), PhenotypeOf("b"), 0.25m)
            ],
            0.9m));

        decoded.SumOfLikelihoods.Should().Be(0.9m);
    }

    #endregion

    #region The frozen slot table

    [Test]
    public void SlotTable_IsTheFrozenV1Order()
    {
        // This array is a persisted contract: changing it reinterprets every row already written. It is pinned here
        // rather than computed, for the same reason the production table is written out rather than computed from
        // (int)locus * 2 + (int)position - both enums are implicitly valued, so inserting a member would move it.
        SubjectGenotypeSetPayloadFormat.Slots.Should().Equal(
            (Locus.A, LocusPosition.One),
            (Locus.A, LocusPosition.Two),
            (Locus.B, LocusPosition.One),
            (Locus.B, LocusPosition.Two),
            (Locus.C, LocusPosition.One),
            (Locus.C, LocusPosition.Two),
            (Locus.Dpb1, LocusPosition.One),
            (Locus.Dpb1, LocusPosition.Two),
            (Locus.Dqb1, LocusPosition.One),
            (Locus.Dqb1, LocusPosition.Two),
            (Locus.Drb1, LocusPosition.One),
            (Locus.Drb1, LocusPosition.Two));
    }

    [Test]
    public void SlotTable_RoundTripsEverySlotToItsOwnPosition()
    {
        // Proves ToSlots and ToPhenotype agree, with a distinct value per slot so a transposition cannot pass.
        var distinctPerSlot = new PhenotypeInfo<string>((locus, position) => $"{locus}-{position}");
        var slots = new string[SubjectGenotypeSetPayloadFormat.SlotCount];

        SubjectGenotypeSetPayloadFormat.ToSlots(distinctPerSlot, slots);

        SubjectGenotypeSetPayloadFormat.ToPhenotype(slots).Should().Be(distinctPerSlot);
    }

    [Test]
    public void EncodeDecode_KeepsDpb1_EvenThoughMatchingNeverUsesIt()
    {
        // All 12 slots are written, with no presence bitmap, so the format is total over PhenotypeInfo<string> and
        // carries no business rule about which loci matter.
        var withDpb1 = new PhenotypeInfoBuilder<string>().WithDataAt(Locus.Dpb1, "DPB1*04:01P", "DPB1*02:01P").Build();

        var decoded = AssertRoundTrips(new SubjectGenotypeSet(
            false,
            [BuildGenotype(withDpb1, withDpb1, fixture.Create<decimal>())],
            fixture.Create<decimal>()));

        decoded.Genotypes.Single().StringMatchableResolution.Dpb1.Position1.Should().Be("DPB1*04:01P");
    }

    #endregion

    #region Stability - what protects rows already written

    [Test]
    public void Encode_IsDeterministic()
    {
        // Catches a hash-set iteration order or a culture-sensitive sort leaking into the bytes. Determinism is what
        // lets two donors sharing a typing share one stored row, so it is a correctness property, not a nicety.
        var first = SubjectGenotypeSetPayload.Encode(CanonicalV1Set());
        var second = SubjectGenotypeSetPayload.Encode(CanonicalV1Set());

        first.Should().Equal(second);
    }

    [Test]
    public void EncodeBody_ForTheCanonicalSet_HasNotChanged()
    {
        // Hashes the UNCOMPRESSED body on purpose. The compressed bytes are a DEFLATE implementation detail that a
        // runtime upgrade may legitimately change; the body is this code's own contract.
        var digest = Convert.ToHexString(SHA256.HashData(SubjectGenotypeSetPayload.EncodeBody(CanonicalV1Set())));

        digest.Should().Be(CanonicalBodySha256);
    }

    [Test]
    public void Decode_ForAPayloadWrittenByV1_StillReadsIt()
    {
        // The only test protecting rows already in the database from a decoder change. The literal below was produced
        // by this encoder and must never be regenerated to make a failing build pass - a change here means a change
        // that would misread stored data.
        var decoded = SubjectGenotypeSetPayload.Decode(Convert.FromHexString(CanonicalV1Payload));

        AssertSameSet(decoded, CanonicalV1Set());
    }

    #endregion

    #region Defensive decode - every malformed payload is one exception type

    [Test]
    public void Decode_ForNullPayload_Throws()
    {
        var act = () => SubjectGenotypeSetPayload.Decode(null);

        act.Should().Throw<ArgumentNullException>();
    }

    [TestCase(0, TestName = "Decode_ForEmptyPayload_Throws")]
    [TestCase(1, TestName = "Decode_ForOneBytePayload_Throws")]
    [TestCase(19, TestName = "Decode_ForPayloadTooShortToHoldAGzipMember_Throws")]
    public void Decode_ForPayloadShorterThanTheMinimum_Throws(int length)
    {
        var act = () => SubjectGenotypeSetPayload.Decode(new byte[length]);

        act.Should().Throw<InvalidDataException>().WithMessage("*too short*");
    }

    [Test]
    public void Decode_ForABareGzipBlob_ThrowsWithoutDecompressingIt()
    {
        // The container marker is deliberately not 0x1F, so this fails on the marker rather than on a confusing
        // body-version error after a successful decompression. Gzipped here rather than hand-written, so the payload
        // really would decompress if the marker check were dropped.
        var bareGzip = SubjectGenotypeSetPayload.Encode(CanonicalV1Set()).Skip(2).ToArray();

        var act = () => SubjectGenotypeSetPayload.Decode(bareGzip);

        act.Should().Throw<InvalidDataException>().WithMessage("*container marker*");
    }

    [Test]
    public void Decode_ForUnknownContainerVersion_Throws()
    {
        var payload = SubjectGenotypeSetPayload.Encode(CanonicalV1Set());
        payload[1] = 99;

        var act = () => SubjectGenotypeSetPayload.Decode(payload);

        act.Should().Throw<InvalidDataException>().WithMessage("*Container version 99*");
    }

    [Test]
    public void Decode_ForUnknownBodyVersion_ThrowsWithAClearMessage()
    {
        var body = SubjectGenotypeSetPayload.EncodeBody(CanonicalV1Set());
        body[0] = 99;

        var act = () => SubjectGenotypeSetPayload.Decode(SubjectGenotypeSetPayload.Wrap(body));

        act.Should().Throw<InvalidDataException>().WithMessage("*Body version 99 is not supported*");
    }

    [Test]
    public void Decode_ForAFlippedByte_Throws()
    {
        // Gzip's CRC32 + ISIZE trailer is the corruption check; this is what makes a separate checksum unnecessary.
        var payload = SubjectGenotypeSetPayload.Encode(CanonicalV1Set());
        payload[payload.Length / 2] ^= 0xFF;

        var act = () => SubjectGenotypeSetPayload.Decode(payload);

        act.Should().Throw<InvalidDataException>();
    }

    // 8 bytes is exactly the gzip trailer and 9 takes one byte of deflate data with it. Both decompressed silently on
    // .NET 10 before the explicit ISIZE check went in - zlib verifies the trailer only when the trailer is present -
    // so these two cases are the regression guard for that check, not incidental coverage. 16 and 40 cut into the
    // compressed body, which zlib does catch.
    [TestCase(8)]
    [TestCase(9)]
    [TestCase(16)]
    [TestCase(40)]
    public void Decode_ForATruncatedPayload_Throws(int bytesRemoved)
    {
        var payload = SubjectGenotypeSetPayload.Encode(CanonicalV1Set());

        var act = () => SubjectGenotypeSetPayload.Decode(payload.Take(payload.Length - bytesRemoved).ToArray());

        act.Should().Throw<InvalidDataException>();
    }

    [Test]
    public void Decode_ForATruncatedBody_ThrowsSayingSo()
    {
        var body = SubjectGenotypeSetPayload.EncodeBody(CanonicalV1Set());

        var act = () => SubjectGenotypeSetPayload.Decode(SubjectGenotypeSetPayload.Wrap(body.Take(body.Length - 4).ToArray()));

        act.Should().Throw<InvalidDataException>().WithMessage("*truncated*");
    }

    [Test]
    public void Decode_ForAReservedFlagBit_Throws()
    {
        var body = SubjectGenotypeSetPayload.EncodeBody(CanonicalV1Set());
        body[1] |= 0b0000_0100;

        var act = () => SubjectGenotypeSetPayload.Decode(SubjectGenotypeSetPayload.Wrap(body));

        act.Should().Throw<InvalidDataException>().WithMessage("*reserved bit*");
    }

    [Test]
    public void Decode_WhenTheWidthFlagContradictsThePoolCount_Throws()
    {
        var body = SubjectGenotypeSetPayload.EncodeBody(CanonicalV1Set());
        body[1] |= SubjectGenotypeSetPayloadFormat.WideIndicesFlag;

        var act = () => SubjectGenotypeSetPayload.Decode(SubjectGenotypeSetPayload.Wrap(body));

        act.Should().Throw<InvalidDataException>().WithMessage("*pool ids are*");
    }

    [Test]
    public void Decode_ForAnOutOfRangePoolId_Throws()
    {
        var body = SubjectGenotypeSetPayload.EncodeBody(CanonicalV1Set());
        var (firstSlotOffset, poolCount) = LocateFirstSlotId(body);

        poolCount.Should().BeLessThanOrEqualTo(
            SubjectGenotypeSetPayloadFormat.NarrowPoolCountLimit, "the canonical set's ids are one byte wide, so one byte holds the first id");

        // One past the last real id: the smallest id that the check must reject.
        body[firstSlotOffset] = (byte)(poolCount + 1);

        var act = () => SubjectGenotypeSetPayload.Decode(SubjectGenotypeSetPayload.Wrap(body));

        act.Should().Throw<InvalidDataException>().WithMessage($"*Pool id {poolCount + 1} is outside*");
    }

    [Test]
    public void Decode_ForBytesAfterTheLastField_Throws()
    {
        var body = SubjectGenotypeSetPayload.EncodeBody(CanonicalV1Set());
        var extraBytes = fixture.CreateMany<byte>().ToArray();

        var act = () => SubjectGenotypeSetPayload.Decode(SubjectGenotypeSetPayload.Wrap(body.Concat(extraBytes).ToArray()));

        act.Should().Throw<InvalidDataException>().WithMessage($"*{extraBytes.Length} byte(s) after its last field*");
    }

    [Test]
    public void Decode_ForAnAbsurdGenotypeCount_ThrowsRatherThanAllocating()
    {
        var body = SubjectGenotypeSetPayload.EncodeBody(CanonicalV1Set());
        BitConverter.GetBytes(int.MaxValue).CopyTo(body, 2);

        var act = () => SubjectGenotypeSetPayload.Decode(SubjectGenotypeSetPayload.Wrap(body));

        act.Should().Throw<InvalidDataException>().WithMessage("*cannot fit*");
    }

    [Test]
    public void Decode_ForAPoolCountThatNeverTerminates_Throws()
    {
        var body = SubjectGenotypeSetPayload.EncodeBody(CanonicalV1Set());

        // Five continuation bytes. BinaryReader.Read7BitEncodedInt gives up on the fifth and raises a FormatException,
        // which is not the failure contract's exception unless the decoder maps it - so this is the test that fails if
        // that mapping is removed.
        for (var offset = PoolCountOffset; offset < PoolCountOffset + 5; offset++)
        {
            body[offset] = 0xFF;
        }

        var act = () => SubjectGenotypeSetPayload.Decode(SubjectGenotypeSetPayload.Wrap(body));

        act.Should().Throw<InvalidDataException>().WithMessage("*malformed length prefix*");
    }

    [Test]
    public void Decode_ForANegativePoolEntryLength_Throws()
    {
        var body = SubjectGenotypeSetPayload.EncodeBody(CanonicalV1Set());

        body[PoolCountOffset].Should()
            .BeLessThan(0x80, "the canonical set's pool count is one byte, so the first entry's length prefix follows it");

        // Decodes to -1, which BinaryReader.ReadString reports as an IOException - the second way out of the contract.
        new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x0F }.CopyTo(body, PoolCountOffset + 1);

        var act = () => SubjectGenotypeSetPayload.Decode(SubjectGenotypeSetPayload.Wrap(body));

        act.Should().Throw<InvalidDataException>().WithMessage("*malformed length prefix*");
    }

    [Test]
    public void Encode_ForNullSet_Throws()
    {
        var act = () => SubjectGenotypeSetPayload.Encode(null);

        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Offset of the body's first variable-length field, the pool count: directly after the version byte, the flags
    /// byte and the int32 genotype count.
    /// </summary>
    private const int PoolCountOffset = 1 + 1 + sizeof(int);

    /// <summary>
    /// Where the first genotype's first slot id sits - directly after the header and the pool entries - and how many
    /// entries the pool holds. Read with the decoder's own primitives, not calculated from the canonical names, so the
    /// offset stays correct whatever width each length prefix takes.
    /// </summary>
    private static (int Offset, int PoolCount) LocateFirstSlotId(byte[] body)
    {
        using var reader = new BinaryReader(new MemoryStream(body, writable: false), SubjectGenotypeSetPayloadFormat.PayloadEncoding);
        reader.BaseStream.Position = PoolCountOffset;

        var poolCount = reader.Read7BitEncodedInt();
        for (var entry = 0; entry < poolCount; entry++)
        {
            reader.ReadString();
        }

        return ((int)reader.BaseStream.Position, poolCount);
    }

    /// <summary>
    /// SHA-256 of <see cref="CanonicalV1Set"/>'s uncompressed body. Regenerating this to fix a red build defeats the
    /// test - see <see cref="Decode_ForAPayloadWrittenByV1_StillReadsIt"/>.
    /// </summary>
    private const string CanonicalBodySha256 = "367EA3DD54405D553DF62E18CE6397FE911623199E4E8E5191859D6C58BD1603";

    /// <inheritdoc cref="CanonicalBodySha256"/>
    private const string CanonicalV1Payload =
        "A7011F8B080000000000000A4DCC410E40301005D0DF2A8A6E5CA3AB192292EE94C476E23A0EEA3A52093AABF733F34741036860174F1C88256108C4FB0"
      + "BB1D1D31C6810BB3E60E9B623B2A7F1374FA99259B429A12AA4E7B556456BCFE46F34F20338CA9706579F67871BC3188192A7000000";

    /// <summary>
    /// A deliberately awkward set, fixed for all time. It exercises, in one payload: a divergent haplotype resolution
    /// (a partial override list), a null slot, an empty-string slot, two decimals that differ only in scale, and a
    /// sum that is not the sum of the genotype likelihoods.
    /// </summary>
    private static SubjectGenotypeSet CanonicalV1Set()
    {
        var stringMatchable = new PhenotypeInfoBuilder<string>()
            .WithDataAt(Locus.A, "A*01:01P", "A*02:01P")
            .WithDataAt(Locus.B, "B*07:02P", null)
            .WithDataAt(Locus.C, string.Empty, "C*07:01P")
            .WithDataAt(Locus.Drb1, "DRB1*15:01P", "DRB1*03:01P")
            .Build();

        var haplotype = stringMatchable
            .SetPosition(Locus.A, LocusPosition.Two, "A*02:01G")
            .SetPosition(Locus.Drb1, LocusPosition.One, "DRB1*15:01G");

        return new SubjectGenotypeSet(
            false,
            [
                BuildGenotype(haplotype, stringMatchable, 1.23m),
                BuildGenotype(stringMatchable, stringMatchable, 1.2300m)
            ],
            0.000000004567m);
    }

    private static GenotypeAtDesiredResolutions BuildGenotype(
        PhenotypeInfo<string> haplotypeResolution,
        PhenotypeInfo<string> stringMatchableResolution,
        decimal likelihood) =>
        new GenotypeAtDesiredResolutionsBuilder()
            .WithHaplotypeResolution(haplotypeResolution)
            .WithStringMatchableResolution(stringMatchableResolution)
            .WithLikelihood(likelihood)
            .Build();

    /// <summary>
    /// A phenotype with a distinct value at every slot. Built rather than taken from AutoFixture:
    /// <c>fixture.Create&lt;PhenotypeInfo&lt;string&gt;&gt;()</c> picks the parameterless constructor and yields
    /// twelve nulls, which would make most of these tests vacuous.
    /// </summary>
    private static PhenotypeInfo<string> PhenotypeOf(string prefix) =>
        new((locus, position) => $"{prefix}-{locus}-{position}");

    private GenotypeAtDesiredResolutions AnyGenotype()
    {
        var suffix = fixture.Create<string>();
        return BuildGenotype(PhenotypeOf($"h{suffix}"), PhenotypeOf($"s{suffix}"), fixture.Create<decimal>());
    }

    /// <summary>
    /// A set whose pool holds exactly <paramref name="distinctNameCount"/> entries: one distinct name per slot, with
    /// both resolutions drawing from the same names so the count is exact.
    /// </summary>
    private static SubjectGenotypeSet SetWithDistinctNames(int distinctNameCount)
    {
        var names = Enumerable.Range(0, distinctNameCount).Select(i => $"name-{i:D5}").ToList();
        const int slotCount = SubjectGenotypeSetPayloadFormat.SlotCount;
        var genotypes = new List<GenotypeAtDesiredResolutions>();

        for (var offset = 0; offset < names.Count; offset += slotCount)
        {
            var firstNameIndex = offset;
            var resolution = new PhenotypeInfo<string>((locus, position) =>
            {
                var ordinal = Array.FindIndex(SubjectGenotypeSetPayloadFormat.Slots, slot => slot.Locus == locus && slot.Position == position);
                var index = firstNameIndex + ordinal;

                // Past the end, fall back to the first name rather than adding a new one, so the pool size is exactly
                // the count asked for and the width boundary is tested where it is claimed to be.
                return index < names.Count ? names[index] : names[0];
            });

            genotypes.Add(BuildGenotype(resolution, resolution, (offset + 1) * 0.0001m));
        }

        return new SubjectGenotypeSet(false, genotypes, 1m);
    }

    private static SubjectGenotypeSet SetWithSumOfLikelihoods(decimal sumOfLikelihoods) =>
        new(false, [BuildGenotype(PhenotypeOf("hla"), PhenotypeOf("hla"), 0.5m)], sumOfLikelihoods);

    private static SubjectGenotypeSet AssertRoundTrips(SubjectGenotypeSet original)
    {
        var decoded = SubjectGenotypeSetPayload.Decode(SubjectGenotypeSetPayload.Encode(original));

        AssertSameSet(decoded, original);

        return decoded;
    }

    private static void AssertSameSet(SubjectGenotypeSet actual, SubjectGenotypeSet expected)
    {
        actual.IsUnrepresented.Should().Be(expected.IsUnrepresented);
        AssertSameDecimal(actual.SumOfLikelihoods, expected.SumOfLikelihoods, "sum of likelihoods");
        actual.Genotypes.Should().HaveCount(expected.Genotypes.Count);

        var actualGenotypes = actual.Genotypes.ToList();
        var expectedGenotypes = expected.Genotypes.ToList();

        for (var i = 0; i < expectedGenotypes.Count; i++)
        {
            actualGenotypes[i].HaplotypeResolution.Should()
                .Be(expectedGenotypes[i].HaplotypeResolution, $"genotype {i}'s haplotype resolution should round trip");
            actualGenotypes[i].StringMatchableResolution.Should()
                .Be(expectedGenotypes[i].StringMatchableResolution, $"genotype {i}'s string matchable resolution should round trip");
            AssertSameDecimal(actualGenotypes[i].GenotypeLikelihood, expectedGenotypes[i].GenotypeLikelihood, $"genotype {i}'s likelihood");
        }
    }

    /// <summary>
    /// Compares the bit pattern, not the value. <c>1.23m == 1.2300m</c>, so a value comparison would pass even if the
    /// encoder had dropped the scale - the one failure this format's "no precision loss" criterion exists to catch.
    /// </summary>
    private static void AssertSameDecimal(decimal actual, decimal expected, string because)
    {
        decimal.GetBits(actual).Should().Equal(decimal.GetBits(expected), $"{because} should keep its exact decimal bits");
    }

    #endregion
}
