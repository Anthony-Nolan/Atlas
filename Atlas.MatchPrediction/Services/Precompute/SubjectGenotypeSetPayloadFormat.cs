using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Services.Precompute;

/// <summary>
/// The constants and the slot table of the V1 <see cref="SubjectGenotypeSetPayload"/> binary format.
///
/// <para>
/// <b>Everything here is a persisted contract.</b> A row written by one release is read by a later one, so a change to
/// any value below changes the meaning of bytes already in the database. Add a new
/// <see cref="BodyVersion"/> instead; do not edit a value in place.
/// </para>
/// </summary>
internal static class SubjectGenotypeSetPayloadFormat
{
    /// <summary>
    /// First byte of every payload. Deliberately NOT gzip's own <c>0x1F</c>, so a bare gzip blob written by some other
    /// code is distinguishable from a payload, instead of being decompressed into a confusing body-version error.
    /// </summary>
    internal const byte ContainerMarker = 0xA7;

    /// <summary>
    /// Second byte. Versions the ENVELOPE - the two prefix bytes and the compression algorithm - not the body.
    /// It is outside the compressed stream, which is the only thing that ever makes the algorithm swappable.
    /// </summary>
    internal const byte ContainerVersion = 0x01;

    /// <summary>First byte INSIDE the compressed stream. Versions the body layout, independently of the envelope.</summary>
    internal const byte BodyVersion = 0x01;

    /// <summary>
    /// Gzip's own CRC32 + ISIZE trailer is the corruption check, so the body carries no checksum and no uncompressed
    /// length. <see cref="CompressionLevel.Optimal"/> is the measured choice: <c>Fastest</c> costs 35% more bytes, and
    /// <c>SmallestSize</c> buys 3% for 70% more encode time.
    /// </summary>
    internal const CompressionLevel Compression = CompressionLevel.Optimal;

    #region Flags byte

    /// <summary>bit 0: pool indices are 2 bytes rather than 1. Recorded explicitly so the decoder can assert it.</summary>
    internal const byte WideIndicesFlag = 0b0000_0001;

    /// <summary>bit 1: <see cref="Models.SubjectGenotypeSet.IsUnrepresented"/>.</summary>
    internal const byte IsUnrepresentedFlag = 0b0000_0010;

    /// <summary>bits 2-7 are reserved. A decoder that sees one set is reading a payload it does not understand.</summary>
    internal const byte ReservedFlagsMask = 0b1111_1100;

    #endregion

    #region Pool sizing

    /// <summary>
    /// Pool index <c>0</c> is the reserved null sentinel, so real entries are numbered <c>1..PoolCount</c>. A pool of
    /// 255 therefore still fits in one byte; 256 does not. The width boundary is on the COUNT, not on the largest
    /// index actually used, so that the width is a pure function of the pool and needs no second pass.
    /// </summary>
    internal const int NullSentinel = 0;

    /// <inheritdoc cref="NullSentinel"/>
    internal const int NarrowPoolCountLimit = 255;

    /// <summary>
    /// A wide index is 2 bytes, so the format cannot address more than this. Truncation caps a set at 2,000 genotypes
    /// (<c>GenotypeImputationSettings.MaximumExpandedGenotypesPerInput</c>), giving at most 2,000 x 24 = 48,000
    /// distinct strings - comfortably under. The encoder still checks, because the cap is configuration, not a law.
    /// </summary>
    internal const int MaxPoolCount = ushort.MaxValue;

    internal static bool IsWide(int poolCount) => poolCount > NarrowPoolCountLimit;

    internal static int IndexWidthInBytes(bool wide) => wide ? 2 : 1;

    #endregion

    #region Genotype record sizing

    /// <summary>Overrides are counted in one byte, and there are only <see cref="SlotCount"/> slots to override.</summary>
    internal const int SlotCount = 12;

    /// <summary>4 x int32 in <c>decimal.GetBits</c> order.</summary>
    internal const int DecimalSizeInBytes = 16;

    /// <summary>
    /// The smallest a genotype record can be: all 12 slots, a zero override count, and the likelihood. Used only to
    /// reject an absurd <c>GenotypeCount</c> in a corrupt payload before it is used to size a list.
    /// </summary>
    internal static int MinimumGenotypeRecordSize(bool wide) =>
        SlotCount * IndexWidthInBytes(wide) + 1 + DecimalSizeInBytes;

    #endregion

    #region The slot table - frozen

    /// <summary>
    /// Slot ordinal -> position, for all six loci including Dpb1.
    ///
    /// <para>
    /// <b>Written out, and deliberately not computed as <c>(int)locus * 2 + (int)position</c>.</b>
    /// <see cref="Locus"/> and <see cref="LocusPosition"/> are implicitly valued, so adding a member - <c>Drb3</c>,
    /// say - would renumber the arithmetic form and silently reinterpret every row already stored. This array cannot
    /// move that way. <c>SubjectGenotypeSetPayloadTests.SlotTable_IsTheFrozenV1Order</c> pins it.
    /// </para>
    ///
    /// <para>
    /// It happens to agree with <see cref="PhenotypeInfo{T}.EachPosition"/>'s order today. Nothing here reads that
    /// order, and nothing should: the persisted contract is this array.
    /// </para>
    /// </summary>
    internal static readonly (Locus Locus, LocusPosition Position)[] Slots =
    [
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
        (Locus.Drb1, LocusPosition.Two)
    ];

    /// <summary>
    /// Derived from <see cref="Slots"/> at type initialisation: for each locus, which slot ordinal holds position 1
    /// and which holds position 2. This is what lets <see cref="ToPhenotype"/> rebuild a phenotype from the table
    /// rather than from a second, hand-written copy of the same ordering that could drift out of step with it.
    /// </summary>
    private static readonly IReadOnlyDictionary<Locus, (int First, int Second)> OrdinalsByLocus = BuildOrdinalsByLocus();

    private static IReadOnlyDictionary<Locus, (int First, int Second)> BuildOrdinalsByLocus()
    {
        var first = new Dictionary<Locus, int>();
        var second = new Dictionary<Locus, int>();

        for (var ordinal = 0; ordinal < Slots.Length; ordinal++)
        {
            var (locus, position) = Slots[ordinal];
            var target = position == LocusPosition.One ? first : second;
            if (!target.TryAdd(locus, ordinal))
            {
                throw new InvalidOperationException($"Slot table is malformed: {locus} {position} appears more than once.");
            }
        }

        var ordinals = new Dictionary<Locus, (int, int)>();
        foreach (var locus in first.Keys)
        {
            ordinals[locus] = (first[locus], second[locus]);
        }

        return ordinals;
    }

    /// <summary>Reads the 12 slot values out of a phenotype, in <see cref="Slots"/> order.</summary>
    internal static void ToSlots(PhenotypeInfo<string> phenotype, string[] destination)
    {
        for (var ordinal = 0; ordinal < Slots.Length; ordinal++)
        {
            var (locus, position) = Slots[ordinal];
            destination[ordinal] = phenotype.GetPosition(locus, position);
        }
    }

    /// <summary>
    /// The inverse of <see cref="ToSlots"/>. Uses the six-<see cref="LocusInfo{T}"/> constructor rather than twelve
    /// <c>SetPosition</c> calls: that overload allocates one object per locus, where each <c>SetPosition</c> would
    /// rebuild the whole phenotype.
    /// </summary>
    internal static PhenotypeInfo<string> ToPhenotype(string[] slotValues) =>
        new(
            valueA: LocusFrom(slotValues, Locus.A),
            valueB: LocusFrom(slotValues, Locus.B),
            valueC: LocusFrom(slotValues, Locus.C),
            valueDpb1: LocusFrom(slotValues, Locus.Dpb1),
            valueDqb1: LocusFrom(slotValues, Locus.Dqb1),
            valueDrb1: LocusFrom(slotValues, Locus.Drb1)
        );

    private static LocusInfo<string> LocusFrom(string[] slotValues, Locus locus)
    {
        var (first, second) = OrdinalsByLocus[locus];
        return new LocusInfo<string>(slotValues[first], slotValues[second]);
    }

    #endregion

    /// <summary>
    /// Throwing UTF-8, with no byte-order mark.
    ///
    /// <para>
    /// Throwing matters on both sides. On encode, an unpaired surrogate in an HLA name would otherwise be written as
    /// <c>U+FFFD</c> and the round trip would silently lose it. On decode, invalid bytes that still satisfy gzip's
    /// CRC would otherwise decode to replacement characters instead of being reported.
    /// </para>
    /// </summary>
    internal static readonly Encoding PayloadEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
}
