namespace Atlas.HlaMetadataDictionary.Models.HLATypings
{
    internal enum SerologySubtype
    {
        // Enum values stored in db; changing values will require rebuild
        // of the matching dictionary.
        Broad = 1,
        Split = 2,
        NotSplit = 3,
        Associated = 4   
    }
}
