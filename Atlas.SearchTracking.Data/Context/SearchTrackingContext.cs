﻿using System.Text.Json;
using Atlas.SearchTracking.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Atlas.SearchTracking.Data.Context
{
    public interface ISearchTrackingContext : IDisposable
    {
        public DbSet<SearchRequest> SearchRequests { get; }

        public DbSet<SearchRequestMatchingAlgorithmAttempts> SearchRequestMatchingAlgorithmAttempts { get; }

        public DbSet<SearchRequestMatchPrediction> SearchRequestMatchPredictions { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }

    public class SearchTrackingContext : DbContext, ISearchTrackingContext
    {
        internal const string Schema = "SearchTracking";

        // ReSharper disable once SuggestBaseTypeForParameter
        public SearchTrackingContext(DbContextOptions<SearchTrackingContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(Schema);

            modelBuilder.Entity<SearchRequestMatchingAlgorithmAttempts>().SetUpModel();
            modelBuilder.Entity<SearchRequest>().SetUpModel();

            modelBuilder.Entity<SearchRequest>()
                .HasOne(x => x.MatchPrediction)
                .WithOne(x => x.SearchRequest)
                .HasForeignKey<SearchRequestMatchPrediction>(x => x.SearchRequestId);

            modelBuilder.Entity<SearchRequest>()
                .HasMany(x => x.MatchingAlgorithmAttempts)
                .WithOne(x => x.SearchRequest)
                .HasForeignKey(x => x.SearchRequestId);

            modelBuilder.Entity<SearchRequest>().Property(e => e.DonorRegistryCodes)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null));

            modelBuilder.Entity<SearchRequest>().Property(e => e.SearchIdentifier)
               .HasConversion(
                   new ValueConverter<Guid, string>(
                       v => v.ToString("D").ToLowerInvariant(),
                       v => Guid.Parse(v))
               )
               .HasMaxLength(36)
               .HasColumnType("nvarchar(36)");

            modelBuilder.Entity<SearchRequest>().Property(e => e.OriginalSearchIdentifier)
                .HasConversion(
                    new ValueConverter<Guid, string>(
                        v => v.ToString("D").ToLowerInvariant(),
                        v => Guid.Parse(v)) 
                )
                .HasMaxLength(36)
                .HasColumnType("nvarchar(36)");

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<SearchRequest> SearchRequests { get; set; }

        public DbSet<SearchRequestMatchingAlgorithmAttempts> SearchRequestMatchingAlgorithmAttempts { get; set; }

        public DbSet<SearchRequestMatchPrediction> SearchRequestMatchPredictions { get; set; }
    }
}
