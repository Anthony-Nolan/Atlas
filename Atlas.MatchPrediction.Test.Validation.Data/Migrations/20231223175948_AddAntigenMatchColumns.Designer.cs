﻿// <auto-generated />
using System;
using Atlas.MatchPrediction.Test.Validation.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Atlas.MatchPrediction.Test.Validation.Data.Migrations
{
    [DbContext(typeof(MatchPredictionValidationContext))]
    [Migration("20231223175948_AddAntigenMatchColumns")]
    partial class AddAntigenMatchColumns
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

                    b.Property<int>("DonorId")
                        .HasColumnType("int");

                    b.Property<string>("MatchPredictionResult")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MatchingResult")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

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

                    b.HasIndex("DonorId");

                    b.HasIndex("SearchRequestRecord_Id", "DonorId", "TotalMatchCount");

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

            modelBuilder.Entity("Atlas.MatchPrediction.Test.Validation.Data.Models.MatchPredictionRequest", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<int>("DonorId")
                        .HasColumnType("int");

                    b.Property<string>("MatchPredictionAlgorithmRequestId")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<int>("PatientId")
                        .HasColumnType("int");

                    b.Property<string>("RequestErrors")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("DonorId");

                    b.HasIndex("PatientId");

                    b.HasIndex("MatchPredictionAlgorithmRequestId", "DonorId", "PatientId");

                    b.ToTable("MatchPredictionRequests");
                });

            modelBuilder.Entity("Atlas.MatchPrediction.Test.Validation.Data.Models.MatchPredictionResults", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("Locus")
                        .HasColumnType("nvarchar(10)");

                    b.Property<int>("MatchPredictionRequestId")
                        .HasColumnType("int");

                    b.Property<int>("MismatchCount")
                        .HasColumnType("int");

                    b.Property<decimal?>("Probability")
                        .HasColumnType("decimal(6,5)");

                    b.HasKey("Id");

                    b.HasIndex("MatchPredictionRequestId", "Locus", "MismatchCount");

                    b.ToTable("MatchPredictionResults");
                });

            modelBuilder.Entity("Atlas.MatchPrediction.Test.Validation.Data.Models.SearchSet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<DateTimeOffset>("CreatedDateTime")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetimeoffset")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<string>("DonorType")
                        .IsRequired()
                        .HasColumnType("nvarchar(10)");

                    b.Property<string>("MatchLoci")
                        .IsRequired()
                        .HasColumnType("nvarchar(256)");

                    b.Property<int>("MismatchCount")
                        .HasColumnType("int");

                    b.Property<bool>("SearchRequestsSubmitted")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false);

                    b.Property<int>("TestDonorExportRecord_Id")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("TestDonorExportRecord_Id");

                    b.ToTable("SearchSets");
                });

            modelBuilder.Entity("Atlas.MatchPrediction.Test.Validation.Data.Models.SubjectInfo", b =>
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

                    b.Property<string>("DonorType")
                        .HasColumnType("nvarchar(10)");

                    b.Property<int?>("ExternalHfSetId")
                        .HasMaxLength(256)
                        .HasColumnType("int");

                    b.Property<string>("ExternalId")
                        .IsRequired()
                        .HasMaxLength(10)
                        .HasColumnType("nvarchar(10)");

                    b.Property<string>("SubjectType")
                        .IsRequired()
                        .HasColumnType("nvarchar(10)");

                    b.HasKey("Id");

                    b.HasIndex("ExternalId")
                        .IsUnique();

                    b.ToTable("SubjectInfo");
                });

            modelBuilder.Entity("Atlas.MatchPrediction.Test.Validation.Data.Models.ValidationSearchRequestRecord", b =>
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
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false);

                    b.Property<int>("SearchSet_Id")
                        .HasColumnType("int");

                    b.Property<bool?>("WasSuccessful")
                        .HasColumnType("bit");

                    b.HasKey("Id");

                    b.HasIndex("AtlasSearchIdentifier");

                    b.HasIndex("PatientId");

                    b.HasIndex("SearchSet_Id");

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
                    b.HasOne("Atlas.MatchPrediction.Test.Validation.Data.Models.SubjectInfo", null)
                        .WithMany()
                        .HasForeignKey("DonorId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.HasOne("Atlas.MatchPrediction.Test.Validation.Data.Models.ValidationSearchRequestRecord", null)
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

            modelBuilder.Entity("Atlas.MatchPrediction.Test.Validation.Data.Models.MatchPredictionRequest", b =>
                {
                    b.HasOne("Atlas.MatchPrediction.Test.Validation.Data.Models.SubjectInfo", null)
                        .WithMany()
                        .HasForeignKey("DonorId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.HasOne("Atlas.MatchPrediction.Test.Validation.Data.Models.SubjectInfo", null)
                        .WithMany()
                        .HasForeignKey("PatientId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();
                });

            modelBuilder.Entity("Atlas.MatchPrediction.Test.Validation.Data.Models.MatchPredictionResults", b =>
                {
                    b.HasOne("Atlas.MatchPrediction.Test.Validation.Data.Models.MatchPredictionRequest", null)
                        .WithMany()
                        .HasForeignKey("MatchPredictionRequestId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Atlas.MatchPrediction.Test.Validation.Data.Models.SearchSet", b =>
                {
                    b.HasOne("Atlas.ManualTesting.Common.Models.Entities.TestDonorExportRecord", null)
                        .WithMany()
                        .HasForeignKey("TestDonorExportRecord_Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Atlas.MatchPrediction.Test.Validation.Data.Models.ValidationSearchRequestRecord", b =>
                {
                    b.HasOne("Atlas.MatchPrediction.Test.Validation.Data.Models.SubjectInfo", null)
                        .WithMany()
                        .HasForeignKey("PatientId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Atlas.MatchPrediction.Test.Validation.Data.Models.SearchSet", null)
                        .WithMany()
                        .HasForeignKey("SearchSet_Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}