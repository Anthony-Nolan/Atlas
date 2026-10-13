using System;

namespace Atlas.HlaMetadataDictionary.ExternalInterface.Models
{
    /// <param name="HlaNomenclatureVersion">The HLA Nomenclature version the dictionary is at.</param>
    /// <param name="SnapshotUtc">
    /// Identifies the set of tables written by this recreation: every one of them ends its name with this time, in UTC,
    /// formatted as <c>yyyyMMddHHmmssfff</c>. Null when the dictionary was not recreated.
    /// </param>
    public record HlaMetadataDictionaryRecreationResult(string HlaNomenclatureVersion, DateTime? SnapshotUtc);
}
