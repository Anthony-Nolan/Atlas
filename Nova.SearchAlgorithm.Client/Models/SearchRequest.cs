using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Client.Models
{
    public enum SearchType
    {
        Adult,
        Cord,
        Nima
    }

    public class SearchRequest
    {
        public SearchType SearchType { get; set; }
        public MismatchCriteria MismatchCriteria { get; set; }
        public IEnumerable<string> RegistriesToSearch { get; set; }
    }
}
