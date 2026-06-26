using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Common.Utils.Extensions;

/// <summary>
/// Helpers for running a unit of work inside a database transaction in a way that is safe regardless of the
/// context's configured execution strategy.
/// </summary>
/// <remarks>
/// The execution-strategy wrapper is required because a context configured with a retrying execution strategy
/// (<c>EnableRetryOnFailure</c>) forbids user-initiated transactions unless they run as a single retriable unit.
/// Under a non-retrying strategy the operation simply runs once. Wrapping unconditionally keeps these helpers
/// correct under either configuration.
/// </remarks>
public static class DbContextTransactionExtensions
{
    extension(DbContext context)
    {
        /// <summary>
        /// Runs <paramref name="operation"/> inside a database transaction, wrapped in the context's execution strategy.
        /// The transaction is committed when the operation completes and rolled back (via disposal) if it throws.
        /// </summary>
        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
        {
            if (context.Database.CurrentTransaction is not null)
            {
                return await operation();
            }

            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await context.Database.BeginTransactionAsync();
                    var result = await operation();
                    await transaction.CommitAsync();
                    return result;
                }
            );
        }

        /// <summary>
        /// Synchronous counterpart to <see cref="ExecuteInTransactionAsync{T}"/>: runs <paramref name="operation"/>
        /// inside a database transaction, wrapped in the context's execution strategy. The transaction is committed
        /// when the operation completes and rolled back (via disposal) if it throws.
        /// </summary>
        public void ExecuteInTransaction(Action operation)
        {
            if (context.Database.CurrentTransaction is not null)
            {
                operation();
                return;
            }

            var strategy = context.Database.CreateExecutionStrategy();
            strategy.Execute(() =>
                {
                    using var transaction = context.Database.BeginTransaction();
                    operation();
                    transaction.Commit();
                }
            );
        }
    }
}