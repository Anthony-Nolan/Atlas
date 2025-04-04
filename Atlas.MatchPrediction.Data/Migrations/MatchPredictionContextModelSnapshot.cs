﻿// <auto-generated />
using System;
using Atlas.MatchPrediction.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Atlas.MatchPrediction.Data.Migrations
{
    [DbContext(typeof(MatchPredictionContext))]
    partial class MatchPredictionContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("MatchPrediction")
                .HasAnnotation("ProductVersion", "6.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("Atlas.MatchPrediction.Data.Models.HaplotypeFrequency", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"), 1L, 1);

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

                    b.Property<int>("SetId")
                        .HasColumnType("int")
                        .HasColumnName("Set_Id");

                    b.Property<int>("TypingCategory")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("SetId");

                    SqlServerIndexBuilderExtensions.IncludeProperties(b.HasIndex("SetId"), new[] { "Id", "A", "B", "C", "DQB1", "DRB1", "Frequency", "TypingCategory" });

                    b.HasIndex("A", "B", "C", "DQB1", "DRB1", "SetId")
                        .IsUnique();

                    SqlServerIndexBuilderExtensions.IncludeProperties(b.HasIndex("A", "B", "C", "DQB1", "DRB1", "SetId"), new[] { "Frequency", "TypingCategory" });

                    b.ToTable("HaplotypeFrequencies", "MatchPrediction");
                });

            modelBuilder.Entity("Atlas.MatchPrediction.Data.Models.HaplotypeFrequencySet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<bool>("Active")
                        .HasColumnType("bit");

                    b.Property<DateTimeOffset>("DateTimeAdded")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("EthnicityCode")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("HlaNomenclatureVersion")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("PopulationId")
                        .HasColumnType("int");

                    b.Property<string>("RegistryCode")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.HasKey("Id");

                    b.HasIndex("Active");

                    b.HasIndex("EthnicityCode", "RegistryCode")
                        .IsUnique()
                        .HasDatabaseName("IX_RegistryCode_And_EthnicityCode")
                        .HasFilter("[Active] = 'True'");

                    b.ToTable("HaplotypeFrequencySets", "MatchPrediction");
                });

            modelBuilder.Entity("Atlas.MatchPrediction.Data.Models.HaplotypeFrequency", b =>
                {
                    b.HasOne("Atlas.MatchPrediction.Data.Models.HaplotypeFrequencySet", "Set")
                        .WithMany()
                        .HasForeignKey("SetId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Set");
                });
#pragma warning restore 612, 618
        }
    }
}
