using Microsoft.WindowsAzure.Storage.Table;

namespace Nova.SearchAlgorithm.Common.Models
{
    public interface IStorableInCloudTable<out TTableEntity> where TTableEntity : TableEntity
    {
        TTableEntity ConvertToTableEntity();
    }
}
