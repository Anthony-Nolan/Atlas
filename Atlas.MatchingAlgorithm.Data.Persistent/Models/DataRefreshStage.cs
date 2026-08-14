namespace Atlas.MatchingAlgorithm.Data.Persistent.Models
{
    /// <summary>
    /// The values of this enum are used to determine the order of a data refresh run.
    /// The numbers themselves are arbitrary and are not persisted - <see cref="DataRefreshRecord"/> stores one
    /// column per member, mapped by name - so renumbering only changes the order stages run in. Do that
    /// deliberately: DataRefreshRunner also uses value ranges to reason about which stages sit between the
    /// two scaling stages.
    /// </summary>
    public enum DataRefreshStage
    {
        /// <summary>
        /// Recreation of HLA Metadata Dictionary
        /// </summary>
        MetadataDictionaryRefresh = 0,
        /// <summary>
        /// Deletion of Indexes on (previously) existing donor data.
        /// Done prior to DB scaling for efficiency.
        /// </summary>
        IndexRemoval = 10,
        /// <summary>
        /// Deletion of all existing donor data.
        /// This includes truncating the donor management log table, which <see cref="DonorImport"/> relies upon - it writes log entries
        /// create-only, so it must never run against a log table still holding entries for the donors being imported.
        /// </summary>
        DataDeletion = 20,
        /// <summary>
        /// Scaling of database to appropriate size for data refresh
        /// </summary>
        DatabaseScalingSetup = 30,
        /// <summary>
        /// Import all donors from the master donor store. No pre-processing to p-groups
        /// </summary>
        DonorImport = 40,
        /// <summary>
        /// Processing imported donors to p-groups.
        /// </summary>
        DonorHlaProcessing = 50,
        /// <summary>
        /// Recreation of Indexes on imported donors.
        /// </summary>
        IndexRecreation = 60,
        /// <summary>
        /// Consumption of all donor updates that have accrued during the data refresh.
        /// Runs BEFORE <see cref="DatabaseScalingTearDown"/>: it is a write-and-read workload against the
        /// refreshed database, and draining it after the scale-down put it on the smallest tier with a cold
        /// buffer pool, where it timed out and failed a run that had otherwise completed.
        /// </summary>
        QueuedDonorUpdateProcessing = 65,
        /// <summary>
        /// Scaling of database to appropriate size for live usage.
        /// </summary>
        DatabaseScalingTearDown = 70,
    }
}