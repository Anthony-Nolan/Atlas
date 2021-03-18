using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atlas.RepeatSearch.Data.Context;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.RepeatSearch.Data.Models
{
    [Table(TableName)]
    public class SearchResult
    {
        private const string TableName = "SearchResults";
        internal static readonly string QualifiedTableName = $"{RepeatSearchContext.Schema}.{TableName}";

        public int CanonicalResultSetId { get; set; }

        public int Id { get; set; }

        [MaxLength(64)]
        [Required]
        public string ExternalDonorCode { get; set; }
    }

    internal static class SearchResultModelBuilder
    {
        public static void SetUpSearchResultModel(this EntityTypeBuilder<SearchResult> searchResult)
        {
            searchResult.HasIndex(r => new {r.ExternalDonorCode, r.CanonicalResultSetId}).IsUnique();
        }
    }
}