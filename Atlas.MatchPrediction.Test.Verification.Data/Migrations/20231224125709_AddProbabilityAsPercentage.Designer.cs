﻿// <auto-generated />
using System;
using Atlas.MatchPrediction.Test.Verification.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Atlas.MatchPrediction.Test.Verification.Data.Migrations
{
    [DbContext(typeof(MatchPredictionVerificationContext))]
    [Migration("20231224125709_AddProbabilityAsPercentage")]
    partial class AddProbabilityAsPercentage
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("Atlas.ManualTesting.Common.Models.Entities.LocusMatchDetails", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<bool?>("IsAntigenMatch_1")
                        .HasColumnType("bit");

                    b.Property<bool?>("IsAntigenMatch_2")
                        .HasColumnType("bit");

                    b.Property<string>("Locus")
                        .IsRequired()
                        .HasColumnType("nvarchar(10)");

                    b.Property<string>("MatchConfidence_1")
                        .HasColumnType("nvarchar(32)");

                    b.Property<string>("MatchConfidence_2")
                        .HasColumnType("nvarchar(32)");

                    b.Property<int?>("MatchCount")
                        .HasColumnType("int");

                    b.Property<string>("MatchGrade_1")
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("MatchGrade_2")
                        .HasColumnType("nvarchar(128)");

                    b.Property<int>("MatchedDonor_Id")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("MatchedDonor_Id", "Locus", "MatchCount");

                    b.ToTable("LocusMatchDetails");
                });

            modelBuilder.Entity("Atlas.ManualTesting.Common.Models.Entities.MatchedDonor", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("DonorCode")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<int?>("DonorHfSetPopulationId")
                        .HasColumnType("int");

                    b.Property<string>("MatchPredictionResult")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MatchingResult")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("PatientHfSetPopulationId")
                        .HasColumnType("int");

                    b.Property<int>("SearchRequestRecord_Id")
                        .HasColumnType("int");

                    b.Property<int>("TotalMatchCount")
                        .HasColumnType("int");

                    b.Property<int>("TypedLociCount")
                        .HasColumnType("int");

                    b.Property<bool?>("WasDonorRepresented")
                        .HasColumnType("bit");

                    b.Property<bool?>("WasPatientRepresented")
                        .HasColumnType("bit");

                    b.HasKey("Id");

                    b.HasIndex("SearchRequestRecord_Id", "DonorCode", "TotalMatchCount");

                    b.ToTable("MatchedDonors");
                });

            modelBuilder.Entity("Atlas.ManualTesting.Common.Models.Entities.MatchedDonorProbability", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("Locus")
                        .HasColumnType("nvarchar(10)");

                    b.Property<int>("MatchedDonor_Id")
                        .HasColumnType("int");

                    b.Property<int>("MismatchCount")
                        .HasColumnType("int");

                    b.Property<decimal?>("Probability")
                        .HasColumnType("decimal(6,5)");

                    b.Property<int?>("ProbabilityAsPercentage")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("MatchedDonor_Id", "Locus", "MismatchCount");

                    b.ToTable("MatchProbabilities");
                });

            modelBuilder.Entity("Atlas.ManualTesting.Common.Models.Entities.TestDonorExportRecord", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<DateTimeOffset?>("DataRefreshCompleted")
                        .HasColumnType("datetimeoffset");

                    b.Property<int?>("DataRefreshRecordId")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset?>("Exported")
                        .HasColumnType("datetimeoffset");

                    b.Property<DateTimeOffset>("Started")
                        .HasColumnType("datetimeoffset");

                    b.Property<bool?>("WasDataRefreshSuccessful")
                        .HasColumnType("bit");

                    b.HasKey("Id");

                    b.ToTable("TestDonorExportRecords");
                });

            modelBuilder.Entity("Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.ExpandedMac", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasColumnType("nvarchar(10)");

                    b.Property<string>("SecondField")
                        .IsRequired()
                        .HasColumnType("nvarchar(10)");

                    b.HasKey("Id");

                    b.HasIndex("Code", "SecondField")
                        .IsUnique();

                    b.ToTable("ExpandedMacs");
                });

            modelBuilder.Entity("Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness.MaskingRecord", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("Locus")
                        .IsRequired()
                        .HasColumnType("nvarchar(10)");

                    b.Property<string>("MaskingRequests")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("TestHarness_Id")
                        .HasColumnType("int");

                    b.Property<string>("TestIndividualCategory")
                        .IsRequired()
                        .HasColumnType("nvarchar(10)");

                    b.HasKey("Id");

                    b.HasIndex("TestHarness_Id");

                    b.ToTable("MaskingRecords");
                });

            modelBuilder.Entity("Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness.NormalisedHaplotypeFrequency", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("A")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<string>("B")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<string>("C")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<int>("CopyNumber")
                        .HasColumnType("int");

                    b.Property<string>("DQB1")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<string>("DRB1")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<decimal>("Frequency")
                        .HasColumnType("decimal(20,20)");

                    b.Property<int>("NormalisedPool_Id")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("NormalisedPool_Id");

                    b.ToTable("NormalisedHaplotypeFrequencies");
                });

            modelBuilder.Entity("Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness.NormalisedPool", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("Comments")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset>("CreatedDateTime")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetimeoffset")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<string>("HaplotypeFrequenciesDataSource")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<int>("HaplotypeFrequencySetId")
                        .HasColumnType("int");

                    b.Property<string>("TypingCategory")
                        .IsRequired()
                        .HasColumnType("nvarchar(20)");

                    b.HasKey("Id");

                    b.ToTable("NormalisedPool");
                });

            modelBuilder.Entity("Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness.Simulant", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("A_1")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<string>("A_2")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<string>("B_1")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<string>("B_2")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<string>("C_1")
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<string>("C_2")
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<string>("DQB1_1")
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<string>("DQB1_2")
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<string>("DRB1_1")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<string>("DRB1_2")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<string>("SimulatedHlaTypingCategory")
                        .IsRequired()
                        .HasColumnType("nvarchar(10)");

                    b.Property<int?>("SourceSimulantId")
                        .HasColumnType("int");

                    b.Property<int>("TestHarness_Id")
                        .HasColumnType("int");

                    b.Property<string>("TestIndividualCategory")
                        .IsRequired()
                        .HasColumnType("nvarchar(10)");

                    b.HasKey("Id");

                    b.HasIndex("TestHarness_Id", "TestIndividualCategory", "SimulatedHlaTypingCategory");

                    b.ToTable("Simulants");
                });

            modelBuilder.Entity("Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness.TestHarness", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("Comments")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset>("CreatedDateTime")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetimeoffset")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<int?>("ExportRecord_Id")
                        .HasColumnType("int");

                    b.Property<int>("NormalisedPool_Id")
                        .HasColumnType("int");

                    b.Property<bool>("WasCompleted")
                        .HasColumnType("bit");

                    b.HasKey("Id");

                    b.HasIndex("ExportRecord_Id")
                        .IsUnique()
                        .HasFilter("[ExportRecord_Id] IS NOT NULL");

                    b.HasIndex("NormalisedPool_Id");

                    b.ToTable("TestHarnesses");
                });

            modelBuilder.Entity("Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification.VerificationRun", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("Comments")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset>("CreatedDateTime")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetimeoffset")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<int>("SearchLociCount")
                        .HasColumnType("int");

                    b.Property<bool>("SearchRequestsSubmitted")
                        .HasColumnType("bit");

                    b.Property<int>("TestHarness_Id")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("TestHarness_Id");

                    b.ToTable("VerificationRuns");
                });

            modelBuilder.Entity("Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification.VerificationSearchRequestRecord", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("AtlasSearchIdentifier")
                        .IsRequired()
                        .HasColumnType("nvarchar(50)");

                    b.Property<int>("DonorMismatchCount")
                        .HasColumnType("int");

                    b.Property<int?>("MatchedDonorCount")
                        .HasColumnType("int");

                    b.Property<int>("PatientId")
                        .HasColumnType("int");

                    b.Property<bool>("SearchResultsRetrieved")
                        .HasColumnType("bit");

                    b.Property<int>("VerificationRun_Id")
                        .HasColumnType("int");

                    b.Property<bool>("WasMatchPredictionRun")
                        .HasColumnType("bit");

                    b.Property<bool?>("WasSuccessful")
                        .HasColumnType("bit");

                    b.HasKey("Id");

                    b.HasIndex("AtlasSearchIdentifier");

                    b.HasIndex("PatientId");

                    b.HasIndex("VerificationRun_Id", "PatientId", "SearchResultsRetrieved");

                    b.ToTable("SearchRequests");
                });

            modelBuilder.Entity("Atlas.ManualTesting.Common.Models.Entities.LocusMatchDetails", b =>
                {
                    b.HasOne("Atlas.ManualTesting.Common.Models.Entities.MatchedDonor", null)
                        .WithMany()
                        .HasForeignKey("MatchedDonor_Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Atlas.ManualTesting.Common.Models.Entities.MatchedDonor", b =>
                {
                    b.HasOne("Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification.VerificationSearchRequestRecord", null)
                        .WithMany()
                        .HasForeignKey("SearchRequestRecord_Id")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();
                });

            modelBuilder.Entity("Atlas.ManualTesting.Common.Models.Entities.MatchedDonorProbability", b =>
                {
                    b.HasOne("Atlas.ManualTesting.Common.Models.Entities.MatchedDonor", null)
                        .WithMany()
                        .HasForeignKey("MatchedDonor_Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness.MaskingRecord", b =>
                {
                    b.HasOne("Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness.TestHarness", null)
                        .WithMany()
                        .HasForeignKey("TestHarness_Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness.NormalisedHaplotypeFrequency", b =>
                {
                    b.HasOne("Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness.NormalisedPool", null)
                        .WithMany()
                        .HasForeignKey("NormalisedPool_Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness.Simulant", b =>
                {
                    b.HasOne("Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness.TestHarness", null)
                        .WithMany()
                        .HasForeignKey("TestHarness_Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness.TestHarness", b =>
                {
                    b.HasOne("Atlas.ManualTesting.Common.Models.Entities.TestDonorExportRecord", null)
                        .WithOne()
                        .HasForeignKey("Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness.TestHarness", "ExportRecord_Id");

                    b.HasOne("Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness.NormalisedPool", null)
                        .WithMany()
                        .HasForeignKey("NormalisedPool_Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification.VerificationRun", b =>
                {
                    b.HasOne("Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness.TestHarness", null)
                        .WithMany()
                        .HasForeignKey("TestHarness_Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification.VerificationSearchRequestRecord", b =>
                {
                    b.HasOne("Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness.Simulant", null)
                        .WithMany()
                        .HasForeignKey("PatientId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification.VerificationRun", null)
                        .WithMany()
                        .HasForeignKey("VerificationRun_Id")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}