using System.Linq;
using System.Threading.Tasks;

namespace Atlas.Common.AzureStorage.Blob
{
    public interface IBlobMetadataAnalyser
    {
        Task<int> NumberOfFilesInFolder(string container, string folder);
    }
    
    public class BlobMetadataAnalyser : AzureStorageBlobClient, IBlobMetadataAnalyser
    {
        public BlobMetadataAnalyser(string azureStorageConnectionString) : base(azureStorageConnectionString)
        {
        }
        
        public async Task<int> NumberOfFilesInFolder(string container, string folder)
        {            
            var containerRef = await GetBlobContainer(container);
            var directory = containerRef.GetDirectoryReference(folder);
            return directory?.ListBlobs().Count() ?? 0;
        }
    }
}