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
    [Migration("20231102141557_ChangeColumnDataType")]
    partial class ChangeColumnDataType
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

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
#pragma warning restore 612, 618
        }
    }
}