using System;
using Atlas.Common.AzureStorage;

namespace Atlas.HlaMetadataDictionary.InternalModels
{
    /// <summary>
    /// One row per HLA nomenclature version, stamped afresh every time that version's data is recreated.
    /// </summary>
    /// <remarks>
    /// The stamp is what lets a running process notice a recreation at a version it is already using. Nothing about
    /// the version string changes in that case, so this row is the only thing that does. See
    /// <c>HlaMetadataDictionaryRecreationWatcher</c>.
    /// </remarks>
    internal class HlaMetadataRecreationRow : AtlasTableEntityBase
    {
        private const string PartitionValue = "Recreations";

        /// <summary>
        /// A value that differs from the last one, and carries no other meaning. A GUID rather than a timestamp so
        /// that two recreations cannot collide however close together they run, and so that nothing depends on the
        /// clocks of the machines that wrote them agreeing.
        /// </summary>
        public string Stamp { get; set; }

        public DateTimeOffset RecreatedAtUtc { get; set; }

        public HlaMetadataRecreationRow() { }

        public HlaMetadataRecreationRow(string hlaNomenclatureVersion)
        {
            PartitionKey = GetPartition();
            RowKey = hlaNomenclatureVersion;
            Stamp = Guid.NewGuid().ToString();
            RecreatedAtUtc = DateTimeOffset.UtcNow;
        }

        public static string GetPartition() => PartitionValue;
    }
}
