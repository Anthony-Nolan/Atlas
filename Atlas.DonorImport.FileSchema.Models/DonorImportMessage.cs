using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.DonorImport.FileSchema.Models
{
    /// <summary>
    /// Result notification message for a donor import.
    /// </summary>
    public class DonorImportMessage
    {
        /// <summary>
        /// Name of donor import file.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Was the file import successful.
        /// </summary>
        public bool WasSuccessful { get; set; }

        /// <summary>
        /// Information about a successful import, where <see cref="WasSuccessful"/> is `true`.
        /// </summary>
        public SuccessfulImportInfo? SuccessfulImportInfo { get; set; }

        /// <summary>
        /// Information about a failed import, where <see cref="WasSuccessful"/> is `false`.
        /// </summary>
        public FailedImportInfo? FailedImportInfo { get; set; }

        #region Obsolete message properties for backwards compatibility

        /// <summary>
        /// <inheritdoc cref="Models.SuccessfulImportInfo.ImportedDonorCount"/>
        /// </summary>
        [Obsolete($"Use {nameof(Models.SuccessfulImportInfo.ImportedDonorCount)} instead.")]
        public int? ImportedDonorCount { get; set; }

        /// <summary>
        /// <inheritdoc cref="Models.SuccessfulImportInfo.FailedDonorCount"/>
        /// </summary>
        [Obsolete($"Use {nameof(Models.SuccessfulImportInfo.FailedDonorCount)} instead.")]
        public int? FailedDonorCount { get; set; }

        /// <summary>
        /// <inheritdoc cref="Models.SuccessfulImportInfo.FailedDonorSummary"/>
        /// </summary>
        [Obsolete($"Use {nameof(Models.SuccessfulImportInfo.FailedDonorSummary)} instead.")]
        public IReadOnlyCollection<FailureSummary>? FailedDonorSummary { get; set; }

        /// <summary>
        /// <inheritdoc cref="Models.FailedImportInfo.FileFailureReason"/>
        /// </summary>
        [Obsolete($"Use {nameof(Models.FailedImportInfo.FileFailureReason)} instead.")]
        public ImportFailureReason? FailureReason { get; set; }

        /// <summary>
        /// <inheritdoc cref="Models.FailedImportInfo.FileFailureDescription"/>
        /// </summary>
        [Obsolete($"Use {nameof(Models.FailedImportInfo.FileFailureDescription)} instead.")]
        public string? FailureReasonDescription { get; set; }

        #endregion

    }

    /// <summary>
    /// Provides info about a successful donor import.
    /// </summary>
    public class SuccessfulImportInfo
    {
        /// <summary>
        /// Number of successfully applied donor updates.
        /// </summary>
        public int ImportedDonorCount { get; set; }

        /// <summary>
        /// Number of donors updates that failed to be applied.
        /// </summary>
        public int FailedDonorCount { get; set; }

        /// <summary>
        /// Summary of donor update failures.
        /// </summary>
        public IReadOnlyCollection<FailureSummary> FailedDonorSummary { get; set; }
    }

    /// <summary>
    /// Provides info about a failed donor import.
    /// </summary>
    public class FailedImportInfo
    {
        /// <summary>
        /// Reason for file import failure.
        /// </summary>
        public ImportFailureReason FileFailureReason { get; set; }

        /// <summary>
        /// Description of file import failure.
        /// </summary>
        public string FileFailureDescription { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ImportFailureReason
    {
        ErrorDuringImport,
        RequestDeadlettered
    }

    public class FailureSummary
    {
        public string Reason { get; set; }
        public int Count { get; set; }
    }
}
