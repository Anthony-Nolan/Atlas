using System;

namespace Atlas.HlaMetadataDictionary.ExternalInterface.Models
{
    /// <summary>
    /// Published whenever the HLA Metadata Dictionary's Azure Table Storage data has been recreated, so that
    /// already-running consumers can drop the in-memory snapshot they took of it.
    /// </summary>
    /// <remarks>
    /// Sent for BOTH recreation routes - a data refresh moving to a new nomenclature version, and a forced recreation
    /// at the version that is already active. The latter is the one that needs it: nothing about the version string
    /// changes, so no consumer has any other way of telling that the underlying data now differs.
    /// </remarks>
    public class HlaMetadataDictionaryUpdatedMessage
    {
        /// <summary>The nomenclature version whose data was recreated.</summary>
        public string HlaNomenclatureVersion { get; set; }

        /// <summary>When the recreation completed. Diagnostic only - consumers do not order on this.</summary>
        public DateTimeOffset UpdatedAtUtc { get; set; }
    }
}
