using System;
using System.Linq;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using EnumStringValues;
using LochNessBuilder;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh
{
    [Builder]
    public static class DataRefreshRecordBuilder
    {
        public static Builder<DataRefreshRecord> New =>
            Builder<DataRefreshRecord>.New
                .With(r => r.WasSuccessful, false)
                .With(r => r.RefreshRequestedUtc, DateTime.UtcNow.AddSeconds(-1))
                .WithDatabase(TransientDatabase.DatabaseA);

        public static Builder<DataRefreshRecord> WithDatabase(this Builder<DataRefreshRecord> builder, TransientDatabase db)
        {
            return builder.With(r => r.Database, db.GetStringValue());
        }

        public static Builder<DataRefreshRecord> SuccessfullyCompleted(this Builder<DataRefreshRecord> builder)
        {
            return builder.With(r => r.WasSuccessful, true).With(r => r.RefreshEndUtc, DateTime.UtcNow);
        }

        public static Builder<DataRefreshRecord> WithStageCompleted(this Builder<DataRefreshRecord> builder, DataRefreshStage stage)
        {
            return stage switch
            {
                DataRefreshStage.MetadataDictionaryRefresh => builder.With(r => r.MetadataDictionaryRefreshCompleted, DateTime.UtcNow).With(r => r.HlaNomenclatureVersion, "version"),
                DataRefreshStage.IndexRemoval => builder.With(r => r.IndexDeletionCompleted, DateTime.UtcNow),
                DataRefreshStage.DataDeletion => builder.With(r => r.DataDeletionCompleted, DateTime.UtcNow),
                DataRefreshStage.DatabaseScalingSetup => builder.With(r => r.DatabaseScalingSetupCompleted, DateTime.UtcNow),
                DataRefreshStage.DonorImport => builder.With(r => r.DonorImportCompleted, DateTime.UtcNow),
                DataRefreshStage.DonorHlaProcessing => builder.With(r => r.DonorHlaProcessingCompleted, DateTime.UtcNow),
                DataRefreshStage.IndexRecreation => builder.With(r => r.IndexRecreationCompleted, DateTime.UtcNow),
                DataRefreshStage.DatabaseScalingTearDown => builder.With(r => r.DatabaseScalingTearDownCompleted, DateTime.UtcNow),
                DataRefreshStage.QueuedDonorUpdateProcessing => builder.With(r => r.QueuedDonorUpdatesCompleted, DateTime.UtcNow),
                _ => throw new ArgumentOutOfRangeException(nameof(stage))
            };
        }

        public static Builder<DataRefreshRecord> WithStagesCompleted(
            this Builder<DataRefreshRecord> builder,
            params DataRefreshStage[] completedStages)
        {
            var editedBuilder = builder;
            foreach (var completedStage in completedStages)
            {
                editedBuilder = editedBuilder.WithStageCompleted(completedStage);
            }

            return editedBuilder;
        }

        public static Builder<DataRefreshRecord> WithStagesCompletedUpToButNotIncluding(
            this Builder<DataRefreshRecord> builder,
            DataRefreshStage firstIncompleteStage)
        {
            var completedStages =
                EnumExtensions.EnumerateValues<DataRefreshStage>()
                    .Where(refreshStage => refreshStage < firstIncompleteStage)
                    .ToArray();

            return builder.WithStagesCompleted(completedStages);
        }

        public static Builder<DataRefreshRecord> WithStagesCompletedUpToAndIncluding(
            this Builder<DataRefreshRecord> builder,
            DataRefreshStage firstIncompleteStage)
        {
            var completedStages =
                EnumExtensions.EnumerateValues<DataRefreshStage>()
                    .Where(refreshStage => refreshStage <= firstIncompleteStage)
                    .ToArray();

            return builder.WithStagesCompleted(completedStages);
        }
    }
}