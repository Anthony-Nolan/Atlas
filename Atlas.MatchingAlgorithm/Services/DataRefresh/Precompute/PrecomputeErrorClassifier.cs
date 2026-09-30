using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Azure;
using Microsoft.Data.SqlClient;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;

/// <summary>What a retry can do about an error of the precomputation of one genotype set value.</summary>
public enum PrecomputeErrorKind
{
    /// <summary>
    /// The typing cannot be imputed, and a retry gives the same error: the HLA Metadata Dictionary has no data for a name
    /// of the typing. The error fails only its own value.
    /// </summary>
    KnownPermanent,

    /// <summary>
    /// A database, storage or network error. The values after it would fail too, so the work stops and keeps what is
    /// done, and a retry can succeed.
    /// </summary>
    KnownTemporary,

    /// <summary>
    /// An error of unknown cause. It counts as temporary, so its value is computed again on a retry. But it fails only its
    /// own value: the work goes on with the others.
    /// </summary>
    Unknown
}

/// <summary>Sorts the errors of the precomputation into <see cref="PrecomputeErrorKind"/>s.</summary>
/// <remarks>
/// <para>
/// <b>Temporary first.</b> A cause that a retry can fix wins over one that it cannot: a retry costs one attempt of a batch,
/// and a permanent failure costs its donors their rows.
/// </para>
///
/// <para>
/// <b><see cref="HlaMetadataDictionaryException"/> is the only known permanent error.</b> The dictionary throws it only to
/// say that a name has no data, and lets a failed storage request leave as itself (see <c>MetadataServiceBase</c>).
/// </para>
/// </remarks>
public static class PrecomputeErrorClassifier
{
    /// <summary>
    /// The transient errors of the SqlClient retry providers, and the network and timeout errors that they leave to the
    /// caller. A database that reaches its size limit (40544) is not here: a retry cannot fix it.
    /// </summary>
    private static readonly HashSet<int> TemporarySqlErrorNumbers =
    [
        .. SqlConfigurableRetryFactory.BaselineTransientErrors,
        // The client-side timeout of a command.
        -2,
        // The connection was lost: the network name is no longer available, a semaphore timeout, and connection resets.
        64, 121, 10053, 10054
    ];

    public static PrecomputeErrorKind Classify(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (IsKnownTemporary(exception))
        {
            return PrecomputeErrorKind.KnownTemporary;
        }

        return exception is HlaMetadataDictionaryException ? PrecomputeErrorKind.KnownPermanent : PrecomputeErrorKind.Unknown;
    }

    /// <summary>
    /// Also through the inner exceptions: a wrapped timeout is still a timeout. An HTTP timeout, for example, is a
    /// <c>TaskCanceledException</c> around a <see cref="TimeoutException"/>.
    /// </summary>
    private static bool IsKnownTemporary(Exception exception) => exception switch
    {
        null => false,
        SqlException sqlException => sqlException.Errors.Cast<SqlError>().Any(error => TemporarySqlErrorNumbers.Contains(error.Number)),
        // 0 is a request that got no response: the network failed.
        RequestFailedException requestFailed => requestFailed.Status is 0 or 408 or 429 or >= 500,
        HttpRequestException or TimeoutException => true,
        AggregateException aggregate => aggregate.InnerExceptions.Any(IsKnownTemporary),
        _ => IsKnownTemporary(exception.InnerException)
    };
}
