using System;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Common.Caching;

/// <summary>
/// Stores a value that is too large for one Redis command in the distributed cache: in chunks of at most
/// <see cref="ChunkSizeInBytes"/>, under one small manifest at the key of the value.
/// </summary>
/// <remarks>
/// <para>
/// Microsoft's guidance for Azure Managed Redis is to keep each value under 100 kB. A large value holds the connection
/// for longer, and the other commands on it can then miss <c>RedisConnection.CommandTimeout</c>: all the lookups of a
/// process share that connection.
/// </para>
///
/// <para>
/// A write stores the chunks first, then the manifest, and the chunks live <see cref="ChunkTimeToLiveMargin"/> longer
/// than the manifest. If a chunk is not stored, the write stops before the manifest. So a manifest names only chunks
/// that were stored. A read gets the manifest, then its chunks, one command at a time. A missing chunk, or a chunk with
/// a length that is different, is a miss: e.g. when Redis evicted the chunk. The next write of the value then stores
/// it again.
/// </para>
///
/// <para>
/// <b>The chunk keys contain the SHA-256 of the value.</b> Processes do not coordinate their writes: when many of them
/// miss one value at the same time, each of them writes it. A value with the same bytes has the same chunk keys, so the
/// writers replace the chunks of each other, and Redis keeps one copy of the value. Only a different value under the
/// same key leaves chunks that no manifest names, until their time to live ends.
/// </para>
///
/// <para>
/// The chunks have the cache name of the key with <c>-chunks</c> after it. So the metrics of the cache name of the key
/// count the reads of whole values, apart from the reads of chunks.
/// </para>
///
/// <para>
/// Like <see cref="IDistributedCacheProvider"/>, these methods never throw because the cache cannot be reached or fails.
/// The manifest is part of the stored format of each caller: change it only with the versions in their cache names.
/// </para>
/// </remarks>
public static class ChunkedDistributedCacheExtensions
{
    /// <summary>The size of each chunk but the last: 96 KiB, under the guidance of 100 kB.</summary>
    public const int ChunkSizeInBytes = 96 * 1024;

    /// <summary>How much longer the chunks live than their manifest.</summary>
    internal static readonly TimeSpan ChunkTimeToLiveMargin = TimeSpan.FromHours(1);

    private const string ChunkCacheNameSuffix = "-chunks";

    /// <summary>
    /// Stores <paramref name="value"/> under <paramref name="key"/>, for <paramref name="timeToLive"/> from now at most.
    /// </summary>
    /// <returns>
    /// <c>false</c> if the value was not stored, because the cache could not be reached. A value stored under the key
    /// before can then still be read, until its own time to live ends.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeToLive"/> is not positive.</exception>
    public static async Task<bool> SetChunkedAsync(
        this IDistributedCacheProvider cache,
        DistributedCacheKey key,
        byte[] value,
        TimeSpan timeToLive,
        CancellationToken cancellationToken = default)
    {
        // Before the first chunk: the chunks of a key that is not valid would be stored, and its manifest not.
        DistributedCacheArguments.ThrowIfNotValid(key);
        DistributedCacheArguments.ThrowIfNotValid(value, timeToLive);

        var manifest = new ChunkManifest(Convert.ToHexStringLower(SHA256.HashData(value)), ChunkCount(value.Length), value.Length);

        for (var index = 0; index < manifest.ChunkCount; index++)
        {
            var offset = index * ChunkSizeInBytes;
            var chunk = value[offset..(offset + Math.Min(ChunkSizeInBytes, value.Length - offset))];

            if (!await cache.SetAsync(ChunkKey(key, manifest, index), chunk, timeToLive + ChunkTimeToLiveMargin, cancellationToken))
            {
                return false;
            }
        }

        return await cache.SetAsync(key, JsonSerializer.SerializeToUtf8Bytes(manifest), timeToLive, cancellationToken);
    }

    /// <returns>
    /// The value that <see cref="SetChunkedAsync"/> stored under <paramref name="key"/>, or <c>null</c> on a miss: no
    /// manifest, a manifest that is not valid, a missing chunk, or a chunk with a length that is different. A read that
    /// could not reach the cache is a miss too.
    /// </returns>
    public static async Task<byte[]> GetChunkedAsync(
        this IDistributedCacheProvider cache,
        DistributedCacheKey key,
        CancellationToken cancellationToken = default)
    {
        DistributedCacheArguments.ThrowIfNotValid(key);

        var manifest = ReadManifest(await cache.GetAsync(key, cancellationToken));
        if (manifest == null)
        {
            return null;
        }

        var value = new byte[manifest.Length];

        // One chunk at a time, so that one read does not fill the connection that the other lookups of the process use.
        for (var index = 0; index < manifest.ChunkCount; index++)
        {
            var offset = index * ChunkSizeInBytes;
            var chunk = await cache.GetAsync(ChunkKey(key, manifest, index), cancellationToken);

            if (chunk == null || chunk.Length != Math.Min(ChunkSizeInBytes, manifest.Length - offset))
            {
                return null;
            }

            chunk.CopyTo(value, offset);
        }

        return value;
    }

    /// <summary>The number of chunks of a value of <paramref name="length"/> bytes.</summary>
    public static int ChunkCount(int length) => length / ChunkSizeInBytes + (length % ChunkSizeInBytes == 0 ? 0 : 1);

    private static DistributedCacheKey ChunkKey(DistributedCacheKey key, ChunkManifest manifest, int index) =>
        new($"{key.CacheName}{ChunkCacheNameSuffix}", $"{key.Id}:{manifest.Sha256}:{index}");

    private static ChunkManifest ReadManifest(byte[] storedManifest)
    {
        if (storedManifest == null)
        {
            return null;
        }

        try
        {
            var manifest = JsonSerializer.Deserialize<ChunkManifest>(storedManifest);
            return manifest is { Sha256.Length: > 0, Length: >= 0 } && manifest.ChunkCount == ChunkCount(manifest.Length) ? manifest : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>The value under the key of a chunked value.</summary>
    /// <param name="Sha256">The SHA-256 of the value, as lowercase hexadecimal: part of each chunk key.</param>
    /// <param name="ChunkCount">The number of chunks.</param>
    /// <param name="Length">The length of the value, in bytes.</param>
    private sealed record ChunkManifest(string Sha256, int ChunkCount, int Length);
}
