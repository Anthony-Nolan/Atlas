namespace Atlas.Common.Sql;

/// <summary>
/// Standard maximum lengths (in characters) for bounded string columns, shared across the Atlas <c>*.Data</c> projects
/// (and the projects that test or consume them) so a single value backs every matching <c>[MaxLength(...)]</c> /
/// <c>HasMaxLength(...)</c> annotation and any runtime truncation logic that mirrors it.
/// </summary>
public static class StringColumnLengths
{
    /// <summary>
    /// A moderately long, single-value bounded text column — e.g. a blob file path or a human-readable failure message.
    /// Maps to <c>nvarchar(1024)</c>.
    /// </summary>
    public const int LongText = 1024;
}
