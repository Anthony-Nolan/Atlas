using Atlas.Client.Models.Search;
using Atlas.DonorImport.Data.Models;

namespace Atlas.ManualTesting.Common.Models
{
    public static class DonorTypeExtensions
    {
        public static DatabaseDonorType ToDatabaseType(this DonorType donorType)
        {
            return donorType switch
            {
                DonorType.Adult => DatabaseDonorType.Adult,
                DonorType.Cord => DatabaseDonorType.Cord,
                _ => throw new ArgumentOutOfRangeException(nameof(donorType), donorType, null)
            };
        }
    }
}
