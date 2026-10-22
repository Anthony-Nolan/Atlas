using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Utils;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies;

/// <summary>
/// Turns the rows of a haplotype frequency set into the value that the distributed cache holds for the set, and back.
///
/// <para>
/// <b>The rows keep the order that SQL gave them.</b> <see cref="FrequencySetCacheEntry.Build"/> interns the rows in
/// that order, and the order sets the interner ids, the order of the frozen dictionary, and so the order of
/// <see cref="FrequencySetCacheEntry.ProjectedPool"/>: the tie-break of truncation. So a set from the distributed cache
/// builds the same entry as a set from SQL. Do not encode a built entry: its order is not the SQL order.
/// </para>
///
/// <para>
/// <b>Layout.</b> One uncompressed prefix byte, <see cref="FormatVersion"/>, then a gzip stream that holds the body:
/// </para>
///
/// <list type="table">
/// <item><term>SetId</term><description>int32. The decoder checks it.</description></item>
/// <item><term>RowCount</term><description>int32. Zero is legal.</description></item>
/// <item><term>Pools</term><description>For each locus (A, B, C, DQB1, DRB1): a 7-bit encoded count, then that many
/// length-prefixed UTF-8 names, in the order the rows first use them.</description></item>
/// <item><term>Names</term><description>For each locus: <c>RowCount</c> 7-bit encoded pool indices. 0 is null; i is
/// the name at place i - 1 of the pool. So null and the empty name stay different.</description></item>
/// <item><term>Frequencies</term><description>For each of the four int32s of <c>decimal.GetBits</c>: <c>RowCount</c>
/// int32s.</description></item>
/// <item><term>Categories</term><description><c>RowCount</c> bytes of <see cref="HaplotypeTypingCategory"/>.</description></item>
/// </list>
///
/// <para>
/// Columns, not rows: columns compress better, to a few bytes for each row.
/// </para>
///
/// <para>
/// <b>The decimals keep every bit</b>, the scale too: SQL gives each frequency with scale 20, and a round trip keeps it.
/// </para>
///
/// <para>
/// <b>The same rows always give the same bytes.</b> So the processes that write one set to the distributed cache at the
/// same time write the same chunks, and the cache keeps one copy.
/// </para>
///
/// <para>
/// <b>Failure contract.</b> Every malformed payload throws <see cref="InvalidDataException"/>, and so does a payload of
/// another set. The version is also in the cache name: change both when the format changes, as the old and the new code
/// read the same cache during a deployment.
/// </para>
/// </summary>
internal static class FrequencySetPayload
{
    /// <summary>The first byte of each payload, outside the gzip stream.</summary>
    internal const byte FormatVersion = 1;

    /// <summary>
    /// <see cref="CompressionLevel.Optimal"/> is the measured choice: <c>Fastest</c> saves little time, and gives
    /// many more bytes.
    /// </summary>
    private const CompressionLevel Compression = CompressionLevel.Optimal;

    private const int PrefixSize = 1;
    private const int DecimalPartCount = 4;

    /// <summary>The body header: the set id and the row count.</summary>
    private const int HeaderSize = sizeof(int) + sizeof(int);

    /// <summary>
    /// The smallest a row can be: five one-byte indices, the four ints of its frequency and its category. Used only to
    /// reject a row count that a body cannot hold, before the count sizes an array.
    /// </summary>
    private const int MinimumRowSize = 5 + DecimalPartCount * sizeof(int) + 1;

    private static readonly Func<LightweightHaplotypeFrequencyRecord, string>[] NamesByLocus =
    [
        row => row.A,
        row => row.B,
        row => row.C,
        row => row.DQB1,
        row => row.DRB1
    ];

    /// <summary>
    /// Throwing UTF-8, with no byte-order mark: an encode does not change a name that UTF-8 cannot hold, and a decode
    /// does not turn bytes that are not valid into replacement characters.
    /// </summary>
    private static readonly Encoding NameEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>Encodes the rows of set <paramref name="setId"/>, in their order.</summary>
    public static byte[] Encode(int setId, IReadOnlyList<LightweightHaplotypeFrequencyRecord> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        using var output = new MemoryStream();
        output.WriteByte(FormatVersion);
        GzipPayloadBody.Write(output, EncodeBody(setId, rows), Compression);

        return output.ToArray();
    }

    /// <summary>The exact inverse of <see cref="Encode"/>, for set <paramref name="setId"/>.</summary>
    /// <exception cref="InvalidDataException">
    /// The payload is not a payload of this format, or it is the payload of another set.
    /// </exception>
    public static List<LightweightHaplotypeFrequencyRecord> Decode(int setId, byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (payload.Length < PrefixSize + GzipPayloadBody.MinimumSize)
        {
            throw new InvalidDataException($"Payload is {payload.Length} bytes; it is too short to hold the format version and a gzip member.");
        }

        if (payload[0] != FormatVersion)
        {
            throw new InvalidDataException($"Format version {payload[0]} is not supported; this build reads version {FormatVersion}.");
        }

        return DecodeBody(setId, GzipPayloadBody.Read(payload, PrefixSize));
    }

    private static byte[] EncodeBody(int setId, IReadOnlyList<LightweightHaplotypeFrequencyRecord> rows)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, NameEncoding, leaveOpen: true))
        {
            writer.Write(setId);
            writer.Write(rows.Count);

            var indicesByLocus = new List<int[]>(NamesByLocus.Length);
            foreach (var nameOf in NamesByLocus)
            {
                indicesByLocus.Add(WritePool(writer, rows, nameOf));
            }

            foreach (var indices in indicesByLocus)
            {
                foreach (var index in indices)
                {
                    writer.Write7BitEncodedInt(index);
                }
            }

            Span<int> bits = stackalloc int[DecimalPartCount];
            for (var part = 0; part < DecimalPartCount; part++)
            {
                foreach (var row in rows)
                {
                    decimal.GetBits(row.Frequency, bits);
                    writer.Write(bits[part]);
                }
            }

            foreach (var row in rows)
            {
                writer.Write((byte)row.TypingCategory);
            }
        }

        return stream.ToArray();
    }

    /// <summary>Writes the pool of one locus, in the order the rows first use the names.</summary>
    /// <returns>The pool index of the name of each row.</returns>
    private static int[] WritePool(BinaryWriter writer, IReadOnlyList<LightweightHaplotypeFrequencyRecord> rows, Func<LightweightHaplotypeFrequencyRecord, string> nameOf)
    {
        var indexByName = new Dictionary<string, int>(StringComparer.Ordinal);
        var pool = new List<string>();
        var indices = new int[rows.Count];

        for (var i = 0; i < rows.Count; i++)
        {
            var name = nameOf(rows[i]);
            if (name == null)
            {
                continue;
            }

            if (!indexByName.TryGetValue(name, out var index))
            {
                pool.Add(name);
                index = pool.Count;
                indexByName.Add(name, index);
            }

            indices[i] = index;
        }

        writer.Write7BitEncodedInt(pool.Count);
        foreach (var name in pool)
        {
            writer.Write(name);
        }

        return indices;
    }

    private static List<LightweightHaplotypeFrequencyRecord> DecodeBody(int setId, byte[] body)
    {
        try
        {
            using var stream = new MemoryStream(body, writable: false);
            using var reader = new BinaryReader(stream, NameEncoding, leaveOpen: true);

            var storedSetId = reader.ReadInt32();
            if (storedSetId != setId)
            {
                throw new InvalidDataException($"Payload holds set {storedSetId}, not set {setId}.");
            }

            var rowCount = reader.ReadInt32();
            if (rowCount < 0 || (long)rowCount * MinimumRowSize > body.Length - HeaderSize)
            {
                throw new InvalidDataException($"Row count {rowCount:N0} cannot fit in a {body.Length:N0}-byte body; the payload is corrupt.");
            }

            var pools = new List<string[]>(NamesByLocus.Length);
            for (var locus = 0; locus < NamesByLocus.Length; locus++)
            {
                pools.Add(ReadPool(reader, rowCount));
            }

            var namesByLocus = new List<string[]>(NamesByLocus.Length);
            foreach (var pool in pools)
            {
                namesByLocus.Add(ReadNames(reader, pool, rowCount));
            }

            var bitsByPart = new List<int[]>(DecimalPartCount);
            for (var part = 0; part < DecimalPartCount; part++)
            {
                bitsByPart.Add(ReadInts(reader, rowCount));
            }

            var rows = new List<LightweightHaplotypeFrequencyRecord>(rowCount);
            Span<int> bits = stackalloc int[DecimalPartCount];
            for (var i = 0; i < rowCount; i++)
            {
                for (var part = 0; part < DecimalPartCount; part++)
                {
                    bits[part] = bitsByPart[part][i];
                }

                rows.Add(new LightweightHaplotypeFrequencyRecord
                {
                    A = namesByLocus[0][i],
                    B = namesByLocus[1][i],
                    C = namesByLocus[2][i],
                    DQB1 = namesByLocus[3][i],
                    DRB1 = namesByLocus[4][i],
                    SetId = setId,
                    Frequency = ReadDecimal(bits),
                    TypingCategory = ReadCategory(reader)
                });
            }

            if (stream.Position != stream.Length)
            {
                throw new InvalidDataException($"Payload holds {stream.Length - stream.Position:N0} byte(s) after its last field, so it was not written by this encoder.");
            }

            return rows;
        }
        catch (EndOfStreamException exception)
        {
            throw new InvalidDataException("Payload ended before the body was fully read; it is truncated.", exception);
        }
        catch (Exception exception) when (exception is FormatException or IOException)
        {
            // The length prefixes that BinaryReader owns: Read7BitEncodedInt raises FormatException for a varint that
            // never ends, and ReadString raises IOException for a length that decodes negative. EndOfStreamException is
            // an IOException, so it stays above.
            throw new InvalidDataException("Payload holds a malformed length prefix, so it was not written by this encoder.", exception);
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException("Payload holds bytes that are not valid UTF-8 where an HLA name was expected.", exception);
        }
    }

    /// <summary>A pool cannot have more names than the set has rows: each name is first used by a row.</summary>
    private static string[] ReadPool(BinaryReader reader, int rowCount)
    {
        var poolCount = reader.Read7BitEncodedInt();
        if (poolCount < 0 || poolCount > rowCount)
        {
            throw new InvalidDataException($"Pool count {poolCount:N0} is outside 0..{rowCount:N0}, the row count.");
        }

        var pool = new string[poolCount];
        for (var i = 0; i < poolCount; i++)
        {
            pool[i] = reader.ReadString();
        }

        return pool;
    }

    private static string[] ReadNames(BinaryReader reader, string[] pool, int rowCount)
    {
        var names = new string[rowCount];
        for (var i = 0; i < rowCount; i++)
        {
            var index = reader.Read7BitEncodedInt();
            if (index < 0 || index > pool.Length)
            {
                throw new InvalidDataException($"Pool index {index:N0} is outside 0..{pool.Length:N0}.");
            }

            names[i] = index == 0 ? null : pool[index - 1];
        }

        return names;
    }

    private static int[] ReadInts(BinaryReader reader, int rowCount)
    {
        var values = new int[rowCount];
        for (var i = 0; i < rowCount; i++)
        {
            values[i] = reader.ReadInt32();
        }

        return values;
    }

    private static decimal ReadDecimal(ReadOnlySpan<int> bits)
    {
        try
        {
            return new decimal(bits);
        }
        catch (ArgumentException exception)
        {
            // The constructor rejects a reserved bit or a scale above 28.
            throw new InvalidDataException($"Payload holds an invalid decimal: [{bits[0]}, {bits[1]}, {bits[2]}, 0x{bits[3]:X8}].", exception);
        }
    }

    private static HaplotypeTypingCategory ReadCategory(BinaryReader reader)
    {
        var category = (HaplotypeTypingCategory)reader.ReadByte();

        return Enum.IsDefined(category)
            ? category
            : throw new InvalidDataException($"Payload holds typing category {(int)category}, which is not defined.");
    }
}
