namespace Atlas.ManualTesting.Common.Models.Entities
{
    public class TestDonorExportRecord
    {
        public int Id { get; set; }

        /// <summary>
        /// Start datetime of export attempt.
        /// </summary>
        public DateTimeOffset Started { get; set; }

        /// <summary>
        /// Datetime test donors were exported to donor import store.
        /// </summary>
        public DateTimeOffset? Exported { get; set; }

        /// <summary>
        /// Datetime data refresh was marked as completed.
        /// </summary>
        public DateTimeOffset? DataRefreshCompleted { get; set; }

        public int? DataRefreshRecordId { get; set; }

        /// <summary>
        /// Was matching algorithm database successfully refreshed with test donors?
        /// </summary>
        public bool? WasDataRefreshSuccessful { get; set; }
    }
}
