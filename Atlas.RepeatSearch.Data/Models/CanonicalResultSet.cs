using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atlas.RepeatSearch.Data.Context;
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
        /// Atlas Ids (NOT external donor codes) of donors in the canonical result set.
        /// </summary>
        public ICollection<SearchResult> SearchResults { get; set; }
    }

    internal static class CanonicalResultSetBuilder
    {
        public static void SetUpResultSetModel(this EntityTypeBuilder<CanonicalResultSet> resultSet)
        {
            resultSet.HasIndex(s => s.OriginalSearchRequestId).IsUnique();
        }
    }
}