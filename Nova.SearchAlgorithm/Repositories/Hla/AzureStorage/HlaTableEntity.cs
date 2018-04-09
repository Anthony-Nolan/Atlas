using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Repositories.Hla.AzureStorage
{
    public class HlaTableEntity : TableEntity
    {
        // TODO:NOVA-918 Correct HLA dictionary fields
        public string Locus { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsDeleted { get; set; }
        public IEnumerable<string> MatchingProteinGroups { get; set; }
        public IEnumerable<string> MatchingSerologies { get; set; }

        public HlaTableEntity() { }

        public HlaTableEntity(string partitionKey, string rowKey) : base(partitionKey, rowKey) { }
    }
}