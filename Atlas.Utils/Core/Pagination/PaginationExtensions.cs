using System.Collections.Generic;
using System.Linq;

namespace Atlas.Utils.Core.Pagination
{
    public static class PaginationExtensions
    {
        public static PaginatedModel<T> ToPaginatedModel<T>(this IEnumerable<T> items)
        {
            var list = items.ToList();
            return new PaginatedModel<T>
            {
                Data = list,
                PageNumber = 1,
                TotalCount = list.Count,
                PageSize = list.Count
            };
        }
    }
}
