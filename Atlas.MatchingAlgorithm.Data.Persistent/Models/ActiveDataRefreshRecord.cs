namespace Atlas.MatchingAlgorithm.Data.Persistent.Models
{
    /// <summary>
    /// The parts of the most recently completed, successful <see cref="DataRefreshRecord"/> that identify the active
    /// transient database. All values come from the same record, so they always describe the same refresh.
    /// </summary>
    /// <param name="Id">The <see cref="DataRefreshRecord.Id"/> of the refresh that filled the active database.</param>
    /// <param name="Database">The transient database that the refresh filled.</param>
    /// <param name="HlaNomenclatureVersion">The HLA nomenclature version that the refresh used.</param>
    public record ActiveDataRefreshRecord(int Id, TransientDatabase Database, string HlaNomenclatureVersion);
}
