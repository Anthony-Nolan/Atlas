using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Atlas.MatchingAlgorithm.Data.Persistent.Models
{
    [Table("DataRefreshHistory")]
    // SQL Column order for this entity is determined by the manual arrangement of steps in the initial migration.
    // These columns can not be re-ordered once this migration has run - see https://github.com/dotnet/efcore/issues/10059
    public class DataRefreshRecord
    {
        public int Id { get; set; }
        public DateTime RefreshBeginUtc { get; set; }
        public DateTime? RefreshEndUtc { get; set; }

        /// <summary>
        /// The string representation of a "TransientDatabase" enum value. 
        /// </summary>
        public string Database { get; set; }

        public string HlaNomenclatureVersion { get; set; }
        public bool? WasSuccessful { get; set; }

        private DateTime? DataDeletionCompleted { get; set; }
        private DateTime? DatabaseScalingSetupCompleted { get; set; }
        private DateTime? MetadataDictionaryRefreshCompleted { get; set; }
        private DateTime? DonorImportCompleted { get; set; }
        private DateTime? DonorHlaProcessingCompleted { get; set; }
        private DateTime? DatabaseScalingTearDownCompleted { get; set; }
        private DateTime? QueuedDonorUpdatesCompleted { get; set; }

        internal DateTime? GetStageCompletionTime(DataRefreshStage stage) =>
            stage switch
            {
                DataRefreshStage.MetadataDictionaryRefresh => MetadataDictionaryRefreshCompleted,
                DataRefreshStage.DataDeletion => DataDeletionCompleted,
                DataRefreshStage.DatabaseScalingSetup => DatabaseScalingSetupCompleted,
                DataRefreshStage.DonorImport => DonorImportCompleted,
                DataRefreshStage.DonorHlaProcessing => DonorHlaProcessingCompleted,
                DataRefreshStage.DatabaseScalingTearDown => DatabaseScalingTearDownCompleted,
                DataRefreshStage.QueuedDonorUpdateProcessing => QueuedDonorUpdatesCompleted,
                _ => throw new ArgumentOutOfRangeException(nameof(stage))
            };

        internal void SetStageCompletionTime(DataRefreshStage stage, DateTime value)
        {
            switch (stage)
            {
                case DataRefreshStage.MetadataDictionaryRefresh:
                    MetadataDictionaryRefreshCompleted = value;
                    break;
                case DataRefreshStage.DataDeletion:
                    DataDeletionCompleted = value;
                    break;
                case DataRefreshStage.DatabaseScalingSetup:
                    DatabaseScalingSetupCompleted = value;
                    break;
                case DataRefreshStage.DonorImport:
                    DonorImportCompleted = value;
                    break;
                case DataRefreshStage.DonorHlaProcessing:
                    DonorHlaProcessingCompleted = value;
                    break;
                case DataRefreshStage.DatabaseScalingTearDown:
                    DatabaseScalingTearDownCompleted = value;
                    break;
                case DataRefreshStage.QueuedDonorUpdateProcessing:
                    QueuedDonorUpdatesCompleted = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stage), stage, null);
            }
        }
    }
}