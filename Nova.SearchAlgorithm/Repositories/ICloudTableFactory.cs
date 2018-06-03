using Microsoft.WindowsAzure.Storage.Table;

namespace Nova.SearchAlgorithm.Repositories
{
    public interface ICloudTableFactory
    {
        CloudTable GetTable(string tableReferenceString);
    }
}