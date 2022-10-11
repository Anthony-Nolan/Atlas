using System;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.ManualTesting.Models
{
    public class MissingDonors
    {
        public int Count { get; set; }
        public IEnumerable<PropertyCount> DonorTypeCounts { get; set; }
        public IEnumerable<PropertyCount> RegistryCounts { get; set; }
        public IEnumerable<PropertyCount> UpdateFileCounts { get; set; }
        public IEnumerable<PropertyCount> LastUpdatedCounts { get; set; }
        public IEnumerable<MissingDonor> Donors { get; set; }
    }

    public class PropertyCount
    {
        public string Value { get; set; }
        public int Count { get; set; }
    }

    public class MissingDonor
    {
        public int AtlasId { get; set; }
        public string ExternalDonorCode { get; set; }
        public string DonorType { get; set; }
        public string RegistryCode { get; set; }
        public string UpdateFile { get; set; }
        public DateTimeOffset LastUpdated { get; set; }
    }

    public static class MissingDonorExtensions
    {
        public static IEnumerable<PropertyCount> GroupDonorsBy(this IEnumerable<MissingDonor> missingDonors, Func<MissingDonor, string> keySelector)
        {
            return missingDonors.GroupBy(keySelector).Select(grp => new PropertyCount { Count = grp.Count(), Value = grp.Key });
        }
    }
}
