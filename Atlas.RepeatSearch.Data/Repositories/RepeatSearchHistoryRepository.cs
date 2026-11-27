using System.Threading.Tasks;
using Atlas.RepeatSearch.Data.Models;
using Atlas.RepeatSearch.Data.Settings;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.RepeatSearch.Data.Repositories;

public interface IRepeatSearchHistoryRepository
{
    /// <returns>
    /// True if the repeat search has a history record: it changed the canonical result set, after its results notification was published.
    /// </returns>
    Task<bool> IsRepeatSearchRecorded(string repeatSearchRequestId);
}

/// <remarks>
/// The history record is written by <see cref="ICanonicalResultSetRepository.ApplyDiffAndRecordRepeatSearch"/>, in the transaction that
/// changes the canonical result set.
/// </remarks>
public class RepeatSearchHistoryRepository : IRepeatSearchHistoryRepository
{
    private readonly string connectionString;

    public RepeatSearchHistoryRepository(ConnectionStrings connectionStrings)
    {
        connectionString = connectionStrings.RepeatSearchSqlConnectionString;
    }

    public async Task<bool> IsRepeatSearchRecorded(string repeatSearchRequestId)
    {
        var sql = @$"
            SELECT CASE WHEN EXISTS (
                SELECT 1 FROM {RepeatSearchHistoryRecord.QualifiedTableName}
                WHERE {nameof(RepeatSearchHistoryRecord.RepeatSearchRequestId)} = @{nameof(repeatSearchRequestId)}
            ) THEN 1 ELSE 0 END";

        await using (var connection = new SqlConnection(connectionString))
        {
            return await connection.ExecuteScalarAsync<bool>(sql, new { repeatSearchRequestId });
        }
    }
}
