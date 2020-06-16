using System;
using System.Linq;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using EnumStringValues;
using LochNessBuilder;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh
{
    [Builder]
    internal static class DataRefreshRecordBuilder
    {
        public static Builder<DataRefreshRecord> New => Builder<DataRefreshRecord>.New;

        public static Builder<DataRefreshRecord> SuccessfullyCompleted(this Builder<DataRefreshRecord> builder)
        {
            return builder.With(r => r.WasSuccessful, true).With(r => r.RefreshEndUtc, DateTime.UtcNow);
        }

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

        public static Builder<DataRefreshRecord> WithStagesCompletedUpTo(
            this Builder<DataRefreshRecord> builder,
            DataRefreshStage firstIncompleteStage)
        {
            var allStages = EnumExtensions.EnumerateValues<DataRefreshStage>();
            return allStages.Where(refreshStage => refreshStage < firstIncompleteStage)
                .Aggregate(builder, (b, stage) => b.WithStageCompleted(stage));
        }
    }
}