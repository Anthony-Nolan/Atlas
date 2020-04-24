using System.Collections.Generic;

namespace Nova.Utils.Pagination
{
    public class PaginatedModel<T>
    {
        public IEnumerable<T> Data { get; set; } = new List<T>();
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public int TotalCount { get; set; }
    }
}
