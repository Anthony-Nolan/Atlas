using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atlas.RepeatSearch.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.RepeatSearch.Data.Models
{
    /// <summary>
    /// One record for each repeat search that changed the canonical result set. The record is written in the same transaction as
    /// the change of the set, so a repeat search that has a record must not change the set again.
    /// </summary>
    [Table(TableName)]
    public class RepeatSearchHistoryRecord
    {
        private const string TableName = "RepeatSearchHistoryRecords";
        internal static readonly string QualifiedTableName = $"{RepeatSearchContext.Schema}.{TableName}";

        public int Id { get; set; }

        [MaxLength(200)]
        [Required]
        public string OriginalSearchRequestId { get; set; }

        [MaxLength(200)]
        [Required]
        public string RepeatSearchRequestId { get; set; }

        public DateTimeOffset SearchCutoffDate { get; set; }

        public DateTimeOffset DateCreated { get; set; }

        public int UpdatedResultCount { get; set; }
        public int AddedResultCount { get; set; }
        public int RemovedResultCount { get; set; }
    }

    internal static class RepeatSearchHistoryRecordModelBuilder
    {
        public static void SetUpRepeatSearchHistoryRecordModel(this EntityTypeBuilder<RepeatSearchHistoryRecord> historyRecord)
        {
            // Each delivery of a repeat search request looks up its record. Not unique: older versions wrote a second record when a repeat
            // search was retried after its history step, and a unique index cannot be built on a table that has these duplicates.
            historyRecord.HasIndex(r => r.RepeatSearchRequestId).IsCreatedOnline();
        }
    }
}
