namespace Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings
{
    public enum SerologySubtype
    {
        // Enum values stored in db; changing values will require rebuild
        // of the Metadata Dictionary StorageTables.
        Broad = 1,
        Split = 2,
        NotSplit = 3,
        Associated = 4   
    }
}
