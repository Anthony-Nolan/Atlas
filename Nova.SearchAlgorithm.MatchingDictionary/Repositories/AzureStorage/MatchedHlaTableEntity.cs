using Microsoft.WindowsAzure.Storage.Table;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    public class MatchedHlaTableEntity : TableEntity
    {
        public string SerialisedMatchedHla { get; set; }

        public MatchedHlaTableEntity() { }

        public MatchedHlaTableEntity(string matchLocus, string hlaName) : base(matchLocus, hlaName)
        {
        }
    }
}