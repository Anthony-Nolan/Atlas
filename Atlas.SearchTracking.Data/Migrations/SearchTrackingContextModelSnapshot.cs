﻿// <auto-generated />
using System;
using Atlas.SearchTracking.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Atlas.SearchTracking.Data.Migrations
{
    [DbContext(typeof(SearchTrackingContext))]
    partial class SearchTrackingContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("SearchTracking")
                .HasAnnotation("ProductVersion", "6.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("Atlas.SearchTracking.Data.Models.SearchRequest", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<bool>("AreBetterMatchesIncluded")
                        .HasColumnType("bit");

                    b.Property<string>("DonorRegistryCodes")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("DonorType")
                        .IsRequired()
                        .HasMaxLength(10)
                        .HasColumnType("nvarchar(10)");

                    b.Property<bool>("IsMatchPredictionRun")
                        .HasColumnType("bit");

                    b.Property<bool>("IsRepeatSearch")
                        .HasColumnType("bit");

                    b.Property<int?>("MatchPrediction_DonorsPerBatch")
                        .HasColumnType("int");

                    b.Property<string>("MatchPrediction_FailureInfo_ExceptionStacktrace")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MatchPrediction_FailureInfo_Message")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MatchPrediction_FailureInfo_Type")
                        .HasColumnType("nvarchar(50)");

                    b.Property<bool?>("MatchPrediction_IsSuccessful")
                        .HasColumnType("bit");

                    b.Property<int?>("MatchPrediction_TotalNumberOfBatches")
                        .HasColumnType("int");

                    b.Property<string>("MatchingAlgorithm_FailureInfo_ExceptionStacktrace")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MatchingAlgorithm_FailureInfo_Message")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MatchingAlgorithm_FailureInfo_Type")
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("MatchingAlgorithm_HlaNomenclatureVersion")
                        .HasMaxLength(10)
                        .HasColumnType("nvarchar(10)");

                    b.Property<bool?>("MatchingAlgorithm_IsSuccessful")
                        .HasColumnType("bit");

                    b.Property<int?>("MatchingAlgorithm_NumberOfMatching")
                        .HasColumnType("int");

                    b.Property<int?>("MatchingAlgorithm_NumberOfResults")
                        .HasColumnType("int");

                    b.Property<int?>("MatchingAlgorithm_RepeatSearch_AddedResultCount")
                        .HasColumnType("int");

                    b.Property<int?>("MatchingAlgorithm_RepeatSearch_RemovedResultCount")
                        .HasColumnType("int");

                    b.Property<int?>("MatchingAlgorithm_RepeatSearch_UpdatedResultCount")
                        .HasColumnType("int");

                    b.Property<bool?>("MatchingAlgorithm_ResultsSent")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("MatchingAlgorithm_ResultsSentTimeUtc")
                        .HasColumnType("datetime2");

                    b.Property<byte?>("MatchingAlgorithm_TotalAttemptsNumber")
                        .HasColumnType("tinyint");

                    b.Property<Guid?>("OriginalSearchRequestId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("RepeatSearchCutOffDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("RequestJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("RequestTimeUtc")
                        .HasColumnType("datetime2");

                    b.Property<bool?>("ResultsSent")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("ResultsSentTimeUtc")
                        .HasColumnType("datetime2");

                    b.Property<string>("SearchCriteria")
                        .IsRequired()
                        .HasMaxLength(10)
                        .HasColumnType("nvarchar(10)");

                    b.Property<Guid>("SearchRequestId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("SearchRequestId")
                        .IsUnique()
                        .HasDatabaseName("IX_SearchRequestId");

                    b.ToTable("SearchRequests", "SearchTracking");
                });

            modelBuilder.Entity("Atlas.SearchTracking.Data.Models.SearchRequestMatchingAlgorithmAttempts", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<DateTime?>("AlgorithmCore_Matching_EndTimeUtc")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("AlgorithmCore_Matching_StartTimeUtc")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("AlgorithmCore_Scoring_EndTimeUtc")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("AlgorithmCore_Scoring_StartTimeUtc")
                        .HasColumnType("datetime2");

                    b.Property<byte>("AttemptNumber")
                        .HasColumnType("tinyint");

                    b.Property<DateTime?>("CompletionTimeUtc")
                        .HasColumnType("datetime2");

                    b.Property<string>("FailureInfo_ExceptionStacktrace")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FailureInfo_Message")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FailureInfo_Type")
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("InitiationTimeUtc")
                        .HasColumnType("datetime2");

                    b.Property<bool?>("IsSuccessful")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("PersistingResults_EndTimeUtc")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("PersistingResults_StartTimeUtc")
                        .HasColumnType("datetime2");

                    b.Property<int>("SearchRequestId")
                        .HasColumnType("int");

                    b.Property<DateTime>("StartTimeUtc")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("SearchRequestId", "AttemptNumber")
                        .HasDatabaseName("IX_SearchRequestId_And_AttemptNumber");

                    b.ToTable("SearchRequestMatchingAlgorithmAttempts", "SearchTracking");
                });

            modelBuilder.Entity("Atlas.SearchTracking.Data.Models.SearchRequestMatchPredictionTiming", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<DateTime?>("AlgorithmCore_RunningBatches_EndTimeUtc")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("AlgorithmCore_RunningBatches_StartTimeUtc")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("CompletionTimeUtc")
                        .HasColumnType("datetime2");

                    b.Property<string>("FailureInfo_ExceptionStacktrace")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FailureInfo_Message")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FailureInfo_Type")
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("InitiationTimeUtc")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("PersistingResults_EndTimeUtc")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("PersistingResults_StartTimeUtc")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("PrepareBatches_EndTimeUtc")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("PrepareBatches_StartTimeUtc")
                        .HasColumnType("datetime2");

                    b.Property<int>("SearchRequestId")
                        .HasColumnType("int");

                    b.Property<DateTime>("StartTimeUtc")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("SearchRequestId")
                        .IsUnique();

                    b.ToTable("SearchRequestMatchPredictionTimings", "SearchTracking");
                });

            modelBuilder.Entity("Atlas.SearchTracking.Data.Models.SearchRequestMatchingAlgorithmAttempts", b =>
                {
                    b.HasOne("Atlas.SearchTracking.Data.Models.SearchRequest", "SearchRequest")
                        .WithMany("SearchRequestMatchingAlgorithmAttempts")
                        .HasForeignKey("SearchRequestId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("SearchRequest");
                });

            modelBuilder.Entity("Atlas.SearchTracking.Data.Models.SearchRequestMatchPredictionTiming", b =>
                {
                    b.HasOne("Atlas.SearchTracking.Data.Models.SearchRequest", "SearchRequest")
                        .WithOne("SearchRequestMatchPredictionTiming")
                        .HasForeignKey("Atlas.SearchTracking.Data.Models.SearchRequestMatchPredictionTiming", "SearchRequestId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("SearchRequest");
                });

            modelBuilder.Entity("Atlas.SearchTracking.Data.Models.SearchRequest", b =>
                {
                    b.Navigation("SearchRequestMatchPredictionTiming");

                    b.Navigation("SearchRequestMatchingAlgorithmAttempts");
                });
#pragma warning restore 612, 618
        }
    }
}