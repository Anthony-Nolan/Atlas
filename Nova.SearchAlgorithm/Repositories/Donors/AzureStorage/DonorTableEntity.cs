using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Repositories.Donors.AzureStorage
{
    public class DonorTableEntity : TableEntity
    {
        public string SerialisedDonor { get; set; }

        public string DonorId { get; set; }
        public string DonorType { get; set; }
        public string RegistryCode { get; set; }
        // TODO:NOVA-919 Rename
        // TODO:NOVA-919 expand into concrete type with Locus, Value
        //
        // This field will store both serologies and pgroups, to simplify querying by match.
        // TODO:NOVA-919 consider splitting into two tables/fields
        public List<string> HlaData { get; set; }

        public DonorTableEntity() { }

        public DonorTableEntity(string partitionKey, string rowKey) : base(partitionKey, rowKey) { }
    }
}