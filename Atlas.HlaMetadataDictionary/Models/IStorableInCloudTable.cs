using Microsoft.WindowsAzure.Storage.Table;

namespace Atlas.HlaMetadataDictionary.Models
{
    public interface IStorableInCloudTable<out TTableEntity> where TTableEntity : TableEntity
    {
        TTableEntity ConvertToTableEntity();
    }
}
