using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.RepeatSearch.Data.Models
{
    public class SearchResult
    {
        public int Id { get; set; }
        
        public int AtlasDonorId { get; set; }
    }

    internal static class SearchResultModelBuilder
    {
        public static void SetUpSearchResultModel(this EntityTypeBuilder<SearchResult> searchResult)
        {
            searchResult.HasIndex(r => r.AtlasDonorId).IsUnique();
        }
    }
}