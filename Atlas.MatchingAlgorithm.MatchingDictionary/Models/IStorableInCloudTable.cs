using Microsoft.WindowsAzure.Storage.Table;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Models
{
    public interface IStorableInCloudTable<out TTableEntity> where TTableEntity : TableEntity
    {
        TTableEntity ConvertToTableEntity();
    }
}
