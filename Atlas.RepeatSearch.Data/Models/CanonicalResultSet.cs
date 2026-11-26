using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atlas.RepeatSearch.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.RepeatSearch.Data.Models
{
    /// <summary>
    /// Represents the latest "canonical" result set for a given search request Id.
    /// Each repeat search run returns the diff between the previous canonical set and the latest one.
    /// The initial canonical result set is the original search result set. 
    /// </summary>
    [Table(TableName)]
    public class CanonicalResultSet
    {
        private const string TableName = "CanonicalResultSets";
        internal static readonly string QualifiedTableName = $"{RepeatSearchContext.Schema}.{TableName}";
        
        public int Id { get; set; }
        
        [MaxLength(200)]
        [Required]
        public string OriginalSearchRequestId { get; set; }

        /// <summary>
        /// A set is <see cref="CanonicalResultSetStatus.InProgress"/> while the results of the original search are stored in chunks,
        /// and <see cref="CanonicalResultSetStatus.Complete"/> when all of them are stored. Repeat searches only use complete sets.
        /// Sets stored before this column existed are complete.
        /// </summary>
        public CanonicalResultSetStatus Status { get; set; }

        /// <summary>
        /// Identifies the delivery that is storing the set. A delivery of the same results message that finds the set in progress
        /// takes it over with a new id; only the delivery with the current id can start a chunk and complete the set.
        /// The claim check and the insert of a chunk are two statements, so a chunk of a superseded delivery that passed the check just
        /// before the take-over can still commit. The unique index on (CanonicalResultSetId, ExternalDonorCode) of
        /// <see cref="SearchResult"/> makes one of the two inserts of the same code fail, and a failed current delivery starts again.
        /// Do not remove that index.
        /// </summary>
        public Guid? StoreAttemptId { get; set; }

        /// <summary>
        /// When the set was last used: when its results were stored, or when a repeat search of it last started.
        /// A set that is not used for the retention period is deleted (see <see cref="CanonicalResultSetStatus.Deleting"/>).
        /// A set that is <see cref="CanonicalResultSetStatus.Deleting"/> keeps this value, because repeat searches do not use it.
        /// </summary>
        public DateTimeOffset LastUsedAt { get; set; }

        /// <summary>
        /// Donors in the canonical result set.
        /// </summary>
        public ICollection<SearchResult> SearchResults { get; set; }
    }

    internal static class CanonicalResultSetBuilder
    {
        public static void SetUpResultSetModel(this EntityTypeBuilder<CanonicalResultSet> resultSet)
        {
            resultSet.HasIndex(s => s.OriginalSearchRequestId).IsUnique();

            // A row inserted without a value counts as used now, so a missing value can never make a set expire.
            resultSet.Property(s => s.LastUsedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

            // The retention job takes the oldest set first.
            resultSet.HasIndex(s => s.LastUsedAt);
        }
    }
}