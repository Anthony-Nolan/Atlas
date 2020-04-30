using System;

namespace Atlas.Utils.Core.Pagination
{
    public sealed class PaginationData : IEquatable<PaginationData>
    {
        private const int DefaultPageNumber = 1;
        private const int DefaultPageSize = 30;

        public PaginationData()
        {
        }

        public PaginationData(int pageNumber, int pageSize)
        {
            PageNumber = pageNumber;
            PageSize = pageSize;
        }

        public static PaginationData Default { get; } = new PaginationData(DefaultPageNumber, DefaultPageSize);

        public int? PageSize { get; set; } = DefaultPageSize;
        public int? PageNumber { get; set; } = DefaultPageNumber;

        public bool Equals(PaginationData other)
        {
            if (other == null)
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return (PageSize == other.PageSize) && (PageNumber == other.PageNumber);
        }

        public override bool Equals(object obj)
        {
            return obj is PaginationData && Equals((PaginationData)obj);
        }

        public override int GetHashCode()
        {
            return (PageSize.GetHashCode() * 397) ^ PageNumber.GetHashCode();
        }
    }
}
