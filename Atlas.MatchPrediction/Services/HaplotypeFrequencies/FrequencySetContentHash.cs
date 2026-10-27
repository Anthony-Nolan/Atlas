using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Atlas.MatchPrediction.Data.Models;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies;

/// <summary>
/// The content hash of a haplotype frequency set: the SHA-256 of its rows, in their order, without the set id. Sets with
/// the same hash have the same rows in the same order, so they build the same <see cref="FrequencySetCacheEntry"/>, and
/// the caches keep one entry for all of them.
///
/// <para>
/// <b>The form of the hashed bytes must not change.</b> SQL keeps the hash of each set
/// (<c>HaplotypeFrequencySets.ContentHash</c>): a hash of another form would not match the stored hashes, and each set
/// would fail the check of its rows. A change of the form needs a new column, or a new backfill of every set. So the
/// form does not depend on <see cref="FrequencySetPayload"/>, whose layout can change with its version.
/// </para>
///
/// <para>The hashed bytes, for each row in order:</para>
/// <list type="bullet">
/// <item>For each of A, B, C, DQB1 and DRB1: one byte, 0 for null and 1 for a name. Then, for a name, the length of its
/// UTF-8 bytes as a 7-bit encoded int, and those bytes. So null and the empty name are different.</item>
/// <item>The four int32s of <c>decimal.GetBits</c> of the frequency, little-endian. So the scale counts, as it does in
/// the arithmetic.</item>
/// <item>The typing category, as one byte.</item>
/// </list>
/// </summary>
internal static class FrequencySetContentHash
{
    /// <summary>The length of a hash, in bytes: the column is <c>binary(32)</c>.</summary>
    public const int SizeInBytes = 32;

    /// <summary>Large enough that the hash gets the bytes in large blocks, not a few bytes at a time.</summary>
    private const int BufferSizeInBytes = 64 * 1024;

    private const byte NullName = 0;
    private const byte Name = 1;

    /// <summary>The encoding of <see cref="FrequencySetPayload"/>: a name that UTF-8 cannot hold throws.</summary>
    private static readonly Encoding NameEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <returns>The content hash of <paramref name="rows"/>, in their order.</returns>
    public static byte[] Calculate(IReadOnlyList<LightweightHaplotypeFrequencyRecord> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        using (var buffer = new BufferedStream(new HashStream(hash), BufferSizeInBytes))
        using (var writer = new BinaryWriter(buffer, NameEncoding, leaveOpen: true))
        {
            Span<int> bits = stackalloc int[4];
            foreach (var row in rows)
            {
                WriteName(writer, row.A);
                WriteName(writer, row.B);
                WriteName(writer, row.C);
                WriteName(writer, row.DQB1);
                WriteName(writer, row.DRB1);

                decimal.GetBits(row.Frequency, bits);
                foreach (var part in bits)
                {
                    writer.Write(part);
                }

                writer.Write((byte)row.TypingCategory);
            }

            writer.Flush();
            buffer.Flush();
        }

        return hash.GetHashAndReset();
    }

    /// <summary>The hash as lowercase hexadecimal: the form in cache keys and logs.</summary>
    public static string ToHex(byte[] contentHash) => Convert.ToHexStringLower(contentHash);

    private static void WriteName(BinaryWriter writer, string name)
    {
        if (name == null)
        {
            writer.Write(NullName);
            return;
        }

        writer.Write(Name);
        writer.Write(name);
    }

    /// <summary>A stream that only gives the bytes written to it to a hash.</summary>
    private sealed class HashStream(IncrementalHash hash) : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count) => hash.AppendData(buffer, offset, count);
        public override void Write(ReadOnlySpan<byte> buffer) => hash.AppendData(buffer);

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
