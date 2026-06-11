using System;
using System.Linq;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using EnumStringValues;
using AutoFixture.Dsl;
using Atlas.Common.Test.SharedTestHelpers.Builders;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;

public static class DataRefreshRecordBuilder
{
    public static IPostprocessComposer<DataRefreshRecord> New =>
        FixtureBuilder.For<DataRefreshRecord>()
            .With(r => r.WasSuccessful, false)
            .With(r => r.RefreshRequestedUtc, DateTime.UtcNow.AddSeconds(-1))
            .WithDatabase(TransientDatabase.DatabaseA);

    public static IPostprocessComposer<DataRefreshRecord> WithDatabase(this IPostprocessComposer<DataRefreshRecord> builder, TransientDatabase db)
    {
        return builder.With(r => r.Database, db.GetStringValue());
    }

    public static IPostprocessComposer<DataRefreshRecord> SuccessfullyCompleted(this IPostprocessComposer<DataRefreshRecord> builder)
    {
        return builder.With(r => r.WasSuccessful, true).With(r => r.RefreshEndUtc, DateTime.UtcNow);
    }

    public static IPostprocessComposer<DataRefreshRecord> WithStageCompleted(this IPostprocessComposer<DataRefreshRecord> builder, DataRefreshStage stage)
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

    public static IPostprocessComposer<DataRefreshRecord> WithStagesCompleted(
        this IPostprocessComposer<DataRefreshRecord> builder,
        params DataRefreshStage[] completedStages)
    {
        var editedBuilder = builder;
        foreach (var completedStage in completedStages)
        {
            editedBuilder = editedBuilder.WithStageCompleted(completedStage);
        }

        return editedBuilder;
    }

    public static IPostprocessComposer<DataRefreshRecord> WithStagesCompletedUpToButNotIncluding(
        this IPostprocessComposer<DataRefreshRecord> builder,
        DataRefreshStage firstIncompleteStage)
    {
        var completedStages =
            EnumExtensions.EnumerateValues<DataRefreshStage>()
                .Where(refreshStage => refreshStage < firstIncompleteStage)
                .ToArray();

        return builder.WithStagesCompleted(completedStages);
    }

    public static IPostprocessComposer<DataRefreshRecord> WithStagesCompletedUpToAndIncluding(
        this IPostprocessComposer<DataRefreshRecord> builder,
        DataRefreshStage firstIncompleteStage)
    {
        var completedStages =
            EnumExtensions.EnumerateValues<DataRefreshStage>()
                .Where(refreshStage => refreshStage <= firstIncompleteStage)
                .ToArray();

        return builder.WithStagesCompleted(completedStages);
    }
}