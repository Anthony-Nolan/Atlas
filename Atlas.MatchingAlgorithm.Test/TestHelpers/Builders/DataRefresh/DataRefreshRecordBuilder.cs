using System;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using LochNessBuilder;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh
{
    [Builder]
    internal static class DataRefreshRecordBuilder
    {
        public static Builder<DataRefreshRecord> New => Builder<DataRefreshRecord>.New;

        public static Builder<DataRefreshRecord> WithStageCompleted(this Builder<DataRefreshRecord> builder, DataRefreshStage stage)
        {
            return stage switch
            {
                DataRefreshStage.MetadataDictionaryRefresh => builder.With(r => r.MetadataDictionaryRefreshCompleted, DateTime.UtcNow),
                DataRefreshStage.DataDeletion => builder.With(r => r.DataDeletionCompleted, DateTime.UtcNow),
                DataRefreshStage.DatabaseScalingSetup => builder.With(r => r.DatabaseScalingSetupCompleted, DateTime.UtcNow),
                DataRefreshStage.DonorImport => builder.With(r => r.DonorImportCompleted, DateTime.UtcNow),
                DataRefreshStage.DonorHlaProcessing => builder.With(r => r.DonorHlaProcessingCompleted, DateTime.UtcNow),
                DataRefreshStage.DatabaseScalingTearDown => builder.With(r => r.DatabaseScalingTearDownCompleted, DateTime.UtcNow),
                DataRefreshStage.QueuedDonorUpdateProcessing => builder.With(r => r.QueuedDonorUpdatesCompleted, DateTime.UtcNow),
                _ => throw new ArgumentOutOfRangeException(nameof(stage))
            };
        }
    }
}