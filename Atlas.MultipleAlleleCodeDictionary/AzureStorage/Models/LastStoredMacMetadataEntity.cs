using Atlas.Common.AzureStorage;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;
using Atlas.MultipleAlleleCodeDictionary.Services;

namespace Atlas.MultipleAlleleCodeDictionary.AzureStorage.Models
{
    /// <summary>
    /// References the last MAC we imported.
    /// MACs are implicitly ordered, and imported in order.
    /// Keeping a record of the last imported MAC allows us to only consider later MACs on future imports.
    ///
    /// The ordering logic is encoded in <see cref="SortingExtension"/>
    /// This entity the latest MAC imported (which could be calculated from existing MAC data using that ordering logic), cached for efficient lookup.
    /// </summary>
    internal class LastStoredMacMetadataEntity : AtlasTableEntityBase, IHasMacCode
    {
        public const string MetadataPartitionKey = "Metadata"; 
        public const string LatestMacRowKey = "LastImported"; 
        
        public LastStoredMacMetadataEntity()
        {
            RowKey = LatestMacRowKey;
            PartitionKey = MetadataPartitionKey;
        }

        /// <inheritdoc />
        public string Code { get; set; }
    }
}