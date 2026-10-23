using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;

namespace Atlas.MatchPrediction.Utils;

/// <summary>
/// The gzip stream that holds the body of a payload, after the uncompressed prefix bytes of the payload. Shared by
/// <c>SubjectGenotypeSetPayload</c> and <c>FrequencySetPayload</c>, which each own their prefix and their body.
/// </summary>
internal static class GzipPayloadBody
{
    /// <summary>
    /// A 10-byte gzip header and an 8-byte gzip trailer. A payload with fewer bytes after its prefix cannot hold a body,
    /// and a caller that checks this first lets <see cref="Read"/> read the trailer without a bounds test.
    /// </summary>
    internal const int MinimumSize = 10 + 8;

    /// <summary>
    /// Gzip's ISIZE field: the uncompressed length modulo 2^32, in the last four bytes of the member.
    /// </summary>
    private const int UncompressedLengthFieldSize = 4;

    /// <summary>Writes <paramref name="body"/> to <paramref name="output"/> as one gzip member.</summary>
    internal static void Write(Stream output, byte[] body, CompressionLevel compression)
    {
        using var gzip = new GZipStream(output, compression, leaveOpen: true);
        gzip.Write(body, 0, body.Length);
    }

    /// <summary>
    /// Returns the decompressed body that follows the first <paramref name="prefixLength"/> bytes of
    /// <paramref name="payload"/>. The inverse of <see cref="Write"/>.
    /// </summary>
    /// <exception cref="InvalidDataException">The gzip member is not valid, or is not complete.</exception>
    internal static byte[] Read(byte[] payload, int prefixLength)
    {
        byte[] body;
        try
        {
            using var input = new MemoryStream(payload, prefixLength, payload.Length - prefixLength, writable: false);
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
    /// This assumes a single gzip member, which is all <see cref="Write"/> ever writes. A concatenated multi-member
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
}
