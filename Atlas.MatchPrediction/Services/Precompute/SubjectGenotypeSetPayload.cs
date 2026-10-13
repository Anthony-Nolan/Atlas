using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Models;

namespace Atlas.MatchPrediction.Services.Precompute;

/// <summary>
/// Turns a <see cref="SubjectGenotypeSet"/> into the bytes stored in
/// <c>SubjectGenotypeSetValues.SubjectGenotypeSetData</c>, and back.
///
/// <para>
/// Pure functions with no dependencies, so no DI registration - the same reason
/// <see cref="MatchProbability.ExpandedGenotypeTruncater"/> is a static class.
/// </para>
///
/// <para>
/// <b>Layout.</b> Two uncompressed prefix bytes - <see cref="SubjectGenotypeSetPayloadFormat.ContainerMarker"/> and
/// <see cref="SubjectGenotypeSetPayloadFormat.ContainerVersion"/> - then a gzip stream holding the body:
/// </para>
///
/// <list type="table">
/// <item><term>BodyVersion</term><description>1 byte.</description></item>
/// <item><term>Flags</term><description>1 byte: wide indices, unrepresented, 6 reserved bits.</description></item>
/// <item><term>GenotypeCount</term><description>int32, little-endian. Zero is legal.</description></item>
/// <item><term>PoolCount</term><description>7-bit encoded int.</description></item>
/// <item><term>Pool</term><description><c>PoolCount</c> length-prefixed UTF-8 strings, ordinal-sorted.</description></item>
/// <item><term>Genotypes</term><description><c>GenotypeCount</c> records, see below.</description></item>
/// <item><term>SumOfLikelihoods</term><description>16 bytes.</description></item>
/// </list>
///
/// <para>
/// A genotype record is the 12 <see cref="GenotypeAtDesiredResolutions.StringMatchableResolution"/> pool ids in
/// <see cref="SubjectGenotypeSetPayloadFormat.Slots"/> order, then a one-byte count of the slots where
/// <see cref="GenotypeAtDesiredResolutions.HaplotypeResolution"/> differs, then those <c>(slot, id)</c> pairs in
/// ascending slot order, then the likelihood. The two resolutions are equal at almost every slot for a P-group
/// frequency set, so the diff is usually empty - but a set imported as SmallGGroup diverges at every typed position,
/// and the format carries that without a special case.
/// </para>
///
/// <para>
/// <b>Decimals keep every bit.</b> They are the four int32s of <c>decimal.GetBits</c>, restored through the
/// <c>decimal(ReadOnlySpan&lt;int&gt;)</c> constructor. That representation distinguishes scale, so <c>1.23m</c> and
/// <c>1.2300m</c> are different payloads and stay different after a round trip - which
/// <c>BinaryReader.ReadDecimal</c> would also do, but without the constructor's documented validity check on all
/// 2,001 decimals.
/// </para>
///
/// <para>
/// <b>Failure contract.</b> Every malformed payload throws <see cref="InvalidDataException"/> - truncated, not gzip,
/// a flipped byte, an unknown version, an out-of-range pool id, a reserved flag bit, a malformed length prefix,
/// bytes after the last field. Callers need one catch, not eight.
/// </para>
/// </summary>
public static class SubjectGenotypeSetPayload
{
    private const int SlotCount = SubjectGenotypeSetPayloadFormat.SlotCount;

    /// <summary>
    /// Encodes <paramref name="genotypeSet"/>. Deterministic: the same set always produces the same bytes, which is
    /// what lets two donors sharing a typing share one stored row.
    /// </summary>
    /// <remarks>
    /// A null <see cref="SubjectGenotypeSet.Genotypes"/> encodes as an empty collection, so a round trip normalises
    /// it. Nothing in the pipeline produces one - <c>GenotypeSetService</c> returns an empty list for the
    /// unrepresented case - so this is a defensive normalisation, not a supported input shape.
    /// </remarks>
    public static byte[] Encode(SubjectGenotypeSet genotypeSet)
    {
        ArgumentNullException.ThrowIfNull(genotypeSet);
        return Wrap(EncodeBody(genotypeSet));
    }

    /// <summary>The exact inverse of <see cref="Encode"/>.</summary>
    /// <exception cref="InvalidDataException">The payload is not a readable V1 payload.</exception>
    public static SubjectGenotypeSet Decode(byte[] payload) => DecodeBody(Unwrap(payload));

    #region Body - the uncompressed form, which is what the stability tests measure

    /// <summary>
    /// The body alone, with no envelope and no compression.
    ///
    /// <para>
    /// Separate from <see cref="Encode"/> so the format-stability test can hash something whose bytes are this code's
    /// contract. A hash of the COMPRESSED payload would pin DEFLATE's output, which a runtime upgrade is entitled to
    /// change. Splitting the two also lets the pool encoding and the compression be priced apart, which is how the
    /// format's sizes were measured.
    /// </para>
    /// </summary>
    internal static byte[] EncodeBody(SubjectGenotypeSet genotypeSet)
    {
        var genotypes = AsList(genotypeSet.Genotypes);
        var pool = HlaStringPool.Build(genotypes);

        if (pool.Count > SubjectGenotypeSetPayloadFormat.MaxPoolCount)
        {
            throw new ArgumentException(
                $"The set holds {pool.Count:N0} distinct HLA names, above the format's limit of "
              + $"{SubjectGenotypeSetPayloadFormat.MaxPoolCount:N0}.", nameof(genotypeSet));
        }

        var wide = SubjectGenotypeSetPayloadFormat.IsWide(pool.Count);

        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, SubjectGenotypeSetPayloadFormat.PayloadEncoding, leaveOpen: true))
        {
            writer.Write(SubjectGenotypeSetPayloadFormat.BodyVersion);
            writer.Write(Flags(wide, genotypeSet.IsUnrepresented));
            writer.Write(genotypes.Count);
            writer.Write7BitEncodedInt(pool.Count);

            foreach (var entry in pool.Entries)
            {
                writer.Write(entry);
            }

            WriteGenotypes(writer, genotypes, pool, wide);
            WriteDecimal(writer, genotypeSet.SumOfLikelihoods);
        }

        return stream.ToArray();
    }

    /// <inheritdoc cref="EncodeBody"/>
    internal static SubjectGenotypeSet DecodeBody(byte[] body)
    {
        try
        {
            using var stream = new MemoryStream(body, writable: false);
            using var reader = new BinaryReader(stream, SubjectGenotypeSetPayloadFormat.PayloadEncoding, leaveOpen: true);

            var bodyVersion = reader.ReadByte();
            if (bodyVersion != SubjectGenotypeSetPayloadFormat.BodyVersion)
            {
                throw new InvalidDataException(
                    $"Body version {bodyVersion} is not supported; this build reads version "
                  + $"{SubjectGenotypeSetPayloadFormat.BodyVersion}.");
            }

            var (wide, isUnrepresented) = ReadFlags(reader.ReadByte());
            var genotypeCount = reader.ReadInt32();
            var poolCount = reader.Read7BitEncodedInt();

            ValidateCounts(genotypeCount, poolCount, wide, body.Length);

            var pool = HlaStringPool.FromOrderedEntries(ReadPoolEntries(reader, poolCount));
            var genotypes = ReadGenotypes(reader, genotypeCount, pool, wide);
            var sumOfLikelihoods = ReadDecimal(reader);

            // The sum is the last field, so nothing may follow it. Gzip's CRC already covers what this encoder wrote,
            // so extra bytes can only come from a body that other code wrote. Accepting them would also let two
            // different payloads decode to one set - the same break in determinism that the override order rules out.
            if (stream.Position != stream.Length)
            {
                throw new InvalidDataException(
                    $"Payload holds {stream.Length - stream.Position:N0} byte(s) after its last field, so it was not written by this encoder.");
            }

            return new SubjectGenotypeSet(isUnrepresented, genotypes, sumOfLikelihoods);
        }
        catch (EndOfStreamException exception)
        {
            throw new InvalidDataException("Payload ended before the body was fully read; it is truncated.", exception);
        }
        catch (Exception exception) when (exception is FormatException or IOException)
        {
            // The two length prefixes BinaryReader owns. Read7BitEncodedInt raises FormatException for a varint that
            // never terminates, and ReadString raises IOException for one that decodes negative. Neither is reachable
            // past gzip's CRC, but both are reachable from a body this encoder did not write - and the failure
            // contract admits no second exception type. EndOfStreamException is an IOException, so it stays above.
            throw new InvalidDataException("Payload holds a malformed length prefix, so it was not written by this encoder.", exception);
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException("Payload holds bytes that are not valid UTF-8 where an HLA name was expected.", exception);
        }
    }

    #endregion

    #region Write

    private static byte Flags(bool wide, bool isUnrepresented)
    {
        var flags = 0;
        if (wide)
        {
            flags |= SubjectGenotypeSetPayloadFormat.WideIndicesFlag;
        }

        if (isUnrepresented)
        {
            flags |= SubjectGenotypeSetPayloadFormat.IsUnrepresentedFlag;
        }

        return (byte)flags;
    }

    private static void WriteGenotypes(BinaryWriter writer, IReadOnlyList<GenotypeAtDesiredResolutions> genotypes, HlaStringPool pool, bool wide)
    {
        var stringMatchableSlots = new string[SlotCount];
        var haplotypeSlots = new string[SlotCount];

        foreach (var genotype in genotypes)
        {
            SubjectGenotypeSetPayloadFormat.ToSlots(genotype.StringMatchableResolution, stringMatchableSlots);
            SubjectGenotypeSetPayloadFormat.ToSlots(genotype.HaplotypeResolution, haplotypeSlots);

            for (var ordinal = 0; ordinal < SlotCount; ordinal++)
            {
                WriteId(writer, pool.IdOf(stringMatchableSlots[ordinal]), wide);
            }

            WriteOverrides(writer, stringMatchableSlots, haplotypeSlots, pool, wide);
            WriteDecimal(writer, genotype.GenotypeLikelihood);
        }
    }

    /// <summary>
    /// The slots where the haplotype resolution differs from the string-matchable one, ascending. Counted in a first
    /// pass because the count is written before the pairs and a <c>varbinary</c> body has no back-patching.
    /// </summary>
    private static void WriteOverrides(
        BinaryWriter writer,
        string[] stringMatchableSlots,
        string[] haplotypeSlots,
        HlaStringPool pool,
        bool wide)
    {
        var overrideCount = 0;
        for (var ordinal = 0; ordinal < SlotCount; ordinal++)
        {
            if (Differs(stringMatchableSlots, haplotypeSlots, ordinal))
            {
                overrideCount++;
            }
        }

        writer.Write((byte)overrideCount);

        for (var ordinal = 0; ordinal < SlotCount; ordinal++)
        {
            if (!Differs(stringMatchableSlots, haplotypeSlots, ordinal))
            {
                continue;
            }

            writer.Write((byte)ordinal);
            WriteId(writer, pool.IdOf(haplotypeSlots[ordinal]), wide);
        }
    }

    private static bool Differs(string[] stringMatchableSlots, string[] haplotypeSlots, int ordinal) =>
        !string.Equals(haplotypeSlots[ordinal], stringMatchableSlots[ordinal], StringComparison.Ordinal);

    private static void WriteId(BinaryWriter writer, int id, bool wide)
    {
        if (wide)
        {
            writer.Write((ushort)id);
        }
        else
        {
            writer.Write((byte)id);
        }
    }

    private static void WriteDecimal(BinaryWriter writer, decimal value)
    {
        Span<int> bits = stackalloc int[4];
        decimal.GetBits(value, bits);

        foreach (var part in bits)
        {
            writer.Write(part);
        }
    }

    #endregion

    #region Read

    private static (bool Wide, bool IsUnrepresented) ReadFlags(byte flags)
    {
        if ((flags & SubjectGenotypeSetPayloadFormat.ReservedFlagsMask) != 0)
        {
            throw new InvalidDataException(
                $"Flags byte 0x{flags:X2} sets a reserved bit, so it was written by a format this build does not understand.");
        }

        return (
            (flags & SubjectGenotypeSetPayloadFormat.WideIndicesFlag) != 0,
            (flags & SubjectGenotypeSetPayloadFormat.IsUnrepresentedFlag) != 0
        );
    }

    /// <summary>
    /// Rejects counts that a corrupt payload could otherwise turn into a multi-gigabyte allocation before the read
    /// of the (absent) data failed. The bound is deliberately loose - it only has to be cheaper than believing the
    /// count outright.
    /// </summary>
    private static void ValidateCounts(int genotypeCount, int poolCount, bool wide, int bodyLength)
    {
        if (genotypeCount < 0)
        {
            throw new InvalidDataException($"Genotype count {genotypeCount} is negative.");
        }

        if (poolCount is < 0 or > SubjectGenotypeSetPayloadFormat.MaxPoolCount)
        {
            throw new InvalidDataException(
                $"Pool count {poolCount} is outside 0..{SubjectGenotypeSetPayloadFormat.MaxPoolCount}.");
        }

        // The width is a pure function of the pool count, so the flag adds no information - which is exactly why it
        // is worth checking. A disagreement means the body was not written by this encoder, and every genotype record
        // after this point would be read at the wrong stride.
        if (wide != SubjectGenotypeSetPayloadFormat.IsWide(poolCount))
        {
            throw new InvalidDataException(
                $"Flags say pool ids are {SubjectGenotypeSetPayloadFormat.IndexWidthInBytes(wide)} byte(s), but a pool of "
              + $"{poolCount} entries is written with {SubjectGenotypeSetPayloadFormat.IndexWidthInBytes(!wide)}.");
        }

        if ((long)genotypeCount * SubjectGenotypeSetPayloadFormat.MinimumGenotypeRecordSize(wide) > bodyLength)
        {
            throw new InvalidDataException(
                $"Genotype count {genotypeCount:N0} cannot fit in a {bodyLength:N0}-byte body; the payload is corrupt.");
        }
    }

    private static string[] ReadPoolEntries(BinaryReader reader, int poolCount)
    {
        var entries = new string[poolCount];
        for (var i = 0; i < poolCount; i++)
        {
            entries[i] = reader.ReadString();
        }

        return entries;
    }

    private static List<GenotypeAtDesiredResolutions> ReadGenotypes(BinaryReader reader, int genotypeCount, HlaStringPool pool, bool wide)
    {
        var genotypes = new List<GenotypeAtDesiredResolutions>(genotypeCount);
        var stringMatchableSlots = new string[SlotCount];
        var haplotypeSlots = new string[SlotCount];

        for (var index = 0; index < genotypeCount; index++)
        {
            // The haplotype resolution starts as a copy and is then patched by the overrides, which is what makes an
            // empty override list mean "the two resolutions are identical" rather than "the haplotype form is absent".
            for (var ordinal = 0; ordinal < SlotCount; ordinal++)
            {
                stringMatchableSlots[ordinal] = pool.NameOf(ReadId(reader, pool, wide));
                haplotypeSlots[ordinal] = stringMatchableSlots[ordinal];
            }

            ReadOverrides(reader, index, haplotypeSlots, pool, wide);

            var stringMatchableResolution = SubjectGenotypeSetPayloadFormat.ToPhenotype(stringMatchableSlots);
            var haplotypeResolution = SubjectGenotypeSetPayloadFormat.ToPhenotype(haplotypeSlots);
            var likelihood = ReadDecimal(reader);

            genotypes.Add(BuildGenotype(haplotypeResolution, stringMatchableResolution, likelihood));
        }

        return genotypes;
    }

    private static void ReadOverrides(BinaryReader reader, int genotypeIndex, string[] haplotypeSlots, HlaStringPool pool, bool wide)
    {
        var overrideCount = reader.ReadByte();
        if (overrideCount > SlotCount)
        {
            throw new InvalidDataException(
                $"Genotype {genotypeIndex} declares {overrideCount} overrides, above the {SlotCount} slots that exist.");
        }

        var previousOrdinal = -1;
        for (var i = 0; i < overrideCount; i++)
        {
            var ordinal = reader.ReadByte();
            if (ordinal >= SlotCount)
            {
                throw new InvalidDataException(
                    $"Genotype {genotypeIndex} overrides slot {ordinal}, which is outside 0..{SlotCount - 1}.");
            }

            // Ascending order is part of the format. Enforcing it also rules out a duplicate ordinal, which would
            // otherwise let two different payloads decode to one genotype and break determinism.
            if (ordinal <= previousOrdinal)
            {
                throw new InvalidDataException(
                    $"Genotype {genotypeIndex} lists override slot {ordinal} after slot {previousOrdinal}; overrides must ascend.");
            }

            previousOrdinal = ordinal;
            haplotypeSlots[ordinal] = pool.NameOf(ReadId(reader, pool, wide));
        }
    }

    /// <summary>
    /// <see cref="GenotypeAtDesiredResolutions"/> is built from an <see cref="ImputedGenotype"/> so that upstream
    /// cannot pair a name form with the wrong likelihood. Here the pairing came from one encoded record, so it holds
    /// by construction. <c>Genotype</c> is left empty: the precompute payload stores the two resolutions and the
    /// likelihood, which is all any consumer of a decoded set reads.
    /// </summary>
    private static GenotypeAtDesiredResolutions BuildGenotype(
        PhenotypeInfo<string> haplotypeResolution,
        PhenotypeInfo<string> stringMatchableResolution,
        decimal likelihood) =>
        new(new ImputedGenotype(null, haplotypeResolution, likelihood), stringMatchableResolution);

    private static int ReadId(BinaryReader reader, HlaStringPool pool, bool wide)
    {
        var id = wide ? reader.ReadUInt16() : reader.ReadByte();

        return pool.IsValidId(id)
            ? id
            : throw new InvalidDataException($"Pool id {id} is outside 0..{pool.Count}.");
    }

    private static decimal ReadDecimal(BinaryReader reader)
    {
        Span<int> bits = stackalloc int[4];
        for (var i = 0; i < bits.Length; i++)
        {
            bits[i] = reader.ReadInt32();
        }

        try
        {
            return new decimal(bits);
        }
        catch (ArgumentException exception)
        {
            // The constructor rejects a reserved-bit or out-of-range-scale pattern, which is a free integrity check
            // on every decimal in the payload. ReadDecimal() on BinaryReader makes no such guarantee.
            throw new InvalidDataException(
                $"Payload holds an invalid decimal: [{bits[0]}, {bits[1]}, {bits[2]}, 0x{bits[3]:X8}].", exception);
        }
    }

    #endregion

    #region Envelope

    /// <summary>
    /// Two prefix bytes, a 10-byte gzip header and an 8-byte gzip trailer. Anything shorter cannot be a payload, and
    /// checking it up front is what lets <see cref="VerifyDeclaredBodyLength"/> read the trailer without a bounds test.
    /// </summary>
    private const int MinimumPayloadSize = 2 + 10 + 8;

    /// <summary>
    /// Gzip's ISIZE field: the uncompressed length modulo 2^32, in the last four bytes of the member.
    /// </summary>
    private const int UncompressedLengthFieldSize = 4;

    /// <summary>
    /// Puts the two prefix bytes in front of a gzipped <paramref name="body"/>.
    ///
    /// <para>
    /// Internal as well as used by <see cref="Encode"/>, so the decoder's tests can put a real envelope around a body
    /// they have deliberately corrupted, without a second copy of this that could drift.
    /// </para>
    /// </summary>
    internal static byte[] Wrap(byte[] body)
    {
        using var output = new MemoryStream(body.Length / 2);
        output.WriteByte(SubjectGenotypeSetPayloadFormat.ContainerMarker);
        output.WriteByte(SubjectGenotypeSetPayloadFormat.ContainerVersion);

        using (var gzip = new GZipStream(output, SubjectGenotypeSetPayloadFormat.Compression, leaveOpen: true))
        {
            gzip.Write(body, 0, body.Length);
        }

        return output.ToArray();
    }

    /// <summary>Checks the prefix and returns the decompressed body. The inverse of <see cref="Wrap"/>.</summary>
    internal static byte[] Unwrap(byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (payload.Length < MinimumPayloadSize)
        {
            throw new InvalidDataException(
                $"Payload is {payload.Length} bytes; it is too short to hold the container prefix and a gzip member "
              + $"(the minimum is {MinimumPayloadSize}).");
        }

        if (payload[0] != SubjectGenotypeSetPayloadFormat.ContainerMarker)
        {
            throw new InvalidDataException(
                $"Payload does not start with the container marker 0x{SubjectGenotypeSetPayloadFormat.ContainerMarker:X2} "
              + $"(found 0x{payload[0]:X2}). This is not a subject genotype set payload.");
        }

        if (payload[1] != SubjectGenotypeSetPayloadFormat.ContainerVersion)
        {
            throw new InvalidDataException(
                $"Container version {payload[1]} is not supported; this build reads version "
              + $"{SubjectGenotypeSetPayloadFormat.ContainerVersion}.");
        }

        byte[] body;
        try
        {
            using var input = new MemoryStream(payload, 2, payload.Length - 2, writable: false);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var decompressed = new MemoryStream(payload.Length * 4);
            gzip.CopyTo(decompressed);
            body = decompressed.ToArray();
        }
        catch (InvalidDataException)
        {
            // Already the contract's exception - gzip raises this for a bad header and for a CRC mismatch, which is
            // how a single flipped byte anywhere in the compressed region is caught.
            throw;
        }
        catch (Exception exception)
        {
            throw new InvalidDataException("Payload's gzip stream could not be read.", exception);
        }

        VerifyDeclaredBodyLength(payload, body.Length);

        return body;
    }

    /// <summary>
    /// Checks the decompressed length against gzip's ISIZE field.
    ///
    /// <para>
    /// <b>Measured, not assumed.</b> zlib verifies the CRC32 + ISIZE trailer only when it is THERE. A payload cut at
    /// exactly the trailer boundary decompresses with no complaint at all - verified against .NET 10, where dropping
    /// the last 8 or 9 bytes of a payload produced no exception. So the trailer alone detects a flipped byte but not
    /// a truncation, and the four bytes read here are what close that gap. Everything after this point trusts the
    /// body's own length, so the check belongs before the body is parsed, not inside it.
    /// </para>
    ///
    /// <para>
    /// This assumes a single gzip member, which is all <see cref="Wrap"/> ever writes. A concatenated multi-member
    /// blob would declare only its last member's length and be rejected here - correctly, since it is not a payload.
    /// </para>
    /// </summary>
    private static void VerifyDeclaredBodyLength(byte[] payload, int bodyLength)
    {
        var declared = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(payload.Length - UncompressedLengthFieldSize));

        if (declared != (uint)bodyLength)
        {
            throw new InvalidDataException(
                $"Gzip trailer declares an uncompressed length of {declared:N0} bytes but {bodyLength:N0} were read; "
              + "the payload is truncated or was not written by this encoder.");
        }
    }

    #endregion

    private static IReadOnlyList<GenotypeAtDesiredResolutions> AsList(ICollection<GenotypeAtDesiredResolutions> genotypes) =>
        genotypes switch
        {
            null => [],
            IReadOnlyList<GenotypeAtDesiredResolutions> list => list,
            _ => new List<GenotypeAtDesiredResolutions>(genotypes)
        };
}
