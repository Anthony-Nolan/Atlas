using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atlas.MatchingAlgorithm.Data.Persistent.Context;

// ReSharper disable MemberCanBePrivate.Global - Properties need to be visible to EF 

namespace Atlas.MatchingAlgorithm.Data.Persistent.Models
{
    [Table(TableName)]
    // SQL Column order for this entity is determined by the manual arrangement of steps in the initial migration.
    // These columns can not be re-ordered once this migration has run - see https://github.com/dotnet/efcore/issues/10059
    public class DataRefreshRecord
    {
        internal const string TableName = "DataRefreshHistory";
        internal static readonly string QualifiedTableName = $"{SearchAlgorithmPersistentContext.Schema}.{TableName}";
        
        public int Id { get; set; }
        public DateTime RefreshBeginUtc { get; set; }
        public DateTime? RefreshEndUtc { get; set; }
        public DateTime? RefreshContinueUtc { get; set; }

        /// <summary>
        /// The string representation of a "TransientDatabase" enum value. 
        /// </summary>
        [Required]
        public string Database { get; set; }

        public string HlaNomenclatureVersion { get; set; }
        public bool? WasSuccessful { get; set; }

        public DateTime? DataDeletionCompleted { get; set; }
        public DateTime? IndexDeletionCompleted { get; set; }
        public DateTime? DatabaseScalingSetupCompleted { get; set; }
        public DateTime? MetadataDictionaryRefreshCompleted { get; set; }
        public DateTime? DonorImportCompleted { get; set; }

        /// <summary>
        /// Few batches behind to make sure when the process is continued from this donor it has processed fully. 
        /// </summary>
        public int? LastSafelyProcessedDonor { get; set; }
        public DateTime? DonorHlaProcessingCompleted { get; set; }
        public DateTime? IndexRecreationCompleted { get; set; }
        public DateTime? DatabaseScalingTearDownCompleted { get; set; }
        public DateTime? QueuedDonorUpdatesCompleted { get; set; }

        // ReSharper disable once UnusedMember.Global This is for manual Support Team use only. Neither written nor read anywhere in the code.
        public string SupportComments { get; set; }

        public bool IsStageComplete(DataRefreshStage stage) => GetStageCompletionTime(stage) != null;

        internal DateTime? GetStageCompletionTime(DataRefreshStage stage) =>
            stage switch
            {
                DataRefreshStage.MetadataDictionaryRefresh => MetadataDictionaryRefreshCompleted,
                DataRefreshStage.IndexRemoval => IndexDeletionCompleted,
                DataRefreshStage.DataDeletion => DataDeletionCompleted,
                DataRefreshStage.DatabaseScalingSetup => DatabaseScalingSetupCompleted,
                DataRefreshStage.DonorImport => DonorImportCompleted,
                DataRefreshStage.DonorHlaProcessing => DonorHlaProcessingCompleted,
                DataRefreshStage.DatabaseScalingTearDown => DatabaseScalingTearDownCompleted,
                DataRefreshStage.IndexRecreation => IndexRecreationCompleted,
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
                case DataRefreshStage.IndexRemoval:
                    IndexDeletionCompleted = value;
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
                case DataRefreshStage.IndexRecreation:
                    IndexRecreationCompleted = value;
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